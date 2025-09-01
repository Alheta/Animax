using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace Animax.AdditionalElements
{
    public class EventPanel : Control
    {
        public Mediator _mediator;
        public ComboBox eventBox;

        public EventPanel()
        {
            Width = 120;
            eventBox = new ComboBox();
            eventBox.FormattingEnabled = true;
            eventBox.DropDownStyle = ComboBoxStyle.DropDownList;
            eventBox.Location = new Point(-10, this.Height / 2 - eventBox.Height/2);
            eventBox.Size = new Size(120, 0);
            eventBox.Anchor = AnchorStyles.Right;

            Controls.Add(eventBox);
        }

        public void AddEvent(string name)
        {
            Event ev = new Event(name);
            _mediator.projectManager.currentProject.events.Add(ev);

            eventBox.DataSource = _mediator.projectManager.currentProject.events;
            eventBox.DisplayMember = "name";
        }
    }
}
