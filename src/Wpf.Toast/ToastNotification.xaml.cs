using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace Wpf.Toast
{
    /// <summary>
    /// Interaction logic for ucToast.xaml
    /// </summary>
    public partial class ToastNotification : Window
    {
        public ToastNotification()
        {
            InitializeComponent();
            grdMain.DataContext = this;

            if(WpfDesignerHelper.Designer.Active)
            {
                Notification = new TextNotification { Text = "Test Test" };
            }
        }

        public static ToastNotification ShowDialog(Window owner, Notification notification)
        {
            var dlg = new ToastNotification();
            dlg.Owner = owner;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dlg.Notification = notification;
            dlg.ShowDialog();
            return dlg;
        }

        public Notification Notification
        {
            get { return (Notification)GetValue(NotificationProperty); }
            set { SetValue(NotificationProperty, value); }
        }
        public static readonly DependencyProperty NotificationProperty =
            DependencyProperty.Register(nameof(Notification), typeof(Notification), typeof(ToastNotification), new PropertyMetadata(null));

        private void btnDismiss_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
    public class ToastNotificationTemplateSelector : DataTemplateSelector
    {
        public DataTemplate DefaultTemplate { get; set; }
        public DataTemplate TextNotificationTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var notification = item as Notification;
            if(notification != null)
            {
                switch(notification.Type)
                {
                    case "Text":
                        return TextNotificationTemplate;
                }
            }

            return DefaultTemplate;
        }
    }
}
