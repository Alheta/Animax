using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.PropertyGridInternal;
using System.Globalization;
using Animax.HandyStuff;
using System.IO;
using Animax.AdditionalElements;

namespace Animax
{
    public class Mediator
    {
     
        public ProjectManager projectManager { get; set; }
        public AnimationPreviewPanel animPreview { get; set; }
        public AnimationPanel animPanel { get; set; }
        public TimelineFramePanel framePanel { get; set; }
        public TimelineLayerPanel layerPanel { get; set; }
        public SpriteSheetPanel spritePanel { get; set; }
        public InstrumentPanel instPanel { get; set; }
        public TimelineMarker marker { get; set; }
        public FramePropertiesPanel propers { get; set; }
        public ComboBox eventsBox { get; set; }
        public Main mainForm { get; set; }
        public ColorManager clrManager { get; set; }

        public ImagePreviewPanel imagePanel = new ImagePreviewPanel();

        public void OnFrameSelectionUpdate(TimelineFramePanel panel)
        {
            spritePanel?.ClearSpriteSheet();

            TimelineFrameItem frameItem = framePanel?.selectedItem;
            if (frameItem != null)
            {
                TimelineLayerItem layerItem = framePanel?.GetLayerOfTheFrame(frameItem);


                //Layer Panel Set up
                if (layerPanel?.selectedItem != null)
                    layerPanel.selectedItem.isSelected = false;

                layerPanel.selectedItem = layerItem;
                layerPanel.selectedItem.isSelected = true;
                foreach (var item2 in layerPanel?.items)
                    item2.Invalidate();

                //Sprite Panel Set Up
                Image imgToLoad = projectManager.GetLayerImage(layerItem.layer);

                marker.frameIndex = framePanel.GetFrameIndex(frameItem);

                if (frameItem.frame.type == FrameType.NORMAL)
                {
                    UpdateProperties((NormalFrame)frameItem.frame);
                    spritePanel?.SetSpriteSheet(imgToLoad, (NormalFrame)frameItem.frame);
                    propers.ChangeMode(FramePropertiesPanel.Mode.BOTH);
                }
                else if (frameItem.frame.type == FrameType.POINT)
                {
                    UpdateProperties((PointFrame)frameItem.frame);
                    propers.ChangeMode(FramePropertiesPanel.Mode.FRAME);
                }
                else if (frameItem.frame.type == FrameType.EVENT)
                    propers.ChangeMode(FramePropertiesPanel.Mode.NONE);
            }
            marker.Invalidate();
        }

        public void OnAnimationSelectionUpdate(AnimationPanel anim)
        {
            spritePanel?.ClearSpriteSheet();

            animPreview?.SetCurrentAnimation((anim.selectedItem == null ? null : anim.selectedItem.animation));
            layerPanel?.UpdateLayers(anim.selectedItem == null ? null : anim.selectedItem.animation);

            OnLayerSelectionUpdate(layerPanel);
            framePanel.selectedItems = new();
            framePanel.fixedHeader.Invalidate();
            marker.Invalidate();
        }

        public void OnLayerSelectionUpdate(TimelineLayerPanel lyr)
        {
            if (animPanel?.selectedItem != null)
            {
                framePanel?.UpdateFrames(lyr.items, layerPanel);
            }
            else
            {
                framePanel?.UpdateFrames(null, layerPanel);
            }
        }

        private bool _editText = false;
        public void UpdateProperties(NormalFrame frame)
        {
            _editText = true;
            propers.elements.x.Text = frame.selection.X.ToString();
            propers.elements.y.Text = frame.selection.Y.ToString();
            propers.elements.width.Text = frame.selection.Width.ToString();
            propers.elements.height.Text = frame.selection.Height.ToString();

            propers.elements.pivotX.Text = frame.pivotPos.X.ToString();
            propers.elements.pivotY.Text = frame.pivotPos.Y.ToString();

            propers.elements.posX.Text = frame.position.X.ToString();
            propers.elements.posY.Text = frame.position.Y.ToString();
            propers.elements.scaleX.Text = frame.scale.X.ToString();
            propers.elements.scaleY.Text = frame.scale.Y.ToString();
            propers.elements.rotation.Text = frame.rotation.ToString();
            propers.elements.visibility.Checked = frame.visible;
            propers.elements.interpolated.Checked = frame.interpolated;

            animPreview.Invalidate();
            _editText = false;
        }
        public void UpdateProperties(PointFrame frame)
        {
            _editText = true;
            propers.elements.posX.Text = frame.position.X.ToString();
            propers.elements.posY.Text = frame.position.Y.ToString();
            propers.elements.scaleX.Text = frame.scale.X.ToString();
            propers.elements.scaleY.Text = frame.scale.Y.ToString();
            propers.elements.rotation.Text = frame.rotation.ToString();
            propers.elements.visibility.Checked = frame.visible;
            propers.elements.interpolated.Checked = frame.interpolated;

            animPreview.Invalidate();
            _editText = false;
        }



