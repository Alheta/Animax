using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;

namespace Animax
{
    public class AnimationPanel : Panel
    {
        public Mediator _mediator;

        public List<AnimationItem> items = new();
        public AnimationItem selectedItem;   
        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem createMenuItem;    

        public event Action<AnimationPanel> AnimationsUpdate;

        private Panel fixedHeader;
        private Panel layerContent;
        private VScrollBar scroller;

        public int totalContentHeight = 0;

        public AnimationPanel()
        {
            this.DoubleBuffered(true);
            AutoScroll = false;
            BorderStyle = BorderStyle.FixedSingle;

            fixedHeader = new Panel();
            fixedHeader.AutoScroll = false;
            fixedHeader.Height = 35;
            fixedHeader.Dock = DockStyle.Top;
            fixedHeader.Paint += OnPaint;

            layerContent = new Panel();
            layerContent.Dock = DockStyle.Fill;
            layerContent.AutoScroll = false;

            scroller = new VScrollBar();
            scroller.Dock = DockStyle.Right;
            scroller.Scroll += Scroller_Scroll;
            scroller.BringToFront();

            layerContent.Controls.Add(scroller);
            Controls.Add(layerContent);
            Controls.Add(fixedHeader);

            contextMenu = new ContextMenuStrip();
            createMenuItem = new ToolStripMenuItem("New Animation");
            createMenuItem.Click += (s, e) => AddAnimation();
            contextMenu.Items.Add(createMenuItem);


            fixedHeader.MouseUp += ClickPanel;
            layerContent.MouseUp += ClickPanel;

            Label header = new Label
            {
                Text = "Animations:",
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
                AnimationsUpdate?.Invoke(this);
                Invalidate();
            }
        }

        private void FixedHeader_MouseUp(object sender, MouseEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void OnPaint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawLine(Pens.Black, new Point(fixedHeader.Left + 4, fixedHeader.Bottom - 1), new Point(fixedHeader.Right - 4, fixedHeader.Bottom - 1));
        }

        public void AddAnimation(Animation animation = null)
        {
            bool newAnim = false;
            var anim = animation;
            if (anim == null)
            {
                anim = new Animation { name = "NewAnimation"};
                newAnim = true;
            }

            _mediator.projectManager.currentProject?.animations.Add(anim);

            AnimationItem item = new AnimationItem(anim)
            {
                Width = Width - 40,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                animationPanel = this
            };

            anim.layers.Add(new EventLayer());

            item.clicked += OnItemClicked;
            item.deleted += OnItemDeleted;
            item.renamed += (a) => Invalidate();

            layerContent.Controls.Add(item.renameBox);
            layerContent.Controls.Add(item);
            items.Add(item);

            LayoutItems();
            if (newAnim)
                item.StartRename();

            _mediator.MarkProjectModified();
        }

        public void UpdateAnimations()
        {
            layerContent.Controls.Clear();
            items.Clear();
            selectedItem = null;

            List<Animation> anims = _mediator.projectManager.currentProject.animations;
            Console.WriteLine(anims.Count);

            foreach (Animation anim in anims)
            {
                AnimationItem animItem = new AnimationItem(anim)
                {
                    Width = Width - 40,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    animationPanel = this
                };

                animItem.clicked += OnItemClicked;
                animItem.deleted += OnItemDeleted;
                animItem.renamed += (a) => Invalidate();

                layerContent.Controls.Add(animItem.renameBox);
                layerContent.Controls.Add(animItem);
                items.Add(animItem);
            }

            LayoutItems();
        }

        private void OnItemClicked(AnimationItem item)
        {
            if (selectedItem != null)
                selectedItem.isSelected = false;

            selectedItem = item;
            selectedItem.isSelected = true;
            AnimationsUpdate?.Invoke(this);
            foreach (var item2 in items)
            {
                item2.Invalidate();
            }
        }

        private void OnItemDeleted(AnimationItem item)
        {
            _mediator.projectManager.currentProject?.animations.Remove(item.animation);

            layerContent.Controls.Remove(item);
            items.Remove(item);
            selectedItem = null;
            AnimationsUpdate?.Invoke(this);

            items.RemoveAll(item => item == null);
            LayoutItems();
            Invalidate();
        }

        private void LayoutItems()
        {
            this.SuspendLayout();
            int y = 2;
            foreach (var item in items)
            {
                item.Location = new Point(4, y);
                y += item.Height + 2;
            }

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
            Scroller_Update();
            layerContent.Invalidate();
        }

        public void Scroller_Update()
        {
            scroller.LargeChange = layerContent.ClientSize.Height;
            scroller.Maximum = Math.Max(totalContentHeight, 1);
            scroller.Visible = totalContentHeight > layerContent.ClientSize.Height;

            if (scroller.Visible) { scroller.Value = 0; }
        }

        //protected override void OnResize(EventArgs eventargs)
        //{
        //    base.OnResize(eventargs);
        //    foreach (var item in items)
        //    {
        //        item.Width = Width - 40;
        //    }
        //}
    }
}
