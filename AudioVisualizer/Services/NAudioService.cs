using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.Dsp;
using NAudio.Wave;

namespace AudioVisualizer.Services
{
    internal sealed class NAudioService : IDisposable
    {
        internal const int BandCount = 12;

        private const int MaxRestartAttempts = 3;
        private const int RestartDelayMs = 500;

        private readonly object _sync = new();
        private readonly DefaultDeviceNotificationClient _notificationClient;
        private readonly float[] _fftBuffer = new float[1024];
        private readonly int _fftSize = 1024;

        private WasapiLoopbackCapture? _capture;
        private MMDeviceEnumerator? _enumerator;
        private int _fftPos;
        private int _sampleRate;
        private bool _disposed;
        private bool _running;
        private bool _isRestarting;
        private bool _notificationsRegistered;

        public event EventHandler<float[]>? BandsAvailable;

        public NAudioService()
        {
            _notificationClient = new DefaultDeviceNotificationClient(OnDefaultRenderDeviceChanged);
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
                EnsureNotificationsRegistered();
                StartCaptureCore();
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

            _fftPos = 0;
            _capture = new WasapiLoopbackCapture();
            _sampleRate = _capture.WaveFormat.SampleRate;
            _capture.DataAvailable += OnAudioData;
            _capture.RecordingStopped += OnRecordingStopped;
            _capture.StartRecording();
        }

        private void StopCaptureCore(bool unregisterNotifications)
        {
            if (_capture is not null)
            {
                _capture.DataAvailable -= OnAudioData;
                _capture.RecordingStopped -= OnRecordingStopped;

                try
                {
                    if (_capture.CaptureState != CaptureState.Stopped)
                        _capture.StopRecording();
                }
                catch
                {
                    // Device may already be invalid after a switch.
                }

                try
                {
                    _capture.Dispose();
                }
                catch
                {
                    // Ignore dispose failures on a dead endpoint.
                }

                _capture = null;
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

        private void OnDefaultRenderDeviceChanged()
        {
            if (_disposed || !_running)
                return;

            _ = RestartCaptureAsync();
        }

        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (_disposed || !_running || _isRestarting)
                return;

            _ = RestartCaptureAsync();
        }

        private async Task RestartCaptureAsync()
        {
            lock (_sync)
            {
                if (_disposed || !_running || _isRestarting)
                    return;

                _isRestarting = true;
            }

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
                    catch
                    {
                        // New endpoint (e.g. Bluetooth) may not be ready yet.
                    }
                }
            }
            finally
            {
                lock (_sync)
                {
                    _isRestarting = false;
                }
            }
        }

        private void OnAudioData(object? sender, WaveInEventArgs e)
        {
            int bytesPerSample = 4;
            int samples = e.BytesRecorded / bytesPerSample;

            for (int i = 0; i < samples; i++)
            {
                float sample = BitConverter.ToSingle(e.Buffer, i * bytesPerSample);
                _fftBuffer[_fftPos++] = sample;

                if (_fftPos >= _fftSize)
                {
                    _fftPos = 0;
                    ProcessFFT();
                }
            }
        }

        private void ProcessFFT()
        {
            Complex[] fft = new Complex[_fftSize];
            for (int i = 0; i < _fftSize; i++)
            {
                fft[i].X = _fftBuffer[i];
                fft[i].Y = 0;
            }

            FastFourierTransform.FFT(true, (int)Math.Log2(_fftSize), fft);

            float[] bands = new float[BandCount];
            float minF = 20f, maxF = 14000f;
            float[] freqEdges = new float[BandCount + 1];

            for (int i = 0; i < BandCount + 1; i++)
            {
                float t = i / (float)BandCount;
                freqEdges[i] = minF * (float)Math.Pow(maxF / minF, t);
            }

            for (int b = 0; b < BandCount; b++)
            {
                int minIndex = (int)(freqEdges[b] / (_sampleRate / (float)_fftSize));
                int maxIndex = (int)(freqEdges[b + 1] / (_sampleRate / (float)_fftSize));
                minIndex = Math.Clamp(minIndex, 0, _fftSize / 2);
                maxIndex = Math.Clamp(maxIndex, 0, _fftSize / 2);

                float sum = 0; int count = 0;
                for (int i = minIndex; i <= maxIndex; i++)
                {
                    float mag = (float)Math.Sqrt(fft[i].X * fft[i].X + fft[i].Y * fft[i].Y);
                    sum += mag; count++;
                }

                float avg = (count > 0) ? sum / count : 0;
                float scaled = (float)Math.Log10(1 + avg * 50f);
                bands[b] = Math.Clamp(scaled, 0, 1);
            }
            BandsAvailable?.Invoke(this, bands);
        }

        private sealed class DefaultDeviceNotificationClient : IMMNotificationClient
        {
            private readonly Action _onDefaultRenderChanged;

            public DefaultDeviceNotificationClient(Action onDefaultRenderChanged)
            {
                _onDefaultRenderChanged = onDefaultRenderChanged;
            }

            public void OnDefaultDeviceChanged(DataFlow dataFlow, Role deviceRole, string defaultDeviceId)
            {
                if (dataFlow == DataFlow.Render)
                    _onDefaultRenderChanged();
            }

            public void OnDeviceAdded(string deviceId) { }

            public void OnDeviceRemoved(string deviceId) { }

            public void OnDeviceStateChanged(string deviceId, DeviceState newState) { }

            public void OnPropertyValueChanged(string deviceId, PropertyKey propertyKey) { }
        }
    }
}
