﻿using MiniLauncher.Data;
using MiniLauncher.Helper;
using MiniLauncher.Network;
using MiniLauncher.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RFSTASIS_Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            GameClient.InitializeNetwork();
            GameClient.Start();
        }
       
        private void ChangeStatus(bool ok)
        {//TODO меняет статус серверов в gui
            //this.Dispatcher.Invoke((Action)(() =>
            //{
            //    //lbStatus.Content = ok ? "Online" : "Offline";
            //}))
        }
       
       
    }
}
