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
    public partial class LayerAdd : Form
    {

        public Mediator _mediator;

        private bool inEditMode;
        private TimelineLayerItem layerToEdit;
        private Main mainForm;
        private TimelineLayerPanel layerPanel;
        private Animation curAnim;
        private List<ImagePreview> loadedImages = new List<ImagePreview>();
        private ImagePreview selectedImage;

        private LayerType selectedLayerType = LayerType.NORMAL;

        ContextMenuStrip strip = new ContextMenuStrip();

        public LayerAdd(TimelineLayerItem layerEdit, Mediator mediator)
        {
            InitializeComponent();

            _mediator = mediator;

            textBox1.Text = "New Layer";
            this.layerToEdit = layerEdit;
            this.mainForm = mediator.mainForm;

            if (layerToEdit != null)
            {
                textBox1.Text = layerToEdit.layer.name;
                selectedImage = new ImagePreview(layerToEdit.layer.assignedImage);
                if (selectedImage != null)
                    pictureBox1.Image = selectedImage.PreviewImage;
            }

            pictureBox1.Click += ShowImageDropDown;
        }

        private void LayerAdd_Load(object sender, EventArgs e)
        {
            if (mainForm == null)
                return;

            curAnim = _mediator.animPanel.selectedItem.animation;
            layerPanel = _mediator.layerPanel;
            inEditMode = layerToEdit == null ? false : true;
            mainForm.Enabled = false;

            foreach (var item in _mediator.projectManager.currentProject.images)
            {
                ImagePreview imgPreview = new ImagePreview(item);
                Console.WriteLine(imgPreview.FilePath);
                imgPreview.Click += imageList_ImageClick;
                loadedImages.Add(imgPreview);
                strip.Items.Add(imgPreview);
            }
        }

        private void LayerAdd_Leave(object sender, EventArgs e)
        {
        }

        //Add layer.
        private void button2_Click(object sender, EventArgs e)
        {
            if (!inEditMode)
            {
                var newLayer = new Layer(selectedLayerType)
                {
                    name = (String.IsNullOrEmpty(textBox1.Text) ? "New Layer" : textBox1.Text)
                };
                _mediator.projectManager.SetLayerImage(newLayer, selectedImage?.ImageResource);

                layerPanel.AddLayer(curAnim, newLayer);
            }
            else if (layerToEdit != null)
            {
                layerToEdit.layer.name = (String.IsNullOrEmpty(textBox1.Text) ? "Layer" : textBox1.Text);
                _mediator.projectManager.SetLayerImage(layerToEdit.layer, selectedImage?.ImageResource);

                _mediator.projectManager.UpdateAllPreviews();
            }

            mainForm.Enabled = true;
            this.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            mainForm.Enabled = true;
            this.Close();
        }

        private void ShowImageDropDown(object sender, EventArgs e)
        {
            strip.Show(this, PointToClient(Cursor.Position));
        }

        private void imageList_ImageClick(object sender, EventArgs e)
        {
            var sel = (ImagePreview)sender;
            selectedImage = new ImagePreview(sel.ImageResource);
            pictureBox1.Image = selectedImage.PreviewImage;
        }

        private void radio_CheckedChanged(object sender, EventArgs e)
        {
            if (radioNormal.Checked)
                selectedLayerType = LayerType.NORMAL;

            if (radioPoint.Checked)
                selectedLayerType = LayerType.POINT;
        }
    }
}
