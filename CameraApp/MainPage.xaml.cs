using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CameraApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MediaCapture _mediaCapture;
        private FrameRenderer _frameRenderer;
        bool _recording;
        bool _paused;

        public MainPage()
        {
            this.InitializeComponent();
            this.UpdateButtonState();
            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            var allGroups = await MediaFrameSourceGroup.FindAllAsync();
            if (allGroups.Count == 0)
            {
                ShowStatus("No source groups found.");
                return;
            }
            MediaFrameSourceGroup selectedGroup = null;

            foreach (var group in allGroups)
            {
                // MediaFrameSourceKind kind = source.Info.SourceKind;
                selectedGroup = group;
                _mediaCapture = new MediaCapture();
                var settings = new MediaCaptureInitializationSettings
                {
                    SourceGroup = group,

                    // This media capture can share streaming with other apps.
                    SharingMode = MediaCaptureSharingMode.SharedReadOnly,

                    // Only stream video and don't initialize audio capture devices.
                    StreamingCaptureMode = StreamingCaptureMode.Video,

                    // Set to CPU to ensure frames always contain CPU SoftwareBitmap images
                    // instead of preferring GPU D3DSurface images.
                    MemoryPreference = MediaCaptureMemoryPreference.Cpu
                };

                await _mediaCapture.InitializeAsync(settings);
                break;
            }

            var startedKinds = new HashSet<MediaFrameSourceKind>();
            foreach (MediaFrameSource source in _mediaCapture.FrameSources.Values)
            {
                MediaFrameSourceKind kind = source.Info.SourceKind;
                if (kind != MediaFrameSourceKind.Color)
                {
                    continue;
                }

                // Ignore this source if we already have a source of this kind.
                if (startedKinds.Contains(kind))
                {
                    continue;
                }

                // Look for a format which the FrameRenderer can render.
                string requestedSubtype = null;
                foreach (MediaFrameFormat format in source.SupportedFormats)
                {
                    requestedSubtype = FrameRenderer.GetSubtypeForFrameReader(kind, format);
                    if (requestedSubtype != null)
                    {
                        // Tell the source to use the format we can render.
                        // await source.SetFormatAsync(format);
                        break;
                    }
                }
                if (requestedSubtype == null)
                {
                    // No acceptable format was found. Ignore this source.
                    continue;
                }

                this._frameRenderer = new FrameRenderer(this.CameraFrame);
                try
                {
                    await this._frameRenderer.OpenReader(_mediaCapture, source, requestedSubtype);
                    startedKinds.Add(kind);
                    ShowStatus($"Started {kind} reader.");
                } 
                catch (Exception ex)
                {
                    ShowStatus(ex.Message);
                    return;
                }
            }

            if (startedKinds.Count == 0)
            {
                ShowStatus($"No eligible sources in {selectedGroup.DisplayName}.");
            }
        }

        private void ShowStatus(string message)
        {
            this.Status.Text = message;
        }

        private async void OnRecord(object sender, RoutedEventArgs e)
        {
            if (_paused && _recording)
            {
                await _mediaCapture.ResumeRecordAsync();
                _paused = false;
                UpdateButtonState();
                return;
            }

            try
            {
                await StartRecording();
                _recording = true;
                _paused = false;
                UpdateButtonState();
            } 
            catch (Exception ex)
            {
                ShowStatus(ex.Message);
            }

        }
        LowLagMediaRecording _mediaRecording;

        private async Task StartRecording()
        {
            // Use the MP4 preset to an obtain H.264 video encoding profile
            //var mep = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD1080p);
            //mep.Audio = null;
            //mep.Container = null;
            ////if (previewEncodingProperties != null)
            //{
            //    mep.Video.Width = 1280;
            //    mep.Video.Height = 720;
            //}

            //StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            //StorageFile videoFile = await storageFolder.CreateFileAsync("recording.mp4", CreationCollisionOption.ReplaceExisting);
            //await _mediaCapture.StartRecordToStorageFileAsync(mep, videoFile);

            var myVideos = await Windows.Storage.StorageLibrary.GetLibraryAsync(Windows.Storage.KnownLibraryId.Videos);
            StorageFile file = await myVideos.SaveFolder.CreateFileAsync("recording.mp4", CreationCollisionOption.ReplaceExisting);
            _mediaRecording = await _mediaCapture.PrepareLowLagRecordToStorageFileAsync(
                    MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto), file);
            await _mediaRecording.StartAsync();

            ShowStatus("Writing to: " + file.Path);
        }

        private async void OnPause(object sender, RoutedEventArgs e)
        {
            if (_recording)
            {
                _paused = true;
                UpdateButtonState();
                await _mediaRecording.PauseAsync(Windows.Media.Devices.MediaCapturePauseBehavior.RetainHardwareResources);
                ShowStatus("Paused");
            }
        }

        private async void OnStop(object sender, RoutedEventArgs e)
        {
            if (_recording)
            {
                _paused = false;
                _recording = false;
                UpdateButtonState();
                await _mediaRecording.StopAsync();
                ShowStatus("Recording stopped");
            }
        }

        void UpdateButtonState()
        {
            RecordButton.Visibility = _recording && !_paused ? Visibility.Collapsed : Visibility.Visible;
            PauseButton.Visibility = _recording && !_paused ? Visibility.Visible : Visibility.Collapsed;
            StopButton.Visibility = _recording ? Visibility.Visible : Visibility.Collapsed;
        }

    }
}
