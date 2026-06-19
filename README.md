# AudioVisualizer

A modern WinUI 3 audio visualizer control that displays real-time audio visualization using FFT (Fast Fourier Transform) analysis. Perfect for media players, audio applications, and entertainment software on Windows.

## Features

- 🎵 Real-time audio visualization with 16-band FFT analysis
- 🎨 Customizable colors and brushes via WinUI theme resources
- ⚡ Smooth animations using Win2D rendering
- 📱 Responsive design that adapts to any container size
- 🔊 System audio loopback capture support
- 💻 Built for WinUI 3 on Windows 10+

## Requirements

- Windows 10.0.17763 or later
- .NET 8.0
- Visual Studio 2022 or later

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package AudioVisualizer
```

Or via Package Manager Console:

```
Install-Package AudioVisualizer
```

## Quick Start

Add the visualizer to your XAML page:

```xml
<Page
    xmlns:local="using:AudioVisualizer"
    ...>
    <Grid>
        <local:AudioVisualizer 
            VisualizerBackgroundBrush="{ThemeResource CardBackgroundFillColorDefaultBrush}"
            VisualizerBarsBrush="{ThemeResource AccentFillColorDefaultBrush}" />
    </Grid>
</Page>
```

The visualizer will automatically capture system audio and display the real-time visualization.

## Customization

You can customize the colors by providing WinUI theme resources:

- **VisualizerBackgroundBrush** - Background color of the visualization area
- **VisualizerBarsBrush** - Color of the frequency bars

Example:

```xml
<local:AudioVisualizer 
    VisualizerBackgroundBrush="Black"
    VisualizerBarsBrush="Cyan" />
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.
