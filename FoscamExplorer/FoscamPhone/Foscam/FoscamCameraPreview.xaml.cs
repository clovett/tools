using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Media;

namespace FoscamExplorer.Foscam
{
    public partial class FoscamCameraPreview : UserControl
    {
        FoscamDevice device;
        const int videoSize = 320;
        const CameraFps videoFps = CameraFps.Fps1; // 1 fps on the start page since we are showing all cameras at once.

        public FoscamCameraPreview()
        {
            InitializeComponent();            
            this.Unloaded += FoscamCameraPreview_Unloaded;
        }

        void FoscamCameraPreview_Unloaded(object sender, RoutedEventArgs e)
        {
            Disconnect();
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
            Disconnect(); // old camera

            if (newCamera != null)
            {
                device = new FoscamDevice() { CameraInfo = newCamera };
                device.Error += OnDeviceError;
                device.FrameAvailable += OnFrameAvailable;
                device.CameraInfo.PropertyChanged += OnCameraPropertyChanged;
                device.StartJpegStream(this.Dispatcher, videoSize, videoFps);
                OnRotationChanged();
                ShowError("Loading...");
            }
        }
        
        private void Disconnect()
        {
            if (device != null)
            {
                device.CameraInfo.PropertyChanged -= OnCameraPropertyChanged;
                device.Error -= OnDeviceError;
                device.FrameAvailable -= OnFrameAvailable;
                device.StopStream();
            }
        }

        void Reconnect()
        {
            if (this.device != null)
            {
                device.StopStream();
                device.StartJpegStream(this.Dispatcher, videoSize, videoFps);
            }
        }

        private void OnRotationChanged()
        {
            bool flip = device.CameraInfo.Rotation == 180;
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
                delayVideoTimer.Tick += new EventHandler((s, e) =>
                {
                    delayVideoTimer.Stop();
                    device.StartJpegStream(this.Dispatcher, videoSize, videoFps);
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
                case "Rotation":
                    OnRotationChanged();
                    break;
                case "LastPingTime":
                    if (ErrorBorder.Visibility == Visibility.Visible && lastFrameTime + 1000 < device.CameraInfo.LastPingTime)
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
                CameraImage.Source = new BitmapImage(new Uri("/Assets/Padlock.png", UriKind.RelativeOrAbsolute));
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
            ErrorBorder.Visibility = Visibility.Visible;
            ErrorMessage.Text = text;
        }

        void HideError()
        {
            ErrorBorder.Visibility = Visibility.Collapsed;
            ErrorMessage.Text = "";
        }


    }
}
