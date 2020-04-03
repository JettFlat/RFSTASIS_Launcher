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
        public string ServerStatus => gameClient.ServerStatus;
        bool _flag = false;
        public bool Flag
        {
            get => _flag;
            set
            {
                _flag = value;
                OnPropertyChanged("FlagText");
            }
        }
        public string FlagText
        {
            get
            {
                if (Flag)
                    return "true";
                else
                    return "false";
            }
        }
        public RelayCommand TestCommand => new RelayCommand(o =>
        { Flag = true; });
    }
}
