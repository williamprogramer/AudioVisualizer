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
    private long _backgroundBrushColorToken;

    /// <summary>
    /// Gets or sets a value indicating whether audio capture and the Win2D animation are paused.
    /// When <see langword="true"/>, capture is stopped and the canvas animation is paused.
    /// When <see langword="false"/>, capture and animation resume.
    /// </summary>
    public bool Paused
    {
        get => (bool)GetValue(PausedProperty);
        set => SetValue(PausedProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="Paused"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PausedProperty = DependencyProperty
        .Register(nameof(Paused), typeof(bool), typeof(AudioVisualizer), new PropertyMetadata(false, OnPausedChanged));

    private static void OnPausedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not AudioVisualizer visualizer || e.NewValue is not bool paused)
            return;

        // Template not applied yet; OnApplyTemplate will honor Paused.
        if (visualizer._canvas is null)
            return;

        visualizer._canvas.Paused = paused;

        if (paused)
            visualizer._naudioService.StopCapture();
        else
            visualizer._naudioService.StartCapture();
    }

    /// <summary>
    /// Gets or sets which audio endpoints are captured for visualization.
    /// </summary>
    public AudioSourceMode AudioSourceMode
    {
        get => (AudioSourceMode)GetValue(AudioSourceModeProperty);
        set => SetValue(AudioSourceModeProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="AudioSourceMode"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty AudioSourceModeProperty = DependencyProperty
        .Register(
            nameof(AudioSourceMode),
            typeof(AudioSourceMode),
            typeof(AudioVisualizer),
            new PropertyMetadata(AudioSourceMode.Output, OnAudioSourceModeChanged));

    private static void OnAudioSourceModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AudioVisualizer visualizer && e.NewValue is AudioSourceMode mode)
            visualizer._naudioService.SetSourceMode(mode);
    }

    /// <summary>
    /// Gets or sets how many frequency bands are analyzed and displayed.
    /// </summary>
    public BandCount BandCount
    {
        get => (BandCount)GetValue(BandCountProperty);
        set => SetValue(BandCountProperty, value);
    }

    /// <summary>
    /// Identifies the <see cref="BandCount"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty BandCountProperty = DependencyProperty
        .Register(
            nameof(BandCount),
            typeof(BandCount),
            typeof(AudioVisualizer),
            new PropertyMetadata(BandCount.Sixteen, OnBandCountChanged));

    private static void OnBandCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not AudioVisualizer visualizer || e.NewValue is not BandCount bandCount)
            return;

        visualizer.ResizeBandBuffers(bandCount);
        visualizer._naudioService.SetBandCount(bandCount);
    }

    /// <summary>
    /// Identifies the VisualizerBackgroundBrush dependency property.
    /// </summary>
    public static readonly DependencyProperty VisualizerBackgroundBrushProperty = DependencyProperty
        .Register(
            nameof(VisualizerBackgroundBrush),
            typeof(Brush),
            typeof(AudioVisualizer),
            new PropertyMetadata(new SolidColorBrush(Colors.Transparent), OnVisualizerBackgroundBrushChanged));

    private static void OnVisualizerBackgroundBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        AudioVisualizer? visualizer = d as AudioVisualizer;
        if (visualizer == null)
            return;

        if (e.OldValue is SolidColorBrush oldBrush)
            oldBrush.UnregisterPropertyChangedCallback(SolidColorBrush.ColorProperty, visualizer._backgroundBrushColorToken);
        if (e.NewValue is SolidColorBrush newBrush)
        {
            visualizer._backgroundBrushColorToken = newBrush.RegisterPropertyChangedCallback(
                SolidColorBrush.ColorProperty, visualizer.OnBackgroundBrushColorChanged);
            if (visualizer._visualizerBackgroundBrush is not null)
                visualizer._visualizerBackgroundBrush.Color = newBrush.Color;
        }
    }

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