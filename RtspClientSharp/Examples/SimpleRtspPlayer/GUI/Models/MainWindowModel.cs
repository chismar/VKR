using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using RtspClientSharp;
using SimpleRtspPlayer.RawFramesDecoding.DecodedFrames;
using SimpleRtspPlayer.RawFramesReceiving;
using StudioServer;

namespace SimpleRtspPlayer.GUI.Models
{
    class MainWindowModel : IMainWindowModel
    {
        private readonly RealtimeVideoSource _realtimeVideoSource = new RealtimeVideoSource();
        private readonly RealtimeVideoSource _realtimeVideoSource2 = new RealtimeVideoSource();
        private readonly RealtimeAudioSource _realtimeAudioSource = new RealtimeAudioSource();

        private IRawFramesSource _rawFramesSource;
        private IRawFramesSource _rawFramesSource2;

        public event EventHandler<string> StatusChanged;

        public IVideoSource VideoSource => _realtimeVideoSource;
        public IVideoSource VideoSource2 => _realtimeVideoSource2;
        CancellationTokenSource _audioToken = new CancellationTokenSource();
        BufferedWaveProvider _audioStream;
        bool _soundIsStarted;
        Task task;
        bool _playBackIsStopped;
        DateTime _lastTime;
        void RunSound()
        {
            try
            {
                if (_audioToken != null && !_audioToken.IsCancellationRequested)
                    _audioToken.Cancel();
            }
            finally
            {

            }
            _audioToken = new CancellationTokenSource();
            var t = _audioToken.Token;
            task = Task.Run(async () =>
            {

                //wi.WaveFormat = new WaveFormat(16000, 1);
                var wo = new WaveOutEvent();
                //wo.OutputWaveFormat = new WaveFormat(44100, 1);
                //wo.DeviceNumber = 1;
                wo.Init(_audioStream);
                wo.Play();
                WaveInEvent wi = null;
                if (SetupState.UseMicInput)
                {
                    wi = new WaveInEvent();
                    wi.DataAvailable += Wi_DataAvailable;
                    wi.WaveFormat = wo.OutputWaveFormat;
                    wi.StartRecording();

                } 
                while (!t.IsCancellationRequested)
                {

                    /*if(_audioStream.BufferedDuration < TimeSpan.FromSeconds(0.1f))
                {
                    _playBackIsStopped = true;
                    wo.Stop();
                }
                if(_playBackIsStopped && _audioStream.BufferedDuration > TimeSpan.FromSeconds(3f))
                {
                    _playBackIsStopped = false;
                    wo.Play();
                }*/
                    await Task.Delay(100);
                }
                wi?.StopRecording();
                wi = null;
                _audioStream = null;
                wo.Stop();
                wo.Dispose();
            });
        }

        private void Wi_DataAvailable(object sender, WaveInEventArgs e)
        {
            _audioStream.AddSamples(e.Buffer, 0, e.BytesRecorded);
        }

        public void Start(ConnectionParameters connectionParameters, ConnectionParameters connectionParameters2)
        {
            //var settings = new VideoEncoderSettings(width: 1920, height: 1080);
            //_outputFile = new MediaBuilder(@"C:\videos\example.mp4").WithVideo(settings).Create();
            //_outputFile.Video.AddFrame(new FFMediaToolkit.Graphics.ImageData());
            //_outputFile.Dispose();
            Start(connectionParameters, ref _rawFramesSource, _realtimeVideoSource, false);
            Start(connectionParameters2, ref _rawFramesSource2, _realtimeVideoSource2, true);
        }
        bool _doAudio;
        private void Start(ConnectionParameters connectionParameters, ref IRawFramesSource _rawFramesSource, RealtimeVideoSource _realtimeVideoSource, bool doAudio)
        {
            if (_rawFramesSource != null)
                return;

            _rawFramesSource = new RawFramesSource(connectionParameters);
            _rawFramesSource.ConnectionStatusChanged += ConnectionStatusChanged;

            _realtimeVideoSource.SetRawFramesSource(_rawFramesSource);
            if (doAudio)
            {
                _doAudio = doAudio;
                _realtimeAudioSource.SetRawFramesSource(_rawFramesSource);
                if (SetupState.UseMicInput)
                    _realtimeAudioSource_FrameReceived(null, null);
                else
                    _realtimeAudioSource.FrameReceived += _realtimeAudioSource_FrameReceived;

            }
            _rawFramesSource.Start();

        }

        private void _realtimeAudioSource_FrameReceived(object sender, IDecodedAudioFrame frame)
        {
            if (!_soundIsStarted)
            {
                _soundIsStarted = true;
                if(SetupState.UseMicInput)
                {
                    _audioStream = new BufferedWaveProvider(new WaveFormat(44100, 1));
                    _audioStream.BufferDuration = TimeSpan.FromSeconds(15);
                    RunSound();
                }
                else
                {
                    _audioStream = new BufferedWaveProvider(new WaveFormat(frame.Format.SampleRate, frame.Format.BitPerSample, frame.Format.Channels));
                    _audioStream.BufferDuration = TimeSpan.FromSeconds(15);
                    _audioStream.AddSamples(frame.DecodedBytes.Array, frame.DecodedBytes.Offset, frame.DecodedBytes.Count);
                    RunSound();
                }
            }
            else
            {
                if (!SetupState.UseMicInput)
                    _audioStream.AddSamples(frame.DecodedBytes.Array, frame.DecodedBytes.Offset, frame.DecodedBytes.Count);

            }
            //Console.WriteLine($"{(_lastTime - DateTime.Now).TotalMilliseconds}, {_audioStream.BufferedDuration.TotalMilliseconds}");
            _lastTime = DateTime.Now;
        }

        public void Stop()
        {
            _soundIsStarted = false;
            _audioToken.Cancel(); 
            if (_doAudio)
            {
                _realtimeAudioSource.SetRawFramesSource(null);
                _realtimeAudioSource.FrameReceived -= _realtimeAudioSource_FrameReceived;
            }
            if (_rawFramesSource != null)
            {
                _rawFramesSource.Stop();
                _realtimeVideoSource.SetRawFramesSource(null);
                _rawFramesSource = null;
            }
            if (_rawFramesSource2 != null)
            {
                _rawFramesSource2.Stop();
                _realtimeVideoSource2.SetRawFramesSource(null);
                _rawFramesSource2 = null;
            }
        }

        private void ConnectionStatusChanged(object sender, string s)
        {
            StatusChanged?.Invoke(this, s);
        }
    }
}