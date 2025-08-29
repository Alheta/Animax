using Animax.HandyStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animax.AdditionalElements
{
    internal class LayerPanel : FloatingControl
    {
        private void InitializeComponent()
        {
            this.headerPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // headerPanel
            // 
            this.headerPanel.Size = new System.Drawing.Size(398, 30);
            // 
            // closeButton
            // 
            this.closeButton.FlatAppearance.BorderSize = 0;
            // 
            // contentPanel
            // 
            this.contentPanel.Size = new System.Drawing.Size(398, 298);
            // 
            // LayerAdd
            // 
            this.Name = "LayerAdd";
            this.Size = new System.Drawing.Size(398, 298);
            this.headerPanel.ResumeLayout(false);
            this.headerPanel.PerformLayout();
            this.ResumeLayout(false);

        }
    }
}
