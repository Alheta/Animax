using Animax.AdditionalElements;
using Animax.HandyStuff;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Animax
{
    public partial class Main : Form
    {
        public List<Animation> animations = new List<Animation>();

        private ToolStripMenuItem imageMenu = new ToolStripMenuItem("Images");

        private ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
        private ToolStripMenuItem newProjectItem = new ToolStripMenuItem("New");
        private ToolStripMenuItem openProjectItem = new ToolStripMenuItem("Open");
        private ToolStripMenuItem saveProjectItem = new ToolStripMenuItem("Save");
        private ToolStripMenuItem saveProjectAsItem = new ToolStripMenuItem("Save As");
        private ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit");

        public Mediator _mediator;
        private ProjectManager projectManager;
        public Main()
        {
            InitializeComponent();
            this.IsMdiContainer = true;

            //Setting up mediator
            _mediator = new Mediator();

            projectManager = new ProjectManager();

            _mediator.projectManager = projectManager;
            _mediator.framePanel = timelineFramePanel1;
            _mediator.layerPanel = timelineLayerPanel1;
            _mediator.spritePanel = spriteSheetPanel1;
            _mediator.animPreview = animationPreviewPanel1;
            _mediator.animPanel = animationPanel1;
            _mediator.instPanel = instrumentPanel1;

            FramePropertiesPanel framePropertiesPanel1 = new FramePropertiesPanel(_mediator);
            TimelineMarker timelineMarker = new TimelineMarker(_mediator);

            _mediator.marker = timelineMarker;
            _mediator.propers = framePropertiesPanel1;
            _mediator.mainForm = this;
            _mediator.clrManager = new HandyStuff.ColorManager();
            _mediator.eventPanel = eventPanel1;

            //Assigning mediator to components
            timelineFramePanel1._mediator = _mediator;
            timelineLayerPanel1._mediator = _mediator;
            animationPanel1._mediator = _mediator;
            projectManager._mediator = _mediator;
            animationPreviewPanel1._mediator = _mediator;
            spriteSheetPanel1._mediator = _mediator;
            eventPanel1._mediator = _mediator;

            spriteSheetPanel1.Controls.Add(framePropertiesPanel1);
            timelineFramePanel1.fixedHeader.Controls.Add(timelineMarker);
            timelineMarker.BringToFront();
            framePropertiesPanel1.Dock = DockStyle.Right;


        }
        private void Main_Load(object sender, EventArgs e)
        {
            this.Text = "ANIMAX";

            menuStrip1.Font = new Font("Segoe UI", 10);

            fileMenu.DropDownItems.Add(newProjectItem);
            fileMenu.DropDownItems.Add(openProjectItem);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(saveProjectItem);
            fileMenu.DropDownItems.Add(saveProjectAsItem);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(exitItem);

            imageMenu.Click += openImageFrom_Click;

            newProjectItem.Click += newMenuItem_Click;
            openProjectItem.Click += openMenuItem_Click;
            saveProjectItem.Click += SaveProjectMenuItem_Click;
            saveProjectAsItem.Click += SaveAsProjectMenuItem_Click;
            exitItem.Click += ExitMenuItem_Click;

            menuStrip1.Items.Add(fileMenu);
            menuStrip1.Items.Add(imageMenu);

            //AnimationPanel
            animationPanel1.AnimationsUpdate += _mediator.OnAnimationSelectionUpdate;

            //TimeLine 
            timelineFramePanel1.lbl = label7;
            timelineFramePanel1.FrameUpdated += _mediator.OnFrameSelectionUpdate;
            timelineLayerPanel1.LayersUpdate += _mediator.OnLayerSelectionUpdate;

            //Spritesheet
            spriteSheetPanel1.SelectionChanged += _mediator.UpdateProperties;

            playButton.Click += button1_Click;

            eventPanel1.AddEvent("AAAA");
            eventPanel1.AddEvent("AAAAbbb");
            eventPanel1.AddEvent("AAadwb");
            eventPanel1.AddEvent("e12314");

            Console.WriteLine(_mediator.projectManager.currentProject.events.Count);

        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Shift | Keys.N))
            {
                if (animationPanel1.selectedItem == null)
                {
                    MessageBox.Show("Please select an animation before adding a layer", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
                }
                _mediator.OpenLayerEditor();
                return true;
            }
            else if (keyData == (Keys.Control | Keys.N))
            {
                if (timelineLayerPanel1.selectedItem != null)
                {
                    timelineFramePanel1.AddFrame(timelineLayerPanel1.selectedItem, false);
                }
                return true;
            }
            if (_mediator.framePanel.selectedItems.Count > 0)
            {
                if (keyData == Keys.Oemplus)
                {
                    timelineFramePanel1.AddSelectedFrameDuration(1);
                    return true;
                }
                else if (keyData == Keys.OemMinus)
                {
                    timelineFramePanel1.AddSelectedFrameDuration(-1);
                    return true;
                }
            }
            if (keyData == Keys.C)
            {
                _mediator.instPanel.SelectInstrument(HandyStuff.Instrument.CURSOR);
            }
            else if (keyData == Keys.M)
            {
                _mediator.instPanel.SelectInstrument(HandyStuff.Instrument.MOVE);
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            if ((string)btn.Tag == "Play")
            {
                timelineFramePanel1.StartAnimation();
                btn.Text = "Pause";
                btn.Tag = "Pause";
            }
            else if ((string)btn.Tag == "Pause")
            {
                timelineFramePanel1.PauseAnimation();
                btn.Text = "Play";
                btn.Tag = "Play";
            }
        }

        private void ButtonAddEvent_Click(object sender, EventArgs e)
        {

        }
        private void button_Click(object sender, EventArgs e)
        {

        }
        private void openImageFrom_Click(object sender, EventArgs e)
        {
            _mediator.OpenImageManager();
        }


        private void newMenuItem_Click(object sender, EventArgs e)
        {
            if (projectManager.CheckSaveBeforeExit())
            {
                projectManager = new ProjectManager();
                _mediator.projectManager = projectManager;
                _mediator.UpdateTitle();
                _mediator.UpdateUIElements();
                projectManager._mediator = _mediator;
            }
        }

        private void openMenuItem_Click(object sender, EventArgs e)
        {
            if (projectManager.CheckSaveBeforeExit())
            {
                if (projectManager.LoadProject())
                {
                    _mediator.UpdateTitle();
                    _mediator.UpdateUIElements();
                    _mediator.projectManager.UpdateAllPreviews();
                }
            }
        }

        private void SaveProjectMenuItem_Click(object sender, EventArgs e)
        {
            projectManager.SaveProject();
            _mediator.UpdateTitle();
        }

        private void SaveAsProjectMenuItem_Click(object sender, EventArgs e)
        {
            projectManager.SaveProjectAs();
            _mediator.UpdateTitle();
        }

        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            if (projectManager.CheckSaveBeforeExit())
            {
                Application.Exit();
            }
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!projectManager.CheckSaveBeforeExit())
            {
                e.Cancel = true;
            }
        }
    }
}
