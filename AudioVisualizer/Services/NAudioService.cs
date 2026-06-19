using NAudio.Wave;
using NAudio.Dsp;
using System;

namespace AudioVisualizer.Services
{
    public sealed class NAudioService
    {
        private WasapiLoopbackCapture? _capture;
        private float[] _fftBuffer = new float[1024];
        private int _fftPos = 0;
        private int _fftSize = 1024;
        private int _sampleRate;

        public event EventHandler<float[]>? BandsAvailable;

        public void StartCapture()
        {
            _capture = new WasapiLoopbackCapture();
            _sampleRate = _capture.WaveFormat.SampleRate;
            _capture.DataAvailable += OnAudioData;
            _capture.StartRecording();
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

            float[] bands = new float[16];
            float minF = 20f, maxF = 20000f;
            float[] freqEdges = new float[17];

            for (int i = 0; i < 17; i++)
            {
                float t = i / 16f;
                freqEdges[i] = minF * (float)Math.Pow(maxF / minF, t);
            }

            for (int b = 0; b < 16; b++)
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
    }
}