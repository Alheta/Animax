using Animax.AdditionalElements;
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
    public partial class ImageForm : Form
    {

        public Mediator _mediator;
        public Main mainForm;

        public ImageForm(Mediator mediator)
        {
            InitializeComponent();

            _mediator = mediator;
            mainForm = mediator.mainForm;

            imagePreviewPanel._mediator = mediator;
        }

        private void ImageForm_Load(object sender, EventArgs e)
        {
            mainForm.Enabled = false;

            imagePreviewPanel.LoadImages();
        }

        private void ImageForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            mainForm.Enabled = true;
        }


        private void buttonAdd_Click(object sender, EventArgs e)
        {
            using (var openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp";
                openDialog.Title = "Open image";

                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (string file in openDialog.FileNames)
                    {
                        ImageResource imgRes = new ImageResource(file);
                        imagePreviewPanel.AddImage(imgRes);
                    }
                }
            }
        }

        private void buttonRemove_Click(object sender, EventArgs e)
        {
            if (imagePreviewPanel.selectedItem != null)
            {
                imagePreviewPanel.RemoveImage(imagePreviewPanel.selectedItem.ImageResource);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
