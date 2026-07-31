using System.Diagnostics;
using Microsoft.UI.Xaml.Controls;

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

    private void OnVisualizerError(object? sender, AudioVisualizer.AudioVisualizerErrorEventArgs e)
    {
        Debug.WriteLine($"[AudioVisualizer] {e.Message} (Recoverable={e.IsRecoverable}) {e.Exception}");
    }
}
