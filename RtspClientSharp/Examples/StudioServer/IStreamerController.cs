using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StudioServer
{
    public enum SessionState
    {
        StreamerLike,
        SideBySide,
        OnlyPresentation,
        OnlyLecturer
    }
    public class SetupState
    {
        public static (byte r, byte g, byte b) ChromakeyColor;
        public static bool UseMicInput = true;
        public static int DeviceNumber = -1;
        public string RootFS => Directory.GetCurrentDirectory();//@"C:\Users\Виталий\AppData\Local\Packages\CanonicalGroupLimited.UbuntuonWindows_79rhkp1fndgsc\LocalState\rootfs\".Replace(@"\", @"/");
        public string FolderForVideos = "/";
        public string LecturerFeedPass;
        public string LecturerFeedLogin;
        public string LecturerFeed;
        public string PresentationFeedPass;
        public string PresentationFeedLogin;
        public string PresentationFeed;
    }


    public interface IStreamerController
    {
        string CurrentSession { get; }
        bool IsActive { get; }
        bool IsRecording { get; }
        SessionState CurrentState { get; }

        Task PrepareSetup();
        Task Run();
        Task Stop();
        Task BeginRecording();
        Task StopRecording();
        Task<string[]> GetFilesForSession(string name);
        Task SetRecordingSession(string name);
        Task ChangeSessionState(SessionState newState); 
        SetupState SetupState { get; }
        event Action StateHasChanged;
    }

    public class GStreamerController : IStreamerController
    {
        static string _snowMixIp = "127.0.0.1";
        static string _snowMixPort = "8855";
        SessionState _cs;
        public SessionState CurrentState { get => _cs; private set { _cs = value; StateHasChanged?.Invoke(); } }
        public string CurrentSession { get; private set; }
        public SetupState SetupState { get; private set; } = new SetupState()
        {
            FolderForVideos = "/home/studio/videos/",
            LecturerFeed = "\"rtsp://192.168.1.70/0\"",//"\"rtsp://admin:Supervisor@192.168.1.70:554/cam/realmonitor?channel=1&subtype=0&unicast=true&proto=Onvif\"",
            PresentationFeed = "\"rtsp://192.168.1.70/0\""//"\"rtsp://admin:Supervisor@192.168.1.70:554/cam/realmonitor?channel=1&subtype=0&unicast=true&proto=Onvif\""//"\"rtsp://192.168.1.71/0\""
        };
        bool _isActive;
        bool _isRecording;
        public bool IsActive { get => _isActive; private set { _isActive = true; StateHasChanged?.Invoke(); } }
        public bool IsRecording { get => _isRecording; private set { _isRecording = true; StateHasChanged?.Invoke(); } }

        private static void Read(StreamReader reader)
        {
            new Thread(() =>
            {
                while (true)
                {
                    int current;
                    while ((current = reader.Read()) >= 0)
                        Console.Write((char)current);
                }
            }).Start();
        }
        Process _bash;
        Process _controlBash;

        public event Action StateHasChanged;

        async Task InitBash()
        {
            if (_bash != null)
                return;
            {

                ProcessStartInfo startInfo = new ProcessStartInfo(@"ubuntu");
                //startInfo.Arguments = "-ttty localhost";
                startInfo.CreateNoWindow = true;
                startInfo.ErrorDialog = false;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardInput = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                Process process = new Process();
                process.StartInfo = startInfo;
                process.Start();
                Read(process.StandardOutput);
                Read(process.StandardError);
                _bash = process;
                _bash.StandardInput.Write("cd /opt/studio/scripts/\n");
                _bash.StandardInput.Write("export DISPLAY=:0\n");
            }
            {

                ProcessStartInfo startInfo = new ProcessStartInfo(@"ubuntu");
                //startInfo.Arguments = "-ttty localhost";
                startInfo.CreateNoWindow = true;
                startInfo.ErrorDialog = false;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardInput = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                Process process = new Process();
                process.StartInfo = startInfo;
                process.Start();
                Read(process.StandardOutput);
                Read(process.StandardError);
                _controlBash = process;
                _controlBash.StandardInput.Write("cd /opt/studio/scripts/\n");
            }
        }
        public async Task StartStudio()
        {
            IsActive = true;
            Console.WriteLine($"{SetupState.FolderForVideos} {SetupState.LecturerFeed} {SetupState.PresentationFeed}");
            _bash.StandardInput.Write($"sudo studio_start {SetupState.LecturerFeed} {SetupState.PresentationFeed} {CurrentSession} \"{SetupState.FolderForVideos}\"\n");
            await ChangeSessionState(SessionState.OnlyLecturer);
        }
        public async Task StopStudio()
        {
            IsActive = false;
            _bash.StandardInput.Write($"sudo -S studio_stop\n");
            _bash.StandardInput.Write("1234\n");
        }
        public async Task RestartStudio()
        {
            await StopStudio();
            await StartStudio();
        }
        public async Task BeginRecording()
        {
            await InitBash();
            await RestartStudio();

            IsRecording = true;
        }
        void SendCommand(string cmd)
        {
            _controlBash.StandardInput.Write("echo \"Set scene\"\n");
            _controlBash.StandardInput.Write($"echo \"tcl eval {cmd}\" | nc {_snowMixIp} {_snowMixPort} -q 0\n");
        }
        public async Task ChangeSessionState(SessionState newState)
        {
            await InitBash();
            switch (newState)
            {
                case SessionState.StreamerLike:
                    SendCommand("SceneSetState 1 1 0");
                    break;
                case SessionState.SideBySide:
                    SendCommand("SceneSetState 2 1 0");
                    break;
                case SessionState.OnlyPresentation:
                    SendCommand("SceneSetState 3 1 0");
                    break;
                case SessionState.OnlyLecturer:
                    SendCommand("SceneSetState 4 1 0");
                    break;
            }
            CurrentState = newState;
        }

        public async Task<string[]> GetFilesForSession(string name)
        {
            //await InitBash();
            var ppath = SetupState.RootFS + SetupState.FolderForVideos;
            if (!Directory.Exists(ppath))
                return Array.Empty<string>();
            return Directory.GetFiles(ppath, $"*{name}.avi");
        }

        public async Task PrepareSetup()
        {
            await InitBash();

        }

        public async Task Run()
        {
            await InitBash();
        }

        public async Task SetRecordingSession(string name)
        {
            await InitBash();
            CurrentSession = name;
            await RestartStudio();

        }

        public async Task Stop()
        {
            await StopBash();
        }

        private Task StopBash()
        {
            _bash.Kill();
            return Task.CompletedTask;
        }

        public async Task StopRecording()
        {
            await InitBash();
            await StopStudio();
            IsRecording = false;
        }
    }
}
