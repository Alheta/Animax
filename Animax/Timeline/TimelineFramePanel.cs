using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Reflection.Emit;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Animax.HandyStuff;
using System.Runtime.Remoting.Messaging;

namespace Animax
{
    public class TimelineFramePanel : Panel
    {
        public List<TimelineFrameItem> items = new();

        public Mediator _mediator;

        //Timeline Stuff
        public TimelineFrameItem selectedItem;
        public List<TimelineFrameItem> selectedItems = new();

        public Panel fixedHeader;
        public Panel layerContent;
        public HScrollBar scroller;

        public event Action<TimelineFramePanel> FrameUpdated;

        public TimelineMarker marker;

        private Rectangle durationHandle;

        private Stopwatch animationStopwatch;
        private long lastFrameTime;
        private bool isPlaying = false;
        private bool isPaused = false;
        private int _targetFPS = 30;
        private System.Windows.Forms.Timer animationTimer;


        private List<TimelineFrameItem> draggingFrames = new();

        public int TargetFPS
        {
            get => _targetFPS;
            set
            {
                _targetFPS = Math.Max(1, Math.Min(120, value));
                frameInterval = 1000.0 / _targetFPS;
            }
        }

        private double frameInterval;


        public TimelineFramePanel()
        {
            this.DoubleBuffered(true);
            AutoScroll = false;
            BorderStyle = BorderStyle.FixedSingle;

            fixedHeader = new Panel();
            fixedHeader.Height = 35;
            fixedHeader.Dock = DockStyle.Top;
            fixedHeader.AutoScroll = false;
            fixedHeader.BackColor = Color.LightGray;
            fixedHeader.Paint += OnHeaderPaint;
            fixedHeader.DoubleBuffered(true);

            fixedHeader.MouseDown += OnRulerMouseDown;
            fixedHeader.MouseMove += OnRulerMouseMove;
            fixedHeader.MouseUp += OnRulerMouseUp;

            layerContent = new Panel();
            layerContent.Dock = DockStyle.Fill;
            layerContent.AutoScroll = false;
            layerContent.Paint += OnContentPaint;
            layerContent.DoubleBuffered(true);

            layerContent.Leave += LayerMouseLeave;
            layerContent.MouseDown += LayerMouseDown;
            layerContent.MouseMove += LayerMouseMove;
            layerContent.MouseUp += LayerMouseUp;

            animationStopwatch = new Stopwatch();
            animationTimer = new Timer { Interval = 16 };
            animationTimer.Tick += AnimationUpdate;
            TargetFPS = 30;

            scroller = new HScrollBar();
            scroller.Dock = DockStyle.Bottom;
            scroller.BringToFront();

            layerContent.Controls.Add(scroller);

            Controls.Add(layerContent);
            Controls.Add(fixedHeader);

        }

        protected override void OnInvalidated(InvalidateEventArgs e)
        {
            base.OnInvalidated(e);
        }

        public void AddSelectedFrameDuration(int frameDuration)
        {
            if (selectedItems.Count > 0)
            {
                foreach (var item in selectedItems)
                {
                    if (item.frame is TransformFrame frame)
                    {
                        frame.duration = Math.Max(1, frame.duration + frameDuration);
                        item.Invalidate();
                        LayoutItems();
                    }
                }
            }
        }

        public TimelineLayerItem GetLayerOfTheFrame(TimelineFrameItem frame)
        {
            foreach (var layerItem in _mediator.layerPanel.items)
            {
                if ((layerItem.layer).GetFrames().Contains(frame.frame))
                    return layerItem;
            }
            return null;
        }

        private TimelineFrameItem CreateFrameItem(Frame frame, TimelineLayerItem layer)
        {
            TimelineFrameItem item = new TimelineFrameItem(frame, layer, _mediator);

            item.clicked += OnItemClicked;
            item.deleted += OnItemDeleted;
            item.deletedAll += DeleteAllSelection;
            layerContent.Controls.Add(item);
            items.Add(item);

            return item;
        }

