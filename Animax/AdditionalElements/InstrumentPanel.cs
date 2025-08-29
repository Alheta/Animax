using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace Animax.HandyStuff
{
    public enum Instrument { CURSOR, MOVE, ROTATE, SCALE };


    public class InstrumentPanel : Panel
    {
        public Dictionary<Button, Instrument> Instruments = new();

        private int buttonSize = 30;

        public Instrument selectedInstrument = Instrument.CURSOR;

        public event Action<Instrument> InstrumentSelected;

        public InstrumentPanel()
        {
            BorderStyle = BorderStyle.FixedSingle;

            Button buttonCursor = new Button
            {
                Text = "",
                Size = new Size(buttonSize, buttonSize),
                Location = new Point(2, 5),
            };

            Button buttonMove = new Button
            {
                Text = "",
                Size = new Size(buttonSize, buttonSize),
                Location = new Point(2, 35),
            };

            Button buttonRotate = new Button
            {
                Text = "",
                Size = new Size(buttonSize, buttonSize),
                Location = new Point(2, 65),
            };


            Instruments.Add(buttonRotate, Instrument.ROTATE);
            Instruments.Add(buttonMove, Instrument.MOVE);
            Instruments.Add(buttonCursor, Instrument.CURSOR);

            Controls.Add(buttonRotate);
            Controls.Add(buttonMove);
            Controls.Add(buttonCursor);

            foreach (var dict in Instruments)
            {
                dict.Key.Click += Button_Click;
            }
        }


        private void Button_Click(object sender, EventArgs e)
        {
            Instruments.TryGetValue((Button)sender, out Instrument inst);
            SelectInstrument(inst);
        }


        public void SelectInstrument(Instrument instrument)
        {
            foreach (var dict in Instruments)
            {
                if (dict.Value != instrument)
                    dict.Key.BackColor = SystemColors.Control;
                else
                    dict.Key.BackColor = SystemColors.Highlight;

            }
            selectedInstrument = instrument;
            InstrumentSelected?.Invoke(selectedInstrument);
        }
    }


    public class Gizmo
    {
        public Instrument Mode { get; set; } = Instrument.CURSOR;
        public PointF Position { get; set; }
        public float Scale { get; set; } = 1f;
        public PointF LastInteractionWorldPos { get; set; }
        public GizmoPart ActivePart => activePart;


        // Размеры
        public float GizmoSize { get; set; } = 150f;
        private float CurrentSize => GizmoSize / Scale;
        private float HotSpotSize => 10 / Scale;

        public enum GizmoPart
        {
            None,
            MoveX, MoveY, MoveAll,
            Rotate,
            ScaleX, ScaleY, ScaleAll
        }

        private GizmoPart activePart = GizmoPart.None;
        private GizmoPart hoverPart = GizmoPart.None;

        public void UpdateHover(PointF mousePos)
        {
            hoverPart = GizmoPart.None;
            if (Mode == Instrument.CURSOR) return;

            switch (Mode)
            {
                case Instrument.MOVE:
                    if (IsInRect(mousePos, Position, HotSpotSize))
                        hoverPart = GizmoPart.MoveAll;
                    else if (IsInRect(mousePos, new PointF(Position.X + CurrentSize / 2, Position.Y), HotSpotSize))
                        hoverPart = GizmoPart.MoveX;
                    else if (IsInRect(mousePos, new PointF(Position.X, Position.Y - CurrentSize / 2), HotSpotSize))
                        hoverPart = GizmoPart.MoveY;
                    break;

                case Instrument.ROTATE:
                    if (IsInCircle(mousePos, Position, CurrentSize / 2))
                        hoverPart = GizmoPart.Rotate;
                    break;

                case Instrument.SCALE:
                    if (IsInRect(mousePos, Position, HotSpotSize))
                        hoverPart = GizmoPart.ScaleAll;
                    else if (IsInRect(mousePos, new PointF(Position.X + CurrentSize / 2, Position.Y), HotSpotSize))
                        hoverPart = GizmoPart.ScaleX;
                    else if (IsInRect(mousePos, new PointF(Position.X, Position.Y - CurrentSize / 2), HotSpotSize))
                        hoverPart = GizmoPart.ScaleY;
                    break;
            }
        }

        public bool StartInteraction(PointF mousePos)
        {
            UpdateHover(mousePos);
            activePart = hoverPart;
            LastInteractionWorldPos = mousePos;
            return activePart != GizmoPart.None;
        }

        public void EndInteraction()
        {
            activePart = GizmoPart.None;
        }

        public void Draw(Graphics g)
        {
            DrawCursorGizmo(g);

            switch (Mode)
            {
                case Instrument.MOVE:
                    DrawMoveGizmo(g);
                    break;
                case Instrument.ROTATE:
                    DrawRotateGizmo(g);
                    break;
                case Instrument.SCALE:
                    DrawScaleGizmo(g);
                    break;
            }
        }

        private void DrawCursorGizmo(Graphics g)
        {
            g.DrawEllipse(Pens.Red, Position.X - 2, Position.Y - 2, 4, 4);
        }

        private void DrawMoveGizmo(Graphics g)
        {
            float halfSize = CurrentSize / 2;

            // Ось X
            using (var pen = new Pen(GetCurrentColor(GizmoPart.MoveX), 3 / Scale))
            {
                g.DrawLine(pen, Position.X, Position.Y, Position.X + halfSize, Position.Y);
                g.FillRectangle(new SolidBrush(GetCurrentColor(GizmoPart.MoveX)),
                    Position.X + halfSize - HotSpotSize / 2, Position.Y - HotSpotSize / 2,
                    HotSpotSize, HotSpotSize);
            }

            // Ось Y
            using (var pen = new Pen(GetCurrentColor(GizmoPart.MoveY), 3 / Scale))
            {
                g.DrawLine(pen, Position.X, Position.Y, Position.X, Position.Y - halfSize);
                g.FillRectangle(new SolidBrush(GetCurrentColor(GizmoPart.MoveY)),
                    Position.X - HotSpotSize / 2, Position.Y - halfSize - HotSpotSize / 2,
                    HotSpotSize, HotSpotSize);
            }

            // Центральный квадрат
            g.FillRectangle(new SolidBrush(GetCurrentColor(GizmoPart.MoveAll)),
                Position.X - HotSpotSize / 2, Position.Y - HotSpotSize / 2,
                HotSpotSize, HotSpotSize);
        }

        private void DrawRotateGizmo(Graphics g)
        {
            using (var pen = new Pen(GetCurrentColor(GizmoPart.Rotate), 3 / Scale))
            {
                g.DrawEllipse(pen,
                    Position.X - CurrentSize / 2,
                    Position.Y - CurrentSize / 2,
                    CurrentSize, CurrentSize);

                g.FillEllipse(new SolidBrush(GetCurrentColor(GizmoPart.Rotate)),
                    Position.X + CurrentSize / 2 - HotSpotSize / 2,
                    Position.Y - HotSpotSize / 2,
                    HotSpotSize, HotSpotSize);
            }
        }

        private void DrawScaleGizmo(Graphics g)
        {
            float halfSize = CurrentSize / 2;

            using (var pen = new Pen(GetCurrentColor(GizmoPart.ScaleX), 3 / Scale))
            {
                g.DrawLine(pen, Position.X, Position.Y, Position.X + halfSize, Position.Y);
                g.DrawRectangle(pen,
                    Position.X + halfSize - HotSpotSize / 2, Position.Y - HotSpotSize / 2,
                    HotSpotSize, HotSpotSize);
            }

            using (var pen = new Pen(GetCurrentColor(GizmoPart.ScaleY), 3 / Scale))
            {
                g.DrawLine(pen, Position.X, Position.Y, Position.X, Position.Y - halfSize);
                g.DrawRectangle(pen,
                    Position.X - HotSpotSize / 2, Position.Y - halfSize - HotSpotSize / 2,
                    HotSpotSize, HotSpotSize);
            }

            g.FillRectangle(new SolidBrush(GetCurrentColor(GizmoPart.ScaleAll)),
                Position.X - HotSpotSize / 2, Position.Y - HotSpotSize / 2,
                HotSpotSize, HotSpotSize);
        }

        private Color GetCurrentColor(GizmoPart part)
        {
            if (activePart == part) return Color.Gold;
            if (hoverPart == part) return Color.Cyan;

            return part switch
            {
                GizmoPart.MoveX => Color.Red,
                GizmoPart.MoveY => Color.Green,
                GizmoPart.MoveAll => Color.Blue,
                GizmoPart.Rotate => Color.Orange,
                GizmoPart.ScaleX => Color.Red,
                GizmoPart.ScaleY => Color.Green,
                GizmoPart.ScaleAll => Color.Blue,
                _ => Color.Gray
            };
        }

        private bool IsInRect(PointF point, PointF center, float size)
        {
            return point.X >= center.X - size / 2 &&
                   point.X <= center.X + size / 2 &&
                   point.Y >= center.Y - size / 2 &&
                   point.Y <= center.Y + size / 2;
        }

        private bool IsInCircle(PointF point, PointF center, float radius)
        {
            float dx = point.X - center.X;
            float dy = point.Y - center.Y;
            return dx * dx + dy * dy <= radius * radius;
        }
    }
}