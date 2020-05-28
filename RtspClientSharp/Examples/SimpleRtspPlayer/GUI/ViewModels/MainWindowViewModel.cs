using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight.Command;
using Jot;
using Jot.Storage;
using RtspClientSharp;
using SimpleRtspPlayer.GUI.Models;
using SimpleRtspPlayer.GUI.Views;
using StudioServer;

namespace SimpleRtspPlayer.GUI.ViewModels
{
    static class SetupStateTracker
    {
        // expose the tracker instance
        public static Tracker Tracker = new Tracker(new JsonFileStore(Directory.GetCurrentDirectory()));

        static SetupStateTracker()
        {
            // tell Jot how to track Window objects
            Tracker.Configure<SetupState>()
                .Id(w => nameof(SetupState))
                .Properties(w => new
                {
                    w.FolderForVideos,
                    w.LecturerFeed,
                    w.LecturerFeedLogin,
                    w.LecturerFeedPass,
                    w.PresentationFeed,
                    w.PresentationFeedLogin,
                    w.PresentationFeedPass,
                    SetupState.ChromakeyColor,
                    SetupState.ChromakeyValues,
                });

        }
    }
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
            LecturerFeed = "rtsp://192.168.1.68:554/cam/realmonitor?channel=1&subtype=0&unicast=true&proto=Onvif",//"rtsp://wowzaec2demo.streamlock.net/vod/mp4:BigBuckBunny_115k.mov",//"\"rtsp://admin:Supervisor@192.168.1.70:554/cam/realmonitor?channel=1&subtype=0&unicast=true&proto=Onvif\"",
            PresentationFeed = "rtsp://192.168.1.70/0",//"rtsp://wowzaec2demo.streamlock.net/vod/mp4:BigBuckBunny_115k.mov"//"\"rtsp://admin:Supervisor@192.168.1.70:554/cam/realmonitor?channel=1&subtype=0&unicast=true&proto=Onvif\""//"\"rtsp://192.168.1.71/0\""
            LecturerFeedPass = "Supervisor",
            LecturerFeedLogin = "admin",
            PresentationFeedLogin = "admin",
            PresentationFeedPass = "Supervisor"
        };

        public string DeviceAddress { get => SetupState.PresentationFeed; set => SetupState.PresentationFeed = value; }
        public string Login { get => SetupState.PresentationFeedLogin; set => SetupState.PresentationFeedLogin = value; }
        public string Password { get => SetupState.PresentationFeedPass; set => SetupState.PresentationFeedPass = value; }
        public string DeviceAddress2 { get => SetupState.LecturerFeed; set => SetupState.LecturerFeed = value; }

        public string Login2 { get => SetupState.LecturerFeedLogin; set => SetupState.LecturerFeedLogin = value; }
        public string Password2 { get => SetupState.LecturerFeedPass; set => SetupState.LecturerFeedPass = value; }

        public IVideoSource VideoSource => _mainWindowModel.VideoSource;
        public IVideoSource VideoSource2 => _mainWindowModel.VideoSource2;
        public bool ShowVideoSource1 = true;
        public bool ShowVideoSource2 = true;

        public RelayCommand StartClickCommand { get; }
        public RelayCommand OpenWebInterface { get; }
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
            SetupStateTracker.Tracker.Track(SetupState);
            StateHasChanged += () =>
            {
                SetupStateTracker.Tracker.Persist(SetupState);
            };
            _mainWindowModel = mainWindowModel ?? throw new ArgumentNullException(nameof(mainWindowModel));

            StartClickCommand = new RelayCommand(OnStartButtonClick, () => _startButtonEnabled);
            OpenWebInterface = new RelayCommand(OpenWebInterfaceMethod, () => _startButtonEnabled);
            StopClickCommand = new RelayCommand(OnStopButtonClick, () => _stopButtonEnabled);
            ClosingCommand = new RelayCommand<CancelEventArgs>(OnClosing);
            serverTask = Task.Run(() =>
            {
                StudioServer.Program.RunServer(this);
            });
        }

        private void OpenWebInterfaceMethod()
        {
            var info = new ProcessStartInfo()
            {
                FileName = "https://localhost:5001",
                UseShellExecute = true
            };
            Process.Start(info);
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
            var cp2 = useSetupState ? GetConparam(SetupState.LecturerFeed, SetupState.LecturerFeedLogin, SetupState.LecturerFeedPass) : GetConparam(DeviceAddress2, Login2, Password2);

            _mainWindowModel.Start(cp1, cp2);
            _mainWindowModel.StatusChanged += MainWindowModelOnStatusChanged;

            _startButtonEnabled = false;
            StartClickCommand.RaiseCanExecuteChanged();
            _stopButtonEnabled = true;
            StopClickCommand.RaiseCanExecuteChanged();
        }
        private static void KillProcessAndChildrens(int pid)
        {
            ManagementObjectSearcher processSearcher = new ManagementObjectSearcher
              ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection processCollection = processSearcher.Get();

            // We must kill child processes first!
            if (processCollection != null)
            {
                foreach (ManagementObject mo in processCollection)
                {
                    KillProcessAndChildrens(Convert.ToInt32(mo["ProcessID"])); //kill child processes(also kills childrens of childrens etc.)
                }
            }

            // Then kill parents.
            try
            {
                Process proc = Process.GetProcessById(pid);
                if (!proc.HasExited) proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }
        private void OnStopButtonClick()
        {
            if (ffmpegRecordingProcess != null)
                KillProcessAndChildrens(ffmpegRecordingProcess.Id);
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

        public async Task PrepareSetup()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                StateHasChanged?.Invoke();
            });
        }

        public async Task Run()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
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
        public string MakeUnique(string dirPath, string name)
        {
            string fileName = Path.GetFileNameWithoutExtension(name);
            string fileExt = Path.GetExtension(name);

            for (int i = 1; ; ++i)
            {
                if (!File.Exists(dirPath + name))
                    return name;

                name = Path.Combine(fileName + i + fileExt);
            }
        }
        void RunFFMpeg()
        {
            if (ffmpegRecordingProcess != null)
                return;
            int exitCode;
            ProcessStartInfo processInfo;
            Process process;
            string input = File.ReadAllText("ffmpeg_record.bat");
            var curDir = Directory.GetCurrentDirectory();
            var dir = curDir + SetupState.FolderForVideos;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var now = DateTime.Now;
            var fileName = $"{now.Day}_{now.Month}_{now.Year}__{now.Hour}_{now.Minute}_{now.Second}__recording.mkv";
            processInfo = new ProcessStartInfo("ffmpeg.exe", @$"-f dshow -i video=""screen-capture-recorder"":audio=""virtual-audio-capturer"" -c:v libx264 -crf 0 -preset ultrafast {MakeUnique(dir, fileName)} ");
            processInfo.CreateNoWindow = true;
            processInfo.WorkingDirectory = dir;
            processInfo.WindowStyle = ProcessWindowStyle.Hidden;
           // processInfo.UseShellExecute = false;
            // *** Redirect the output ***
        //    processInfo.RedirectStandardError = true;
        //    processInfo.RedirectStandardOutput = true;
            ffmpegRecordingProcess = Process.Start(processInfo);
            Task.Run(() =>
            {
                try
                {
                    ffmpegRecordingProcess.WaitForExit();

                    // *** Read the streams ***
                    // Warning: This approach can lead to deadlocks, see Edit #2
        //            string output = ffmpegRecordingProcess.StandardOutput.ReadToEnd();
        //            string error = ffmpegRecordingProcess.StandardError.ReadToEnd();

                    exitCode = ffmpegRecordingProcess.ExitCode;

                 //   Console.WriteLine("output>>" + (String.IsNullOrEmpty(output) ? "(none)" : output));
               //     Console.WriteLine("error>>" + (String.IsNullOrEmpty(error) ? "(none)" : error));
                //    Console.WriteLine("ExitCode: " + exitCode.ToString(), "ExecuteCommand");
                    
                    KillProcessAndChildrens(ffmpegRecordingProcess.Id);
                }
                finally
                {
                    ffmpegRecordingProcess = null;
                }
            });
        }
        public async Task BeginRecording()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                this.
                useSetupState = true;
                OnStartButtonClick();
                useSetupState = false;
                IsRecording = true;
                StateHasChanged?.Invoke();
            });
        }

        public async Task StopRecording()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                IsRecording = false;
                OnStopButtonClick();
                StateHasChanged?.Invoke();
            });
        }

        public async Task<string[]> GetFilesForSession(string name)
        {
            var ppath = SetupState.RootFS + SetupState.FolderForVideos;
            if (!Directory.Exists(ppath))
                return Array.Empty<string>();
            return Directory.GetFiles(ppath, $"*.mkv").Select(x=>Path.GetFileName(x)).ToArray();
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
                switch (CurrentState)
                {
                    case SessionState.StreamerLike:
                        HideAndShowVideosAndFuckOff.ShowViewViewBackground = true;
                        HideAndShowVideosAndFuckOff.ShowViewViewWithChromakey = true;
                        HideAndShowVideosAndFuckOff.AsStreamer = true;
                        HideAndShowVideosAndFuckOff.OnlyLecturer = false;
                        break;
                    case SessionState.SideBySide:
                        HideAndShowVideosAndFuckOff.ShowViewViewBackground = true;
                        HideAndShowVideosAndFuckOff.ShowViewViewWithChromakey = true;
                        HideAndShowVideosAndFuckOff.AsStreamer = false;
                        HideAndShowVideosAndFuckOff.OnlyLecturer = false;
                        break;
                    case SessionState.OnlyPresentation:
                        HideAndShowVideosAndFuckOff.ShowViewViewBackground = true;
                        HideAndShowVideosAndFuckOff.ShowViewViewWithChromakey = false;
                        HideAndShowVideosAndFuckOff.AsStreamer = false;
                        HideAndShowVideosAndFuckOff.OnlyLecturer = false;
                        break;
                    case SessionState.OnlyLecturer:
                        HideAndShowVideosAndFuckOff.ShowViewViewBackground = false;
                        HideAndShowVideosAndFuckOff.ShowViewViewWithChromakey = true;
                        HideAndShowVideosAndFuckOff.AsStreamer = false;
                        HideAndShowVideosAndFuckOff.OnlyLecturer = true;
                        break;
                }
                HideAndShowVideosAndFuckOff.Update?.Invoke();
                StateHasChanged?.Invoke();
            });
        }
    }
}