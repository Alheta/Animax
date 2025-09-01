using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Animax
{
    public class ImagePreview : UserControl
    {
        public ImageResource ImageResource { get; set; }
        public Image PreviewImage = new Bitmap(1, 1);
        public string FilePath;
        public bool isSelected { get; set; }


        public event Action<ImagePreview> clicked;

        public ImagePreview()
        {
            this.Text = "";
            this.FilePath = "";
            
        }
        public ImagePreview(ImageResource resource)
        {
            this.ImageResource = resource;
            this.AutoSize = false;
            this.Width = 300;
            this.Height = 80;

            if (resource != null)
            {
                this.FilePath = resource?.FilePath;
                if (File.Exists(resource.FilePath))
                    PreviewImage = Image.FromFile(resource.FilePath);
            }
            else
            {
                this.FilePath = "";
                PreviewImage = new Bitmap(1, 1);
            }

            MouseDown += (s, e) =>
            {
                Console.WriteLine("CLICK");
                clicked.Invoke(this);
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.FillRectangle(isSelected ? SystemBrushes.Highlight : Brushes.White, e.ClipRectangle);

            Rectangle imageRect = new Rectangle(5, 5, 64, 64);

            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

            if (PreviewImage != null)
            {
                e.Graphics.DrawImage(PreviewImage, imageRect);
            }

            string[] parts = FilePath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            string[] lastParts = parts.Skip(Math.Max(0, parts.Length - 5)).ToArray();

            using (Brush brush = new SolidBrush(isSelected ? Color.White : Color.Black))
            {
                string text = string.Join("/", lastParts);
                Rectangle textBound = new Rectangle(e.ClipRectangle.X, e.ClipRectangle.Y, e.ClipRectangle.Width, e.ClipRectangle.Height);
                textBound.X += imageRect.Width;
                textBound.Width -= imageRect.Width;
                StringFormat format = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.None };
                e.Graphics.DrawString(text, this.Font, brush, textBound, format);
            }
        }

    }
}
