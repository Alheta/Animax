using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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

        public void AddImage(ImageResource imgRes)
        {
            _mediator.projectManager.currentProject.images.Add(imgRes);
            imgRes.Index = _mediator.projectManager.currentProject.images.IndexOf(imgRes);

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
                Controls.Add(preview);
            }
        }

        private void OnItemClicked(ImagePreview item)
        {
            if (selectedItem != null)
                selectedItem.isSelected = false;

            selectedItem = item;
            selectedItem.isSelected = true;
            foreach (var item2 in items)
            {
                item2.Invalidate();
            }
        }
    }
}
