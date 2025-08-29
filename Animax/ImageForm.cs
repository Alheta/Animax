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

        }

        private void ImageForm_Load(object sender, EventArgs e)
        {
            mainForm.Enabled = false;

            imagePreviewPanel1.LoadImages();
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
                    }
                }
            }
        }

        private void buttonRemove_Click(object sender, EventArgs e)
        {
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImageForm));
            this.buttonAdd = new System.Windows.Forms.Button();
            this.buttonRemove = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonAdd
            // 
            resources.ApplyResources(this.buttonAdd, "buttonAdd");
            this.buttonAdd.Name = "buttonAdd";
            this.buttonAdd.UseVisualStyleBackColor = true;
            this.buttonAdd.Click += new System.EventHandler(this.buttonAdd_Click);
            // 
            // buttonRemove
            // 
            resources.ApplyResources(this.buttonRemove, "buttonRemove");
            this.buttonRemove.Name = "buttonRemove";
            this.buttonRemove.UseVisualStyleBackColor = true;
            this.buttonRemove.Click += new System.EventHandler(this.buttonRemove_Click);
            // 
            // button1
            // 
            resources.ApplyResources(this.button1, "button1");
            this.button1.Name = "button1";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // ImageForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.button1);
            this.Controls.Add(this.buttonRemove);
            this.Controls.Add(this.buttonAdd);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "ImageForm";
            this.ShowIcon = false;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ImageForm_FormClosing);
            this.Load += new System.EventHandler(this.ImageForm_Load);
            this.ResumeLayout(false);

        }
    }
}
