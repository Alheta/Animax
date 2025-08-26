using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Animax
{
    public class TimelineFrameItem : UserControl
    {
        public Mediator _mediator;

        public Frame frame { get; }
        public LinearGradientBrush color;
        private int baseWidth;

        public bool isSelected { get; set; }
        public bool isLastSelected { get; set; }
        public event Action<TimelineFrameItem> clicked;
        public event Action<TimelineFrameItem> deleted;
        public event Action deletedAll;

        private ContextMenuStrip itemMenu;

        public TimelineFrameItem(Frame frm, TimelineLayerItem layer, Mediator mediator)
        {
            this.DoubleBuffered(true);
            _mediator = mediator;
            frame = frm;
            color = layer.color;
            Width = _mediator.layerPanel.frameWidth;
            Height = _mediator.layerPanel.lineHeight;
            baseWidth = _mediator.layerPanel.frameWidth;

            itemMenu = new ContextMenuStrip();
            itemMenu.Items.Add("Delete", null, (s, e) => deleted?.Invoke(this));
            itemMenu.Items.Add("Delete Selected", null, (s, e) => deletedAll?.Invoke());
            itemMenu.Items.Add(new ToolStripSeparator());

            MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    itemMenu.Show(this, PointToClient(Cursor.Position));
                }
                else if (e.Button == MouseButtons.Left)
                {
                    clicked.Invoke(this);
                }
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Brush selected = SystemBrushes.Highlight;
            Brush lastSelected = Brushes.Gold;
            Brush toRender = isLastSelected ? lastSelected : isSelected ? selected : color;

            e.Graphics.FillRectangle(toRender, this.ClientRectangle);

            Rectangle rect = this.ClientRectangle;
            e.Graphics.DrawRectangle(Pens.Gray, 0, 0, rect.Width - 1, rect.Height - 1);

            int dotSize = 7;

            Point dotPos = new Point(baseWidth / 2 - dotSize / 2, this.ClientRectangle.Height / 2 - dotSize / 2);
            Rectangle rectForDot = new Rectangle(dotPos.X - 1, dotPos.Y, dotSize, dotSize);
            e.Graphics.FillEllipse(Brushes.Black, rectForDot);
        }
    }
}
