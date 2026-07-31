namespace AudioVisualizer;

/// <summary>
/// Specifies which audio endpoints the visualizer captures for FFT analysis.
/// </summary>
public enum AudioSourceMode
{
    /// <summary>
    /// Capture system playback via WASAPI loopback (default output device).
    /// </summary>
    Output = 0,

    /// <summary>
    /// Capture the default microphone / input device.
    /// </summary>
    Input = 1,

    /// <summary>
    /// Capture both output loopback and microphone, merging bands with per-band maximum.
    /// </summary>
    Both = 2
}
