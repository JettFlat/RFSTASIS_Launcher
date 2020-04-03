using System;
namespace RFSTASIS_Launcher
{
    public class VM : VMBase
    {
        static readonly GameClient gameClient = Model.GClient;
        public VM() : base()
        {
            gameClient.PropertyChanged += (s, e) => { OnPropertyChanged(e.PropertyName); };
        }
        public bool IsServerOnline => gameClient.IsServerOnline;
        public string ServerStatus => gameClient.ServerStatus;

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
            gameClient.clientSettings.Password = passwordBox.Password;
            gameClient.Start();
        });
    }
}
