using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Dsp;
using NAudio.Wave;

namespace AudioVisualizer.Services
{
    internal sealed partial class NAudioService : IDisposable
    {
        internal const int BandCount = 12;

        private const int MaxRestartAttempts = 3;
        private const int RestartDelayMs = 500;
        private const int FftSize = 1024;

        private readonly object _sync = new();
        private readonly DefaultDeviceNotificationClient _notificationClient;
        private readonly float[] _fftBufferOut = new float[FftSize];
        private readonly float[] _fftBufferIn = new float[FftSize];
        private readonly float[] _latestOutBands = new float[BandCount];
        private readonly float[] _latestInBands = new float[BandCount];

        private WasapiLoopbackCapture? _loopbackCapture;
        private WasapiCapture? _micCapture;
        private MMDeviceEnumerator? _enumerator;
        private int _fftPosOut;
        private int _fftPosIn;
        private int _sampleRateOut;
        private int _sampleRateIn;
        private bool _disposed;
        private bool _running;
        private bool _isRestarting;
        private bool _notificationsRegistered;
        private AudioSourceMode _sourceMode = AudioSourceMode.Output;

        public event EventHandler<float[]>? BandsAvailable;
        public event EventHandler<AudioVisualizerErrorEventArgs>? Error;

        public NAudioService()
        {
            _notificationClient = new DefaultDeviceNotificationClient(OnDefaultDeviceChanged);
        }

        public void SetSourceMode(AudioSourceMode mode)
        {
            lock (_sync)
            {
                if (_disposed || _sourceMode == mode)
                    return;

                _sourceMode = mode;

                if (!_running)
                    return;

                try
                {
                    StartCaptureCore();
                }
                catch (Exception ex)
                {
                    RaiseError("Failed to apply audio source mode.", ex, isRecoverable: false);
                }
            }
        }

        public void StartCapture()
        {
            if (_disposed)
                return;

            lock (_sync)
            {
                if (_disposed)
                    return;

                _running = true;
                try
                {
                    EnsureNotificationsRegistered();
                    StartCaptureCore();
                }
                catch (Exception ex)
                {
                    RaiseError("Failed to start audio capture.", ex, isRecoverable: false);
                }
            }
        }

        public void StopCapture()
        {
            lock (_sync)
            {
                _running = false;
                StopCaptureCore(unregisterNotifications: false);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_sync)
            {
                if (_disposed)
                    return;

                _disposed = true;
                _running = false;
                StopCaptureCore(unregisterNotifications: true);
            }
        }

        private void EnsureNotificationsRegistered()
        {
            if (_notificationsRegistered)
                return;

            _enumerator = new MMDeviceEnumerator();
            _enumerator.RegisterEndpointNotificationCallback(_notificationClient);
            _notificationsRegistered = true;
        }

        private void StartCaptureCore()
        {
            StopCaptureCore(unregisterNotifications: false);

            Array.Clear(_latestOutBands);
            Array.Clear(_latestInBands);

            bool needOutput = _sourceMode is AudioSourceMode.Output or AudioSourceMode.Both;
            bool needInput = _sourceMode is AudioSourceMode.Input or AudioSourceMode.Both;

            Exception? outputError = null;
            Exception? inputError = null;
            bool outputStarted = false;
            bool inputStarted = false;

            if (needOutput)
            {
                try
                {
                    StartLoopbackCapture();
                    outputStarted = true;
                }
                catch (Exception ex)
                {
                    outputError = ex;
                }
            }

            if (needInput)
            {
                try
                {
                    StartMicCapture();
                    inputStarted = true;
                }
                catch (Exception ex)
                {
                    inputError = ex;
                }
            }

            if (_sourceMode == AudioSourceMode.Both)
            {
                if (!outputStarted && !inputStarted)
                {
                    throw new InvalidOperationException(
                        "Failed to start both output and input audio capture.",
                        outputError ?? inputError);
                }

                if (!outputStarted && outputError is not null)
                    RaiseError("Failed to start output (loopback) capture.", outputError, isRecoverable: true);

                if (!inputStarted && inputError is not null)
                    RaiseError("Failed to start input (microphone) capture.", inputError, isRecoverable: true);

                return;
            }

            if (_sourceMode == AudioSourceMode.Output && !outputStarted)
                throw outputError ?? new InvalidOperationException("Failed to start output capture.");

            if (_sourceMode == AudioSourceMode.Input && !inputStarted)
                throw inputError ?? new InvalidOperationException("Failed to start input capture.");
        }

