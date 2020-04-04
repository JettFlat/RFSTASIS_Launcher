﻿using System;
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
            if (quality== "Low")
                return 0;
            return 0;
        }
    }
}
