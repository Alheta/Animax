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
    [XmlRoot("AnimationProject")]
    public class Project
    {
        [XmlArray("Images")]
        [XmlArrayItem("Image")]
        public List<ImageResource> images { get; set; } = new List<ImageResource>();

        [XmlArray("Events")]
        [XmlArrayItem("Event")] 
        public List<Event> events { get; set; } = new List<Event>();

        [XmlArray("Animations")]
        [XmlArrayItem("Animation")] 
        public List<Animation> animations { get; set; } = new List<Animation>();

        [XmlIgnore]
        public string filePath;
    }

    [Serializable]
    public class Animation
    {
        [XmlAttribute("name")] public string name { get; set; }
        [XmlAttribute("isDefault")] public bool isDefault { get; set; } = false;
        [XmlAttribute("isLooping")] public bool isLooping { get; set; } = false;
        [XmlAttribute("duration")] public int duration { get; set; } = 1;

        [XmlArray("Layers")]
        [XmlArrayItem(typeof(NormalLayer))]
        [XmlArrayItem(typeof(PointLayer))]
        [XmlArrayItem(typeof(EventLayer))]
        public List<Layer> layers { get; set; } = new List<Layer>();
    }

    public enum LayerType
    {
        NORMAL,
        POINT,
        EVENT
    }

    [Serializable]
    [XmlInclude(typeof(NormalLayer))]
    [XmlInclude(typeof(PointLayer))]
    [XmlInclude(typeof(EventLayer))]
    public abstract class Layer
    {
        [XmlIgnore] public abstract string name { get; set; }
        [XmlIgnore] public virtual bool visible { get; set; }
        public abstract IEnumerable<Frame> GetFrames();
        public abstract (Frame currentFrame, Frame nextFrame, float progress) GetCurrentFrameData(int markerIndex);
    }

    [Serializable]
    public class NormalLayer : Layer
    {
        [XmlAttribute("name")] public override string name { get; set; } = "NewLayer";
        [XmlAttribute("visible")] public override bool visible { get; set; } = true;

        [XmlAttribute("imageIndex")] public int imageIndex { get; set; } = -1;
        [XmlIgnore] public ImageResource assignedImage { get; set; }

        [XmlArray("Frames")]
        [XmlArrayItem(typeof(NormalFrame))]
        public List<NormalFrame> frames { get; set; } = new List<NormalFrame>();
        public override IEnumerable<Frame> GetFrames() => frames.Cast<NormalFrame>();
        public override (Frame currentFrame, Frame nextFrame, float progress) GetCurrentFrameData(int markerIndex) { return FrameUtils.GetFrameData(frames, markerIndex); }
    }

    [Serializable]
    public class PointLayer : Layer 
    {
        [XmlAttribute("name")] public override string name { get; set; } = "NewLayer";
        [XmlAttribute("visible")] public override bool visible { get; set; } = true;

        [XmlArray("Frames")]
        [XmlArrayItem(typeof(PointFrame))]
        public List<PointFrame> frames { get; set; } = new List<PointFrame>();
        public override IEnumerable<Frame> GetFrames() => frames.Cast<PointFrame>();
        public override (Frame currentFrame, Frame nextFrame, float progress) GetCurrentFrameData(int markerIndex) { return FrameUtils.GetFrameData(frames, markerIndex); }
    }

    [Serializable]
    public class EventLayer : Layer 
    {
        [XmlIgnore]
        public override string name
        {
            get => "Events";
            set { }
        }

        [XmlIgnore]
        public override bool visible
        {
            get => true;
            set { }
        }

        [XmlArray("Frames")]
        [XmlArrayItem(typeof(EventFrame))]
        public List<EventFrame> frames { get; set; } = new List<EventFrame>();
        public override IEnumerable<Frame> GetFrames() => frames.Cast<EventFrame>();
        public override (Frame currentFrame, Frame nextFrame, float progress) GetCurrentFrameData(int markerIndex)
        {
            return (null, null, 0);
        }

    }


    [Serializable]
    [XmlInclude(typeof(NormalFrame))]
    [XmlInclude(typeof(PointFrame))]
    [XmlInclude(typeof(EventFrame))]
    public abstract class Frame 
    {
        public abstract int GetDuration();
    }

    [Serializable]
    public abstract class TransformFrame : Frame
    {
        [XmlAttribute("positionX")] public int positionX { get; set; }
        [XmlAttribute("positionY")] public int positionY { get; set; }
        [XmlAttribute("scaleX")] public int scaleX { get; set; } = 100;
        [XmlAttribute("scaleY")] public int scaleY { get; set; } = 100;
        [XmlAttribute("rotation")] public int rotation { get; set; }
        [XmlAttribute("interpolated")] public bool interpolated { get; set; } = true;
        [XmlAttribute("duration")] public int duration { get; set; } = 1;
        [XmlAttribute("visible")] public bool visible { get; set; } = true;

        [XmlIgnore]
        public Point position
        {
            get => new Point(positionX, positionY);
            set { positionX = value.X; positionY = value.Y; }
        }

        [XmlIgnore]
        public Point scale
        {
            get => new Point(scaleX, scaleY);
            set { scaleX = value.X; scaleY = value.Y; }
        }

        public abstract Frame Interpolate(Frame nextFrame, float progress);
        public override int GetDuration() => duration;

    }

    [Serializable]
    public class NormalFrame : TransformFrame
    {
        [XmlAttribute("selectionX")] public int selectionX { get; set; }
        [XmlAttribute("selectionY")] public int selectionY { get; set; }
        [XmlAttribute("selectionWidth")] public int selectionWidth { get; set; }
        [XmlAttribute("selectionHeight")] public int selectionHeight { get; set; }
        [XmlAttribute("pivotX")] public int pivotPosX { get; set; }
        [XmlAttribute("pivotY")] public int pivotPosY { get; set; }

        [XmlIgnore]
        public (Bitmap savedImage, Point relativePivot) imagePreview;

        public NormalFrame() { }
        public NormalFrame(int dur) { duration = Math.Max(1, dur); }

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
                    g.DrawImage(sourceImage, new Rectangle(0, 0, selection.Width, selection.Height),
                              selection, GraphicsUnit.Pixel);
                }
            }
            catch
            {
                imagePreview.savedImage = null;
            }
        }

        public override Frame Interpolate(Frame nextFrame, float progress)
        {
            if (nextFrame is not NormalFrame nextNormalFrame || !interpolated)
                return this;

            return new NormalFrame(1)
            {
                position = new Point(
                    (int)(position.X + (nextNormalFrame.position.X - position.X) * progress),
                    (int)(position.Y + (nextNormalFrame.position.Y - position.Y) * progress)
                ),
                scale = new Point(
                    (int)(scale.X + (nextNormalFrame.scale.X - scale.X) * progress),
                    (int)(scale.Y + (nextNormalFrame.scale.Y - scale.Y) * progress)
                ),
                rotation = (int)(rotation + (nextNormalFrame.rotation - rotation) * progress),
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
        public Point pivotPos
        {
            get => new Point(pivotPosX, pivotPosY);
            set { pivotPosX = value.X; pivotPosY = value.Y; }
        }
    }

    [Serializable]
    public class PointFrame : TransformFrame
    {
        public PointFrame() { }
        public PointFrame(int dur) { duration = Math.Max(1, dur); }

        public override Frame Interpolate(Frame nextFrame, float progress)
        {
            if (nextFrame is not PointFrame nextPointFrame || !interpolated)
                return this;

            return new PointFrame(1)
            {
                position = new Point(
                      (int)(position.X + (nextPointFrame.position.X - position.X) * progress),
                      (int)(position.Y + (nextPointFrame.position.Y - position.Y) * progress)
                    ),
                scale = new Point(
                      (int)(scale.X + (nextPointFrame.scale.X - scale.X) * progress),
                      (int)(scale.Y + (nextPointFrame.scale.Y - scale.Y) * progress)
                    ),
                rotation = (int)(rotation + (nextPointFrame.rotation - rotation) * progress),
                visible = this.visible,
                interpolated = this.interpolated
            };
        }
    }

    [Serializable]
    public class EventFrame : Frame
    {
        [XmlAttribute("eventId")] public int eventId { get; set; }
        [XmlAttribute("targetFrame")] public int targetFrame { get; set; }
        
        public EventFrame() { }
        public EventFrame(int targetFrame, int eventId) 
        {
            this.targetFrame = targetFrame;
            this.eventId = eventId;
        }
        public override int GetDuration() => 1;
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

    [Serializable]
    public class Event
    {
        [XmlAttribute("name")] public string name { get; set; } = "newEvent";
        [XmlAttribute("index")] public int index { get; set; } = 0;

        public Event() { }
        public Event(string name) 
        {
            this.name = name;
        }
    }

    public static class FrameUtils
    {
        public static (Frame currentFrame, Frame nextFrame, float progress) GetFrameData<T>(
            List<T> frames, int markerIndex) where T : TransformFrame
        {
            if (frames == null || frames.Count == 0)
                return (null, null, 0);

            int accumulatedDuration = 0;
            Frame lastFrame = frames.Count > 0 ? frames[frames.Count - 1] : null;

            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                int frameDuration = Math.Max(1, frame.duration);

                if (markerIndex < accumulatedDuration + frameDuration)
                {
                    float progress = (float)(markerIndex - accumulatedDuration) / frameDuration;
                    Frame nextFrame = (i < frames.Count - 1) ? frames[i + 1] : null;
                    return (frame, nextFrame, progress);
                }

                accumulatedDuration += frameDuration;
            }

            return (lastFrame, null, 0);
        }
    }
}