        public void UpdateFrames(List<TimelineLayerItem> layers = null)
        {
            selectedItems = new();

            foreach (var frm in items)
            {
                if (layerContent.Controls.Contains(frm))
                    layerContent.Controls.Remove(frm);
            }
            items.Clear();

            if (layers == null || _mediator.layerPanel == null)
            {
                Invalidate();
                layerContent.Invalidate();
                fixedHeader.Invalidate();
                return;
            }


            foreach (TimelineLayerItem lyr in layers)
            {
                foreach (Frame frm in lyr.layer.GetFrames().ToList())
                {
                    CreateFrameItem(frm, lyr);
                }
            }

            LayoutItems();

            _mediator.marker.animationDuration = _mediator.layerPanel.selectedAnimation == null ? 1 : _mediator.layerPanel.selectedAnimation.duration;

            Invalidate();
            layerContent.Invalidate();

        }

        public NormalFrame GetSelectedNormalFrame()
        {
            if (selectedItem != null && selectedItem.frame is NormalFrame frame)
                return frame;

            return null;
        }

        public void AddFrame(TimelineLayerItem layer, bool copyCat)
        {
            if (layer == null)
                return;

            Frame frame = null;

            switch (layer.layer)
            {
                case NormalLayer normalLayer:
                    frame = new NormalFrame(1)
                    {
                        selection = new Rectangle(0, 0, 32, 32),
                        pivotPos = new Point(0, 0),
                        scale = new Point(100, 100),
                        rotation = 0
                    };

                    _mediator.spritePanel.SetSpriteSheet(normalLayer.assignedImage?.Image, (NormalFrame)frame);
                    ((NormalFrame)frame).UpdatePreview(normalLayer.assignedImage?.Image);
                    normalLayer.frames.Add((NormalFrame)frame);
                    break;

                case PointLayer pointLayer:
                    frame = new PointFrame(1)
                    {
                        position = new Point(0, 0),
                        scale = new Point(100, 100),
                        rotation = 0
                    };
                    pointLayer.frames.Add((PointFrame)frame);
                    _mediator.spritePanel.ClearSpriteSheet();
                    break;

                case EventLayer eventLayer:
                    foreach (EventFrame ef in eventLayer.frames)
                        if (ef.targetFrame == _mediator.marker.frameIndex) return;

                    frame = new EventFrame(_mediator.marker.frameIndex, 0);
                    eventLayer.frames.Add((EventFrame)frame);
                    break;

                default:
                    throw new ArgumentException("Unknown layer type");
            }

            TimelineFrameItem item = CreateFrameItem(frame, layer);


            item.Invalidate();
            _mediator.marker.Invalidate();
            LayoutItems();

            Invalidate();
            layerContent.Invalidate();

            _mediator.animPreview.Invalidate();

            SetFramesOnIndex(currentFrameIndex, true);

            _mediator.MarkProjectModified();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
        }

        private void OnHeaderPaint(object sender, PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(220, 220, 220)), e.ClipRectangle);

            DrawFrameRuler(e);

            if (_mediator?.animPanel.selectedItem == null) return;

            Rectangle durationRect = new Rectangle(0, 0, _mediator.layerPanel.frameWidth * _mediator.animPanel.selectedItem.animation.duration, fixedHeader.Height);
            durationHandle = new Rectangle(_mediator.layerPanel.frameWidth * _mediator.animPanel.selectedItem.animation.duration - 3, 0, 6, fixedHeader.Height);

