using System;
namespace RFSTASIS_Launcher
{
    public class VM : VMBase
    {
        readonly GameClient gameClient = Model.GClient;
        public VM() : base()
        {
            gameClient.PropertyChanged += (s, e) => { OnPropertyChanged(e.PropertyName); };
        }
        public string Text => "Flex";
        public bool IsServerOnline => gameClient.IsServerOnline;
        public string ServerStatus
        {
            get
            {
                if (IsServerOnline)
                    return "Online";
                else
                    return "Offline";
            }
        }
        public RelayCommand Start => new RelayCommand(o =>{ gameClient.Start(); });
    }
}
