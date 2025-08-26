using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Animax.HandyStuff;

namespace Animax
{
    public class TimelineLayerItem : UserControl
    {

        public Mediator _mediator;

        public Layer layer { get; }
        public bool isSelected { get;set; }
        public event Action<TimelineLayerItem> clicked;
        public event Action<TimelineLayerItem> changedVisibility;
        public event Action<TimelineLayerItem> deleted;

        public LinearGradientBrush color;

        private ContextMenuStrip itemMenu;

        public int frameWidth;

        public TimelineLayerItem(Layer lyr, Mediator mediator)
        {
            this.DoubleBuffered(true);
            _mediator = mediator;
            layer = lyr;
            Height = _mediator.layerPanel.lineHeight;

            if (layer.type != LayerType.EVENT)
            {
                itemMenu = new ContextMenuStrip();
                itemMenu.Items.Add("Properties", null, (s, e) => _mediator.OpenLayerEditor(this));
                itemMenu.Items.Add("Toggle Visible", null, (s, e) => ToggleVisible());
                itemMenu.Items.Add("Delete", null, (s, e) => deleted?.Invoke(this));
            }

            MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && layer.type != LayerType.EVENT)
                {
                    itemMenu.Show(this, PointToClient(Cursor.Position));
                }
                Console.WriteLine(layer.imageIndex);
                clicked.Invoke(this);
            };

            switch (layer.type)
            {
                case LayerType.NORMAL:
                    color = ColorManager.ApplyGradient(ColorManager.AnimaxGradients.LAYER_NORMAL, this.ClientRectangle);
                    break;
                case LayerType.POINT:
                    color = ColorManager.ApplyGradient(ColorManager.AnimaxGradients.LAYER_POINT, this.ClientRectangle);
                    break;
                case LayerType.EVENT:
                    color = ColorManager.ApplyGradient(ColorManager.AnimaxGradients.LAYER_EVENT, this.ClientRectangle);
                    break;
            }
        }

        public void ToggleVisible()
        {
            layer.isVisible = !layer.isVisible;
            changedVisibility?.Invoke(this);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Brush selected = SystemBrushes.Highlight;
            Brush toRender = isSelected ? selected : layer.isVisible ? color : Brushes.Gray;
            e.Graphics.FillRectangle(toRender, this.ClientRectangle);

            Rectangle rect = this.ClientRectangle;
            Rectangle itemRec = new Rectangle(rect.X, rect.Y, rect.Width-1, rect.Height-1);
            Rectangle labeRec = new Rectangle(itemRec.X + 4, itemRec.Y + itemRec.Height / 3, itemRec.Width - 4, itemRec.Height / 3);

            var format = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisWord };
            e.Graphics.DrawString(layer.name, Font, (isSelected ? Brushes.White : Brushes.Black), labeRec, format);

            e.Graphics.DrawRectangle(Pens.Black, itemRec);

        }
    }
}
