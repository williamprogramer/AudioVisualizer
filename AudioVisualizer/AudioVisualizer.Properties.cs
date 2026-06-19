using AudioVisualizer.Services;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AudioVisualizer;
public sealed partial class AudioVisualizer : Control
{
    private CanvasAnimatedControl? _canvas;
    private CanvasSolidColorBrush? _visualizerBackgroundBrush;
    private CanvasSolidColorBrush? _visualizerBarsBrush;
    private readonly NAudioService _naudioService = new();
    public static readonly DependencyProperty VisualizerBackgroundBrushProperty = DependencyProperty.Register(
        nameof(VisualizerBackgroundBrush), typeof(Brush), typeof(AudioVisualizer),
        new PropertyMetadata(new SolidColorBrush(Colors.Transparent)));
    public Brush VisualizerBackgroundBrush
    {
        get => (Brush)GetValue(VisualizerBackgroundBrushProperty);
        set => SetValue(VisualizerBackgroundBrushProperty, value);
    }
    public static readonly DependencyProperty VisualizerBarsBrushProperty =
        DependencyProperty.Register(
            nameof(VisualizerBarsBrush),
            typeof(Brush),
            typeof(AudioVisualizer),
            new PropertyMetadata(new SolidColorBrush(Colors.Transparent)));

    public Brush VisualizerBarsBrush
    {
        get => (Brush)GetValue(VisualizerBarsBrushProperty);
        set => SetValue(VisualizerBarsBrushProperty, value);
    }
}