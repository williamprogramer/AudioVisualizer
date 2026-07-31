# AudioVisualizer

A modern WinUI 3 audio visualizer control that displays real-time audio visualization using FFT (Fast Fourier Transform) analysis. Perfect for media players, audio applications, and entertainment software on Windows.

![AudioVisualizer Preview](assets/audiovisualizer_preview.gif)

## Features

- Real-time WASAPI loopback capture with 12-band FFT analysis (log-spaced ~20 Hz–14 kHz)
- Mirrored bar layout (bass in the center, highs outward; bars grow above and below center)
- Customizable colors via WinUI theme resources, with live updates for theme and accent changes
- `Paused` property to pause or resume the Win2D animation
- Automatic capture rebind when the default output device changes (e.g. speakers ↔ Bluetooth)
- Smooth animations using Win2D rendering
- Responsive design that adapts to any container size
- Built for WinUI 3 on Windows 10+ (x86, x64, and ARM64)

## Requirements

- Windows 10.0.17763 or later
- .NET 8.0 or later
- A WinUI 3 host app (Windows App SDK)

## Installation

Nuget: https://www.nuget.org/packages/AudioVisualizer

Install via NuGet Package Manager:

```bash
dotnet add package AudioVisualizer --version 1.1.0
```

Or via Package Manager Console:

```
Install-Package AudioVisualizer -Version 1.1.0
```

**Current stable version: 1.1.0**

## Quick Start

Add the visualizer to your XAML page:

```xml
<Page
    xmlns:local="using:AudioVisualizer"
    ...>
    <Grid>
        <local:AudioVisualizer
            Paused="False"
            VisualizerBackgroundBrush="Transparent"
            VisualizerBarsBrush="{ThemeResource AccentFillColorDefaultBrush}" />
    </Grid>
</Page>
```

The visualizer will automatically capture system audio and display the real-time visualization.

## Customization

You can customize appearance and playback with these properties:

- **VisualizerBackgroundBrush** - Background color of the visualization area
- **VisualizerBarsBrush** - Color of the frequency bars (prefer a `SolidColorBrush` or theme resource for live accent/theme updates)
- **Paused** - When `true`, pauses the Win2D animation; when `false`, resumes it

Example:

```xml
<local:AudioVisualizer
    Paused="False"
    VisualizerBackgroundBrush="Black"
    VisualizerBarsBrush="Cyan" />
```

### Error handling

Subscribe to the `Error` event to learn when capture fails in a non-recoverable way (for example, start failure or exhausted device-rebind retries). Teardown failures on a dead audio endpoint are swallowed internally and do not raise this event.

```csharp
visualizer.Error += (sender, e) =>
{
    // e.Message, e.Exception, e.IsRecoverable
};
```

## Audio device changes

When the default Windows output device changes, the control restarts loopback capture on the new endpoint automatically. Bars should resume within a few seconds without restarting the app.

## Dependencies

This package targets `net8.0-windows10.0.26100.0` and brings:

- NAudio 2.3.0
- Microsoft.Graphics.Win2D 1.4.0

## License

This project is licensed under the MIT License - see the LICENSE file for details.
