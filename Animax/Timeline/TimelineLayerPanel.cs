using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Animax
{
    public class TimelineLayerPanel : Panel
    {
        public Mediator _mediator;

        public List<TimelineLayerItem> items = new();
        public TimelineLayerItem eventItem;

        public TimelineLayerItem selectedItem;
        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem createMenuItem;
        public Animation selectedAnimation;

        private Panel fixedHeader;
        private Panel layerContent;
        private VScrollBar scroller;

        public event Action<TimelineLayerPanel> LayersUpdate;

        public readonly int frameWidth = 15;
        public readonly int lineHeight = 25;

        public int totalContentHeight = 0;

        public TimelineLayerPanel()
        {
            this.DoubleBuffered(true);
            AutoScroll = false;
            BorderStyle = BorderStyle.FixedSingle;
            Resize += (s,e) => Scroller_Update();

            fixedHeader = new Panel();
            fixedHeader.AutoScroll = false;
            fixedHeader.Height = 35;
            fixedHeader.Dock = DockStyle.Top;
            fixedHeader.Paint += OnPaint;

            layerContent = new Panel();
            layerContent.Dock = DockStyle.Fill;
            layerContent.AutoScroll = false;
            //layerContent.MouseWheel += LayerContent_MouseWheel;

            scroller = new VScrollBar();
            scroller.Dock = DockStyle.Left;
            scroller.Scroll += Scroller_Scroll;
            scroller.BringToFront();


            layerContent.Controls.Add(scroller);
            Controls.Add(layerContent);
            Controls.Add(fixedHeader);

            contextMenu = new ContextMenuStrip();
            createMenuItem = new ToolStripMenuItem("New Layer");
            createMenuItem.Click += (s, e) => _mediator.OpenLayerEditor();
            contextMenu.Items.Add(createMenuItem);


            fixedHeader.MouseUp += ClickPanel;
            layerContent.MouseUp += ClickPanel;


            Label header = new Label
            {
                Text = "Layers:",
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                AutoSize = true,
                Padding = new Padding(8),
                BackColor = Color.Transparent
            };


            fixedHeader.Controls.Add(header);

            Scroller_Update();
        }

        private void ClickPanel(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                contextMenu.Show(this, PointToClient(Cursor.Position));
            else if (e.Button == MouseButtons.Left)
            {
                selectedItem = null;
                foreach (var item in items)
                {
                    item.isSelected = false;
                    item.Invalidate();
                }
                LayersUpdate?.Invoke(this);
                Invalidate();
            }
        }
        public void UpdateLayers(Animation anim = null)
        {
            foreach (var lyr in items)
            {
                if (layerContent.Controls.Contains(lyr))
                    layerContent.Controls.Remove(lyr);
            }
            items.Clear();

            if (anim == null)
            {
                Invalidate();
                return;
            }

            selectedAnimation = anim;
            foreach (Layer lyr in selectedAnimation.layers)
            {
                TimelineLayerItem item = new TimelineLayerItem(lyr, _mediator)
                {
                    Width = Width,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                };
                item.clicked += OnItemClicked;
                item.deleted += OnItemDelete;
                layerContent.Controls.Add(item);
                items.Add(item);
            }

            items.Sort((a, b) =>
            {
                int orderA = a.layer switch
                {
                    NormalLayer => 0,
                    PointLayer => 1,
                    EventLayer => 3,
                    _ => 2
                };

                int orderB = b.layer switch
                {
                    NormalLayer => 0,
                    PointLayer => 1,
                    EventLayer => 3,
                    _ => 2
                };

                return orderA.CompareTo(orderB);
            });

            LayoutItems();
            Invalidate();
        }

        public void OnPaint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawLine(Pens.Black, new Point(fixedHeader.Left+4, fixedHeader.Bottom-1), new Point(fixedHeader.Right-4, fixedHeader.Bottom-1));
        }

        public void OnItemClicked(TimelineLayerItem item)
        {
            if (selectedItem != null)
                selectedItem.isSelected = false;

            selectedItem = item;
            selectedItem.isSelected = true;
            LayersUpdate?.Invoke(this);
            foreach (var item2 in items)
                item2.Invalidate();
        }

        public void OnItemDelete(TimelineLayerItem item)
        {
            var layer = item.layer;

            layerContent.Controls.Remove(item);
            items.Remove(item);
            selectedAnimation.layers.Remove(layer);
            selectedItem = null;

            LayersUpdate?.Invoke(this);
            selectedAnimation.layers.RemoveAll(l => l == null);

            item.Invalidate();
            LayoutItems();
        }


        public void AddLayer(Animation anim, Layer layer)
        {
            TimelineLayerItem item = new TimelineLayerItem(layer, _mediator)
            {
                Width = this.Width,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };

            item.clicked += OnItemClicked;
            item.deleted += OnItemDelete;
            anim.layers.Add(layer);
            layerContent.Controls.Add(item);
            items.Add(item);

            UpdateLayers(anim);

            _mediator.MarkProjectModified();
        }

        public void LayoutItems()
        {
            this.SuspendLayout();
            int y = 2;
            foreach (var item in items)
            {
                item.Location = new Point(30, y);
                item.Width = layerContent.Width - item.Location.X - 4;
                y += item.Height + 2;
            }

            _mediator.framePanel.LayoutItems();

            totalContentHeight = y;
            Scroller_Update();
            Invalidate();
            this.ResumeLayout(true);
        }


        public void Scroller_Scroll(object sender, ScrollEventArgs e)
        {
            int scrollOffset = scroller.Value;

            foreach (var item in items)
            {
                int originalY = item.Tag != null ? (int)item.Tag : item.Top + scrollOffset;

                item.Top = originalY - scrollOffset;

                if (item.Tag == null)
                    item.Tag = originalY;
            }

            layerContent.Invalidate();
            _mediator.framePanel.LayoutItems();
            _mediator.framePanel.layerContent.Invalidate();
        }

        public void Scroller_Update()
        {
            scroller.LargeChange = layerContent.ClientSize.Height;
            scroller.Maximum = Math.Max(totalContentHeight, 1);
            scroller.Visible = totalContentHeight > layerContent.ClientSize.Height;
            
            if (scroller.Visible) { scroller.Value = 0; }
        }

        //private void LayerContent_MouseWheel(object sender, MouseEventArgs e)
        //{
        //    if (scroller.Visible)
        //    {
        //        int newValue = scroller.Value - e.Delta / 4;
        //        newValue = Math.Max(0, Math.Min(scroller.Maximum - scroller.LargeChange + 1, newValue));
        //        scroller.Value = newValue;
        //        var scrollArgs = new ScrollEventArgs(ScrollEventType.ThumbPosition, newValue);
        //        Scroller_Scroll(this, scrollArgs);
        //    }
        //}
    }
}
