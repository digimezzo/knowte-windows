using System;
using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Knowte.Common.Controls
{
    public partial class MetroNotification : UserControl
    {
        private Timer timer = new Timer();
        private int showSeconds;
        private int elapsedSeconds;
  
        public Brush TitleForeground
        {
            get { return (Brush)GetValue(TitleForegroundProperty); }

            set
            {
                SetValue(TitleForegroundProperty, value);
                Debug.WriteLine(value);
            }
        }

        public int AnimationDuration
        {
            get { return Convert.ToInt32(GetValue(AnimationDurationProperty)); }

            set
            {
                SetValue(AnimationDurationProperty, value);
                Debug.WriteLine(value);
            }
        }

        public bool ShowTimer
        {
            get { return Convert.ToBoolean(GetValue(ShowTimerProperty)); }

            set
            {
                SetValue(ShowTimerProperty, value);
                Debug.WriteLine(value);
            }
        }

        public int CornerRadius
        {
            get { return Convert.ToInt32(GetValue(CornerRadiusProperty)); }

            set
            {
                SetValue(CornerRadiusProperty, value);
                Debug.WriteLine(value);
            }
        }

        public Brush NotificationColor
        {
            get { return (Brush)GetValue(NotificationColorProperty); }

            set
            {
                SetValue(NotificationColorProperty, value);
                Debug.WriteLine(value);
            }
        }
    
        public static readonly DependencyProperty TitleForegroundProperty = 
            DependencyProperty.Register("TitleForeground", typeof(Brush), typeof(MetroNotification), new PropertyMetadata(null));
        public static readonly DependencyProperty AnimationDurationProperty = 
            DependencyProperty.Register("AnimationDuration", typeof(int), typeof(MetroNotification), new PropertyMetadata(null));
        public static readonly DependencyProperty ShowTimerProperty = 
            DependencyProperty.Register("ShowTimer", typeof(int), typeof(MetroNotification), new PropertyMetadata(null));
        public static readonly DependencyProperty CornerRadiusProperty = 
            DependencyProperty.Register("CornerRadius", typeof(int), typeof(MetroNotification), new PropertyMetadata(null));
        public static readonly DependencyProperty NotificationColorProperty = 
            DependencyProperty.Register("NotificationColor", typeof(Brush), typeof(MetroNotification), new PropertyMetadata(null));
     
        public MetroNotification()
        {
            // This call is required by the designer.
            InitializeComponent();

            this.timer.Elapsed += new ElapsedEventHandler(TimerHandler);
        }
     
        public void TimerHandler(object sender, ElapsedEventArgs e)
        {
            this.elapsedSeconds += 1;

            if (this.elapsedSeconds >= this.showSeconds)
            {
                this.StopTimer();
                Application.Current.Dispatcher.Invoke(new Action(Hide));
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    
        public void Show(string title, string body)
        {
            // Make sure the timer is not started. We don't want the notification to accidentally hide.
            this.StopTimer();

            // This if prevents popping up several times when the same message has to be displayed
            if (!body.Equals(NotificationBody.Content))
            {
                this.NotificationTitle.Content = title.ToUpper();
                this.NotificationBody.Content = body;

                // Create a storyboard to contain the animations.
                Storyboard storyboard = new Storyboard();
                TimeSpan duration = new TimeSpan(0, 0, 0, 0, this.AnimationDuration);

                // Create a DoubleAnimation to fade the not selected option control
                DoubleAnimation animation = new DoubleAnimation();

                animation.From = 0.0;
                animation.To = this.MaxHeight;

                animation.Duration = new Duration(duration);

                // Configure the animation to target de property Opacity
                Storyboard.SetTargetName(animation, "MetroNotificationWindow");
                Storyboard.SetTargetProperty(animation, new PropertyPath(HeightProperty));

                // Add the animation to the storyboard
                storyboard.Children.Add(animation);

                // Begin the storyboard
                storyboard.Begin(this);
            }
        }

        public void Show(string title, string body, int showSeconds)
        {
            this.showSeconds = showSeconds;
            this.Show(title, body);
            this.StartTimer();
        }

        public void Hide()
        {
            // Stops the timer, so we don't get a premature hide
            StopTimer();

            // Prevents flickering when trying to hide when already hidden
            if (this.IsNotifying())
            {
                this.NotificationTitle.Content = "";
                this.NotificationBody.Content = "";

                // Create a storyboard to contain the animations.
                Storyboard storyboard = new Storyboard();
                TimeSpan duration = new TimeSpan(0, 0, 0, 0, this.AnimationDuration);

                // Create a DoubleAnimation to fade the not selected option control
                DoubleAnimation animation = new DoubleAnimation();

                animation.From = this.MaxHeight;
                animation.To = 0.0;

                animation.Duration = new Duration(duration);

                // Configure the animation to target de property Opacity
                Storyboard.SetTargetName(animation, "MetroNotificationWindow");
                Storyboard.SetTargetProperty(animation, new PropertyPath(HeightProperty));

                // Add the animation to the storyboard
                storyboard.Children.Add(animation);

                // Begin the storyboard
                storyboard.Begin(this);
            }
        }

        public bool ConditionalHide(string iBody)
        {
            bool retVal = false;

            // Prevents flickering when trying to hide when already hidden

            if (this.IsNotifying())
            {

                if (this.NotificationBody.Content != null)
                {
                    if (this.NotificationBody.Content.Equals(iBody))
                    {
                        this.Hide();
                        retVal = true;
                    }
                }
            }
            return retVal;
        }

        public bool IsNotifying()
        {
            bool retVal = true;

            if (this.Height == 0)
            {
                retVal = false;
            }

            return retVal;
        }
  
        private void StopTimer()
        {
            if (this.timer != null)
            {
                this.timer.Enabled = false;
            }

            this.elapsedSeconds = 0;
        }

        private void StartTimer()
        {
            this.elapsedSeconds = 0;

            if (this.timer != null)
            {
                this.timer.Interval = TimeSpan.FromSeconds(1).TotalMilliseconds;
                timer.Enabled = true;
            }
        }
    }
}