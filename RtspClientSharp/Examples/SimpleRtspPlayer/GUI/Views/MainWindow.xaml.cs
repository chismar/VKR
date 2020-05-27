using Gma.System.MouseKeyHook;
using SimpleRtspPlayer.GUI.ViewModels;
using StudioServer;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace SimpleRtspPlayer.GUI.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private IKeyboardMouseEvents m_GlobalHook;
        public MainWindow()
        {
            InitializeComponent(); 
            m_GlobalHook = Hook.GlobalEvents();

            m_GlobalHook.KeyUp += M_GlobalHook_KeyUp; 
        }

        private void M_GlobalHook_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            var ctx = this.DataContext as MainWindowViewModel;
            var key = e.KeyCode;
            Task.Run(async () =>
            {
                switch (key)
                {
                    case Keys.NumPad7:
                        await ctx.ChangeSessionState(SessionState.OnlyLecturer);
                        break;
                    case Keys.NumPad8:
                        await ctx.ChangeSessionState(SessionState.SideBySide);
                        break;
                    case Keys.NumPad9:
                        await ctx.ChangeSessionState(SessionState.OnlyPresentation);
                        break;
                    case Keys.NumPad4:
                        await ctx.ChangeSessionState(SessionState.StreamerLike);
                        break;
                    case Keys.NumPad5:
                        await ctx.Stop();
                        await ctx.Run();
                        await ctx.BeginRecording();
                        break;
                    case Keys.NumPad6:
                        await ctx.StopRecording();
                        await ctx.Stop();
                        break;
                }
            });
        }

    }
}