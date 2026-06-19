using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AudioVisualizer
{
    public sealed partial class AudioVisualizer : Control
    {
        private readonly float[] _smoothBands = new float[16];
        private float[] _latestBands = new float[16];

        public AudioVisualizer()
        {
            DefaultStyleKey = typeof(AudioVisualizer);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _canvas = GetTemplateChild("AudioVisualizerCanvas") as CanvasAnimatedControl;
            if (_canvas != null)
            {
                _canvas.CreateResources += OnCreateResources;
                _canvas.Update += OnUpdate;
                _canvas.Draw += OnDraw;

                _naudioService.BandsAvailable += OnBandsAvailable;
                _naudioService.StartCapture();
            }
        }

        private void OnBandsAvailable(object? sender, float[] bands)
        {
            _latestBands = bands;
        }

        private void OnCreateResources(CanvasAnimatedControl sender, CanvasCreateResourcesEventArgs args)
        {
            SolidColorBrush? visualizerBarsBrush = VisualizerBarsBrush as SolidColorBrush;
            _visualizerBarsBrush = new CanvasSolidColorBrush(sender, visualizerBarsBrush?.Color ?? Colors.DeepSkyBlue);
            SolidColorBrush? visualizerBackgroundBrush = VisualizerBackgroundBrush as SolidColorBrush;
            _visualizerBackgroundBrush = new CanvasSolidColorBrush(sender, visualizerBackgroundBrush?.Color ?? Colors.Transparent);
        }

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

        private float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}