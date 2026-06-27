using AudioVisualizer.Services;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AudioVisualizer;

/// <summary>
/// A WinUI 3 audio visualizer control that displays real-time audio visualization using FFT analysis.
/// </summary>
public sealed partial class AudioVisualizer : Control
{
    private CanvasAnimatedControl? _canvas;
    private CanvasSolidColorBrush? _visualizerBackgroundBrush;
    private CanvasSolidColorBrush? _visualizerBarsBrush;
    private readonly NAudioService _naudioService = new();
    private long _barsBrushColorToken;

    /// <summary>
    /// Identifies the VisualizerBackgroundBrush dependency property.
    /// </summary>
    public static readonly DependencyProperty VisualizerBackgroundBrushProperty = DependencyProperty.Register(
        nameof(VisualizerBackgroundBrush), typeof(Brush), typeof(AudioVisualizer),
        new PropertyMetadata(new SolidColorBrush(Colors.Transparent)));

    /// <summary>
    /// Gets or sets the background brush for the visualizer area.
    /// </summary>
    public Brush VisualizerBackgroundBrush
    {
        get => (Brush)GetValue(VisualizerBackgroundBrushProperty);
        set => SetValue(VisualizerBackgroundBrushProperty, value);
    }

    /// <summary>
    /// Identifies the VisualizerBarsBrush dependency property.
    /// </summary>
    public static readonly DependencyProperty VisualizerBarsBrushProperty =
        DependencyProperty.Register(
            nameof(VisualizerBarsBrush),
            typeof(Brush),
            typeof(AudioVisualizer),
            new PropertyMetadata(new SolidColorBrush(Colors.Transparent), OnVisualizerBarsBrushChanged));

    private static void OnVisualizerBarsBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        AudioVisualizer? visualizer = d as AudioVisualizer;
        if (visualizer == null)
            return;

        if (e.OldValue is SolidColorBrush oldBrush)
            oldBrush.UnregisterPropertyChangedCallback(SolidColorBrush.ColorProperty, visualizer._barsBrushColorToken);
        if (e.NewValue is SolidColorBrush newBrush)
        {
            visualizer._barsBrushColorToken = newBrush.RegisterPropertyChangedCallback(
                SolidColorBrush.ColorProperty, visualizer.OnBarsBrushColorChanged);
            if (visualizer._visualizerBarsBrush is not null)
                visualizer._visualizerBarsBrush.Color = newBrush.Color;
        }
    }

    /// <summary>
    /// Gets or sets the brush color for the frequency visualization bars.
    /// </summary>
    public Brush VisualizerBarsBrush
    {
        get => (Brush)GetValue(VisualizerBarsBrushProperty);
        set => SetValue(VisualizerBarsBrushProperty, value);
    }
}