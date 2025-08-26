using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animax.HandyStuff
{
    public class ColorManager
    {
        public enum AnimaxGradients
        {
            LAYER_NORMAL,
            LAYER_POINT,
            LAYER_EVENT,
            TIMELINE_DURATION,

            TIMELINE_RULER,
            TIMELINE_RULER_DARK,

        }

        public static LinearGradientBrush CreateGradient(Rectangle rect, Color color1, Color color2, float angle)
        {
            LinearGradientBrush brush = new LinearGradientBrush(rect, color1, color2, angle);

            return brush;
        }

        public static LinearGradientBrush ApplyGradient(AnimaxGradients type, Rectangle rect)
        {
            var brush = type switch
            {
                AnimaxGradients.LAYER_NORMAL => CreateGradient(rect, Color.FromArgb(255, 66, 245, 215), Color.FromArgb(255, 50, 209, 196), 90f),
                AnimaxGradients.LAYER_POINT => CreateGradient(rect, Color.FromArgb(255, 131, 242, 109), Color.FromArgb(255, 60, 214, 65), 90f),
                AnimaxGradients.LAYER_EVENT => CreateGradient(rect, Color.FromArgb(255, 255, 145, 240), Color.FromArgb(255, 212, 97, 206), 90f),

                AnimaxGradients.TIMELINE_DURATION => CreateGradient(rect, Color.FromArgb(120, 255, 250, 201), Color.FromArgb(80, 224, 194, 72), 90f),

                AnimaxGradients.TIMELINE_RULER => CreateGradient(rect, Color.FromArgb(255, 237, 237, 237), Color.FromArgb(255, 180, 180, 180), 90f),
                AnimaxGradients.TIMELINE_RULER_DARK => CreateGradient(rect, Color.FromArgb(255, 200, 200, 200), Color.FromArgb(255, 150, 150, 150), 90f),
                _ => CreateGradient(rect, Color.White, Color.Black, 90f)
            };

            return brush;
        }
    }
}
