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

            if (layer is not EventLayer)
            {
                itemMenu = new ContextMenuStrip();
                itemMenu.Items.Add("Properties", null, (s, e) => _mediator.OpenLayerEditor(this));
                itemMenu.Items.Add("Toggle Visible", null, (s, e) => ToggleVisible());
                itemMenu.Items.Add("Delete", null, (s, e) => deleted?.Invoke(this));

                MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        itemMenu.Show(this, PointToClient(Cursor.Position));
                    }

                    clicked.Invoke(this);
                };
            }
            else
            {
                layer.name = "Events";
                MouseDown += (s, e) => clicked.Invoke(this); 
            }
            color = layer switch
            {
                NormalLayer => ColorManager.ApplyGradient(ColorManager.AnimaxGradients.LAYER_NORMAL, this.ClientRectangle),
                PointLayer => ColorManager.ApplyGradient(ColorManager.AnimaxGradients.LAYER_POINT, this.ClientRectangle),
                EventLayer => ColorManager.ApplyGradient(ColorManager.AnimaxGradients.LAYER_EVENT, this.ClientRectangle),
                _ => ColorManager.ApplyGradient(ColorManager.AnimaxGradients.LAYER_NORMAL, this.ClientRectangle),
            };
        }

        public void ToggleVisible()
        {
            switch (layer)
            {
                case NormalLayer normalLayer:
                    normalLayer.visible = !normalLayer.visible;
                    changedVisibility?.Invoke(this);
                    break;
                case PointLayer pointLayer:
                    pointLayer.visible = !pointLayer.visible;
                    changedVisibility?.Invoke(this);
                    break;
            }
            _mediator.animPreview.Invalidate();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Brush toRender = isSelected ? SystemBrushes.Highlight : layer.visible ? color : Brushes.Gray;
            e.Graphics.FillRectangle(toRender, this.ClientRectangle);

            Rectangle rect = this.ClientRectangle;
            Rectangle itemRec = new Rectangle(rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
            Rectangle labeRec = new Rectangle(itemRec.X + 4, itemRec.Y + itemRec.Height / 4, itemRec.Width - 4, itemRec.Height / 2);

            var format = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisWord };
            e.Graphics.DrawString(layer.name, Font, (isSelected ? Brushes.White : Brushes.Black), labeRec, format);

            e.Graphics.DrawRectangle(Pens.Black, itemRec);
        }
    }
}
