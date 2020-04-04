using System;
using System.Windows;

namespace RFSTASIS_Launcher
{
    public class VM : VMBase
    {
        static readonly GameClient gameClient = Model.GClient;
        public VM() : base()
        {
            gameClient.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "ServerMessage")
                {
                    MessageBox.Show(gameClient.ServerMessage, "Error");
                }
                OnPropertyChanged(e.PropertyName);
            };
        }
        public bool IsServerOnline => gameClient.IsServerOnline;
        public string ServerStatus => gameClient.ServerStatus;
        public bool IsResiveNow => !gameClient.IsResiveNow;
        bool _WindowMode = !gameClient.clientSettings.engineSettings.FullScreen;
        public bool WindowMode
        {
            get => _WindowMode;
            set
            {
                _WindowMode = value;
                gameClient.clientSettings.engineSettings.FullScreen = !_WindowMode;
                OnPropertyChanged();
                gameClient.clientSettings.Serialize();
            }
        }

        public string NickName
        {
            get => gameClient.clientSettings.NickName;
            set
            {
                gameClient.clientSettings.NickName = value;
                OnPropertyChanged();
                gameClient.clientSettings.Serialize();
            }
        }
        public RelayCommand Start => new RelayCommand(o =>
        {
            var passwordBox = o as System.Windows.Controls.PasswordBox;
            gameClient.clientSettings.WriteEngineSettings();
            gameClient.clientSettings.Password = passwordBox.Password;
            gameClient.Start();
        });
    }
}
