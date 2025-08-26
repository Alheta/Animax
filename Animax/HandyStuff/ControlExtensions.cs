using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

public static class ControlExtensions
{
    public static void DoubleBuffered(this Control control, bool enable)
    {
        try
        {
            var doubleBufferedProperty = typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            doubleBufferedProperty?.SetValue(control, enable, null);

            if (enable)
            {
                var setStyleMethod = typeof(Control).GetMethod("SetStyle",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                var updateStylesMethod = typeof(Control).GetMethod("UpdateStyles",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (setStyleMethod != null && updateStylesMethod != null)
                {
                    setStyleMethod.Invoke(control, new object[] {
                        ControlStyles.OptimizedDoubleBuffer |
                        ControlStyles.AllPaintingInWmPaint |
                        ControlStyles.UserPaint, true });

                    updateStylesMethod.Invoke(control, null);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enabling double buffering: {ex.Message}");
        }
    }
}