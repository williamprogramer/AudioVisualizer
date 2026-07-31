using System.Diagnostics;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace AudioVisualizer_SampleApp;

/// <summary>
/// The main content page displayed inside the application window.
/// Add your UI logic, event handlers, and data binding here.
/// </summary>
public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();
        Visualizer.Error += OnVisualizerError;
    }

    private void OnSourceModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Visualizer is null || SourceModeCombo.SelectedItem is not ComboBoxItem item)
            return;

        Visualizer.AudioSourceMode = item.Tag switch
        {
            "Input" => AudioVisualizer.AudioSourceMode.Input,
            "Both" => AudioVisualizer.AudioSourceMode.Both,
            _ => AudioVisualizer.AudioSourceMode.Output
        };
    }

    private void OnPausedToggled(object sender, RoutedEventArgs e)
    {
        if (Visualizer is null || sender is not ToggleSwitch toggle)
            return;

        Visualizer.Paused = toggle.IsOn;
    }

    private void OnBarsBrushChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Visualizer is null || BarsBrushCombo.SelectedItem is not ComboBoxItem item || item.Tag is not string tag)
            return;

        Visualizer.VisualizerBarsBrush = ResolveBrush(tag);
    }

    private void OnBackgroundBrushChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Visualizer is null || BackgroundBrushCombo.SelectedItem is not ComboBoxItem item || item.Tag is not string tag)
            return;

        Visualizer.VisualizerBackgroundBrush = ResolveBrush(tag);
    }

    private static Brush ResolveBrush(string tag)
    {
        if (tag == "Accent"
            && Application.Current.Resources.TryGetValue("AccentFillColorDefaultBrush", out object? accent)
            && accent is Brush accentBrush)
        {
            return accentBrush;
        }

        return new SolidColorBrush(ParseColor(tag));
    }

    private static Color ParseColor(string tag)
    {
        if (tag == "Transparent")
            return Colors.Transparent;

        if (tag.StartsWith('#') && tag.Length == 7
            && byte.TryParse(tag[1..3], System.Globalization.NumberStyles.HexNumber, null, out byte r)
            && byte.TryParse(tag[3..5], System.Globalization.NumberStyles.HexNumber, null, out byte g)
            && byte.TryParse(tag[5..7], System.Globalization.NumberStyles.HexNumber, null, out byte b))
        {
            return Color.FromArgb(255, r, g, b);
        }

        return tag switch
        {
            "Cyan" => Colors.Cyan,
            "DeepSkyBlue" => Colors.DeepSkyBlue,
            "White" => Colors.White,
            "Orange" => Colors.Orange,
            "Black" => Colors.Black,
            "Red" => Colors.Red,
            _ => Colors.Transparent
        };
    }

    private void OnVisualizerError(object? sender, AudioVisualizer.AudioVisualizerErrorEventArgs e)
    {
        Debug.WriteLine($"[AudioVisualizer] {e.Message} (Recoverable={e.IsRecoverable}) {e.Exception}");
    }
}
