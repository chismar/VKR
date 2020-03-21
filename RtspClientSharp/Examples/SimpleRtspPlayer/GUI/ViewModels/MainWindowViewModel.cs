using System;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;
using System.Windows;
using GalaSoft.MvvmLight.Command;
using RtspClientSharp;
using SimpleRtspPlayer.GUI.Models;

namespace SimpleRtspPlayer.GUI.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private const string RtspPrefix = "rtsp://";
        private const string HttpPrefix = "http://";

        private string _status = string.Empty;
        private readonly IMainWindowModel _mainWindowModel;
        private bool _startButtonEnabled = true;
        private bool _stopButtonEnabled;

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

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindowViewModel(IMainWindowModel mainWindowModel)
        {
            _mainWindowModel = mainWindowModel ?? throw new ArgumentNullException(nameof(mainWindowModel));
            
            StartClickCommand = new RelayCommand(OnStartButtonClick, () => _startButtonEnabled);
            StopClickCommand = new RelayCommand(OnStopButtonClick, () => _stopButtonEnabled);
            ClosingCommand = new RelayCommand<CancelEventArgs>(OnClosing);
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
            var cp1 = GetConparam(DeviceAddress, Login, Password);
            var cp2= GetConparam(DeviceAddress2, Login2, Password2);

            _mainWindowModel.Start(cp1, cp2);
            _mainWindowModel.StatusChanged += MainWindowModelOnStatusChanged;

            _startButtonEnabled = false;
            StartClickCommand.RaiseCanExecuteChanged();
            _stopButtonEnabled = true;
            StopClickCommand.RaiseCanExecuteChanged();
        }

        private void OnStopButtonClick()
        {
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
    }
}