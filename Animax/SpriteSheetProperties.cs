using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Animax
{
    public class SpriteSheetProperties
    {
        public int X { get; set; }
        public int Y { get; set; }  
        public int Width { get; set; }
        public int Height { get; set; }
        public int PivotX { get; set; }
        public int PivotY { get; set; }
    }

    public class PropertiesTab
    {
        public TextBox x;
        public TextBox y;
        public TextBox width;
        public TextBox height;
        public TextBox pivotX;
        public TextBox pivotY;
        public TextBox posX;
        public TextBox posY;
        public TextBox scaleX;
        public TextBox scaleY;
        public TextBox rotation;
        public CheckBox visibility;
        public CheckBox interpolated;
    }
}
