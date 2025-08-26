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
        public PropertiesTab propers { get; set; }

        public ComboBox eventsBox { get; set; }

        public Main mainForm { get; set; }

        public ColorManager clrManager { get; set; }
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
                spritePanel?.SetSpriteSheet(imgToLoad, frameItem.frame);
                UpdateProperties(frameItem.frame, true);
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
        public void UpdateProperties(Frame frame, bool updateFrameProps)
        {
            _editText = true;
            propers.x.Text = frame.selection.X.ToString();
            propers.y.Text = frame.selection.Y.ToString();
            propers.width.Text = frame.selection.Width.ToString();
            propers.height.Text = frame.selection.Height.ToString();

            propers.pivotX.Text = frame.pivotPos.X.ToString();
            propers.pivotY.Text = frame.pivotPos.Y.ToString();

            if (updateFrameProps)
            {
                propers.posX.Text = frame.position.X.ToString();
                propers.posY.Text = frame.position.Y.ToString();
                propers.scaleX.Text = frame.scale.X.ToString();
                propers.scaleY.Text = frame.scale.Y.ToString();
                propers.rotation.Text = frame.rotation.ToString();
                propers.visibility.Checked = frame.visible;
                propers.interpolated.Checked = frame.interpolated;
            }

            animPreview.Invalidate();
            _editText = false;
        }
        public void UpdateProperties(Frame frame)
        {
            _editText = true;
            propers.x.Text = frame.selection.X.ToString();
            propers.y.Text = frame.selection.Y.ToString();
            propers.width.Text = frame.selection.Width.ToString();
            propers.height.Text = frame.selection.Height.ToString();

            propers.pivotX.Text = frame.pivotPos.X.ToString();
            propers.pivotY.Text = frame.pivotPos.Y.ToString();

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
                    X = SafeParseInt(propers.x.Text),
                    Y = SafeParseInt(propers.y.Text),
                    Width = SafeParseInt(propers.width.Text, 1, -4096, 4096),
                    Height = SafeParseInt(propers.height.Text, 1, -4096, 4096),
                    PivotX = SafeParseInt(propers.pivotX.Text),
                    PivotY = SafeParseInt(propers.pivotY.Text)
                });

                if (framePanel?.selectedItem != null)
                {
                    framePanel.selectedItem.frame.position = new PointF(SafeParseInt(propers.posX.Text), SafeParseInt(propers.posY.Text));
                    framePanel.selectedItem.frame.rotation = SafeParseInt(propers.rotation.Text);
                    framePanel.selectedItem.frame.scale = new Point(
                        SafeParseInt(propers.scaleX.Text, 100, -1000, 1000),
                        SafeParseInt(propers.scaleY.Text, 100, -1000, 1000)
                    );

                    framePanel.selectedItem.frame.visible = propers.visibility.Checked;
                    framePanel.selectedItem.frame.interpolated = propers.interpolated.Checked;

                    animPanel.Invalidate();
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

        public void RemoveAllEventFrames(FrameEvent ev)
        {
            foreach (Animation anim in mainForm.animations)
            {
                foreach (Layer lyr in anim.layers)
                {
                    foreach (Frame frm in lyr.frames)
                    {
                        if (frm.type == LayerType.EVENT && frm.frameEvent.eventName == ev.eventName)
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
