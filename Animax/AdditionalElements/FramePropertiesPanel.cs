using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace Animax.HandyStuff
{
    public class FramePropertiesPanel : Panel
    {
        public Mediator _mediator;

        public PropertiesTab elements;

        private int gap = 10;

        private Size defaultSize = new Size(50, 20);
        private Size wideSize => new Size(defaultSize.Width * 2 + gap, defaultSize.Height);

        private int leftPos = 5;
        private int rightPos => 5 + defaultSize.Width + gap;

        private List<Control> items = new();
        private List<Control> selectionList = new();
        private List<Control> frameList = new();

        public enum Mode { SELECTION, FRAME, BOTH, NONE};
        public Mode mode = Mode.BOTH;

        public FramePropertiesPanel(Mediator mediator)
        {
            _mediator = mediator;
            Width = wideSize.Width + gap * 2;

            InitSelectionProps();
        }

        public void InitSelectionProps()
        {
            Label header = new Label
            {
                AutoSize = false,
                Location = new Point(leftPos, 10),
                Size = new Size(rightPos + defaultSize.Width, 20),
                Text = "SELECTION\nPROPERTIES",
                Font = new Font("Microsoft Sans Serif", 6),
                TextAlign = ContentAlignment.MiddleCenter,
            };
            Controls.Add(header);

            Label header2 = new Label
            {
                AutoSize = false,
                Location = new Point(leftPos, 140),
                Size = new Size(rightPos + defaultSize.Width, 20),
                Text = "FRAME\nPROPERTIES",
                Font = new Font("Microsoft Sans Serif", 6),
                TextAlign = ContentAlignment.MiddleCenter,
            };
            Controls.Add(header2);

            elements = new PropertiesTab
            {
                x = MakeTextBox(defaultSize, new Point(leftPos, 30), Mode.SELECTION),
                y = MakeTextBox(defaultSize, new Point(rightPos, 30), Mode.SELECTION),
                width = MakeTextBox(defaultSize, new Point(leftPos, 65), Mode.SELECTION),
                height = MakeTextBox(defaultSize, new Point(rightPos, 65), Mode.SELECTION),

                pivotX = MakeTextBox(defaultSize, new Point(leftPos, 100), Mode.SELECTION),
                pivotY = MakeTextBox(defaultSize, new Point(rightPos, 100), Mode.SELECTION),

                posX = MakeTextBox(defaultSize, new Point(leftPos, 160), Mode.FRAME),
                posY = MakeTextBox(defaultSize, new Point(rightPos, 160), Mode.FRAME),

                scaleX = MakeTextBox(defaultSize, new Point(leftPos, 195), Mode.FRAME),
                scaleY = MakeTextBox(defaultSize, new Point(rightPos, 195), Mode.FRAME),

                rotation = MakeTextBox(wideSize, new Point(leftPos, 230), Mode.FRAME),

                visibility = MakeCheckBox("VISIBLE", wideSize, new Point(leftPos, 265), Mode.FRAME),
                interpolated = MakeCheckBox("INTERPOLATION", wideSize, new Point(leftPos, 285), Mode.FRAME),

            };

            MakeLabel("SEL X", new Size(defaultSize.Width, 10), new Point(leftPos, 30 + defaultSize.Height));
            MakeLabel("SEL Y", new Size(defaultSize.Width, 10), new Point(rightPos, 30 + defaultSize.Height));
            MakeLabel("WIDTH", new Size(defaultSize.Width, 10), new Point(leftPos, 65 + defaultSize.Height));
            MakeLabel("HEIGHT", new Size(defaultSize.Width, 10), new Point(rightPos, 65 + defaultSize.Height));
            MakeLabel("PIVOT X", new Size(defaultSize.Width, 10), new Point(leftPos, 100 + defaultSize.Height));
            MakeLabel("PIVOT Y", new Size(defaultSize.Width, 10), new Point(rightPos, 100 + defaultSize.Height));
            MakeLabel("POS X", new Size(defaultSize.Width, 10), new Point(leftPos, 160 + defaultSize.Height));
            MakeLabel("POS Y", new Size(defaultSize.Width, 10), new Point(rightPos, 160 + defaultSize.Height));
            MakeLabel("SCALE X", new Size(defaultSize.Width, 10), new Point(leftPos, 195 + defaultSize.Height));
            MakeLabel("SCALE Y", new Size(defaultSize.Width, 10), new Point(rightPos, 195 + defaultSize.Height));
            MakeLabel("ROTATION ANGLE", new Size(wideSize.Width, 10), new Point(leftPos, 230 + defaultSize.Height));
        }

        public TextBox MakeTextBox(Size size, Point location, Mode mode)
        {
            TextBox txt = new TextBox
            {
                Size = size,
                Location = location,
                Text = ""
            };
            txt.KeyPress += propertiesFields_KeyPress;
            txt.KeyDown += propertiesFields_KeyDown;
            txt.TextChanged += _mediator.UpdateValuesFromTextBoxes;

            Controls.Add(txt);

            if (mode == Mode.SELECTION)
                selectionList.Add(txt);
            else if (mode == Mode.FRAME)
                frameList.Add(txt);

            items.Add(txt);

            return txt;
        }

        public CheckBox MakeCheckBox(string text, Size size, Point location, Mode mode)
        {
            CheckBox check = new CheckBox
            {
                AutoSize = false,
                Location = location,
                Size = size,
                Text = text,
                Font = new Font("Microsoft Sans Serif", 6),
                TextAlign = ContentAlignment.MiddleCenter,
            };
            check.CheckedChanged += _mediator.UpdateValuesFromTextBoxes;

            Controls.Add(check);

            if (mode == Mode.SELECTION)
                selectionList.Add(check);
            else if (mode == Mode.FRAME)
                frameList.Add(check);

            items.Add(check);

            return check;
        }

        public void MakeLabel(string text, Size size, Point location)
        {
            Label label = new Label
            {
                AutoSize = false,
                Location = location,
                Size = size,
                Text = text,
                Font = new Font("Microsoft Sans Serif", 6),
                TextAlign = ContentAlignment.MiddleCenter,
            };
            Controls.Add(label);
        }

        public void propertiesFields_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox txt = (TextBox)sender;
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) &&
                !(e.KeyChar == '-' && txt.SelectionStart == 0 && !txt.Text.Contains("-")))
            {
                e.Handled = true;
            }
        }

        private void propertiesFields_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        public void ChangeMode(Mode mode)
        {
            switch (mode)
            {
                case Mode.SELECTION:
                    foreach (Control o in items)
                        o.Enabled = false;

                    foreach (Control o in selectionList)
                        o.Enabled = true;

                    break;

                case Mode.FRAME:
                    foreach (Control o in items)
                        o.Enabled = false;

                    foreach (Control o in frameList)
                        o.Enabled = true;

                    break;

                case Mode.BOTH:
                    foreach (Control o in items)
                        o.Enabled = true;
                    break;

                case Mode.NONE:
                    foreach (Control o in items)
                        o.Enabled = false;
                    break;
            }
        }
    }
}
