using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight.Command;
using RtspClientSharp;
using SimpleRtspPlayer.GUI.Models;
using StudioServer;

namespace SimpleRtspPlayer.GUI.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged, IStreamerController
    {
        private const string RtspPrefix = "rtsp://";
        private const string HttpPrefix = "http://";

        private string _status = string.Empty;
        private readonly IMainWindowModel _mainWindowModel;
        private bool _startButtonEnabled = true;
        private bool _stopButtonEnabled;
        bool useSetupState = false;
        public SetupState SetupState { get; private set; } = new SetupState()
        {
            FolderForVideos = "/videos/",
            LecturerFeed = "rtsp://wowzaec2demo.streamlock.net/vod/mp4:BigBuckBunny_115k.mov",//"\"rtsp://admin:Supervisor@192.168.1.70:554/cam/realmonitor?channel=1&subtype=0&unicast=true&proto=Onvif\"",
            PresentationFeed = "rtsp://wowzaec2demo.streamlock.net/vod/mp4:BigBuckBunny_115k.mov"//"\"rtsp://admin:Supervisor@192.168.1.70:554/cam/realmonitor?channel=1&subtype=0&unicast=true&proto=Onvif\""//"\"rtsp://192.168.1.71/0\""
        };

        public string DeviceAddress { get; set; } = "rtsp://wowzaec2demo.streamlock.net/vod/mp4:BigBuckBunny_115k.mov";

        public string Login { get; set; } = "admin";
        public string Password { get; set; } = "123456";
        public string DeviceAddress2 { get; set; } = "rtsp://wowzaec2demo.streamlock.net/vod/mp4:BigBuckBunny_115k.mov";

        public string Login2 { get; set; } = "admin";
        public string Password2 { get; set; } = "123456";

        public IVideoSource VideoSource => _mainWindowModel.VideoSource;
        public IVideoSource VideoSource2 => _mainWindowModel.VideoSource2;

        public RelayCommand StartClickCommand { get; }
        public RelayCommand StopClickCommand { get; }
        public RelayCommand<CancelEventArgs> ClosingCommand { get; }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        public string CurrentSession { get; private set; }

        public bool IsActive { get; private set; }

        public bool IsRecording { get; private set; }

        public SessionState CurrentState { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public event Action StateHasChanged;
        Task serverTask;
        public MainWindowViewModel(IMainWindowModel mainWindowModel)
        {
            _mainWindowModel = mainWindowModel ?? throw new ArgumentNullException(nameof(mainWindowModel));
            
            StartClickCommand = new RelayCommand(OnStartButtonClick, () => _startButtonEnabled);
            StopClickCommand = new RelayCommand(OnStopButtonClick, () => _stopButtonEnabled);
            ClosingCommand = new RelayCommand<CancelEventArgs>(OnClosing);
            serverTask = Task.Run(() =>
            {
                StudioServer.Program.RunServer(this);
            });
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        ConnectionParameters GetConparam(string address, string login, string password)
        {
            if (!address.StartsWith(RtspPrefix) && !address.StartsWith(HttpPrefix))
                address = RtspPrefix + address;

            if (!Uri.TryCreate(address, UriKind.Absolute, out Uri deviceUri))
            {
                MessageBox.Show("Invalid device address", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            var credential = new NetworkCredential(login, password);

            var connectionParameters = !string.IsNullOrEmpty(deviceUri.UserInfo) ? new ConnectionParameters(deviceUri) :
                new ConnectionParameters(deviceUri, credential);

            connectionParameters.RtpTransport = RtpTransportProtocol.UDP;
            connectionParameters.CancelTimeout = TimeSpan.FromSeconds(1);
            return connectionParameters;
        }
        private void OnStartButtonClick()
        {

            RunFFMpeg();
            var cp1 = useSetupState ? GetConparam(SetupState.PresentationFeed, SetupState.PresentationFeedLogin, SetupState.PresentationFeedPass) : GetConparam(DeviceAddress, Login, Password);
            var cp2= useSetupState ? GetConparam(SetupState.LecturerFeed, SetupState.LecturerFeedLogin, SetupState.LecturerFeedPass) : GetConparam(DeviceAddress2, Login2, Password2);

            _mainWindowModel.Start(cp1, cp2);
            _mainWindowModel.StatusChanged += MainWindowModelOnStatusChanged;

            _startButtonEnabled = false;
            StartClickCommand.RaiseCanExecuteChanged();
            _stopButtonEnabled = true;
            StopClickCommand.RaiseCanExecuteChanged();
        }

        private void OnStopButtonClick()
        {
            ffmpegRecordingProcess?.Kill();
            _mainWindowModel.Stop();
            _mainWindowModel.StatusChanged -= MainWindowModelOnStatusChanged;

            _stopButtonEnabled = false;
            StopClickCommand.RaiseCanExecuteChanged();
            _startButtonEnabled = true;
            StartClickCommand.RaiseCanExecuteChanged();
            Status = string.Empty;
        }

        private void MainWindowModelOnStatusChanged(object sender, string s)
        {
            Application.Current.Dispatcher.Invoke(() => Status = s);
        }

        private void OnClosing(CancelEventArgs args)
        {
            _mainWindowModel.Stop();
        }

        public async Task PrepareSetup(SetupState state)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                SetupState = state;
                StateHasChanged?.Invoke();
            });
        }

        public async Task Run(SetupState state)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                this.SetupState = state;
                IsActive = true;
                if (!IsRecording)
                    Task.Run(BeginRecording);
                StateHasChanged?.Invoke();
            });
        }

        public async Task Stop()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                IsActive = false;
                if (IsRecording)
                    Task.Run(StopRecording);
                StateHasChanged?.Invoke();
            });
        }
        Process ffmpegRecordingProcess;
        void RunFFMpeg()
        {
            int exitCode;
            ProcessStartInfo processInfo;
            Process process;
            string input = File.ReadAllText("ffmpeg_record.bat");
            processInfo = new ProcessStartInfo("ffmpeg.exe", @"-f dshow -i video=""screen-capture-recorder"":audio=""virtual-audio-capturer"" -c:v libx264 -crf 0 -preset ultrafast output.mkv ");
            processInfo.CreateNoWindow = true;
            processInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            processInfo.UseShellExecute = false;
            // *** Redirect the output ***
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;

            Task.Run(() =>
            {
                ffmpegRecordingProcess = Process.Start(processInfo);
                ffmpegRecordingProcess.WaitForExit();

                // *** Read the streams ***
                // Warning: This approach can lead to deadlocks, see Edit #2
                string output = ffmpegRecordingProcess.StandardOutput.ReadToEnd();
                string error = ffmpegRecordingProcess.StandardError.ReadToEnd();

                exitCode = ffmpegRecordingProcess.ExitCode;

                Console.WriteLine("output>>" + (String.IsNullOrEmpty(output) ? "(none)" : output));
                Console.WriteLine("error>>" + (String.IsNullOrEmpty(error) ? "(none)" : error));
                Console.WriteLine("ExitCode: " + exitCode.ToString(), "ExecuteCommand");
                ffmpegRecordingProcess.Close();
            });
        }
        public async Task BeginRecording()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                useSetupState = true;
                OnStartButtonClick();
                useSetupState = false;
                StateHasChanged?.Invoke();
            });
        }

        public async Task StopRecording()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                OnStopButtonClick();
                StateHasChanged?.Invoke();
            });
        }

        public async Task<string[]> GetFilesForSession(string name)
        {
            var ppath = SetupState.RootFS + SetupState.FolderForVideos;
            if (!Directory.Exists(ppath))
                return Array.Empty<string>();
            return Directory.GetFiles(ppath, $"*{name}.avi");
        }

        public async Task SetRecordingSession(string name)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                CurrentSession = name;
                StateHasChanged?.Invoke();
            });
        }

        public async Task ChangeSessionState(SessionState newState)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                CurrentState = newState;
                StateHasChanged?.Invoke();
            });
        }
    }
}