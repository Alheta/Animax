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
 
        private LayerType selectedLayerType = LayerType.NORMAL;
        public LayerAdd(TimelineLayerItem layerEdit, Mediator mediator)
        {
            InitializeComponent();

            _mediator = mediator;

            textBox1.Text = "New Layer";
            this.layerToEdit = layerEdit;
            this.mainForm = mediator.mainForm;

            imagePreviewPanel1._mediator = mediator;
            imagePreviewPanel1.LoadImages();

            if (layerToEdit != null)
            {
                textBox1.Text = layerToEdit.layer.name;

                if (layerToEdit.layer is NormalLayer l)
                    imagePreviewPanel1.SetSelectedItem(imagePreviewPanel1.FindPreviewByImageResource(l.assignedImage));
            }
        }

        private void LayerAdd_Load(object sender, EventArgs e)
        {
            if (mainForm == null)
                return;

            inEditMode = layerToEdit == null ? false : true;
            mainForm.Enabled = false;

            if (inEditMode)
            {
                if (layerToEdit.layer is NormalLayer)
                    radioNormal.Checked = true;
                else
                    radioPoint.Checked = true;

                label1.Visible = false;
                panel1.Visible = false;
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
                if (selectedLayerType == LayerType.NORMAL)
                {
                    NormalLayer layer = new NormalLayer();
                    layer.name = (String.IsNullOrEmpty(textBox1.Text) ? "New Layer" : textBox1.Text);
                    if (imagePreviewPanel1.selectedItem != null)
                        _mediator.projectManager.SetLayerImage(layer, imagePreviewPanel1.selectedItem.ImageResource);
                    _mediator.layerPanel.AddLayer(_mediator.animPanel.selectedItem.animation, layer);

                }
                else if (selectedLayerType == LayerType.POINT)
                {
                    PointLayer layer = new PointLayer();
                    layer.name = (String.IsNullOrEmpty(textBox1.Text) ? "New Layer" : textBox1.Text);
                    _mediator.layerPanel.AddLayer(_mediator.animPanel.selectedItem.animation, layer);
                }
            }
            else
            {
                layerToEdit.layer.name = (String.IsNullOrEmpty(textBox1.Text) ? "Layer" : textBox1.Text);

                if (layerToEdit.layer is NormalLayer layer)
                {
                    if (imagePreviewPanel1.selectedItem != null)
                        _mediator.projectManager.SetLayerImage(layer, imagePreviewPanel1.selectedItem.ImageResource);
                    _mediator.projectManager.UpdateAllPreviews();
                }
            }

            mainForm.Enabled = true;
            this.Close();
        }


        private void radio_CheckedChanged(object sender, EventArgs e)
        {
            if (radioNormal.Checked)
            {
                selectedLayerType = LayerType.NORMAL;
                imagePreviewPanel1.Enabled = true;
            }

            if (radioPoint.Checked)
            {
                selectedLayerType = LayerType.POINT;
                imagePreviewPanel1.Enabled = false;
            }
        }

        private void LayerAdd_FormClosing(object sender, FormClosingEventArgs e)
        {
            mainForm.Enabled = true;
        }
    }
}
