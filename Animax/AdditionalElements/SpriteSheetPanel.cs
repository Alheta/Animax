using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Animax
{
    public class SpriteSheetPanel : Panel
    {
        private Image spriteSheet;
        public Rectangle selection;
        public PointF pivot;
        private Point imageOffset = Point.Empty;
        private NormalFrame selectedFrame;

        private float zoom = 5f;

        private bool isSelecting = false;
        private bool isResizing = false;
        private bool isPanning = false;
        private bool isMovingPivot = false;
        private bool isMovingSelection = false;

        private Point selectionStart;
        private Point panStart;
        private Point moveOffset;
        private PointF pivotOffset;

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
            this.selection = frame.selection;
            this.pivot = frame.pivotPos;
            this.selectedFrame = frame;

            this.Invalidate();
        }

        public void ClearSpriteSheet()
        {
            this.spriteSheet = null;
            this.selection = Rectangle.Empty;
            this.pivot = PointF.Empty;
            this.selectedFrame = null;

            this.Invalidate();
        }

        public bool HasSpriteSheet()
        { 
            return this.spriteSheet != null;
        }

        public void ResetSelection()
        {
            this.selection = Rectangle.Empty;
            this.pivot = PointF.Empty;
            this.Invalidate();
        }

        public void UpdateSelection(SpriteSheetProperties prop)
        {
            if (!HasSpriteSheet())
                return;

            selection = new Rectangle(prop.X, prop.Y, prop.Width, prop.Height);
            selectedFrame.pivotPos = new PointF(prop.PivotX, prop.PivotY);
            this.Invalidate();
        }

        public Bitmap GetSelectedImage()
        {
            if (selection.IsEmpty)
                return null;

            Rectangle sourceRect = new Rectangle(selection.X, selection.Y, selection.Width, selection.Height);
            Bitmap cropped = new Bitmap(sourceRect.Width, sourceRect.Height);

            using (Graphics g = Graphics.FromImage(cropped))
            {
                g.DrawImage(spriteSheet, new Rectangle(0, 0, sourceRect.Width, sourceRect.Height), selection, GraphicsUnit.Pixel);
            }

            return cropped;
        }

        public PointF GetSelectePivot()
        {
            if (selection.IsEmpty)
                return Point.Empty;

            PointF pivotRelative = new PointF(selection.X - selectedFrame.pivotPos.X, selection.Y - selectedFrame.pivotPos.Y);

            return pivotRelative;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

            e.Graphics.Clear(Color.LightGray);

            if (spriteSheet == null)
                return;

            if (zoom > 0.8f)
                DrawGrid(e.Graphics, showSheetGrid);

            e.Graphics.TranslateTransform(imageOffset.X, imageOffset.Y);
            e.Graphics.ScaleTransform(zoom, zoom);

            e.Graphics.DrawImage(spriteSheet, 0, 0);


            if (selection.Width > 0 && selection.Height > 0)
            {
                using (Pen selPen = new Pen(Color.Red, 2 / zoom))
                {
                    selPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    e.Graphics.DrawRectangle(selPen, selection);
                }
                DrawHandles(e.Graphics);

                using (var fillBrush = new SolidBrush(Color.Purple))
                using (var borderPen = new Pen(Color.White, 0.4f / zoom))
                {
                    e.Graphics.FillRectangle(fillBrush, pivot.X - 0.5f, pivot.Y - 0.5f, 1, 1);
                    e.Graphics.DrawRectangle(borderPen, pivot.X - 0.5f, pivot.Y - 0.5f, 1, 1);
                }

            }
        }

        private void DrawHandles(Graphics g)
        {
            var handles = GetHandleRects();
            using (var fillBrush = new SolidBrush(Color.White))
            using (var borderPen = new Pen(Color.Black, 0.4f / zoom))
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

                if (!selection.IsEmpty)
                {
                    if (ModifierKeys.HasFlag(Keys.Control) && selection.Contains(imgPoint))
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
                    else if (selection.Contains(imgPoint))
                    {
                        isMovingSelection = true;
                        moveOffset = new Point(imgPoint.X - selection.X, imgPoint.Y - selection.Y);
                        pivotOffset = new PointF(imgPoint.X - pivot.X, imgPoint.Y - pivot.Y);
                        return;
                    }
                }

                if (selection.IsEmpty)
                {
                    isSelecting = true;
                    selectionStart = imgPoint;
                    selection = new Rectangle(selectionStart, Size.Empty);
                    pivot = selectionStart;
                    Invalidate();
                }
            }

            if (e.Button == MouseButtons.Right)
            {
                isMovingPivot = true;
                PointF mousePos = (PointF)ScreenToImage(e.Location);
                float snappedX = (float)Math.Round(mousePos.X);
                float snappedY = (float)Math.Round(mousePos.Y);

                pivot = new PointF(snappedX, snappedY);
                selectedFrame.pivotPos = pivot;
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

                    selection = new Rectangle(x, y, w, h);
                    pivot = new PointF(x, y);

                    selectedFrame.imagePreview.relativePivot = GetSelectePivot();
                    selectedFrame.pivotPos = pivot;
                }
                else if (isResizing)
                    ResizeSelection(e.Location);
                else if (isMovingSelection)
                {
                    Point imgPoint = ScreenToImage(e.Location);
                    selection.X = imgPoint.X - moveOffset.X;
                    selection.Y = imgPoint.Y - moveOffset.Y;

                    selectedFrame.imagePreview.relativePivot = GetSelectePivot();

                    if(ModifierKeys.HasFlag(Keys.Alt))
                    {
                        pivot.X = imgPoint.X - pivotOffset.X;
                        pivot.Y = imgPoint.Y - pivotOffset.Y;
                    }
                    selectedFrame.pivotPos = pivot;
                }

                selectedFrame.imagePreview.savedImage = GetSelectedImage();
                selectedFrame.selection = selection;

                SelectionChanged?.Invoke(selectedFrame);

                Invalidate();
            }

            if (e.Button == MouseButtons.Right)
            {
                if (isMovingPivot)
                {
                    PointF mousePos = (PointF)ScreenToImage(e.Location);
                    float snappedX = (float)Math.Ceiling(mousePos.X);
                    float snappedY = (float)Math.Ceiling(mousePos.Y);

                    pivot = new PointF(snappedX, snappedY);

                    selectedFrame.imagePreview.relativePivot = GetSelectePivot();
                    selectedFrame.pivotPos = pivot;

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
                selectedFrame.selection = selection;
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
            var sel = selection;

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

            selection = sel;
        }

        private Point ScreenToImage(Point screen)
        {
            return new Point(
                (int)((screen.X - imageOffset.X) / zoom),
                (int)((screen.Y - imageOffset.Y) / zoom)
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

        public Rectangle SelectionRect
        {
            get { return selection; }
            set
            {
                selection = value;
                Invalidate();
            }
        }
    }
}