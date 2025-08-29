using Animax.HandyStuff;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;


namespace Animax
{
    public enum LayerType { NORMAL, POINT, EVENT };
 
    [XmlRoot("AnimationProject")]
    public class Project
    {
        public string name { get; set; } = "New Project";
        public List<Animation> animations { get; set; } = new List<Animation>();
        public List<ImageResource> images { get; set; } = new List<ImageResource>();

        [XmlIgnore]
        public string filePath;
    }

    [Serializable]
    public class Animation
    {
        [XmlAttribute("name")] public string name { get; set; } = "New Animation";
        [XmlAttribute("isDefault")] public bool isDefault { get; set; } = false;
        [XmlAttribute("isLooping")] public bool isLooping { get; set; } = false;
        [XmlAttribute("duration")] public int duration { get; set; } = 1;
        public List<Layer> layers { get; set; } = new List<Layer>();
    }

    [Serializable]
    public class Layer
    {
        [XmlAttribute("name")] public string name { get; set; } = "NewLayer";
        [XmlAttribute("visible")] public bool isVisible { get; set; } = true;
        [XmlAttribute("imageIndex")] public int imageIndex { get; set; } = -1;
        [XmlAttribute("type")] public LayerType type { get; set; }

        [XmlArray("Frames")]
        [XmlArrayItem(typeof(NormalFrame))]
        [XmlArrayItem(typeof(PointFrame))]
        [XmlArrayItem(typeof(EventFrame))]
        public List<Frame> frames { get; set; } = new List<Frame>();

        public Layer(LayerType layerType)
        {
            type = layerType;
        }

        public Layer()
        {
            type = LayerType.NORMAL;
        }

        [XmlIgnore]
        public ImageResource assignedImage{ get; set; }
    }

    [Serializable]
    [XmlInclude(typeof(NormalFrame))]
    [XmlInclude(typeof(PointFrame))]
    [XmlInclude(typeof(EventFrame))]
    public abstract class Frame
    {
        [XmlAttribute("type")]
        public abstract FrameType type { get; }

        [XmlAttribute("duration")]
        public int duration { get; set; } = 1;

        [XmlIgnore]
        public abstract bool hasVisualContent { get; }
        public abstract Frame Interpolate(Frame nextFrame, float progress);
    }

    public enum FrameType
    {
        NORMAL,
        POINT,
        EVENT
    }

    [Serializable]
    public class NormalFrame : Frame
    {
        [XmlAttribute("selectionX")] public int selectionX { get; set; }
        [XmlAttribute("selectionY")] public int selectionY { get; set; }
        [XmlAttribute("selectionWidth")] public int selectionWidth { get; set; }
        [XmlAttribute("selectionHeight")] public int selectionHeight { get; set; }
        [XmlAttribute("pivotX")] public float pivotPosX { get; set; }
        [XmlAttribute("pivotY")] public float pivotPosY { get; set; }
        [XmlAttribute("interpolated")] public bool interpolated { get; set; } = true;
        [XmlAttribute("visible")] public bool visible { get; set; } = true;
        [XmlAttribute("positionX")] public float positionX { get; set; }
        [XmlAttribute("positionY")] public float positionY { get; set; }
        [XmlAttribute("scaleX")] public float scaleX { get; set; } = 100;
        [XmlAttribute("scaleY")] public float scaleY { get; set; } = 100;
        [XmlAttribute("rotation")] public float rotation { get; set; } = 0;

        public override FrameType type => FrameType.NORMAL;
        public override bool hasVisualContent => true;

        [XmlIgnore]
        public (Bitmap savedImage, PointF relativePivot) imagePreview;

        public NormalFrame() { }

        public NormalFrame(int frameDuration)
        {
            duration = Math.Max(1, frameDuration);
        }

        public void UpdatePreview(Image sourceImage)
        {
            imagePreview.savedImage?.Dispose();

            if (sourceImage == null || selection.IsEmpty)
            {
                imagePreview.savedImage = null;
                return;
            }

            try
            {
                imagePreview.savedImage = new Bitmap(selection.Width, selection.Height);
                using (var g = Graphics.FromImage(imagePreview.savedImage))
                {
                    g.DrawImage(sourceImage,
                        new Rectangle(0, 0, selection.Width, selection.Height),
                        selection,
                        GraphicsUnit.Pixel);
                }
            }
            catch
            {
                imagePreview.savedImage = null;
            }
        }

