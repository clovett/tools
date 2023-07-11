//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace CameraApp
{
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    class FrameRenderer
    {
        private Image _imageElement;
        private SoftwareBitmap _backBuffer;
        private bool _taskRunning = false;
        MediaFrameReader _sourceReader;

        public FrameRenderer(Image imageElement)
        {
            _imageElement = imageElement;
            _imageElement.Source = new SoftwareBitmapSource();
        }

        public async Task OpenReader(MediaCapture capture, MediaFrameSource source, string requestedSubType)
        {
            MediaFrameReader frameReader = await capture.CreateFrameReaderAsync(source, requestedSubType);
            frameReader.FrameArrived += FrameReader_FrameArrived;
            _sourceReader = frameReader;

            MediaFrameReaderStartStatus status = await frameReader.StartAsync();
            if (status != MediaFrameReaderStartStatus.Success)
            {
                throw new Exception($"Unable to start frame reader. Error: {status}");
            }
        }

        private void FrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            // TryAcquireLatestFrame will return the latest frame that has not yet been acquired.
            // This can return null if there is no such frame, or if the reader is not in the
            // "Started" state. The latter can occur if a FrameArrived event was in flight
            // when the reader was stopped.
            using (var frame = sender.TryAcquireLatestFrame())
            {
                if (frame != null)
                {
                    this.ProcessFrame(frame);
                }
            }
        }

        public void ProcessFrame(MediaFrameReference frame)
        {
            var softwareBitmap = FrameRenderer.ConvertToDisplayableImage(frame?.VideoMediaFrame);

            RenderFrame(softwareBitmap);
        }

        public void RenderFrame(SoftwareBitmap softwareBitmap)
        {
            if (softwareBitmap != null)
            {
                // Swap the processed frame to _backBuffer and trigger UI thread to render it
                softwareBitmap = Interlocked.Exchange(ref _backBuffer, softwareBitmap);

                // UI thread always reset _backBuffer before using it.  Unused bitmap should be disposed.
                softwareBitmap?.Dispose();

                // Changes to xaml ImageElement must happen in UI thread through Dispatcher
                var task = _imageElement.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    async () =>
                    {
                        // Don't let two copies of this task run at the same time.
                        if (_taskRunning)
                        {
                            return;
                        }
                        _taskRunning = true;

                        // Keep draining frames from the backbuffer until the backbuffer is empty.
                        SoftwareBitmap latestBitmap;
                        while ((latestBitmap = Interlocked.Exchange(ref _backBuffer, null)) != null)
                        {
                            var imageSource = (SoftwareBitmapSource)_imageElement.Source;
                            await imageSource.SetBitmapAsync(latestBitmap);
                            latestBitmap.Dispose();
                        }

                        _taskRunning = false;
                    });
            }
        }
        // Function delegate that transforms a scanline from an input image to an output image.
        private unsafe delegate void TransformScanline(int pixelWidth, byte* inputRowBytes, byte* outputRowBytes);

        /// <summary>
        /// Determines the subtype to request from the MediaFrameReader that will result in
        /// a frame that can be rendered by ConvertToDisplayableImage.
        /// </summary>
        /// <returns>Subtype string to request, or null if subtype is not renderable.</returns>
        public static string GetSubtypeForFrameReader(MediaFrameSourceKind kind, MediaFrameFormat format)
        {
            // Note that media encoding subtypes may differ in case.
            // https://docs.microsoft.com/en-us/uwp/api/Windows.Media.MediaProperties.MediaEncodingSubtypes
            string subtype = format.Subtype;
            switch (kind)
            {
                // For color sources, we accept anything and request that it be converted to Bgra8.
                case MediaFrameSourceKind.Color:
                    return MediaEncodingSubtypes.Bgra8;
                // No other source kinds are supported by this class.
                default:
                    return null;
            }
        }

        /// <summary>
        /// Converts a frame to a SoftwareBitmap of a valid format to display in an Image control.
        /// </summary>
        /// <param name="inputFrame">Frame to convert.</param>
        private static unsafe SoftwareBitmap ConvertToDisplayableImage(VideoMediaFrame inputFrame)
        {
            SoftwareBitmap result = null;
            using (var inputBitmap = inputFrame?.SoftwareBitmap)
            {
                if (inputBitmap != null)
                {
                    switch (inputFrame.FrameReference.SourceKind)
                    {
                        case MediaFrameSourceKind.Color:
                            // XAML requires Bgra8 with premultiplied alpha.
                            // We requested Bgra8 from the MediaFrameReader, so all that's
                            // left is fixing the alpha channel if necessary.
                            if (inputBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8)
                            {
                                Debug.WriteLine("Color frame in unexpected format.");
                            }
                            else if (inputBitmap.BitmapAlphaMode == BitmapAlphaMode.Premultiplied)
                            {
                                // Already in the correct format.
                                result = SoftwareBitmap.Copy(inputBitmap);
                            }
                            else
                            {
                                // Convert to premultiplied alpha.
                                result = SoftwareBitmap.Convert(inputBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                            }
                            break;
                    }
                }
            }
            return result;
        }
    }
}
