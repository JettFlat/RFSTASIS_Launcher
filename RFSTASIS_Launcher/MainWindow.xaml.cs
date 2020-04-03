using MiniLauncher.Data;
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
        private NetworkClient networkClient;
        public MainWindow()
        {
            InitializeComponent();
            InitializeNetwork();
            //Task.Run(() =>
            //{
            //    Thread.Sleep(5000);
            //    networkClient.DoLogin("jett", "NVcjk347dl");
            //});
            (new Thread(() =>
            {
                Thread.Sleep(5000);
                networkClient.DoLogin("jett", "NVcjk347dl");
            })).Start();
        }
        private void InitializeNetwork()
        {
            var serverCfg = Model.SettingsCur.Servercfg;
            string ip = serverCfg.LogginAddress.Split(':')[0];
            int port = int.Parse(serverCfg.LogginAddress.Split(':')[1]);
            networkClient = new NetworkClient(ip, port);
            networkClient.OnError += NetworkClient_OnError;
            networkClient.OnConnected += NetworkClient_OnConnected;
            networkClient.ClientEvents += NetworkClient_ClientEvents;
            (new Thread(() =>
            {
                networkClient.StartClient();
            })).Start();
        }
        private void NetworkClient_OnError(object sender, EventArgs e)
        {
            ChangeStatus(false);
            EnableLoginBtn(false);
        }
        private void EnableLoginBtn(bool state)
        {
            //this.Dispatcher.Invoke((Action)(() =>
            //{
            //   // bLogin.IsEnabled = state;//TODO Упарвление кнопкой
            //}));
        }
        private void ChangeStatus(bool ok)
        {//TODO меняет статус серверов в gui
            //this.Dispatcher.Invoke((Action)(() =>
            //{
            //    //lbStatus.Content = ok ? "Online" : "Offline";
            //}))
        }
        private void NetworkClient_OnConnected(object sender, EventArgs e)
        {
            ChangeStatus(true);
        }
        private void NetworkClient_ClientEvents(object sender, NetworkClientEventArgs e)
        {
            switch (e.CState)
            {
                case NetworkClientEventArgs.Callback.CRYPTO_KEY_INFORM:
                    EnableLoginBtn(true);
                    break;
                case NetworkClientEventArgs.Callback.LOGIN_ACCOUNT_WRONG_LOGIN:
                    EnableLoginBtn(true);
                    MessageBox.Show("Wrong Login", "Error");
                    break;
                case NetworkClientEventArgs.Callback.LOGIN_ACCOUNT_WRONG_PW:
                    EnableLoginBtn(true);
                    MessageBox.Show("Wrong Password", "Error");
                    break;
                case NetworkClientEventArgs.Callback.LOGIN_ACCOUNT_SERVER_CLOSED:
                    EnableLoginBtn(true);
                    MessageBox.Show("Login Server Closed, Contact Admin", "Error");
                    break;
                case NetworkClientEventArgs.Callback.LOGIN_ACCOUNT_BANNED:
                    MessageBox.Show("Account Blocked", "Error");
                    break;
                case NetworkClientEventArgs.Callback.SERVER_LIST_INFORM:
                    FillServerList(e.Servers);
                    break;
                case NetworkClientEventArgs.Callback.SERVER_SESSION_RESULT:
                    RunGame(e.DefaultSet);
                    break;
            }
        }
        private void FillServerList(List<ServerState> _serverList)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                networkClient.SelectWordlRequest(Model.SettingsCur.Servercfg.ServerIndexSelect);
            }));

        }
        private void RunGame(Default_Set defaultSet)
        {
            defaultSet.nation_code = 3;
            ClientRunHelper.WriteTmp(@"System/DefaultSet.tmp", defaultSet);
            ClientRunHelper.RunClient(@"RF_Online.bin");
            networkClient.StopListen();
            Environment.Exit(0);
        }
    }
}
