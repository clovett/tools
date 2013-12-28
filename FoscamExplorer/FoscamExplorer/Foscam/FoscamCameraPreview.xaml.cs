using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace FoscamExplorer
{
    public sealed partial class FoscamCameraPreview : UserControl
    {
        FoscamDevice device;

        public FoscamCameraPreview()
        {
            this.InitializeComponent();
            this.Unloaded += FoscamCameraPreview_Unloaded;
        }

        void FoscamCameraPreview_Unloaded(object sender, RoutedEventArgs e)
        {
            device.CameraInfo.PropertyChanged -= OnCameraPropertyChanged;
            device.Error -= OnDeviceError;
            device.FrameAvailable -= OnFrameAvailable;
            device.StopStream();
        }

        public CameraInfo Camera
        {
            get { return (CameraInfo)GetValue(CameraProperty); }
            set { SetValue(CameraProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Camera.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CameraProperty =
            DependencyProperty.Register("Camera", typeof(CameraInfo), typeof(FoscamCameraPreview), new PropertyMetadata(null, new PropertyChangedCallback(OnCameraChanged)));

        static void OnCameraChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FoscamCameraPreview)d).OnCameraChanged((CameraInfo)e.OldValue, (CameraInfo)e.NewValue);
        }

        void OnCameraChanged(CameraInfo oldCameara, CameraInfo newCamera)
        {
            if (device != null)
            {
                oldCameara.PropertyChanged -= OnCameraPropertyChanged;
                device.Error -= OnDeviceError;
                device.FrameAvailable -= OnFrameAvailable;
                device.StopStream();
            }


            if (newCamera != null)
            {
                device = new FoscamDevice() { CameraInfo = newCamera };
                device.Error += OnDeviceError;
                device.FrameAvailable += OnFrameAvailable;
                newCamera.PropertyChanged += OnCameraPropertyChanged;
                device.StartJpegStream();
                OnRotationChanged();
            }
        }

        void Reconnect()
        {
            if (this.device != null)
            {
                device.StopStream();
                device.StartJpegStream();
            }
        }

        private void OnRotationChanged()
        {
            bool flip = device.CameraInfo.Flipped;
            if (flip)
            {
                CameraImage.RenderTransform = new RotateTransform() { Angle = 180, CenterX = CameraImage.Width / 2, CenterY = CameraImage.Height / 2 };
            }
            else
            {
                CameraImage.RenderTransform = null;
            }
        }

        DispatcherTimer delayVideoTimer;

        void DelayStartVideo(TimeSpan delay)
        {
            if (delayVideoTimer == null)
            {
                delayVideoTimer = new DispatcherTimer();
                delayVideoTimer.Interval = delay;
                delayVideoTimer.Tick += new EventHandler<object>((s, e) =>
                {
                    delayVideoTimer.Stop();
                    device.StartJpegStream();
                    delayVideoTimer = null;
                });
            }

            delayVideoTimer.Start();
        }

        void OnCameraPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "UserName":
                case "Password":
                case "Fps":
                    DelayStartVideo(TimeSpan.FromMilliseconds(250));
                    break;
                case "Flipped":
                    OnRotationChanged();
                    break;
                case "LastPingTime":
                    if (ErrorBorder.Visibility == Windows.UI.Xaml.Visibility.Visible && lastFrameTime + 1000 < device.CameraInfo.LastPingTime)
                    {
                        DelayStartVideo(TimeSpan.FromMilliseconds(250));
                    }
                    break;
            }
            
        }

        int lastFrameTime;

        void OnFrameAvailable(object sender, FrameReadyEventArgs e)
        {
            lastFrameTime = Environment.TickCount;
            HideError();
            CameraImage.Source = e.BitmapSource;
        }

        void OnDeviceError(object sender, ErrorEventArgs e)
        {
            if (e.HttpResponse == System.Net.HttpStatusCode.Unauthorized)
            {
                CameraImage.Source = new BitmapImage(new Uri("ms-appx:/Assets/Padlock.png", UriKind.RelativeOrAbsolute));
            }
            else if (e.HttpResponse == System.Net.HttpStatusCode.ServiceUnavailable && lastFrameTime != 0 && lastFrameTime + 1000 < Environment.TickCount)
            {
                // one retry - this handles the case where the computer comes out of sleep since we get no other events telling us this.
                lastFrameTime = Environment.TickCount;
                Reconnect();
            }
            else
            {
                ShowError(e.Message);
            }
        }

        void ShowError(string text)
        {
            ErrorBorder.Visibility = Windows.UI.Xaml.Visibility.Visible;
            ErrorMessage.Text = text;
        }

        void HideError()
        {
            ErrorBorder.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            ErrorMessage.Text = "";
        }

    }
}
