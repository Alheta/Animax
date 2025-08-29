using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace Animax
{
    public class ProjectManager
    {

        public Mediator _mediator;

        public Project currentProject;
        public bool isModified = false;
        public bool hasFilePath => !string.IsNullOrEmpty(currentProject?.filePath);

        public ProjectManager()
        {
            CreateNewProject();
        }

        public void CreateNewProject()
        {
            currentProject = new Project();
            isModified = false;
        }

        public bool SaveProject(string filePath = null)
        {
            try
            {
                foreach (var animation in currentProject.animations)
                {
                    foreach (var layer in animation.layers)
                    {
                        if (layer.assignedImage != null)
                        {
                            layer.imageIndex = currentProject.images.IndexOf(layer.assignedImage);
                        }
                        else
                        {
                            layer.imageIndex = -1;
                        }
                    }
                }

                if (string.IsNullOrEmpty(filePath) && string.IsNullOrEmpty(currentProject.filePath))
                {
                    return SaveProjectAs();
                }

                string savePath = filePath ?? currentProject.filePath;

                var serializer = new XmlSerializer(typeof(Project));
                using (var writer = XmlWriter.Create(savePath, new XmlWriterSettings { Indent = true }))
                {
                    serializer.Serialize(writer, currentProject);
                }

                currentProject.filePath = savePath;
                isModified = false;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occured while saving file: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public bool SaveProjectAs()
        {
            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Animation Project Files (*.anmx)|*.anmx|All files (*.*)|*.*";
                saveDialog.Title = "Save ANIMAX file";
                saveDialog.DefaultExt = "anmx";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    return SaveProject(saveDialog.FileName);
                }
            }
            return false;
        }

        public bool LoadProject(string filePath = null)
        {
                if (string.IsNullOrEmpty(filePath))
                {
                    using (var openDialog = new OpenFileDialog())
                    {
                        openDialog.Filter = "Animation Project Files (*.anmx)|*.anmx|All files (*.*)|*.*";
                        openDialog.Title = "Open ANIMAX file";

                        if (openDialog.ShowDialog() == DialogResult.OK)
                        {
                            filePath = openDialog.FileName;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                var serializer = new XmlSerializer(typeof(Project));
                using (var reader = XmlReader.Create(filePath))
                {
                    var loadedProject = (Project)serializer.Deserialize(reader);
                    loadedProject.filePath = filePath;
                    currentProject = loadedProject;
                    isModified = false;

                }
                RestoreImageReferences();

                return true;
        }

        public bool CheckSaveBeforeExit()
        {
            if (isModified)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Save?",
                    "Warning",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    return SaveProject();
                }
                else if (result == DialogResult.Cancel)
                {
                    return false;
                }
            }
            return true;
        }

        public void SetLayerImage(Layer layer, ImageResource imageResource)
        {
            if (imageResource == null)
            {
                layer.imageIndex = -1;
                layer.assignedImage = null;
                return;
            }

            int index = currentProject.images.IndexOf(imageResource);
            if (index == -1)
            {
                currentProject.images.Add(imageResource);
                index = currentProject.images.Count - 1;
            }

            layer.imageIndex = index;
            layer.assignedImage = imageResource;

            foreach (NormalFrame frame in layer.frames)
            {
                frame.imagePreview.savedImage = _mediator.spritePanel.GetSelectedImage();
            }
        }
        public Image GetLayerImage(Layer layer)
        {
            if (layer.assignedImage != null)
            {
                return layer.assignedImage.Image;
            }

            Bitmap transparentImage = new Bitmap(1, 1);
            using (Graphics g = Graphics.FromImage(transparentImage))
            {
                g.Clear(Color.Transparent);
            }
            return transparentImage;
        }

        public void RestoreImageReferences()
        {
            foreach (var animation in currentProject.animations)
            {
                foreach (var layer in animation.layers)
                {
                    if (layer.imageIndex >= 0 && layer.imageIndex < currentProject.images.Count)
                    {
                        layer.assignedImage = currentProject.images[layer.imageIndex];
                    }
                    else
                    {
                        layer.assignedImage = null;
                        layer.imageIndex = -1;
                    }
                }
            }

            for (int i = 0; i < currentProject.images.Count; i++)
            {
                currentProject.images[i].Index = i;
            }
        }

        public void UpdateAllPreviews()
        {
            foreach (var anim in currentProject.animations)
            {
                foreach (var layer in anim.layers)
                {
                    var sourceImage = layer.assignedImage?.Image;
                    foreach (Frame frame in layer.frames)
                    {
                        if (frame is NormalFrame frm)
                            frm.UpdatePreview(sourceImage);
                    }
                }
            }
            _mediator.animPanel.Invalidate();
        }
    }
}
