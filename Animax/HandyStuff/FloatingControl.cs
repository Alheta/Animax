using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace Animax.HandyStuff
{
    public class FloatingControl : UserControl
    {
        protected Panel headerPanel;
        protected Label titleLabel;
        protected Button closeButton;
        protected Panel contentPanel;

        public event EventHandler CloseRequested;

        public FloatingControl()
        {
            InitializeComponent();
        }


        private void InitializeComponent()
        {
            this.Size = new Size(400, 300);
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.BorderStyle = BorderStyle.FixedSingle;

            headerPanel = new Panel
            {
                Height = 30,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(30, 30, 30),
                Cursor = Cursors.SizeAll
            };

            titleLabel = new Label
            {
                Text = "Panel Title",
                ForeColor = Color.White,
                Location = new Point(10, 5),
                AutoSize = true
            };

            closeButton = new Button
            {
                Text = "X",
                Size = new Size(25, 25),
                Location = new Point(365, 3),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Left | AnchorStyles.Top
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (s, e) => CloseRequested?.Invoke(this, EventArgs.Empty);

            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(60, 60, 60),
                Padding = new Padding(10)
            };

            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(closeButton);

            this.Controls.Add(headerPanel);
            this.Controls.Add(contentPanel);

            // Настройка перетаскивания
            SetupDragging();
        }

        protected virtual void SetupDragging()
        {
            headerPanel.MouseDown += (s, e) => { /* логика перетаскивания */ };
            headerPanel.MouseMove += (s, e) => { /* логика перетаскивания */ };
            headerPanel.MouseUp += (s, e) => { /* логика перетаскивания */ };
        }

        public string PanelTitle
        {
            get => titleLabel.Text;
            set => titleLabel.Text = value;
        }
    }
}
