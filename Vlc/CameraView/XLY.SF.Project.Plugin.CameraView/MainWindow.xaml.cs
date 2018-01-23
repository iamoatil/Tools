using System;
using System.IO;
using System.Windows;


namespace XLY.SF.Project.CameraView
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CameraViewModel vm=new CameraViewModel();
            vm.DevLooksManager.Initialize(Path.GetFullPath("DeviceLooks"));
            this.DataContext = vm;
            
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            takePhoto.player.Stop(true);
        }
    }

    public class Program
    {
        [STAThread]
        static void Main()
        {
            MainWindow window = new MainWindow();
            window.ShowDialog();
        }
    }
    
}
