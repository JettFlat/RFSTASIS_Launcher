using System;
using System.Collections.Generic;
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
        public int DownloadProgres
        {
            get => gameClient.Servak.DownloadProgres;
            set
            {

            }
        }
        public string DisplayedImage
        {
            get
            {
                if (System.IO.File.Exists("System/bg.jpg"))
                { }
                    return @"System/bg.jpg";
            }
        }
        public string DownloadedFileName => gameClient.Servak.DownloadedFileName;
        public bool IsServerOnline => gameClient.IsServerOnline;
        public string ServerStatus => gameClient.ServerStatus;
        Visibility _IsSettingsVisible = Visibility.Collapsed;
        public Visibility IsSettingsVisible
        {
            get => _IsSettingsVisible;
            set
            {
                _IsSettingsVisible = value;
                OnPropertyChanged();
            }
        }
        Visibility _IsLoginVisible = Visibility.Visible;
        public Visibility IsLoginVisible
        {
            get => _IsLoginVisible;
            set
            {
                _IsLoginVisible = value;
                OnPropertyChanged();
            }
        }
        public List<string> Resolutions { get; } = GameClient.ClientSettings.GetResolutions();
        string _SelectedResolution = gameClient.clientSettings.engineSettings.Resolution;
        public string SelectedResolution
        {
            get => _SelectedResolution;
            set
            {
                _SelectedResolution = value;
                OnPropertyChanged();
                gameClient.clientSettings.engineSettings.Resolution = _SelectedResolution;
                gameClient.clientSettings.Serialize();
            }
        }
        public List<string> Quality { get; } = new List<string> { "Very High", "High", "Medium", "Low" };
        public List<string> GlowQuality { get; } = new List<string> { "High", "Medium", "Low" };
        string _SelectedTexture = GetTextSettings(gameClient.clientSettings.engineSettings.TextureDetail);
        public string SelectedTexture
        {
            get => _SelectedTexture;
            set
            {
                _SelectedTexture = value;
                OnPropertyChanged();
                gameClient.clientSettings.engineSettings.TextureDetail = SetTextSettings(_SelectedTexture);
                gameClient.clientSettings.Serialize();
            }
        }

        string _SelectedDLight = GetTextSettings(gameClient.clientSettings.engineSettings.DynamicLight);
        public string SelectedDLight
        {
            get => _SelectedDLight;
            set
            {
                _SelectedDLight = value;
                OnPropertyChanged();
                gameClient.clientSettings.engineSettings.DynamicLight = SetTextSettings(_SelectedDLight);
                gameClient.clientSettings.Serialize();
            }
        }
        string _SelectedGEffect = GetTextSettings(gameClient.clientSettings.engineSettings.GlowEffect);
        public string SelectedGEffect
        {
            get => _SelectedGEffect;
            set
            {
                _SelectedGEffect = value;
                OnPropertyChanged();
                gameClient.clientSettings.engineSettings.GlowEffect = SetTextSettings(_SelectedGEffect);
                gameClient.clientSettings.Serialize();
            }
        }
        string _SelectedShadow = GetTextSettings(gameClient.clientSettings.engineSettings.ShadowDetail);
        public string SelectedShadow
        {
            get => _SelectedShadow;
            set
            {
                _SelectedShadow = value;
                OnPropertyChanged();
                gameClient.clientSettings.engineSettings.ShadowDetail = SetTextSettings(_SelectedShadow);
                gameClient.clientSettings.Serialize();
            }
        }
        string _SelectedGamma = gameClient.clientSettings.engineSettings.Gamma.ToString().Replace(',', '.');
        public string SelectedGamma
        {
            get => _SelectedGamma;
            set
            {
                _SelectedGamma = value;
                OnPropertyChanged();
                var gamma = Decimal.Parse(_SelectedGamma.Replace('.', ','));
                gameClient.clientSettings.engineSettings.Gamma = gamma;
                gameClient.clientSettings.Serialize();
            }
        }
        public List<string> Gammalvls { get; } = new List<string> { "0.8", "1.0", "1.2", "1.4", "1.6", "1.8" };
        bool _IsDTextures = gameClient.clientSettings.engineSettings.DetailedTextures;
        public bool IsDTextures
        {
            get => _IsDTextures;
            set
            {
                _IsDTextures = value;
                OnPropertyChanged();
                gameClient.clientSettings.engineSettings.DetailedTextures = _IsDTextures;
                gameClient.clientSettings.Serialize();
            }
        }
        bool _IsMAcceleration = gameClient.clientSettings.engineSettings.MouseAcceleration;
        public bool IsMAcceleration
        {
            get => _IsMAcceleration;
            set
            {
                _IsMAcceleration = value;
                OnPropertyChanged();
                gameClient.clientSettings.engineSettings.MouseAcceleration = _IsMAcceleration;
                gameClient.clientSettings.Serialize();
            }
        }

        public GameClient.ClientSettings clientSettings
        {
            get => gameClient.clientSettings;
            set
            {
                clientSettings = value;
                gameClient.clientSettings = clientSettings;
                OnPropertyChanged();
                gameClient.clientSettings.Serialize();
            }
        }
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

        public RelayCommand OpenSettings => new RelayCommand(o =>
        {

            IsSettingsVisible = Visibility.Visible;
            IsLoginVisible = Visibility.Collapsed;
        });
        public RelayCommand OpenLogin => new RelayCommand(o =>
        {

            IsSettingsVisible = Visibility.Collapsed;
            IsLoginVisible = Visibility.Visible;
        });
        public RelayCommand SaveSettings => new RelayCommand(o =>
        {
            gameClient.clientSettings.Serialize();
            gameClient.clientSettings.WriteEngineSettings();
        });
        static string GetTextSettings(int i)
        {
            if (i > 2)
                return "Very High";
            if (i == 2)
                return "High";
            if (i == 1)
                return "Medium";
            if (i < 1)
                return "Low";
            return "null";
        }
        static int SetTextSettings(string quality)
        {
            if (quality == "Very High")
                return 3;
            if (quality == "High")
                return 2;
            if (quality == "Medium")
                return 1;
            if (quality == "Low")
                return 0;
            return 0;
        }
    }
}
