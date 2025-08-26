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
        public List<FrameEvent> events = new List<FrameEvent>();

        private ToolStripMenuItem imageMenu = new ToolStripMenuItem("Image");
        private ToolStripMenuItem addImageMenuItem = new ToolStripMenuItem("AddImage");
        private ToolStripMenuItem loadedImagesMenuItem = new ToolStripMenuItem("Loaded Images");

        private ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
        private ToolStripMenuItem newProjectItem = new ToolStripMenuItem("New");
        private ToolStripMenuItem openProjectItem = new ToolStripMenuItem("Open");
        private ToolStripMenuItem saveProjectItem = new ToolStripMenuItem("Save");
        private ToolStripMenuItem saveProjectAsItem = new ToolStripMenuItem("Save As");
        private ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit");

        public Mediator _mediator;
        private ProjectManager projectManager;

        private TextBox txt;
        public Main()
        {
            InitializeComponent();

            projectManager = new ProjectManager();

            //Setting up mediator
            _mediator = new Mediator();

            _mediator.projectManager = projectManager;

            _mediator.framePanel = timelineFramePanel1;
            _mediator.layerPanel = timelineLayerPanel1;
            _mediator.spritePanel = spriteSheetPanel1;
            _mediator.animPreview = animationPreviewPanel1;
            _mediator.animPanel = animationPanel1;
            _mediator.instPanel = instrumentPanel1;
            _mediator.marker = new TimelineMarker(_mediator);
            _mediator.propers = new PropertiesTab
            {
                x = textBoxX,
                y = textBoxY,
                width = textBoxWidth,
                height = textBoxHeight,
                pivotX = textBoxPivotX,
                pivotY = textBoxPivotY,
                posX = textBoxPosX,
                posY = textBoxPosY,
                scaleX = textBoxScaleX,
                scaleY = textBoxScaleY,
                rotation = textBoxRotation,
                visibility = checkBoxVis,
                interpolated = checkBoxInterp
            };
            _mediator.eventsBox = comboBox1;
            _mediator.mainForm = this;
            _mediator.clrManager = new HandyStuff.ColorManager();

            //Assigning mediator to components
            timelineFramePanel1._mediator = _mediator;
            timelineLayerPanel1._mediator = _mediator;
            animationPanel1._mediator = _mediator;
            projectManager._mediator = _mediator;

            animationPreviewPanel1._mediator = _mediator;

            events = new List<FrameEvent>();
            comboBox1.DataSource = events;
            comboBox1.DisplayMember = "eventName";

            txt = new TextBox();
            txt.Visible = false;
            splitContainerPlus4.Panel1.Controls.Add(txt);
        }
        private void Main_Load(object sender, EventArgs e)
        {
            this.Text = "ANIMAX";

            openFileDialog1.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp";
            menuStrip1.Font = new Font("Segoe UI", 10);

            fileMenu.DropDownItems.Add(newProjectItem);
            fileMenu.DropDownItems.Add(openProjectItem);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(saveProjectItem);
            fileMenu.DropDownItems.Add(saveProjectAsItem);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(exitItem);

            imageMenu.DropDownItems.Add(addImageMenuItem);
            imageMenu.DropDownItems.Add(new ToolStripSeparator());
            imageMenu.DropDownItems.Add(loadedImagesMenuItem);
            imageMenu.DropDownOpening += OpenLoadedImagesDialog;

            newProjectItem.Click += newMenuItem_Click;
            openProjectItem.Click += openMenuItem_Click;
            saveProjectItem.Click += SaveProjectMenuItem_Click;
            saveProjectAsItem.Click += SaveAsProjectMenuItem_Click;
            exitItem.Click += ExitMenuItem_Click;

            addImageMenuItem.Click += AddImageMenuItem_Click;

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

            //Properties fields
            var propertiesTextBoxes = panelProperties.Controls.OfType<TextBox>().ToList();
            var propertiesCheckBoxes = panelProperties.Controls.OfType<CheckBox>().ToList();
            foreach (TextBox txt in propertiesTextBoxes)
            {
                txt.KeyPress += propertiesFields_KeyPress;
                txt.TextChanged += _mediator.UpdateValuesFromTextBoxes;
            }
            foreach (CheckBox chk in propertiesCheckBoxes)
                chk.CheckedChanged += _mediator.UpdateValuesFromTextBoxes;

            playButton.Click += button1_Click;
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

        private void propertiesFields_KeyPress(object sender, KeyPressEventArgs e)
        {
            TextBox txt = (TextBox)sender;
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) &&
                !(e.KeyChar == '-' && txt.SelectionStart == 0 && !txt.Text.Contains("-")) &&
                !(e.KeyChar == '.' && !txt.Text.Contains(".")))
            {
                e.Handled = true;
            }
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
            if (comboBox1.Visible == true)
            {
                comboBox1.Visible = false;
                txt.Visible = true;
                txt.Location = comboBox1.Location;
                txt.Size = comboBox1.Size;
            }
            else if (!String.IsNullOrEmpty(txt.Text.Trim()))
            {
                if (!events.Any(ev => ev.eventName.Equals(txt.Text.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    comboBox1.Visible = true;
                    txt.Visible = false;
                    events.Add(new FrameEvent(txt.Text));

                    txt.Text = "";

                    comboBox1.DataSource = null;
                    comboBox1.DataSource = events;
                    comboBox1.DisplayMember = "eventName";
                }
            }
        }
        private void button_Click(object sender, EventArgs e)
        {
            if (comboBox1.Visible == false)
            {
                comboBox1.Visible = true;
                txt.Visible = false;
                txt.Text = "";
            }
            else if (comboBox1.SelectedItem != null)
            {
                var ev = comboBox1.SelectedItem as FrameEvent;
                if (ev != null)
                {
                    _mediator.RemoveAllEventFrames(ev);
                    events.Remove(ev);

                    comboBox1.DataSource = null;
                    comboBox1.DataSource = events;
                    comboBox1.DisplayMember = "eventName";
                }
            }
        }


        private void AddImageMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (string file in openFileDialog1.FileNames)
                {
                    ImageResource imgRes = new ImageResource(file);
                    projectManager.currentProject.images.Add(imgRes);
                    imgRes.Index = projectManager.currentProject.images.IndexOf(imgRes);
                }
            }
        }

        private void OpenLoadedImagesDialog(object sender, EventArgs e)
        {
            loadedImagesMenuItem.DropDownItems.Clear();
            foreach (ImageResource img in projectManager.currentProject.images)
            {
                loadedImagesMenuItem.DropDownItems.Add(new ImagePreview(img));
            }
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
