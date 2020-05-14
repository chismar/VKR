using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StudioServer.Data
{
    public class StudioControl : IDisposable
    {
        Task readTask;
        CancellationTokenSource _cts;
        public SetupState SetupState => StreamerController.SetupState;
        public StudioControl()
        {
            _cts = new CancellationTokenSource();
            readTask = Task.Run(async () =>
            {
               
            });
        }
        public void Dispose()
        {
            if (!_cts.IsCancellationRequested)
                _cts.Cancel();
        }
        public IStreamerController StreamerController;
        public async Task<string> StartStudio(SetupState state)
        {
            await StreamerController.Run();
            await StreamerController.SetRecordingSession("TestSession");
            //await StreamerController.BeginRecording();
            return "Done";
        }

        public async Task<string[]> GetFiles()
        {
            return await StreamerController.GetFilesForSession(StreamerController.CurrentSession);
        }
        public async Task<string> StopStudio()
        {
            await StreamerController.StopRecording();
            await StreamerController.Stop();
            return "Done";
        }
        public async Task<string> ChangeStudioScene(string towards)
        {
            if (towards == "Presenter")
                await StreamerController.ChangeSessionState(SessionState.OnlyLecturer);
            else if (towards == "Presentation")
                await StreamerController.ChangeSessionState(SessionState.OnlyPresentation);
            else if (towards == "SideBySide")
                await StreamerController.ChangeSessionState(SessionState.SideBySide);
            else if (towards == "Streamer")
                await StreamerController.ChangeSessionState(SessionState.StreamerLike);
            //var path = Path.GetFullPath(Path.Combine($"../OBSCommand/OBSCommand.exe"));
            //Process.Start(path, $"/scene={towards}");
            return "Done";
        }

    }
}
