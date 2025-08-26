using Animax.HandyStuff;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace Animax
{
    public class AnimationPreviewPanel : Panel
    {

        public Mediator _mediator;


        private Animation currentAnimation;
        private Image previewImage;
        private Frame selectedFrame;
        private Point imageOffset = Point.Empty;

        private float zoom = 5f;

        private bool isPanning = false;
        private Point panStart;

        public bool showSheetGrid = true;


        public AnimationPreviewPanel()
        {
            this.DoubleBuffered = true;
            this.MouseWheel += OnMouseWheel;

            //gizmo = new Gizmo();
        }

        public void SetImage(Image image, Frame frame)
        {
            this.previewImage = image;
            this.selectedFrame = frame;
            this.Invalidate();
        }

        public void SetCurrentAnimation(Animation anim = null)
        {
            this.currentAnimation = anim;
            this.Invalidate();
        }

        public void ClearSpriteSheet()
        {
            this.previewImage = null;
            this.selectedFrame = null;
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (currentAnimation == null)
                return;

            GraphicsState originalState = e.Graphics.Save();

            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

            e.Graphics.Clear(Color.LightGray);

            DrawGrid(e.Graphics, true);
            DrawGlobalCross(e.Graphics);

            e.Graphics.TranslateTransform(imageOffset.X, imageOffset.Y);
            e.Graphics.ScaleTransform(zoom, zoom);

            float horLength = 20;
            float verLength = 10;
            using (Pen pen = new Pen(Color.Yellow, 1.25f))
            {
                e.Graphics.DrawLine(pen, 0 - horLength, 0, 0 + horLength, 0);
                e.Graphics.DrawLine(pen, 0, 0 - verLength, 0, 0 + verLength);
                e.Graphics.DrawEllipse(pen, 0 - horLength / 2, 0 - verLength / 2, horLength, verLength);
            }

            foreach (var frame in _mediator.framePanel.currentFrames)
            {
                if (frame.Key.isVisible && frame.Value != null && frame.Value.imagePreview.savedImage != null && frame.Value.visible)
                {
                    GraphicsState frameState = e.Graphics.Save();
                    e.Graphics.TranslateTransform(
                        frame.Value.position.X,
                        -frame.Value.position.Y);

                    e.Graphics.RotateTransform(frame.Value.rotation);

                    float scaleX = frame.Value.scale.X / 100f;
                    float scaleY = frame.Value.scale.Y / 100f;
                    e.Graphics.ScaleTransform(
                        scaleX == 0 ? 0.01f : scaleX,
                        scaleY == 0 ? 0.01f : scaleY);

                    e.Graphics.TranslateTransform(
                        frame.Value.imagePreview.relativePivot.X,
                        frame.Value.imagePreview.relativePivot.Y);

                    e.Graphics.DrawImage(
                        frame.Value.imagePreview.savedImage,
                        0, 0,
                        frame.Value.imagePreview.savedImage.Width,
                        frame.Value.imagePreview.savedImage.Height);
                    e.Graphics.Restore(frameState);
                }
            }
            e.Graphics.Restore(originalState);
        }


        private void DrawGlobalCross(Graphics g)
        {
            float centerX = imageOffset.X;
            float centerY = imageOffset.Y;

            using (Pen pen = new Pen(Color.DarkGray, 5f))
            {
                g.DrawLine(pen, 0, centerY, Width, centerY);
                g.DrawLine(pen, centerX, 0, centerX, Height);
            }
        }

        private void DrawGrid(Graphics g, bool sheet)
        {
            int baseSpacing = 16;
            if (sheet)
            {
                using (Pen gridPen = new Pen(Color.Gray, 0.75f / zoom))
                {
                    float startXWorld = -imageOffset.X / zoom;
                    float startYWorld = -imageOffset.Y / zoom;

                    float endXWorld = (Width - imageOffset.X) / zoom;
                    float endYWorld = (Height - imageOffset.Y) / zoom;

                    int startGridX = (int)Math.Floor(startXWorld / baseSpacing) * baseSpacing;
                    int startGridY = (int)Math.Floor(startYWorld / baseSpacing) * baseSpacing;

                    for (int x = startGridX; x <= endXWorld; x += baseSpacing)
                    {
                        float screenX = x * zoom + imageOffset.X;
                        g.DrawLine(gridPen, screenX, 0, screenX, Height);
                    }

                    for (int y = startGridY; y <= endYWorld; y += baseSpacing)
                    {
                        float screenY = y * zoom + imageOffset.Y;
                        g.DrawLine(gridPen, 0, screenY, Width, screenY);
                    }
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                isPanning = true;
                panStart = e.Location;
                return;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle && isPanning)
            {
                int dx = e.X - panStart.X;
                int dy = e.Y - panStart.Y;
                imageOffset.X += dx;
                imageOffset.Y += dy;
                panStart = e.Location;
                Invalidate();
                return;
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle && isPanning)
                isPanning = false;
        }
        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            float oldZoom = zoom;

            float delta = e.Delta > 0 ? 0.1f : -0.1f;
            zoom = Math.Max(0.1f, zoom + delta);

            var mousePos = e.Location;
            var imagePosBefore = new PointF(
                (mousePos.X - imageOffset.X) / oldZoom,
                (mousePos.Y - imageOffset.Y) / oldZoom);

            var imagePosAfter = new PointF(
                (mousePos.X - imageOffset.X) / zoom,
                (mousePos.Y - imageOffset.Y) / zoom);

            imageOffset.X += (int)((imagePosAfter.X - imagePosBefore.X) * zoom);
            imageOffset.Y += (int)((imagePosAfter.Y - imagePosBefore.Y) * zoom);

            Invalidate();
        }

        private Point ScreenToImage(Point screen)
        {
            return new Point(
                (int)((screen.X - imageOffset.X) / zoom),
                (int)((screen.Y - imageOffset.Y) / zoom)
            );
        }
    }
}