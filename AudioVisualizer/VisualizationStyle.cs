namespace AudioVisualizer;

/// <summary>
/// Specifies how frequency bands are laid out when drawing the visualizer.
/// </summary>
public enum VisualizationStyle
{
    /// <summary>
    /// Mirrored bars with bass in the center, growing above and below the midline (default).
    /// </summary>
    Mirrored = 0,

    /// <summary>
    /// Classic equalizer bars growing upward from the bottom edge.
    /// </summary>
    BarsBottom = 1,

    /// <summary>
    /// Equalizer bars growing downward from the top edge.
    /// </summary>
    BarsTop = 2,

    /// <summary>
    /// Horizontal bars stacked top-to-bottom (bass at top), growing to the right.
    /// </summary>
    BarsLeft = 3,

    /// <summary>
    /// Horizontal bars stacked top-to-bottom (bass at top), growing to the left from the right edge.
    /// </summary>
    BarsRight = 4,

    /// <summary>
    /// Radial spokes around the center; length follows band magnitude.
    /// </summary>
    Circular = 5
}
