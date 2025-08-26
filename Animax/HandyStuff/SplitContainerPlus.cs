using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Animax
{
    public class SplitContainerPlus : SplitContainer
    {
        public SplitContainerPlus()
        {
            this.DoubleBuffered(true);

            this.SplitterMoved += OnSplitterMoved;

        }


        private void OnSplitterMoved(object sender, SplitterEventArgs e)
        {
            RefreshChildControls(Panel1);
            RefreshChildControls(Panel2);
        }

        private void RefreshChildControls(Control parent)
        {
            parent.Refresh();
            parent.Invalidate();
            foreach (Control control in parent.Controls)
            {
                control.Refresh();
                control.Invalidate();
                if (control.HasChildren)
                {
                    RefreshChildControls(control);
                }
            }
        }

    }
}