            e.Graphics.FillRectangle(ColorManager.ApplyGradient(ColorManager.AnimaxGradients.TIMELINE_DURATION, durationRect), durationRect);
            e.Graphics.FillRectangle(Brushes.Black, durationHandle);
        }

        private void OnContentPaint(object sender, PaintEventArgs e)
        {
            base.OnPaint(e);

            DrawEmptyTimeline(e); 
        }

        private void DrawFrameRuler(PaintEventArgs e)
        {
            int startX = 0;
            int y = 0;
            int height = fixedHeader.Height;

            int frameWidth = _mediator != null ? _mediator.layerPanel.frameWidth : 20;

            int visibleWidth = this.DisplayRectangle.Width;
            int maxVisibleFrames = Math.Max(30, (visibleWidth - 20) / frameWidth + 5);
            Font font = new Font("Segoe UI", 8);
            Brush brush = Brushes.Black;
            Pen pen = new Pen(Color.Black);

            for (int i = 0; i < maxVisibleFrames; i++)
            {
                int x = startX + i * frameWidth;


                Rectangle r = new Rectangle(x, y, frameWidth, height);
                e.Graphics.FillRectangle(ColorManager.ApplyGradient(ColorManager.AnimaxGradients.TIMELINE_RULER, r), r);

                if (i % 5 == 0)
                {
                    e.Graphics.FillRectangle(ColorManager.ApplyGradient(ColorManager.AnimaxGradients.TIMELINE_RULER_DARK, r), r);

                    string label = i.ToString();
                    SizeF labelSize = e.Graphics.MeasureString(label, font);
                    float labelX = x + frameWidth / 2 - labelSize.Width / 2;
                    float labelY = height / 2 - labelSize.Height / 2;
                    e.Graphics.DrawString(label, font, brush, labelX, labelY);
                }

                int lineLength = height / 4;

                e.Graphics.DrawLine(pen, x, y, x, y+lineLength);
                e.Graphics.DrawLine(pen, x, y + height - lineLength, x, height);

            }

            e.Graphics.DrawLine(Pens.Black, new Point(fixedHeader.Left, fixedHeader.Bottom - 1), new Point(fixedHeader.Right, fixedHeader.Bottom - 1));

        }

        private void DrawEmptyTimeline(PaintEventArgs e)
        {
            if (_mediator == null || _mediator.layerPanel == null)
                return;

            int frameWidth = _mediator.layerPanel.frameWidth;
            int visibleWidth = this.DisplayRectangle.Width;
            int maxVisibleFrames = Math.Max(30, (visibleWidth - 20) / frameWidth + 5);


            foreach (var layerView in _mediator.layerPanel.items)
            {
                var layer = layerView.layer;

                int y = layerView.Location.Y;
                int currentX = 0;

                for (int i = 0; i < maxVisibleFrames; i++)
                {

                    if (i % 5 == 0)
                        e.Graphics.FillRectangle(Brushes.LightGray, new Rectangle(currentX, y, frameWidth, layerView.Height));
                    else
                        e.Graphics.FillRectangle(Brushes.White, new Rectangle(currentX, y, frameWidth, layerView.Height));

                    e.Graphics.DrawRectangle(Pens.LightGray, new Rectangle(currentX, y, frameWidth, layerView.Height));
                    currentX += frameWidth;
                }
            }
        }

        private int originalStartIndex;
        private void LayerMouseDown(object sender, MouseEventArgs e)
        {
            if (_mediator == null || _mediator.layerPanel == null)
                return;

        }

        private void LayerMouseMove(object sender, MouseEventArgs e)
        {

        }

        private void LayerMouseUp(object sender, MouseEventArgs e)
        {

        }


        private void LayerMouseLeave(object sender, EventArgs e)
        {

        }

        public void OnItemClicked(TimelineFrameItem item)
        {
            bool isShiftHeld = Control.ModifierKeys.HasFlag(Keys.Shift);
            ToggleSelection(item, isShiftHeld);

            FrameUpdated?.Invoke(this);

            foreach (var item2 in items)
                item2.Invalidate();


            if (selectedItems.Count > 0)
            {
                originalStartIndex = GetFrameIndex(item);
            }
        }

        public void OnItemDeleted(TimelineFrameItem item)
        {
            TimelineLayerItem layer = GetLayerOfTheFrame(item);

            item.isSelected = false;
            item.isLastSelected = false;

            layerContent.Controls.Remove(item);
            items.Remove(item);

            switch (layer?.layer)
            {
                case NormalLayer nl:
                    nl.frames.Remove((NormalFrame)item.frame);
                    break;
                case PointLayer pl:
                    pl.frames.Remove((PointFrame)item.frame);
                    break;
                case EventLayer el:
                    el.frames.Remove((EventFrame)item.frame);
                    break;
            }


            if (selectedItem == item) selectedItem = null;
            FrameUpdated?.Invoke(this);

            LayoutItems();
        }

        public void DeleteAllSelection()
        {
            foreach(var item in selectedItems)
            {
                OnItemDeleted(item);
            }
        }


        private void ToggleSelection(TimelineFrameItem item, bool multiMode)
        {
            selectedItem = null;

            if (multiMode)
            {
                if (selectedItems.Contains(item))
                {

                    if (item.isSelected == true && item.isLastSelected == false)
                    {
                        foreach (var item2 in items)
                            item2.isLastSelected = false;

                        item.isLastSelected = true;
                    }
                }
                else
                {
                    foreach (var item2 in items)
                        item2.isLastSelected = false;

                    item.isSelected = true;
                    item.isLastSelected = true;
                    selectedItems.Add(item);
                }
            }
            else
            {
                foreach (var item2 in items)
                {
                    item2.isSelected = false;
                    item2.isLastSelected = false;
                    selectedItems.Remove(item2);
                    if (item == item2)
                    {
                        item2.isSelected = true;
                        item2.isLastSelected = true;
                        selectedItems.Add(item2);
                    }
                }
            }

            foreach (var item2 in items)
                if (item2.isLastSelected) selectedItem = item2;

        }

        private List<TimelineFrameItem> GetFramesForLayer(TimelineLayerItem layer)
        {
            return items.Where(item => layer.layer.GetFrames().Contains(item.frame)).ToList();
        }
        public void LayoutItems()
        {
            if (_mediator.layerPanel == null || _mediator.layerPanel.items == null)
                return;

            layerContent.SuspendLayout();

            foreach (var layerView in _mediator.layerPanel.items)
            {
                var layer = layerView.layer;

                var framesForLayer = items
                    .Where(item => layer.GetFrames().Contains(item.frame))
                    .ToList();

                int y = layerView.Location.Y;
                int currentX = 0;

                foreach (var frameView in framesForLayer)
                {
                    int duration = Math.Max(1, frameView.frame.GetDuration());
                    int newWidth = _mediator.layerPanel.frameWidth * duration;

                    if (layer is EventLayer && frameView.frame is EventFrame eFrame)
                    {
                        frameView.SuspendLayout();
                        frameView.Width = newWidth;
                        frameView.Location = new Point(frameView.Width * eFrame.targetFrame, y);
                        frameView.ResumeLayout();
                    }
                    else
                    {
                        if (frameView.Width != newWidth || frameView.Location.X != currentX || frameView.Location.Y != y)
                        {
                            frameView.SuspendLayout();
                            frameView.Width = newWidth;
                            frameView.Location = new Point(currentX, y);
                            frameView.ResumeLayout();
                        }
                    }

                    currentX += frameView.Width;
                }
            }
            layerContent.Invalidate();
            layerContent.ResumeLayout();
        }

        //Playback Stuff

        public System.Windows.Forms.Label lbl;

        public int currentFrameIndex
        {
            get => _currentFrameIndex;
            set
            {
                if (_mediator != null && _mediator.layerPanel != null)
                {
                    int clamped = Math.Max(0, value);

                    if (clamped > _mediator.animPanel.selectedItem?.animation.duration - 1)
                    {
                        if (_mediator.animPanel.selectedItem.animation.isLooping == true)
                            _currentFrameIndex = 0;
                    }
                    else
                        _currentFrameIndex = clamped;
                }
                fixedHeader.Invalidate();
                layerContent.Invalidate();
                _mediator?.animPreview.Invalidate();

            }
        }
        private int _currentFrameIndex = 0;

        public Dictionary<Layer, Frame> currentFrames = new Dictionary<Layer, Frame>();

        public void StartAnimation()
        {
            if (isPlaying && !isPaused) return;

            currentFrameIndex = 0;

            animationStopwatch.Restart();
            lastFrameTime = 0;
            isPlaying = true;
            isPaused = false;
            animationTimer.Start();
        }

        public void PauseAnimation()
        {
            if (!isPlaying) return;

            isPaused = true;
            animationStopwatch.Stop();
            animationTimer.Stop();
        }

        public void ResumeAnimation()
        {
            if (!isPlaying || !isPaused) return;

            isPaused = false;
            animationStopwatch.Start();
            animationTimer.Start();
        }

        public void StopAnimation()
        {
            isPlaying = false;
            isPaused = false;
            animationStopwatch.Reset();
            animationTimer.Stop();
        }

        private void AnimationUpdate(object sender, EventArgs e)
        {
            if (!isPlaying || isPaused) return;

            double elapsed = animationStopwatch.Elapsed.TotalMilliseconds;
            double delta = elapsed - lastFrameTime;

            if (delta >= frameInterval)
            {
                int framesToUpdate = (int)(delta / frameInterval);
                lastFrameTime += framesToUpdate * (long)frameInterval;

                if (InvokeRequired)
                {
                    Invoke(new Action(() => UpdateAnimationFrame(framesToUpdate)));
                }
                else
                {
                    UpdateAnimationFrame(framesToUpdate);
                }
            }
        }

        private void UpdateAnimationFrame(int framesToUpdate)
        {
            for (int i = 0; i < framesToUpdate; i++)
            {
                currentFrameIndex++;

                if (currentFrameIndex >= _mediator?.animPanel.selectedItem?.animation.duration)
                {
                    if (_mediator.animPanel.selectedItem.animation.isLooping)
                    {
                        currentFrameIndex = 0;
                    }
                    else
                    {
                        StopAnimation();
                        return;
                    }
                }

                _mediator.marker.frameIndex = currentFrameIndex;
                SetFramesOnIndex(currentFrameIndex, false);
            }
        }

        public int GetFrameIndex(TimelineFrameItem frame)
        {
            TimelineLayerItem layer = GetLayerOfTheFrame(frame);
            if (layer?.layer == null) return 0;

            int index = 0;
            if (layer.layer is EventLayer)
            {
                return index;
            }
            else
            {
                foreach (TransformFrame frm in layer.layer.GetFrames())
                {
                    if (frm != frame.frame)
                        index += frm.duration;
                    else
                        return index;
                }
            }
            return index;
        }

        public (Frame currentFrame, Frame nextFrame, float progress) GetCurrentFrameData(Layer layer, int markerIndex)
        {
            if (layer == null)
                return (null, null, 0);

            return layer.GetCurrentFrameData(markerIndex);
        }

        public void SetFramesOnIndex(int index, bool forced)
        {
            if (_mediator.layerPanel == null)
                return;

            if (forced) { PauseAnimation(); }

            currentFrames.Clear();

            foreach (TimelineLayerItem item in _mediator.layerPanel.items.AsEnumerable().Reverse())
            {
                var (currentFrame, nextFrame, progress) = GetCurrentFrameData(item.layer, index);

                if (currentFrame != null)
                {
                    Frame frameToAdd = currentFrame;

                    if (currentFrame is NormalFrame normal &&
                        normal.interpolated && nextFrame != null && normal.duration > 1)
                        frameToAdd = normal.Interpolate(nextFrame, progress);
                    else if (currentFrame is PointFrame point &&
                             point.interpolated && nextFrame != null && point.duration > 1)
                        frameToAdd = point.Interpolate(nextFrame, progress);

                    currentFrames.Add(item.layer, frameToAdd);
                }
                else
                {
                    currentFrames.Add(item.layer, null);
                }
            }

            Invalidate();
            _mediator?.animPreview.Invalidate();
        }

        private bool _isDragging = false;
        private bool _isDraggingDuration = false;
        private int _lastFrameIndex = -1;

        private void OnRulerMouseDown(object sender, MouseEventArgs e)
        {
            if (_mediator.animPanel.selectedItem == null) return;
            if (e.Button != MouseButtons.Left) return;

            if (durationHandle.Contains(e.Location) || ModifierKeys.HasFlag(Keys.Control))
            {
                _isDraggingDuration = true;
            }
            else
            {
                int currentFrame = e.X / _mediator.layerPanel.frameWidth;
                _mediator.marker.frameIndex = currentFrame;
                _lastFrameIndex = currentFrame;
                _isDragging = true;
            }
            PauseAnimation();
        }

        private void OnRulerMouseMove(object sender, MouseEventArgs e)
        {
            if (_mediator.animPanel.selectedItem == null) return;

            if (_isDragging)
            {
                int currentFrame = e.X / _mediator.layerPanel.frameWidth;
                if (currentFrame != _lastFrameIndex)
                {
                    _mediator.marker.frameIndex = currentFrame;
                    _lastFrameIndex = currentFrame;
                }
            }
            else if (_isDraggingDuration)
            {
                int duration = Math.Max(1, e.X / _mediator.layerPanel.frameWidth);
                if (duration != _mediator.animPanel.selectedItem.animation.duration)
                {
                    _mediator.animPanel.selectedItem.animation.duration = duration;
                    fixedHeader.Invalidate();
                    _mediator.marker.Invalidate();
                }
            }
        }

        private void OnRulerMouseUp(object sender, MouseEventArgs e)
        {
            if (_mediator.animPanel.selectedItem == null) return;

            if (_isDragging)
                _isDragging = false;

            if (_isDraggingDuration)
                _isDraggingDuration = false;
        }
    }
}
