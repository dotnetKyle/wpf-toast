using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
        DoubleAnimationUsingKeyFrames appearAnimation;
        DoubleAnimationUsingKeyFrames disappearAnimation;

        Storyboard appearStoryBoard;
        Storyboard disappearStoryBoard;

        public ToastNotification()
        {
            InitializeComponent();
            grdMain.DataContext = this;

            if(WpfDesignerHelper.Designer.Active)
            {
                Notification = new TextNotification { Text = "Test Test" };
            }

            Loaded += ToastNotification_Loaded;
        }


        public int VisibilityInSeconds
        {
            get { return (int)GetValue(VisibilityInSecondsProperty); }
            set { SetValue(VisibilityInSecondsProperty, value); }
        }
        public static readonly DependencyProperty VisibilityInSecondsProperty =
            DependencyProperty.Register(nameof(VisibilityInSeconds), typeof(int), typeof(ToastNotification), new PropertyMetadata(1));




        private void ToastNotification_Loaded(object sender, RoutedEventArgs e)
        {
            // Configure animations on load
            appearStoryBoard = new Storyboard();
            disappearStoryBoard = new Storyboard();

            appearAnimation = new DoubleAnimationUsingKeyFrames
            {
                KeyFrames = new DoubleKeyFrameCollection
                {
                    new SplineDoubleKeyFrame{ KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0 }, // from invisible
                    new SplineDoubleKeyFrame{ KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1)), Value = 1 } // to visible
                }
            };
            disappearAnimation = new DoubleAnimationUsingKeyFrames
            {
                KeyFrames = new DoubleKeyFrameCollection
                {
                    new SplineDoubleKeyFrame{ KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 1 }, // from visible
                    new SplineDoubleKeyFrame{ KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1)), Value = 0 } // back to invisible
                }
            };

            // configure appear animation
            appearStoryBoard.Children.Add(appearAnimation);
            Storyboard.SetTarget(appearAnimation, this);
            Storyboard.SetTargetProperty(appearAnimation, new PropertyPath(OpacityProperty));
            appearStoryBoard.Completed += AppearStoryBoard_Completed;

            // configure disappear animation
            disappearStoryBoard.Children.Add(disappearAnimation);
            Storyboard.SetTarget(disappearAnimation, this);
            Storyboard.SetTargetProperty(disappearAnimation, new PropertyPath(OpacityProperty));
            disappearStoryBoard.Completed += DisappearStoryBoard_Completed;

            recalculatePosition();

            // On load, begin appear
            appearStoryBoard.Begin();
            Debug.WriteLine("Starting appaear");
        }

        private async void AppearStoryBoard_Completed(object sender, EventArgs e)
        {
            try
            {
                // once appear is finished, beign disappear
                Debug.WriteLine("Starting disappaear");
                
                await Task.Delay(TimeSpan.FromSeconds(VisibilityInSeconds));

                disappearStoryBoard.Begin();
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void DisappearStoryBoard_Completed(object sender, EventArgs e)
        {
            Debug.WriteLine("Close");
            this.Close();
        }

        public static ToastNotification ShowDialog(Window owner, Notification notification)
            => ShowDialog(owner, notification, 1);
        public static ToastNotification ShowDialog(Window owner, Notification notification, int visibilityInSeconds)
        {
            var dlg = new ToastNotification();
            owner.LocationChanged += dlg.owner_LocationChanged;
            owner.SizeChanged += dlg.owner_SizeChanged;
            dlg.Owner = owner;

            dlg.WindowStartupLocation = WindowStartupLocation.Manual;

            dlg.Notification = notification;
            dlg.VisibilityInSeconds = visibilityInSeconds;
            dlg.Show();
            return dlg;
        }

        void recalculatePosition()
        {
            appearStoryBoard.Pause();
            disappearStoryBoard.Pause();

            var newLeft = Owner.Left;
            var newTop = Owner.Top;
            var newRight = Owner.Left + Owner.Width;
            var newBottom = Owner.Top + Owner.Height;

            // if supposed to be centered toast
            if(true)
            {
                var centerX = newRight - newLeft;
                var centerY = newBottom - newTop;

                this.Left = centerX - (this.Width / 2);
                this.Top = centerY - (this.Height / 2);
            }

            appearStoryBoard.Resume();
            disappearStoryBoard.Resume();
            Debug.WriteLine($"Repositioned: {this.Left},{this.Top}");
        }

        void owner_SizeChanged(object sender, SizeChangedEventArgs e)
            => recalculatePosition();

        void owner_LocationChanged(object sender, EventArgs e)
            => recalculatePosition();

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
