using AudioVisualizer.Services;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AudioVisualizer
{
    /// <summary>
    /// A WinUI 3 audio visualizer control that displays real-time audio visualization using FFT analysis.
    /// </summary>
    public sealed partial class AudioVisualizer : Control
    {
        private float[] _smoothBands = new float[16];
        private float[] _latestBands = new float[16];

        /// <summary>
        /// Occurs when audio capture fails in a non-recoverable way (for example, start failure or exhausted device rebind retries).
        /// </summary>
        public event EventHandler<AudioVisualizerErrorEventArgs>? Error;

        /// <summary>
        /// Initializes a new instance of the AudioVisualizer control.
        /// </summary>
        public AudioVisualizer()
        {
            DefaultStyleKey = typeof(AudioVisualizer);
            Unloaded += OnUnloaded;
        }

        /// <summary>
        /// Called when the control template is applied to initialize visualization resources.
        /// </summary>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _canvas = GetTemplateChild("AudioVisualizerCanvas") as CanvasAnimatedControl;
            if (_canvas is not null)
            {
                _canvas.CreateResources += OnCreateResources;
                _canvas.Update += OnUpdate;
                _canvas.Draw += OnDraw;
                _canvas.ActualThemeChanged += OnActualThemeChanged;

                _naudioService.BandsAvailable -= OnBandsAvailable;
                _naudioService.BandsAvailable += OnBandsAvailable;
                _naudioService.Error -= OnCaptureError;
                _naudioService.Error += OnCaptureError;
                _naudioService.SetBandCount(BandCount);
                _naudioService.SetSourceMode(AudioSourceMode);
                _canvas.Paused = Paused;
                if (!Paused)
                    _naudioService.StartCapture();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _naudioService.BandsAvailable -= OnBandsAvailable;
            _naudioService.Error -= OnCaptureError;
            _naudioService.StopCapture();
        }

        private void OnCaptureError(object? sender, AudioVisualizerErrorEventArgs e)
        {
            DispatcherQueue dispatcher = DispatcherQueue;
            if (dispatcher is null || dispatcher.HasThreadAccess)
            {
                Error?.Invoke(this, e);
                return;
            }

            dispatcher.TryEnqueue(() => Error?.Invoke(this, e));
        }

        private void OnActualThemeChanged(FrameworkElement sender, object args)
        {
            CanvasAnimatedControl? canvas = sender as CanvasAnimatedControl;
            if (canvas is not null)
            {
                _visualizerBarsBrush = new CanvasSolidColorBrush(canvas, (VisualizerBarsBrush as SolidColorBrush)?.Color ?? Colors.DeepSkyBlue);
                _visualizerBackgroundBrush = new CanvasSolidColorBrush(canvas, (VisualizerBackgroundBrush as SolidColorBrush)?.Color ?? Colors.Transparent);
            }
        }

        /// <summary>
        /// Called when the VisualizerBarsBrush property changes to update the visualizer bars color.
        /// </summary>
        /// <param name="sender">The dependency object that triggered the change.</param>
        /// <param name="dp">The dependency property that changed.</param>
        private void OnBarsBrushColorChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (_visualizerBarsBrush != null && sender is SolidColorBrush brush)
                _visualizerBarsBrush.Color = brush.Color;
        }

        /// <summary>
        /// Called when the VisualizerBackgroundBrush color changes to update the canvas clear color.
        /// </summary>
        /// <param name="sender">The dependency object that triggered the change.</param>
        /// <param name="dp">The dependency property that changed.</param>
        private void OnBackgroundBrushColorChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (_visualizerBackgroundBrush != null && sender is SolidColorBrush brush)
                _visualizerBackgroundBrush.Color = brush.Color;
        }

        private void OnBandsAvailable(object? sender, float[] bands)
        {
            float[] latest = _latestBands;
            if (bands.Length != latest.Length)
                return;

            Array.Copy(bands, latest, latest.Length);
        }

        private void OnCreateResources(CanvasAnimatedControl sender, CanvasCreateResourcesEventArgs args)
        {
            _visualizerBarsBrush = new CanvasSolidColorBrush(sender, (VisualizerBarsBrush as SolidColorBrush)?.Color ?? Colors.DeepSkyBlue);
            _visualizerBackgroundBrush = new CanvasSolidColorBrush(sender, (VisualizerBackgroundBrush as SolidColorBrush)?.Color ?? Colors.Transparent);
        }

        /// <summary>
        /// Called on each update tick to smoothly interpolate the visualizer bars based on the latest audio frequency bands.
        /// </summary>
        /// <param name="sender">The animated control that triggered the update.</param>
        /// <param name="args">The event arguments containing update information.</param>
        private void OnUpdate(ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args)
        {
            // Snapshot references so a BandCount change on the UI thread cannot resize mid-loop.
            float[] smoothBands = _smoothBands;
            float[] latestBands = _latestBands;
            int count = Math.Min(smoothBands.Length, latestBands.Length);
            for (int i = 0; i < count; i++)
            {
                smoothBands[i] = Lerp(smoothBands[i], latestBands[i], 0.2f);
                smoothBands[i] = Math.Max(smoothBands[i], 0.02f);
            }
        }

        private void OnDraw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            var ds = args.DrawingSession;
            float width = (float)sender.Size.Width;
            float height = (float)sender.Size.Height;
            ds.Clear(_visualizerBackgroundBrush!.Color);

            // Snapshot so Length and indexing stay consistent if BandCount changes mid-draw.
            float[] smoothBands = _smoothBands;
            int bandCount = smoothBands.Length;
            if (bandCount == 0 || _visualizerBarsBrush is null)
                return;

            switch (_visualizationStyle)
            {
                case VisualizationStyle.BarsBottom:
                    DrawBarsBottom(ds, smoothBands, width, height);
                    break;
                case VisualizationStyle.BarsTop:
                    DrawBarsTop(ds, smoothBands, width, height);
                    break;
                case VisualizationStyle.BarsLeft:
                    DrawBarsLeft(ds, smoothBands, width, height);
                    break;
                case VisualizationStyle.BarsRight:
                    DrawBarsRight(ds, smoothBands, width, height);
                    break;
                case VisualizationStyle.Circular:
                    DrawCircular(ds, smoothBands, width, height);
                    break;
                default:
                    DrawMirrored(ds, smoothBands, width, height);
                    break;
            }
        }

        private void DrawMirrored(CanvasDrawingSession ds, float[] smoothBands, float width, float height)
        {
            int bandCount = smoothBands.Length;
            int totalBars = bandCount * 2;
            float barWidth = width / totalBars;
            float spacing = barWidth * 0.2f;
            float centerY = height / 2f;

            for (int i = 0; i < totalBars; i++)
            {
                int bandIndex = (i < bandCount) ? bandCount - 1 - i : i - bandCount;
                float magnitude = Math.Clamp(smoothBands[bandIndex], 0, 1);
                float barHeight = magnitude * height;
                float halfHeight = barHeight / 2f;

                float x = i * barWidth;
                float y = centerY - halfHeight;

                ds.FillRectangle(x + spacing, y, barWidth - spacing * 2, barHeight, _visualizerBarsBrush);
            }
        }

        private void DrawBarsBottom(CanvasDrawingSession ds, float[] smoothBands, float width, float height)
        {
            int bandCount = smoothBands.Length;
            float barWidth = width / bandCount;
            float spacing = barWidth * 0.2f;

            for (int i = 0; i < bandCount; i++)
            {
                float magnitude = Math.Clamp(smoothBands[i], 0, 1);
                float barHeight = magnitude * height;
                float x = i * barWidth;
                float y = height - barHeight;

                ds.FillRectangle(x + spacing, y, barWidth - spacing * 2, barHeight, _visualizerBarsBrush);
            }
        }

        private void DrawBarsTop(CanvasDrawingSession ds, float[] smoothBands, float width, float height)
        {
            int bandCount = smoothBands.Length;
            float barWidth = width / bandCount;
            float spacing = barWidth * 0.2f;

            for (int i = 0; i < bandCount; i++)
            {
                float magnitude = Math.Clamp(smoothBands[i], 0, 1);
                float barHeight = magnitude * height;
                float x = i * barWidth;

                ds.FillRectangle(x + spacing, 0, barWidth - spacing * 2, barHeight, _visualizerBarsBrush);
            }
        }

        private void DrawBarsLeft(CanvasDrawingSession ds, float[] smoothBands, float width, float height)
        {
            int bandCount = smoothBands.Length;
            float rowHeight = height / bandCount;
            float spacing = rowHeight * 0.2f;

            for (int i = 0; i < bandCount; i++)
            {
                float magnitude = Math.Clamp(smoothBands[i], 0, 1);
                float barWidth = magnitude * width;
                float y = i * rowHeight;

                ds.FillRectangle(0, y + spacing, barWidth, rowHeight - spacing * 2, _visualizerBarsBrush);
            }
        }

        private void DrawBarsRight(CanvasDrawingSession ds, float[] smoothBands, float width, float height)
        {
            int bandCount = smoothBands.Length;
            float rowHeight = height / bandCount;
            float spacing = rowHeight * 0.2f;

            for (int i = 0; i < bandCount; i++)
            {
                float magnitude = Math.Clamp(smoothBands[i], 0, 1);
                float barWidth = magnitude * width;
                float y = i * rowHeight;
                float x = width - barWidth;

                ds.FillRectangle(x, y + spacing, barWidth, rowHeight - spacing * 2, _visualizerBarsBrush);
            }
        }

        private void DrawCircular(CanvasDrawingSession ds, float[] smoothBands, float width, float height)
        {
            int bandCount = smoothBands.Length;
            float centerX = width / 2f;
            float centerY = height / 2f;
            float maxRadius = Math.Min(width, height) * 0.5f;
            float innerRadius = maxRadius * 0.35f;
            float radialRange = maxRadius - innerRadius;
            // One spoke per band on each half of the circle (mirrored).
            float strokeWidth = Math.Max(2f, maxRadius * 0.04f * (16f / bandCount));

            for (int i = 0; i < bandCount; i++)
            {
                float magnitude = Math.Clamp(smoothBands[i], 0, 1);
                float outerRadius = innerRadius + magnitude * radialRange;

                // Right half: bass near top, treble toward bottom (clockwise from -90°).
                float angleRight = (float)(-Math.PI / 2d + Math.PI * (i + 0.5d) / bandCount);
                DrawCircularSpoke(ds, centerX, centerY, angleRight, innerRadius, outerRadius, strokeWidth);

                // Left half: mirrored (counter-clockwise from -90°).
                float angleLeft = (float)(-Math.PI / 2d - Math.PI * (i + 0.5d) / bandCount);
                DrawCircularSpoke(ds, centerX, centerY, angleLeft, innerRadius, outerRadius, strokeWidth);
            }
        }

        private void DrawCircularSpoke(
            CanvasDrawingSession ds,
            float centerX,
            float centerY,
            float angle,
            float innerRadius,
            float outerRadius,
            float strokeWidth)
        {
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);

            float x0 = centerX + cos * innerRadius;
            float y0 = centerY + sin * innerRadius;
            float x1 = centerX + cos * outerRadius;
            float y1 = centerY + sin * outerRadius;

            ds.DrawLine(x0, y0, x1, y1, _visualizerBarsBrush, strokeWidth);
        }

        private void ResizeBandBuffers(BandCount bandCount)
        {
            int count = (int)bandCount;
            if (_smoothBands.Length == count && _latestBands.Length == count)
                return;

            _smoothBands = new float[count];
            _latestBands = new float[count];
        }

        /// <summary>
        /// Linearly interpolates between two float values based on a given interpolation factor.
        /// </summary>
        /// <param name="a">The starting value.</param>
        /// <param name="b">The ending value.</param>
        /// <param name="t">The interpolation factor, typically between 0 and 1.</param>
        /// <returns>The interpolated value.</returns>
        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
