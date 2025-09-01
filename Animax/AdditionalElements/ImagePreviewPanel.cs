using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace Animax.AdditionalElements
{
    public class ImagePreviewPanel : Panel
    {
        public Mediator _mediator;

        public List<ImagePreview> items = new();
        public ImagePreview selectedItem;

        public ImagePreviewPanel()
        {
            this.DoubleBuffered(true);
            AutoScroll = false;
            BorderStyle = BorderStyle.FixedSingle;
        }

        public ImagePreview FindPreviewByImageResource(ImageResource imageResource)
        {
            if (imageResource == null) return null;

            return items.FirstOrDefault(preview =>
                preview.ImageResource != null &&
                preview.ImageResource.Equals(imageResource));
        }

        public void AddImage(ImageResource imgRes)
        {
            _mediator.projectManager.currentProject.images.Add(imgRes);
            imgRes.Index = _mediator.projectManager.currentProject.images.IndexOf(imgRes);
            LoadImages();
        }

        public void RemoveImage(ImageResource imgRes)
        {
            _mediator.projectManager.currentProject.images.Remove(imgRes);
            FindPreviewByImageResource(imgRes).Dispose();
            LoadImages();
        }

        public void LoadImages()
        {
            Controls.Clear();
            items.Clear();
            foreach (var img in _mediator.projectManager.currentProject.images)
            {
                ImagePreview preview = new ImagePreview(img);
                preview.clicked += OnItemClicked;
                preview.Width = this.Width;
                items.Add(preview);
                Controls.Add(preview);
            }
            LayoutItems();
        }

        private void OnItemClicked(ImagePreview item)
        {
            SetSelectedItem(item);
        }

        public void SetSelectedItem(ImagePreview item)
        {
            if (item != null)
            {
                if (selectedItem != null)
                    selectedItem.isSelected = false;

                selectedItem = item;
                selectedItem.isSelected = true;
                foreach (var item2 in items)
                    item2.Invalidate();
            }
        }

        private void LayoutItems()
        {
            this.SuspendLayout();
            int y = 2;
            foreach (var item in items)
            {
                item.Location = new Point(0, y);
                y += item.Height + 2;
            }

            Invalidate();
            this.ResumeLayout(true);
        }

    }
}
