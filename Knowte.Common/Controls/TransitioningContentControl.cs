using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Knowte.Common.Controls
{
    public class TransitioningContentControl : ContentControl
    {
        public static readonly DependencyProperty FadeInProperty = 
            DependencyProperty.Register("FadeIn", typeof(bool), typeof(TransitioningContentControl), new PropertyMetadata(null));
        public static readonly DependencyProperty FadeInTimeoutProperty = 
            DependencyProperty.Register("FadeInTimeout", typeof(double), typeof(TransitioningContentControl), new PropertyMetadata(null));
        public static readonly DependencyProperty SlideInProperty = 
            DependencyProperty.Register("SlideIn", typeof(bool), typeof(TransitioningContentControl), new PropertyMetadata(null));
        public static readonly DependencyProperty SlideInTimeoutProperty = 
            DependencyProperty.Register("SlideInTimeout", typeof(double), typeof(TransitioningContentControl), new PropertyMetadata(null));
        public static readonly DependencyProperty SlideInFromProperty = 
            DependencyProperty.Register("SlideInFrom", typeof(int), typeof(TransitioningContentControl), new PropertyMetadata(null));
        public static readonly DependencyProperty SlideInToProperty = 
            DependencyProperty.Register("SlideInTo", typeof(int), typeof(TransitioningContentControl), new PropertyMetadata(null));
        public static readonly DependencyProperty RightToLeftProperty = 
            DependencyProperty.Register("RightToLeft", typeof(bool), typeof(TransitioningContentControl), new PropertyMetadata(null));

        public bool FadeIn
        {
            get { return Convert.ToBoolean(GetValue(FadeInProperty)); }

            set { SetValue(FadeInProperty, value); }
        }

        public double FadeInTimeout
        {
            get { return Convert.ToDouble(GetValue(FadeInTimeoutProperty)); }

            set { SetValue(FadeInTimeoutProperty, value); }
        }

        public bool SlideIn
        {
            get { return Convert.ToBoolean(GetValue(SlideInProperty)); }

            set { SetValue(SlideInProperty, value); }
        }

        public double SlideInTimeout
        {
            get { return Convert.ToDouble(GetValue(SlideInTimeoutProperty)); }

            set { SetValue(SlideInTimeoutProperty, value); }
        }

        public int SlideInFrom
        {
            get { return Convert.ToInt32(GetValue(SlideInFromProperty)); }

            set { SetValue(SlideInFromProperty, value); }
        }

        public int SlideInTo
        {
            get { return Convert.ToInt32(GetValue(SlideInToProperty)); }

            set { SetValue(SlideInToProperty, value); }
        }

        public bool RightToLeft
        {
            get { return Convert.ToBoolean(GetValue(RightToLeftProperty)); }

            set { SetValue(RightToLeftProperty, value); }
        }
   
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            this.DoAnimation();
        }
      
        private void DoAnimation()
        {
            if (this.FadeInTimeout != 0 && this.FadeIn)
            {
                DoubleAnimation da = new DoubleAnimation();
                da.From = 0;
                da.To = 1;
                da.Duration = new Duration(TimeSpan.FromSeconds(this.FadeInTimeout));
                this.BeginAnimation(OpacityProperty, da);
            }


            if (this.SlideInTimeout != 0 && this.SlideInTimeout > 0 && this.SlideIn)
            {
                if (!this.RightToLeft)
                {
                    ThicknessAnimation ta = new ThicknessAnimation();
                    ta.From = new Thickness(this.SlideInFrom, this.Margin.Top, this.SlideInTo - this.SlideInFrom, this.Margin.Bottom);
                    ta.To = new Thickness(this.SlideInTo, this.Margin.Top, this.SlideInTo, this.Margin.Bottom);
                    ta.Duration = new Duration(TimeSpan.FromSeconds(this.SlideInTimeout));
                    this.BeginAnimation(MarginProperty, ta);
                }
                else
                {
                    ThicknessAnimation ta = new ThicknessAnimation();
                    ta.From = new Thickness(this.SlideInTo - this.SlideInFrom, this.Margin.Top, this.SlideInFrom, this.Margin.Bottom);
                    ta.To = new Thickness(this.SlideInTo, this.Margin.Top, this.SlideInTo, this.Margin.Bottom);
                    ta.Duration = new Duration(TimeSpan.FromSeconds(this.SlideInTimeout));
                    this.BeginAnimation(MarginProperty, ta);
                }
            }
        }
    }
}