        public void UpdateValuesFromTextBoxes(object sender, EventArgs e)
        {
            if (spritePanel?.selection == null || _editText)
                return;

            try
            {
                _editText = true;

                spritePanel.UpdateSelection(new SpriteSheetProperties
                {
                    X = SafeParseInt(propers.elements.x.Text),
                    Y = SafeParseInt(propers.elements.y.Text),
                    Width = SafeParseInt(propers.elements.width.Text, 1, -4096, 4096),
                    Height = SafeParseInt(propers.elements.height.Text, 1, -4096, 4096),
                    PivotX = SafeParseInt(propers.elements.pivotX.Text),
                    PivotY = SafeParseInt(propers.elements.pivotY.Text)
                });

                if (framePanel?.selectedItem != null)
                {
                    if (framePanel.selectedItem.frame.type != FrameType.EVENT)
                    {
                        //Im too lazy to make this code more compact, so it'll just be two same instances of code :p
                        //Maybe layer idk
                        if (framePanel.selectedItem.frame.type == FrameType.NORMAL)
                        {
                            var frame = (NormalFrame)framePanel.selectedItem.frame;

                            frame.position = new PointF(SafeParseInt(propers.elements.posX.Text), SafeParseInt(propers.elements.posY.Text));
                            frame.rotation = SafeParseInt(propers.elements.rotation.Text);
                            frame.scale = new Point(
                                SafeParseInt(propers.elements.scaleX.Text, 100, -1000, 1000),
                                SafeParseInt(propers.elements.scaleY.Text, 100, -1000, 1000)
                            );

                            frame.visible = propers.elements.visibility.Checked;
                            frame.interpolated = propers.elements.interpolated.Checked;
                        }

                        else if (framePanel.selectedItem.frame.type == FrameType.POINT)
                        {
                            var frame = (PointFrame)framePanel.selectedItem.frame;

                            frame.position = new PointF(SafeParseInt(propers.elements.posX.Text), SafeParseInt(propers.elements.posY.Text));
                            frame.rotation = SafeParseInt(propers.elements.rotation.Text);
                            frame.scale = new Point(
                                SafeParseInt(propers.elements.scaleX.Text, 100, -1000, 1000),
                                SafeParseInt(propers.elements.scaleY.Text, 100, -1000, 1000)
                            );

                            frame.visible = propers.elements.visibility.Checked;
                            frame.interpolated = propers.elements.interpolated.Checked;
                        }

                        animPanel.Invalidate();
                    }
                }

                MarkProjectModified();
            }
            finally
            {
                _editText = false;
                animPreview.Invalidate();
            }
        }

        private int SafeParseInt(string input, int defaultValue = 0, int min = int.MinValue, int max = int.MaxValue)
        {
            if (string.IsNullOrWhiteSpace(input))
                return defaultValue;

            bool negative = false;
            StringBuilder digits = new StringBuilder();

            foreach (char c in input)
            {
                if (char.IsDigit(c))
                {
                    digits.Append(c);
                }
                else if (c == '-' && digits.Length == 0)
                {
                    negative = true;
                }
            }

            if (digits.Length == 0)
                return defaultValue;

            try
            {
                int value = int.Parse(digits.ToString());
                if (negative)
                    value = -value;

                if (value < min) return min;
                if (value > max) return max;
                return value;
            }
            catch
            {
                return defaultValue;
            }
        }

        public void OpenLayerEditor(TimelineLayerItem layerI = null)
        {
            LayerAdd layerAdd = new LayerAdd(layerI, this);
            layerAdd.Show();
        }

        public void OpenImageManager()
        {
            ImageForm imgPan = new ImageForm(this);
            imgPan.Show();
        }

        public void RemoveAllEventFrames(FrameEvent ev)
        {
            foreach (Animation anim in mainForm.animations)
            {
                foreach (Layer lyr in anim.layers)
                {
                    foreach (Frame frm in lyr.frames)
                    {
                        if (frm.type == FrameType.EVENT)
                        {
                            lyr.frames.Remove(frm);
                            layerPanel.UpdateLayers(anim);
                        }
                    }
                }
            }
        }
        public void UpdateTitle()
        {
            string modified = projectManager.isModified ? "*" : "";
            string filePath = projectManager.hasFilePath ?
                Path.GetFileName(projectManager.currentProject.filePath) : "New Project";

            mainForm.Text = $"ANIMAX - {filePath}{modified}";
        }

        public void MarkProjectModified()
        {
            projectManager.isModified = true;
            UpdateTitle();
        }

        public void UpdateUIElements()
        {
            animPanel?.UpdateAnimations();
            layerPanel?.UpdateLayers();
            framePanel?.UpdateFrames();
        }
    }
}
