using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
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
        /// <summary>
        /// An array to store the smoothed audio frequency bands for visualization.
        /// </summary>
        private readonly float[] _smoothBands = new float[16];
        /// <summary>
        /// An array to store the latest audio frequency bands received from the audio service.
        /// </summary>
        private float[] _latestBands = new float[16];

        /// <summary>
        /// Initializes a new instance of the AudioVisualizer control.
        /// </summary>
        public AudioVisualizer()
        {
            DefaultStyleKey = typeof(AudioVisualizer);
        }

        /// <summary>
        /// Called when the control template is applied to initialize visualization resources.
        /// </summary>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _canvas = GetTemplateChild("AudioVisualizerCanvas") as CanvasAnimatedControl;
            if (_canvas != null)
            {
                _canvas.CreateResources += OnCreateResources;
                _canvas.Update += OnUpdate;
                _canvas.Draw += OnDraw;
                _canvas.ActualThemeChanged += OnActualThemeChanged;

                _naudioService.BandsAvailable += OnBandsAvailable;
                _naudioService.StartCapture();
            }
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

        private void OnBandsAvailable(object? sender, float[] bands)
        {
            _latestBands = bands;
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
            for (int i = 0; i < 16; i++)
            {
                _smoothBands[i] = Lerp(_smoothBands[i], _latestBands[i], 0.2f);
                _smoothBands[i] = Math.Max(_smoothBands[i], 0.02f);
            }
        }

        private void OnDraw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            var ds = args.DrawingSession;
            float width = (float)sender.Size.Width;
            float height = (float)sender.Size.Height;
            ds.Clear(_visualizerBackgroundBrush!.Color);
            int totalBars = 32;
            float barWidth = width / totalBars;
            float spacing = barWidth * 0.2f;
            float centerY = height / 2f;

            for (int i = 0; i < totalBars; i++)
            {
                int bandIndex = (i < 16) ? 15 - i : i - 16;
                float magnitude = Math.Clamp(_smoothBands[bandIndex], 0, 1);
                float barHeight = magnitude * height;
                float halfHeight = barHeight / 2f;

                float x = i * barWidth;
                float y = centerY - halfHeight;

                ds.FillRectangle(x + spacing, y, barWidth - spacing * 2, barHeight, _visualizerBarsBrush);
            }
        }
        /// <summary>
        /// Linearly interpolates between two float values based on a given interpolation factor.
        /// </summary>
        /// <param name="a">The starting value.</param>
        /// <param name="b">The ending value.</param>
        /// <param name="t">The interpolation factor, typically between 0 and 1.</param>
        /// <returns>The interpolated value.</returns>
        private float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}