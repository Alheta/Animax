using System;
using System.Drawing;
using System.Windows.Forms;

namespace Animax
{
    public class TimelineMarker : UserControl
    {
        public Mediator _mediator;
        public int frameWidth { get; set; }
        public int panelHeight { get; set; }
        public int animationDuration { get; set; }

        public int frameIndex
        {
            get => _frameIndex;
            set
            {
                int clamped = Math.Max(0, value);
                if (_frameIndex != clamped)
                {
                    _frameIndex = clamped;
                    _mediator.framePanel.currentFrameIndex= _frameIndex;
                    _mediator.framePanel.SetFramesOnIndex(_mediator.framePanel.currentFrameIndex, false);
                }
                RefreshPos();
                Console.WriteLine(_frameIndex);
            }
        }
        private int _frameIndex = 0;

        public Color MarkerColor { get; set; } = Color.DodgerBlue;

        Timer Wriggler = new Timer();

        public TimelineMarker(Mediator mediator)
        {
            _mediator = mediator;
            frameWidth = _mediator.layerPanel.frameWidth;
            panelHeight = _mediator.framePanel.fixedHeader.Height;
            Width = frameWidth;
            Height = panelHeight;
        }

        protected override void OnPaintBackground(PaintEventArgs pevent){ }

        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle mainRec = new Rectangle(0, 0, this.Width, 20);

            int centerX = mainRec.Left + mainRec.Width / 2;

            Point[] triangle = new Point[]
            {
                new Point(centerX - this.Width/ 2, mainRec.Bottom),
                new Point(centerX + this.Width/ 2, mainRec.Bottom),
                new Point(centerX, mainRec.Bottom + 10)
            };
            e.Graphics.FillPolygon(Brushes.Blue, triangle);
            e.Graphics.FillRectangle(Brushes.Blue, mainRec);

            //e.Graphics.DrawLine(Pens.Blue, new Point(centerX, Top), new Point(centerX, Bottom));
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084;
            const int HTTRANSPARENT = -1;

            if (m.Msg == WM_NCHITTEST)
            {
                Point cursorPos = new Point(
                    m.LParam.ToInt32() & 0xFFFF,  // X
                    m.LParam.ToInt32() >> 16);     // Y
                cursorPos = PointToClient(cursorPos);

                m.Result = (IntPtr)HTTRANSPARENT;
                return;
            }

            base.WndProc(ref m);
        }

        private void UpdatePosFromIndex()
        {
            this.Left = (frameIndex * frameWidth);
        }

        public void RefreshPos()
        {
            UpdatePosFromIndex();
            Invalidate();
        }
    }
}