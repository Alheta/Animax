using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace Animax
{
    public class SpriteSheetPanel : Panel
    {
        public Mediator _mediator;

        private Image spriteSheet;
        public Rectangle selection;

        private Point imageOffset = Point.Empty;
        private NormalFrame selectedFrame => _mediator.framePanel.GetSelectedNormalFrame();

        private float zoom = 5f;

        private bool isSelecting = false;
        private bool isResizing = false;
        private bool isPanning = false;
        private bool isMovingPivot = false;
        private bool isMovingSelection = false;

        private Point selectionStart;
        private Point panStart;
        private Point moveOffset;
        private Point pivotOffset;

        public event Action<NormalFrame> SelectionChanged;

        private resizeHandleType activeHandle = resizeHandleType.None;
        private enum resizeHandleType { None, TopLeft, TopRight, BottomLeft, BottomRight }

        public bool showSheetGrid = true;

        public const int HANDLE_SIZE = 2;

        public SpriteSheetPanel()
        {
            this.DoubleBuffered = true;
            this.MouseWheel += OnMouseWheel;
        }

        public void SetSpriteSheet(Image image, NormalFrame frame)
        {
            this.spriteSheet = image;
            this.Invalidate();
        }

        public void ClearSpriteSheet()
        {
            this.spriteSheet = null;
            this.Invalidate();
        }

        public bool HasSpriteSheet()
        { 
            return this.spriteSheet != null;
        }

        public void ResetSelection()
        {
            this.Invalidate();
        }

        public void UpdateSelection(SpriteSheetProperties prop)
        {
            if (!HasSpriteSheet())
                return;

            selectedFrame.selection = new Rectangle(prop.X, prop.Y, prop.Width, prop.Height);
            selectedFrame.pivotPos = new Point(prop.PivotX, prop.PivotY);
            this.Invalidate();
        }

        public Bitmap GetSelectedImage()
        {
            if (selectedFrame == null) return null;
            if (selectedFrame.selection.IsEmpty) return null;

            Rectangle sourceRect = selectedFrame.selection;

            Bitmap cropped = new Bitmap(sourceRect.Width, sourceRect.Height);

            using (Graphics g = Graphics.FromImage(cropped))
            {
                g.DrawImage(
                    spriteSheet,
                    new Rectangle(0, 0, sourceRect.Width, sourceRect.Height),
                    sourceRect,
                    GraphicsUnit.Pixel
                );
            }

            return cropped;
        }

        public Point GetSelectePivot()
        {
            if (selectedFrame.selection.IsEmpty)
                return Point.Empty;

            Point pivotRelative = new Point(
                selectedFrame.pivotPos.X - selectedFrame.selection.X,
                selectedFrame.pivotPos.Y - selectedFrame.selection.Y
            );

            return pivotRelative;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.Clear(Color.LightGray);

            if (spriteSheet == null)
                return;

            GraphicsState originalState = e.Graphics.Save();
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

            if (zoom > 0.8f)
                DrawGrid(e.Graphics, showSheetGrid);

            e.Graphics.TranslateTransform(imageOffset.X, imageOffset.Y);
            e.Graphics.ScaleTransform(zoom, zoom);

            e.Graphics.DrawImage(spriteSheet, 0, 0, spriteSheet.Width, spriteSheet.Height);
            e.Graphics.Restore(originalState);
            DrawSelection(e.Graphics);

        }
        private void DrawSelection(Graphics g)
        {
            if (selectedFrame == null) return;

            if (selectedFrame.selection.Width > 0 && selectedFrame.selection.Height > 0)
            {
                Rectangle screenSelection = ImageToScreen(selectedFrame.selection);

                using (Pen selPen = new Pen(Color.Red, 2)) 
                {
                    selPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    g.DrawRectangle(selPen, screenSelection);
                }

                DrawHandles(g);
                DrawPivot(g);
            }
        }

        private void DrawPivot(Graphics g)
        {
            Point screenPivot = new Point(
                (int)(selectedFrame.pivotPos.X * zoom + imageOffset.X),
                (int)(selectedFrame.pivotPos.Y * zoom + imageOffset.Y)
            );

            using (var fillBrush = new SolidBrush(Color.Purple))
            using (var borderPen = new Pen(Color.White, 1f))
            {
                float pivotSize = 6f;
                g.FillEllipse(fillBrush,
                    screenPivot.X - pivotSize / 2,
                    screenPivot.Y - pivotSize / 2,
                    pivotSize, pivotSize);
                g.DrawEllipse(borderPen,
                    screenPivot.X - pivotSize / 2,
                    screenPivot.Y - pivotSize / 2,
                    pivotSize, pivotSize);
            }
        }

        private void DrawHandles(Graphics g)
        {
            var handles = GetHandleRectsScreenSpace();

            using (var fillBrush = new SolidBrush(Color.White))
            using (var borderPen = new Pen(Color.Black, 1f))
            {
                foreach (var rect in handles)
                {
                    g.FillRectangle(fillBrush, rect);
                    g.DrawRectangle(borderPen, rect);
                }
            }
        }

        private void DrawGrid(Graphics g, bool sheet)
        {
            int baseSpacing = 16;

            using (Pen gridPen = new Pen(Color.Black, 1f / zoom))
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


        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (selectedFrame == null)
                return;

            if (e.Button == MouseButtons.Middle)
            {
                isPanning = true;
                panStart = e.Location;
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                var imgPoint = ScreenToImage(e.Location);

                if (!selectedFrame.selection.IsEmpty)
                {
                    if (ModifierKeys.HasFlag(Keys.Control) && selectedFrame.selection.Contains(imgPoint))
                    {
                        ResetSelection();
                        Invalidate();
                        return;
                    }

                    activeHandle = GetHandleAtPointScreenSpace(e.Location);
                    if (activeHandle != resizeHandleType.None)
                    {
                        isResizing = true;
                        return;
                    }
                    else if (selectedFrame.selection.Contains(imgPoint))
                    {
                        isMovingSelection = true;
                        moveOffset = new Point(imgPoint.X - selectedFrame.selection.X, imgPoint.Y - selectedFrame.selection.Y);
                        pivotOffset = new Point(imgPoint.X - selectedFrame.pivotPos.X, imgPoint.Y - selectedFrame.pivotPos.Y);
                        return;
                    }
                }

                if (selectedFrame.selection.IsEmpty)
                {
                    isSelecting = true;
                    selectionStart = imgPoint;
                    selectedFrame.selection = new Rectangle(selectionStart, Size.Empty);
                    selectedFrame.pivotPos = selectionStart;
                    Invalidate();
                }
            }

            if (e.Button == MouseButtons.Right)
            {

                isMovingPivot = true;
                PointF mousePos = (Point)ScreenToImage(e.Location);
                int snappedX = (int)Math.Round(mousePos.X);
                int snappedY = (int)Math.Round(mousePos.Y);

                selectedFrame.pivotPos = new Point(snappedX, snappedY);
                selectedFrame.imagePreview.relativePivot = GetSelectePivot();

                SelectionChanged?.Invoke(selectedFrame);
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (selectedFrame == null)
                return;

            if (e.Button == MouseButtons.Middle && isPanning)
            {
                int dx = e.X - panStart.X;
                int dy = e.Y - panStart.Y;
                imageOffset.X += dx;
                imageOffset.Y += dy;
                panStart = e.Location;
                Invalidate();
            }

            if (e.Button == MouseButtons.Left)
            {
                if (isSelecting && !isResizing)
                {
                    Point current = ScreenToImage(e.Location);
                    int x = Math.Min(selectionStart.X, current.X);
                    int y = Math.Min(selectionStart.Y, current.Y);
                    int w = Math.Abs(current.X - selectionStart.X);
                    int h = Math.Abs(current.Y - selectionStart.Y);

                    w = Math.Max(1, w);
                    h = Math.Max(1, h);

                    selectedFrame.selection = new Rectangle(x, y, w, h);

                    selectedFrame.pivotPos = new Point(x, y);
                    selectedFrame.imagePreview.relativePivot = GetSelectePivot();
                }
                else if (isResizing)
                    ResizeSelection(e.Location);
                else if (isMovingSelection)
                {
                    Point imgPoint = ScreenToImage(e.Location);
                    selectedFrame.selectionX = (imgPoint.X - moveOffset.X);
                    selectedFrame.selectionY = (imgPoint.Y - moveOffset.Y);

                    if(ModifierKeys.HasFlag(Keys.Alt))
                    {
                        selectedFrame.pivotPos = new Point(imgPoint.X - pivotOffset.X, imgPoint.Y - pivotOffset.Y);
                    }
                }

                selectedFrame.imagePreview.savedImage = GetSelectedImage();
                selectedFrame.imagePreview.relativePivot = GetSelectePivot();

                SelectionChanged?.Invoke(selectedFrame);

                Invalidate();
            }

            if (e.Button == MouseButtons.Right)
            {
                if (isMovingPivot)
                {
                    PointF mousePos = (Point)ScreenToImage(e.Location);
                    int snappedX = (int)Math.Round(mousePos.X);
                    int snappedY = (int)Math.Round(mousePos.Y);

                    selectedFrame.pivotPos = new Point(snappedX, snappedY);
                    selectedFrame.imagePreview.relativePivot = GetSelectePivot();

                    SelectionChanged?.Invoke(selectedFrame);

                    Invalidate();
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (selectedFrame == null)
                return;

            if (e.Button == MouseButtons.Middle && isPanning)
            {
                isPanning = false;
            }
            if (e.Button == MouseButtons.Left)
            {
                if (isSelecting)
                    isSelecting = false;

                if (isResizing)
                    isResizing = false;

                if (isMovingSelection)
                    isMovingSelection = false;

                selectedFrame.imagePreview.savedImage = GetSelectedImage();

                SelectionChanged?.Invoke(selectedFrame);

                activeHandle = resizeHandleType.None;
            }

            if (e.Button == MouseButtons.Right)
            {
                if (isMovingPivot)
                {
                    isMovingPivot = false;
                }
            }
        }

        private resizeHandleType GetHandleAtPointScreenSpace(Point mouse)
        {
            Rectangle[] handles = GetHandleRectsScreenSpace();

            for (int i = 0; i < handles.Length; i++)
            {
                if (handles[i].Contains(mouse))
                    return (resizeHandleType)(i + 1);
            }

            return resizeHandleType.None;
        }

        private Rectangle[] GetHandleRects()
        {
            int half = HANDLE_SIZE / 2;

            Rectangle selection = selectedFrame.selection;

            return new Rectangle[]
            {
                new Rectangle(selection.Left - half, selection.Top - half, HANDLE_SIZE, HANDLE_SIZE),       // TopLeft
                new Rectangle(selection.Right - half, selection.Top - half, HANDLE_SIZE, HANDLE_SIZE),      // TopRight
                new Rectangle(selection.Left - half, selection.Bottom - half, HANDLE_SIZE, HANDLE_SIZE),    // BottomLeft
                new Rectangle(selection.Right - half, selection.Bottom - half, HANDLE_SIZE, HANDLE_SIZE),   // BottomRight
            };
        }

        private Rectangle[] GetHandleRectsScreenSpace()
        {
            Rectangle[] imgRects = GetHandleRects();
            Rectangle[] screenRects = new Rectangle[imgRects.Length];

            for (int i = 0; i < imgRects.Length; i++)
            {
                screenRects[i] = new Rectangle(
                    (int)(imgRects[i].X * zoom + imageOffset.X),
                    (int)(imgRects[i].Y * zoom + imageOffset.Y),
                    (int)(imgRects[i].Width * zoom),
                    (int)(imgRects[i].Height * zoom)
                );
            }

            return screenRects;
        }

        private void ResizeSelection(Point mouse)
        {
            Point imgPoint = ScreenToImage(mouse);
            Rectangle sel = selectedFrame.selection;

            switch (activeHandle)
            {
                case resizeHandleType.BottomRight:
                    sel.Width = Math.Max(1, imgPoint.X - sel.Left);
                    sel.Height = Math.Max(1, imgPoint.Y - sel.Top);
                    break;

                case resizeHandleType.TopLeft:
                    {
                        int newX = Math.Min(imgPoint.X, sel.Right - 1);
                        int newY = Math.Min(imgPoint.Y, sel.Bottom - 1);
                        sel.Width = sel.Right - newX;
                        sel.Height = sel.Bottom - newY;
                        sel.X = newX;
                        sel.Y = newY;
                        break;
                    }

                case resizeHandleType.TopRight:
                    {
                        int newY = Math.Min(imgPoint.Y, sel.Bottom - 1);
                        int newRight = Math.Max(imgPoint.X, sel.Left + 1);
                        sel.Height = sel.Bottom - newY;
                        sel.Width = newRight - sel.Left;
                        sel.Y = newY;
                        break;
                    }

                case resizeHandleType.BottomLeft:
                    {
                        int newX = Math.Min(imgPoint.X, sel.Right - 1);
                        int newBottom = Math.Max(imgPoint.Y, sel.Top + 1);
                        sel.Width = sel.Right - newX;
                        sel.Height = newBottom - sel.Top;
                        sel.X = newX;
                        break;
                    }
            }
            selectedFrame.selection = sel;
        }

        private Point ScreenToImage(Point screen)
        {
            return new Point(
                (int)((screen.X - imageOffset.X) / zoom),
                (int)((screen.Y - imageOffset.Y) / zoom)
            );
        }

        private Rectangle ImageToScreen(Rectangle imageRect)
        {
            return new Rectangle(
                (int)(imageRect.X * zoom + imageOffset.X),
                (int)(imageRect.Y * zoom + imageOffset.Y),
                (int)(imageRect.Width * zoom),
                (int)(imageRect.Height * zoom)
            );
        }

        private double Distance(Point p1, Point p2)
        {
            int dx = p1.X - p2.X;
            int dy = p1.Y - p2.Y;
            return Math.Sqrt(dx * dx + dy * dy);
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
    }
}