using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Wpf.Toast
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ShowNotificationService service;
        Timer _timer;
        public MainWindow()
        {
            InitializeComponent();

            service = new ShowNotificationService();

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var notifs = await service.GetNotificationsAsync();

                _timer = new Timer(2000);
                _timer.Elapsed += _timer_Elapsed;
                _timer.AutoReset = false;
                _timer.Start();
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() => 
                ToastNotification.ShowDialog(this, new TextNotification { Text = "Blah Blah Blah" }, 5)
            );
        }
    }
}