        private void StartLoopbackCapture()
        {
            _fftPosOut = 0;
            _loopbackCapture = new WasapiLoopbackCapture();
            _sampleRateOut = _loopbackCapture.WaveFormat.SampleRate;
            _loopbackCapture.DataAvailable += OnLoopbackData;
            _loopbackCapture.RecordingStopped += OnRecordingStopped;
            _loopbackCapture.StartRecording();
        }

        private void StartMicCapture()
        {
            _fftPosIn = 0;
            _micCapture = new WasapiCapture();
            _sampleRateIn = _micCapture.WaveFormat.SampleRate;
            _micCapture.DataAvailable += OnMicData;
            _micCapture.RecordingStopped += OnRecordingStopped;
            _micCapture.StartRecording();
        }

        private void StopCaptureCore(bool unregisterNotifications)
        {
            if (_loopbackCapture is not null)
            {
                _loopbackCapture.DataAvailable -= OnLoopbackData;
                _loopbackCapture.RecordingStopped -= OnRecordingStopped;
                DisposeCapture(_loopbackCapture);
                _loopbackCapture = null;
            }

            if (_micCapture is not null)
            {
                _micCapture.DataAvailable -= OnMicData;
                _micCapture.RecordingStopped -= OnRecordingStopped;
                DisposeCapture(_micCapture);
                _micCapture = null;
            }

            if (unregisterNotifications && _notificationsRegistered && _enumerator is not null)
            {
                try
                {
                    _enumerator.UnregisterEndpointNotificationCallback(_notificationClient);
                }
                catch
                {
                    // Ignore unregister failures during teardown.
                }

                _enumerator.Dispose();
                _enumerator = null;
                _notificationsRegistered = false;
            }
        }

        private static void DisposeCapture(WasapiCapture capture)
        {
            try
            {
                if (capture.CaptureState != CaptureState.Stopped)
                    capture.StopRecording();
            }
            catch
            {
                // Device may already be invalid after a switch.
            }

            try
            {
                capture.Dispose();
            }
            catch
            {
                // Ignore dispose failures on a dead endpoint.
            }
        }

        private void OnDefaultDeviceChanged(DataFlow dataFlow)
        {
            if (_disposed || !_running)
                return;

            bool affectsOutput = dataFlow == DataFlow.Render
                && (_sourceMode is AudioSourceMode.Output or AudioSourceMode.Both);
            bool affectsInput = dataFlow == DataFlow.Capture
                && (_sourceMode is AudioSourceMode.Input or AudioSourceMode.Both);

            if (!affectsOutput && !affectsInput)
                return;

            _ = RestartCaptureAsync(null);
        }

        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (_disposed || !_running || _isRestarting)
                return;

            _ = RestartCaptureAsync(e.Exception);
        }

        private async Task RestartCaptureAsync(Exception? stopException)
        {
            lock (_sync)
            {
                if (_disposed || !_running || _isRestarting)
                    return;

                _isRestarting = true;
            }

            Exception? lastException = stopException;

            try
            {
                for (int attempt = 0; attempt < MaxRestartAttempts; attempt++)
                {
                    if (_disposed || !_running)
                        return;

                    if (attempt > 0)
                        await Task.Delay(RestartDelayMs).ConfigureAwait(false);

                    try
                    {
                        lock (_sync)
                        {
                            if (_disposed || !_running)
                                return;

                            StartCaptureCore();
                        }

                        return;
                    }
                    catch (Exception ex)
                    {
                        // New endpoint (e.g. Bluetooth) may not be ready yet.
                        lastException = ex;
                    }
                }

                RaiseError(
                    "Failed to restart audio capture after the audio device changed.",
                    lastException,
                    isRecoverable: false);
            }
            finally
            {
                lock (_sync)
                {
                    _isRestarting = false;
                }
            }
        }

        private void RaiseError(string message, Exception? exception, bool isRecoverable)
        {
            Error?.Invoke(this, new AudioVisualizerErrorEventArgs(message, exception, isRecoverable));
        }

        private void OnLoopbackData(object? sender, WaveInEventArgs e)
        {
            if (_loopbackCapture is null)
                return;

            FeedFftBuffer(e, _fftBufferOut, ref _fftPosOut, _sampleRateOut, _latestOutBands, _loopbackCapture.WaveFormat);
        }

        private void OnMicData(object? sender, WaveInEventArgs e)
        {
            if (_micCapture is null)
                return;

            FeedFftBuffer(e, _fftBufferIn, ref _fftPosIn, _sampleRateIn, _latestInBands, _micCapture.WaveFormat);
        }

