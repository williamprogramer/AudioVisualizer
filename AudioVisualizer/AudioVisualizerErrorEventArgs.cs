namespace AudioVisualizer;

/// <summary>
/// Provides data for the <see cref="AudioVisualizer.Error"/> event when capture fails.
/// </summary>
public sealed class AudioVisualizerErrorEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AudioVisualizerErrorEventArgs"/> class.
    /// </summary>
    /// <param name="message">A human-readable description of the error.</param>
    /// <param name="exception">The underlying exception, if any.</param>
    /// <param name="isRecoverable">
    /// <see langword="true"/> if the control may still recover; otherwise <see langword="false"/> for a final failure.
    /// </param>
    public AudioVisualizerErrorEventArgs(string message, Exception? exception, bool isRecoverable)
    {
        Message = message;
        Exception = exception;
        IsRecoverable = isRecoverable;
    }

    /// <summary>
    /// Gets a human-readable description of the error.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the underlying exception, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets a value indicating whether the control may still recover from this error.
    /// Final capture failures report <see langword="false"/>.
    /// </summary>
    public bool IsRecoverable { get; }
}
