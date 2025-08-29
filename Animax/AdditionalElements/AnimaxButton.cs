using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Animax.HandyStuff
{
    public class AnimaxButton : Control
    {

        private bool isHovered = false;
        private bool isPressed = false;
        private ButtonShape shape = ButtonShape.Rectangle;
        private int cornerRadius = 10;
        private Image buttonImage;
        private Size imageSize = new Size(16, 16);

        private Color borderColor = Color.Gray;
        private Color hoverBorderColor = Color.DodgerBlue;
        private Color pressedBorderColor = Color.RoyalBlue;
        private Color hoverBackColor = Color.LightSkyBlue;
        private Color pressedBackColor = Color.SteelBlue;


        [Category("Appearance")]
        public ButtonShape Shape
        {
            get => shape;
            set { shape = value; Invalidate(); }
        }

        [Category("Appearance")]
        public int CornerRadius
        {
            get => cornerRadius;
            set { cornerRadius = value; Invalidate(); }
        }

        [Category("Appearance")]
        public Image Image
        {
            get => buttonImage;
            set { buttonImage = value; Invalidate(); }
        }

        [Category("Appearance")]
        public Size ImageSize
        {
            get => imageSize;
            set { imageSize = value; Invalidate(); }
        }

        public AnimaxButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint |
                     ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(100, 40);
            BackColor = Color.LightGray;
            ForeColor = Color.Black;
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Color curBorderColor = isPressed ? pressedBorderColor : isHovered ? hoverBorderColor : borderColor;
            Color curBackColor = isPressed ? pressedBackColor : isHovered ? hoverBackColor : BackColor;

        }
    }

    #region

    public enum ButtonShape 
    {
        Rectangle,
        RoundedRectangle,
        Circle
    }

    #endregion
}
