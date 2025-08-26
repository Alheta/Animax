using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Animax
{
    internal class TitleBar : UserControl
    {
        private Button btnClose;
        private Button btnMaximize;
        private Button btnMinimize;
        private Form mainForm;
        private PictureBox appIcon;
        private Label lblTitle;


        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int RESIZE_MARGIN = 8;
        private const int WM_NCHITTEST = 0x84;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int HTCAPTION = 2;

        [DllImport("user32.dll")]
        private static extern bool AnimateWindow(IntPtr hWnd, int time, int flags);
        private const int AW_SLIDE = 0x40000;
        private const int AW_HIDE = 0x10000;
        private const int AW_ACTIVATE = 0x20000;
        private const int AW_VER_NEGATIVE = 0x00000004;
        private const int AW_VER_POSITIVE = 0x00000008;

        public TitleBar()
        {
            BackColor = SystemColors.ActiveCaption;

            DoubleBuffered = true;
            appIcon = new PictureBox
            {
                Size = new Size(20, 20),
                Location = new Point(8, 8),
                SizeMode = PictureBoxSizeMode.StretchImage,
            };
            Controls.Add(appIcon);

            lblTitle = new Label
            {
                Text = "Title",
                Location = new Point(32, 8),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true
            };
            Controls.Add(lblTitle);

            btnClose = CreateButton("x", this.BackColor, CloseForm);
            btnMaximize = CreateButton("o", this.BackColor, MaximizeForm);
            btnMinimize = CreateButton("_", this.BackColor, MinimizeForm);

            Controls.AddRange(new[] { btnClose, btnMaximize, btnMinimize});

            this.Resize += titleBar_Resize;
            this.Load += titleBar_Load;
            this.MouseDown += titleBar_MouseDown;
        }

        private Button CreateButton(string text, Color backColor, EventHandler onClick)
        {
            Button btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = backColor,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(40, 40),
                Margin = new Padding(0),
                TabStop = false,
                FlatAppearance = { BorderSize = 0 },
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                UseMnemonic = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            btn.Click += onClick;   
            return btn;
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);

            mainForm = this.FindForm();
            mainForm.BackColor = this.BackColor;
        }

        private void titleBar_Load(object sender, EventArgs e)
        {
            if(mainForm?.Icon != null)
            {
                Bitmap bmp = mainForm.Icon.ToBitmap();
                appIcon.Image = new Bitmap(bmp, appIcon.Size);
            }
            lblTitle.Text = mainForm.Text;
        }

        private void titleBar_Resize(object sender, EventArgs e)
        {
            int offset = 0;
            btnClose.Location = new Point(Width - 40 + offset, 0);
            btnMaximize.Location = new Point(Width - 80 + offset, 0);
            btnMinimize.Location = new Point(Width - 120 + offset, 0);
        }


        private void titleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(mainForm.Handle, 0xA1, HTCAPTION, 0);
            }
        }

        private void CloseForm(object sender, EventArgs e) => mainForm?.Close();

        private void MinimizeForm(object sender, EventArgs e)
        {
            if (mainForm != null)
            {
                //AnimateWindow(mainForm.Handle, 200, AW_SLIDE | AW_HIDE | AW_VER_NEGATIVE);
                mainForm.WindowState = FormWindowState.Minimized;
            }
        }

        private void MaximizeForm(object sender, EventArgs e)
        {
            if (mainForm != null)
            {
                //AnimateWindow(mainForm.Handle, 200, AW_SLIDE | AW_ACTIVATE | AW_VER_POSITIVE);
                mainForm.WindowState = mainForm.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
            }
        }
    }
}