        public override Frame Interpolate(Frame nextFrame, float progress)
        {
            if (nextFrame is not NormalFrame nextNormalFrame)
                return this;

            return new NormalFrame(1)
            {
                position = new PointF(
                    position.X + (nextNormalFrame.position.X - position.X) * progress,
                    position.Y + (nextNormalFrame.position.Y - position.Y) * progress
                ),
                scale  = new PointF(
                    scale.X + (nextNormalFrame.scale.X - scale.X) * progress,
                    scale.Y + (nextNormalFrame.scale.Y - scale.Y) * progress
                ),
                rotation = rotation + (nextNormalFrame.rotation - rotation) * progress,
                selection = this.selection,
                pivotPos = this.pivotPos,
                visible = this.visible,
                interpolated = this.interpolated,
                imagePreview = this.imagePreview
            };
        }

        [XmlIgnore]
        public Rectangle selection
        {
            get => new Rectangle(selectionX, selectionY, selectionWidth, selectionHeight);
            set
            {
                selectionX = value.X;
                selectionY = value.Y;
                selectionWidth = value.Width;
                selectionHeight = value.Height;
            }
        }

        [XmlIgnore]
        public PointF pivotPos
        {
            get => new PointF(pivotPosX, pivotPosY);
            set
            {
                pivotPosX = value.X;
                pivotPosY = value.Y;
            }
        }

        [XmlIgnore]
        public PointF position
        {
            get => new PointF(positionX, positionY);
            set
            {
                positionX = value.X;
                positionY = value.Y;
            }
        }

        [XmlIgnore]
        public PointF scale
        {
            get => new PointF(scaleX, scaleY);
            set
            {
                scaleX = value.X;
                scaleY = value.Y;
            }
        }
    }

    [Serializable]
    public class PointFrame : Frame
    {
        [XmlAttribute("interpolated")] public bool interpolated { get; set; } = true;
        [XmlAttribute("visible")] public bool visible { get; set; } = true;
        [XmlAttribute("positionX")] public float positionX { get; set; }
        [XmlAttribute("positionY")] public float positionY { get; set; }
        [XmlAttribute("scaleX")] public float scaleX { get; set; } = 100;
        [XmlAttribute("scaleY")] public float scaleY { get; set; } = 100;
        [XmlAttribute("rotation")] public float rotation { get; set; } = 0;
        public override FrameType type => FrameType.POINT;
        public override bool hasVisualContent => true;
        public PointFrame() { }

        public PointFrame(int frameDuration)
        {
            duration = Math.Max(1, frameDuration);
        }

        public override Frame Interpolate(Frame nextFrame, float progress)
        {
            if (nextFrame is not PointFrame nextPointFrame)
                return this;

            return new PointFrame(1)
            {
                position = new PointF(
                    position.X + (nextPointFrame.position.X - position.X) * progress,
                    position.Y + (nextPointFrame.position.Y - position.Y) * progress
                ),
                scale = new PointF(
                    scale.X + (nextPointFrame.scale.X - scale.X) * progress,
                    scale.Y + (nextPointFrame.scale.Y - scale.Y) * progress
                ),
                rotation = rotation + (nextPointFrame.rotation - rotation) * progress,
                visible = this.visible,
                interpolated = this.interpolated,
            };
        }

        [XmlIgnore]
        public bool isSelected = false;

        [XmlIgnore]
        public PointF position
        {
            get => new PointF(positionX, positionY);
            set
            {
                positionX = value.X;
                positionY = value.Y;
            }
        }

        [XmlIgnore]
        public PointF scale
        {
            get => new PointF(scaleX, scaleY);
            set
            {
                scaleX = value.X;
                scaleY = value.Y;
            }
        }
    }

    [Serializable]
    public class EventFrame : Frame
    {
        [XmlElement("Event")] public FrameEvent frameEvent { get; set; }

        public override FrameType type => FrameType.EVENT;
        public override bool hasVisualContent => false;

        public EventFrame() { }

        public EventFrame(int frameDuration, FrameEvent fEvent)
        {
            duration = Math.Max(1, frameDuration);
            frameEvent = fEvent;
        }
        public override Frame Interpolate(Frame nextFrame, float progress)
        {
            return this;
        }
    }

    [Serializable]
    public class FrameEvent
    {
        public string eventName;
        public FrameEvent(string name)
        {
            eventName = name;
        }

        public FrameEvent()
        {
            eventName = "NewEvent";
        }
    }

    [Serializable]
    public class ImageResource
    {
        [XmlAttribute("path")] public string FilePath { get; set; }
        [XmlAttribute("index")] public int Index { get; set; }

        [XmlIgnore]
        public string Name { get; set; }
        [XmlIgnore]
        public Image Image
        {
            get
            {
                try
                {
                    return Image.FromFile(FilePath);
                }
                catch
                {
                    return new Bitmap(1, 1);
                }
            }
        }

        public ImageResource() { }

        public ImageResource(string filePath)
        {
            FilePath = filePath;
            Name = Path.GetFileName(filePath);
        }
    }
}
