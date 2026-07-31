namespace AudioVisualizer;

/// <summary>
/// Specifies how many frequency bands the visualizer analyzes and displays (mirrored on both sides).
/// </summary>
public enum BandCount
{
    /// <summary>8 frequency bands.</summary>
    Eight = 8,

    /// <summary>16 frequency bands (default).</summary>
    Sixteen = 16,

    /// <summary>32 frequency bands.</summary>
    ThirtyTwo = 32,

    /// <summary>64 frequency bands.</summary>
    SixtyFour = 64
}
