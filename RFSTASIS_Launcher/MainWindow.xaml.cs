using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RFSTASIS_Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                Model.GClient.InitializeNetwork();
                GameClient.ClientSettings.GetResolutions();
                Task.Run(()=>Model.GClient.GetUpdates());
            }
            catch (Exception exc)
            {
                exc.Write();
                exc.ShowMessage();
            }
        }

        private void StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}
