using SimpleRtspPlayer.GUI.ViewModels;
using StudioServer;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SimpleRtspPlayer.GUI.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent(); 
            this.KeyDown += new KeyEventHandler(OnButtonKeyDown);
        }

        private void OnButtonKeyDown(object sender, KeyEventArgs e)
        {
            var ctx = this.DataContext as MainWindowViewModel;
            var key = e.Key;
            Task.Run(async () =>
            {
                switch (key)
                {
                    case Key.NumPad7:
                        await ctx.ChangeSessionState(SessionState.OnlyLecturer);
                        break;
                    case Key.NumPad8:
                        await ctx.ChangeSessionState(SessionState.SideBySide);
                        break;
                    case Key.NumPad9:
                        await ctx.ChangeSessionState(SessionState.OnlyPresentation);
                        break;
                    case Key.NumPad4:
                        await ctx.ChangeSessionState(SessionState.StreamerLike);
                        break;
                    case Key.NumPad5:
                        await ctx.Stop();
                        await ctx.Run();
                        await ctx.BeginRecording();
                        break;
                    case Key.NumPad6:
                        await ctx.StopRecording();
                        await ctx.Stop();
                        break;
                }
            }); 
            
        }
    }
}