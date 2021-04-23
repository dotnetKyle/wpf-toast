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
        const int animationLengthInMs = 500;
        DoubleAnimationUsingKeyFrames appearAnimation;
        DoubleAnimationUsingKeyFrames disappearAnimation;

        Storyboard appearStoryBoard;
        Storyboard disappearStoryBoard;

        System.Timers.Timer _hangTimer;

        bool appearComplete = false;
        bool disappearStarted = false;

        public ToastNotification()
        {
            InitializeComponent();
            grdMain.DataContext = this;

            if(WpfDesignerHelper.Designer.Active)
            {
                Notification = new TextNotification { Text = "Test Test" };
            }

            Loaded += ToastNotification_Loaded;
            Unloaded += ToastNotification_Unloaded;
        }


        public int VisibilityInSeconds
        {
            get { return (int)GetValue(VisibilityInSecondsProperty); }
            set { SetValue(VisibilityInSecondsProperty, value); }
        }
        public static readonly DependencyProperty VisibilityInSecondsProperty =
            DependencyProperty.Register(nameof(VisibilityInSeconds), typeof(int), typeof(ToastNotification), new PropertyMetadata(1));
        
        private void ToastNotification_Unloaded(object sender, RoutedEventArgs e)
        {
            // remove events on Owner
            Owner.LocationChanged -= owner_LocationChanged;
            Owner.SizeChanged -= owner_SizeChanged;
            Owner.StateChanged -= Owner_StateChanged;
        }

        private void ToastNotification_Loaded(object sender, RoutedEventArgs e)
        {
            // setup events on Owner
            Owner.LocationChanged += owner_LocationChanged;
            Owner.SizeChanged += owner_SizeChanged;
            Owner.StateChanged += Owner_StateChanged;

            this.MouseEnter += ToastNotification_MouseEnter;
            this.MouseLeave += ToastNotification_MouseLeave;

            // Configure animations on load
            appearStoryBoard = new Storyboard();
            disappearStoryBoard = new Storyboard();

            appearAnimation = new DoubleAnimationUsingKeyFrames
            {
                KeyFrames = new DoubleKeyFrameCollection
                {
                    new SplineDoubleKeyFrame{ KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 0 }, // from invisible
                    new SplineDoubleKeyFrame{ KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(animationLengthInMs)), Value = 1 } // to visible
                }
            };
            disappearAnimation = new DoubleAnimationUsingKeyFrames
            {
                KeyFrames = new DoubleKeyFrameCollection
                {
                    new SplineDoubleKeyFrame{ KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 1 }, // from visible
                    new SplineDoubleKeyFrame{ KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(animationLengthInMs)), Value = 0 } // back to invisible
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
            appearStoryBoard.Begin(this, true);
        }

        private void ToastNotification_MouseLeave(object sender, MouseEventArgs e)
        {
            if (appearComplete == false)
            {
                // complete the appearance
                completeAppear();
            }
            else if (disappearStarted == false)
            {
                // restart the hang time
                _hangTimer.Start();
            }
            else
            {
                // restart the disappear 
                disappearStoryBoard.Begin(this, true);
            }
        }

        private void ToastNotification_MouseEnter(object sender, MouseEventArgs e)
        {
            if(appearComplete == false)
            {
                // stop the appear animation and make visible until mouse leave
                appearStoryBoard.Stop(this);
                this.Opacity = 1.0;
            }
            else if(disappearStarted == false)
            {
                // stop the hang time and leave visible until mouse leave
                _hangTimer.Stop();
            }
            else
            {
                // stop the disappear and make visible until mouse leave
                disappearStoryBoard.Stop(this);
                this.Opacity = 1.0;
            }
        }

        void completeAppear()
        {
            appearComplete = true;
            _hangTimer = new Timer(VisibilityInSeconds * 1000);
            _hangTimer.AutoReset = false;
            _hangTimer.Elapsed += hangTimerElapsed;
            _hangTimer.Start();
        }
        void beginDisappear()
        {
            disappearStarted = true;
            disappearStoryBoard.Begin(this, true);
        }
        void hangTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _hangTimer.Stop();
            _hangTimer = null;
            Dispatcher.Invoke(beginDisappear);
        }

        private async void AppearStoryBoard_Completed(object sender, EventArgs e)
        {
            try
            {
                completeAppear();
                // once appear is finished, beign disappear                
                await Task.Delay(TimeSpan.FromSeconds(VisibilityInSeconds));

            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void DisappearStoryBoard_Completed(object sender, EventArgs e)
        {
            this.Close();
        }

        public static ToastNotification Show(Window owner, Notification notification)
            => Show(owner, notification, 2);
        public static ToastNotification Show(Window owner, Notification notification, int visibilityInSeconds)
        {
            var dlg = new ToastNotification();
            dlg.Owner = owner;

            dlg.WindowStartupLocation = WindowStartupLocation.Manual;

            dlg.Notification = notification;
            dlg.VisibilityInSeconds = visibilityInSeconds;
            dlg.ShowInTaskbar = false;
            dlg.Show();
            
            // set focus back to owner so that Toast doesn't cause issues with what was happening on the owner
            dlg.Owner.Focus();

            return dlg;
        }

        void recalculatePosition()
        {
            if(appearComplete == false)
                appearStoryBoard.Pause(this);
            if(disappearStarted == true)
                disappearStoryBoard.Pause(this);

            WindowDimensions dim;
            if (Owner.WindowState == WindowState.Maximized)
            {
                var handle = new System.Windows.Interop.WindowInteropHelper(Owner).Handle;
                var screen = System.Windows.Forms.Screen.FromHandle(handle);
                
                dim = WindowDimensions.Create(screen.WorkingArea.Left, screen.WorkingArea.Top, screen.WorkingArea.Width, screen.WorkingArea.Height);
            }
            else if(Owner.WindowState == WindowState.Normal)
            {
                dim = WindowDimensions.Create(Owner.Left, Owner.Top, Owner.ActualWidth, Owner.ActualHeight);
            }
            else
            {
                // if minimized, do no reposition and leave paused
                return;
            }

            // if supposed to be centered toast
            if (true)
            {
                this.Left = dim.CenterX - (this.Width / 2);
                this.Top = dim.CenterY - (this.Height / 2);
            }

            if (appearComplete == false)
                appearStoryBoard.Resume(this);
            if (disappearStarted == true)
                disappearStoryBoard.Resume(this);
        }

        void Owner_StateChanged(object sender, EventArgs e)
            => recalculatePosition();

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
    public class WindowDimensions
    {
        private WindowDimensions() { }

        public static WindowDimensions Create(double left, double top, double width, double height)
        {
            if (width < 0)
                throw new ArgumentException("Width cannot be less than zero", nameof(width));
            if (height < 0)
                throw new ArgumentException("Height cannot be less than zero", nameof(height));

            var w = new WindowDimensions
            { 
                Left = left,
                Top = top,
                Width = width,
                Height = height,
                Right = left + width,
                Bottom = top + height
            };

            //Debug.WriteLine($"{left},{top},{width},{height}");
            //Debug.WriteLine($" #####################################################\r\n");
            //Debug.WriteLine($"{w.Left}x,{w.Top}y");
            //Debug.WriteLine($"           <------- {w.Width} ------->");
            //Debug.WriteLine($"       |-------------------------------|    ^");
            //Debug.WriteLine($"       |                               |    |");
            //Debug.WriteLine($"       |                               |    |");
            //Debug.WriteLine($"       |                               |  {w.Height}");
            //Debug.WriteLine($"       |                               |    |");
            //Debug.WriteLine($"       |                               |    |");
            //Debug.WriteLine($"       |-------------------------------|    v");
            //Debug.WriteLine($"                                    {w.Right}x,{w.Bottom}y");
            //Debug.WriteLine($"      center: {w.CenterX}x,{w.CenterY}y");
            //Debug.WriteLine("");
            //Debug.WriteLine("### Math: #################");
            //Debug.WriteLine($"Right => {left}l + {width}w = {w.Right}");
            //Debug.WriteLine($"Bottom => {top}t + {height}h = {w.Bottom}");
            //Debug.WriteLine($"CenterX => {w.Left}L + ({w.Width}W / 2) = {w.CenterX}");
            //Debug.WriteLine($"CenterY => {w.Top}B + ({w.Height}H / 2) = {w.CenterY}");
            //Debug.WriteLine($"\r\n #####################################################");

            return w;
        }

        public double Left { get; private set; }
        public double Right { get; private set; }
        public double Top { get; private set; }
        public double Bottom { get; private set; }
        public double Width { get; private set; }
        public double Height { get; private set; }
        public double CenterX => Left + (Width / 2);
        public double CenterY => Top + (Height / 2);
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