        private void FeedFftBuffer(
            WaveInEventArgs e,
            float[] fftBuffer,
            ref int fftPos,
            int sampleRate,
            float[] latestBands,
            WaveFormat format)
        {
            int bytesPerFrame = format.BlockAlign;
            if (bytesPerFrame <= 0)
                return;

            int frames = e.BytesRecorded / bytesPerFrame;

            for (int f = 0; f < frames; f++)
            {
                float sample = ReadSample(e.Buffer, f * bytesPerFrame, format);
                fftBuffer[fftPos++] = sample;

                if (fftPos >= FftSize)
                {
                    fftPos = 0;
                    ProcessFFT(fftBuffer, sampleRate, latestBands);
                    PublishBands();
                }
            }
        }

        private static float ReadSample(byte[] buffer, int offset, WaveFormat format)
        {
            // Use the first channel only when multi-channel.
            if (format.Encoding == WaveFormatEncoding.IeeeFloat || format.BitsPerSample == 32)
                return BitConverter.ToSingle(buffer, offset);

            if (format.BitsPerSample == 16)
                return BitConverter.ToInt16(buffer, offset) / 32768f;

            if (format.BitsPerSample == 24)
            {
                int value = buffer[offset] | (buffer[offset + 1] << 8) | (buffer[offset + 2] << 16);
                if ((value & 0x800000) != 0)
                    value |= unchecked((int)0xFF000000);
                return value / 8388608f;
            }

            return 0f;
        }

        private void PublishBands()
        {
            float[] bands = new float[BandCount];

            switch (_sourceMode)
            {
                case AudioSourceMode.Input:
                    Array.Copy(_latestInBands, bands, BandCount);
                    break;
                case AudioSourceMode.Both:
                    for (int i = 0; i < BandCount; i++)
                        bands[i] = Math.Max(_latestOutBands[i], _latestInBands[i]);
                    break;
                default:
                    Array.Copy(_latestOutBands, bands, BandCount);
                    break;
            }

            BandsAvailable?.Invoke(this, bands);
        }

        private static void ProcessFFT(float[] fftBuffer, int sampleRate, float[] destinationBands)
        {
            Complex[] fft = new Complex[FftSize];
            for (int i = 0; i < FftSize; i++)
            {
                fft[i].X = fftBuffer[i];
                fft[i].Y = 0;
            }

            FastFourierTransform.FFT(true, (int)Math.Log2(FftSize), fft);

            float minF = 20f, maxF = 14000f;
            float[] freqEdges = new float[BandCount + 1];

            for (int i = 0; i < BandCount + 1; i++)
            {
                float t = i / (float)BandCount;
                freqEdges[i] = minF * (float)Math.Pow(maxF / minF, t);
            }

            for (int b = 0; b < BandCount; b++)
            {
                int minIndex = (int)(freqEdges[b] / (sampleRate / (float)FftSize));
                int maxIndex = (int)(freqEdges[b + 1] / (sampleRate / (float)FftSize));
                minIndex = Math.Clamp(minIndex, 0, FftSize / 2);
                maxIndex = Math.Clamp(maxIndex, 0, FftSize / 2);

                float sum = 0; int count = 0;
                for (int i = minIndex; i <= maxIndex; i++)
                {
                    float mag = (float)Math.Sqrt(fft[i].X * fft[i].X + fft[i].Y * fft[i].Y);
                    sum += mag; count++;
                }

                float avg = (count > 0) ? sum / count : 0;
                float scaled = (float)Math.Log10(1 + avg * 50f);
                destinationBands[b] = Math.Clamp(scaled, 0, 1);
            }
        }

        private sealed class DefaultDeviceNotificationClient : IMMNotificationClient
        {
            private readonly Action<DataFlow> _onDefaultDeviceChanged;

            public DefaultDeviceNotificationClient(Action<DataFlow> onDefaultDeviceChanged)
            {
                _onDefaultDeviceChanged = onDefaultDeviceChanged;
            }

            public void OnDefaultDeviceChanged(DataFlow dataFlow, Role deviceRole, string defaultDeviceId)
            {
                if (dataFlow is DataFlow.Render or DataFlow.Capture)
                    _onDefaultDeviceChanged(dataFlow);
            }

            public void OnDeviceAdded(string deviceId) { }

            public void OnDeviceRemoved(string deviceId) { }

            public void OnDeviceStateChanged(string deviceId, DeviceState newState) { }

            public void OnPropertyValueChanged(string deviceId, PropertyKey propertyKey) { }
        }
    }
}
