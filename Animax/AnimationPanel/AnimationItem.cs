using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Data;
using Microsoft.SqlServer.Server;

namespace Animax
{
    public class AnimationItem : UserControl
    {
        public Animation animation { get; }
        public bool isSelected { get; set; }
        public event Action<AnimationItem> clicked;
        public event Action<AnimationItem> deleted;
        public event Action<Animation> renamed;

        public TextBox renameBox;
        private ContextMenuStrip itemMenu;
        public bool isRenaming => renameBox.Visible;

        public AnimationPanel animationPanel;

        public AnimationItem(Animation anim)
        {
            animation = anim;
            Height = 25;
            renameBox = new TextBox { Visible = false, BorderStyle = BorderStyle.FixedSingle };
            renameBox.KeyDown += RenameBox_KeyDown;
            renameBox.Leave += (s, e) => FinishRename();

            itemMenu = new ContextMenuStrip();
            itemMenu.Items.Add("Rename", null, (s, e) => StartRename());
            itemMenu.Items.Add("Delete", null, (s, e) => deleted?.Invoke(this));
            itemMenu.Items.Add("Toggle Default", null, (s, e) => ToggleDefault());
            itemMenu.Items.Add("Toggle Looping", null, (s, e) => ToggleLooping());

            MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    itemMenu.Show(this, PointToClient(Cursor.Position));
                }
                clicked.Invoke(this);
            };

        }
        private void RenameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                FinishRename();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }
        public void StartRename()
        {
            renameBox.Multiline = true;
            renameBox.Bounds = new Rectangle(this.Bounds.X+30, this.Bounds.Y+5, this.Width-35, this.Height-10);
            renameBox.TextAlign = HorizontalAlignment.Right;
            renameBox.Font = Font;
            renameBox.Text = animation.name;
            renameBox.Visible = true;
            renameBox.Focus();
            renameBox.SelectAll();
            renameBox.BringToFront();

        }

        public void FinishRename()
        {
            animation.name = string.IsNullOrEmpty(renameBox.Text) ? "NewAnimation" : renameBox.Text.Trim();
            renameBox.Visible = false;
            renamed?.Invoke(animation);
            Invalidate();
        }

        public void ToggleDefault()
        {
            foreach (var item in animationPanel.items)
            {
                if (item != this)
                {
                    item.animation.isDefault = false;
                    item.Invalidate();
                }
                else
                {
                    item.animation.isDefault = !item.animation.isDefault;
                    item.Invalidate();
                }
            }
        }
        public void ToggleLooping()
        {
            animation.isLooping = !animation.isLooping;
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.Clear(isSelected ? SystemColors.Highlight : SystemColors.ControlLight);

            if (animation.isDefault)
            {
                e.Graphics.FillEllipse(Brushes.Green, 4, 10, 8, 8);
                e.Graphics.DrawEllipse(Pens.White, 4, 10, 8, 8);
            }
            if (animation.isLooping)
            {
                e.Graphics.FillEllipse(Brushes.Orange, 20, 10, 8, 8);
                e.Graphics.DrawEllipse(Pens.White, 20, 10, 8, 8);
            }

            var format = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisWord };
            e.Graphics.DrawString(animation.name, Font, (isSelected ? Brushes.White : Brushes.Black), new Rectangle(30, 5, Width - 35, Height-10), format);

            Rectangle rect = this.ClientRectangle;
            Rectangle itemRec = new Rectangle(rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
            e.Graphics.DrawRectangle(Pens.Black, itemRec);
        }
    }
}
