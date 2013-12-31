/* 
 * ratul took this code from codeplex and slightly modified  it. 
 * the code comes with the following license
 * 
Microsoft Public License (Ms-PL)
This license governs use of the accompanying software. If you use the software, you accept this license. If you do not accept the license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under U.S. copyright law.
A "contribution" is the original software, or any additions or changes to the software.
A "contributor" is any person that distributes its contribution under this license.
"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

3. Conditions and Limitations
(A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
(B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
(C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
(D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
(E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.
*/


using System;
using System.Text;
using System.Net;
using System.IO;

using System.Threading;
using System.Windows;
using Windows.Storage.Streams;
#if WINDOWS_PHONE
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using CoreDispatcher = System.Windows.Threading.Dispatcher;
#else
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml;
using Windows.UI.Core;
#endif
using System.Net.Http;
using System.Threading.Tasks;


namespace FoscamExplorer
{
    public class MjpegDecoder
    {
        // magic 2 byte header for JPEG images
        private readonly byte[] JpegHeader = new byte[] { 0xff, 0xd8 };

        // pull down 1024 bytes at a time
        private const int ChunkSize = 1024;

        // used to cancel reading the stream
        private bool _streamActive;

        // number of milliseconds expected between frames.
        private int millisecondsPerFrame;

        // event to get the buffer above handed to you
        public event EventHandler<FrameReadyEventArgs> FrameReady;

        public event EventHandler<ErrorEventArgs> Error;

        CoreDispatcher dispatcher;

        public MjpegDecoder(CoreDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        HttpClient videoHttp;
        static int nextStream;
        int id = nextStream++;

        public async void GetVideoStream(Uri uri, int millisecondsPerFrame, NetworkCredential credential)
        {
            try
            {
                Log.WriteLine("GetVideoStream id=" + id);
                this.millisecondsPerFrame = millisecondsPerFrame; 
                HttpClientHandler settings = new HttpClientHandler();
                settings.Credentials = credential;

                videoHttp = new HttpClient(settings);
                HttpResponseMessage msg = await videoHttp.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                if (msg.StatusCode == HttpStatusCode.OK)
                {
                    var task = Task.Run(new Action(() =>
                    {
                        ParseStream(msg);
                    }));

                    StartWatchdog();
                }
                else
                {
                    OnError(new ErrorEventArgs() { Message = msg.StatusCode.ToString(), HttpResponse = msg.StatusCode });
                }
            }
            catch (Exception ex)
            {
                HttpRequestException re = ex as HttpRequestException;
                if (re != null)
                {
                    WebException we = re.InnerException as WebException;
                    if (we != null)
                    {
                        WebExceptionStatus status = we.Status;
                        OnError(new ErrorEventArgs() { Message = ex.Message });
                    }
                    else
                    {
                        OnError(new ErrorEventArgs() { Message = ex.Message });
                    }
                }
                else
                {
                    OnError(new ErrorEventArgs() { Message = ex.Message });
                }
            }
        }

        DispatcherTimer watchdogTimer;
        int timeoutCount;
        int lastFrameTime;

        private void StartWatchdog()
        {
            // when the device goes away (e.g. reboot), the HttpContent stream blocks indefinitely, 
            // which is at best a memory leak, so this watchdog task checks for this condition.
            watchdogTimer = new DispatcherTimer();            
            watchdogTimer.Interval = TimeSpan.FromSeconds(1);
            watchdogTimer.Tick += OnWatchDogTick;
            watchdogTimer.Start();
        }

        private void StopWatchdog()
        {
            if (watchdogTimer != null)
            {
                watchdogTimer.Tick -= OnWatchDogTick;
                watchdogTimer.Stop();
                watchdogTimer = null;
            }
        }

        private void OnWatchDogTick(object sender, object args)
        {
            if (lastFrameTime + (millisecondsPerFrame * 5) < Environment.TickCount)
            {
                timeoutCount++;

                if (timeoutCount > 10)
                {
                    // 30 seconds of nothing is long enough, time to shut this puppy down.
                    StopStream();
                }
            }
            else
            {
                timeoutCount = 0;
            }
        }

        void OnError(ErrorEventArgs e)
        {
            if (dispatcher == null)
            {
                return;
            }
            var quiet = dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() => 
            {
                if (Error != null)
                {
                    Error(this, e);
                }
            }));
        }

        public async void StopStream()
        {
            _streamActive = false;

            ManualResetEvent evt = this.terminated;
            if (evt != null)
            {
                await Task.Run(new Action(() =>
                {
                    evt.WaitOne(3000);
                }));
            }

            using (content)
            {
                content = null;
            }

            StopWatchdog();
        }

        HttpContent content;
        ManualResetEvent terminated;

        private async void ParseStream(HttpResponseMessage response)
        {
            byte[] imageBuffer = new byte[1024 * 1024];

            // get the response
            try
            {
                using (response)
                {
                    terminated = new ManualResetEvent(false);
                    content = response.Content;

                    // find our magic boundary value
                    string contentType = content.Headers.ContentType.MediaType;
                    if (contentType == "text/plain")
                    {
                        // getting an error ?
                        string message = await content.ReadAsStringAsync();
                        throw new Exception(message);
                    }

                    if (contentType != "multipart/x-mixed-replace")
                    {
                        throw new Exception("Invalid content-type header.  The camera is likely not returning a proper MJPEG stream.");
                    }

                    string boundary = null;
                    foreach (var param in content.Headers.ContentType.Parameters)
                    {
                        if (param.Name == "boundary")
                        {
                            boundary = param.Value;
                        }
                    }
                    byte[] boundaryBytes = Encoding.UTF8.GetBytes(boundary.StartsWith("--") ? boundary : "--" + boundary);

                    using (var videoStream = await content.ReadAsStreamAsync())
                    {
                        BinaryReader br = new BinaryReader(videoStream);

                        _streamActive = true;

                        byte[] buff = br.ReadBytes(ChunkSize);

                        while (_streamActive)
                        {
                            // find the JPEG header
                            int imageStart = buff.Find(JpegHeader);

                            if (imageStart != -1)
                            {
                                // copy the start of the JPEG image to the imageBuffer
                                int size = buff.Length - imageStart;
                                Array.Copy(buff, imageStart, imageBuffer, 0, size);

                                while (_streamActive)
                                {
                                    buff = br.ReadBytes(ChunkSize);

                                    // find the boundary text
                                    int imageEnd = buff.Find(boundaryBytes);
                                    if (imageEnd != -1)
                                    {
                                        // copy the remainder of the JPEG to the imageBuffer
                                        Array.Copy(buff, 0, imageBuffer, size, imageEnd);
                                        size += imageEnd;

                                        byte[] frame = new byte[size];
                                        Array.Copy(imageBuffer, 0, frame, 0, size);

                                        ProcessFrame(frame);

                                        // copy the leftover data to the start
                                        Array.Copy(buff, imageEnd, buff, 0, buff.Length - imageEnd);

                                        // fill the remainder of the buffer with new data and start over
                                        byte[] temp = br.ReadBytes(imageEnd);

                                        Array.Copy(temp, 0, buff, buff.Length - imageEnd, temp.Length);
                                        break;
                                    }

                                    // copy all of the data to the imageBuffer
                                    Array.Copy(buff, 0, imageBuffer, size, buff.Length);
                                    size += buff.Length;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (Error != null)
                {
                    HttpStatusCode code = HttpStatusCode.ServiceUnavailable;
                    try
                    {
                        WebException we = ex as WebException;
                        if (we != null)
                        {
                            HttpWebResponse httpResponse = we.Response as HttpWebResponse;
                            if (httpResponse != null)
                            {
                                code = httpResponse.StatusCode;
                            }
                        }
                    }
                    catch
                    {
                        // response is in a bad state..
                    }

                    OnError(new ErrorEventArgs() { Message = ex.Message, HttpResponse = code });
                }
            }

            // response has been disposed, so clear the content also
            content = null;
            Log.WriteLine("Closing VideoStream id=" + id);
            terminated.Set();
        }

        private void ProcessFrame(byte[] frame)
        {
            lastFrameTime = Environment.TickCount;

            // get onto the UI thread
            var quiet = dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(() => 
            {
                var result = SendFrame(frame);
            }));

        }

        private async System.Threading.Tasks.Task SendFrame(byte[] frame)
        {
            // create a new BitmapImage from the JPEG bytes
            BitmapSource bitmap = await WpfUtilities.LoadImageAsync(new MemoryStream(frame));

            // tell whoever's listening that we have a frame to draw
            if (FrameReady != null)
            {
                FrameReady(this, new FrameReadyEventArgs { FrameBuffer = frame, BitmapSource = bitmap });
            }
        }

    }

    public class FrameReadyEventArgs : EventArgs
    {
        public byte[] FrameBuffer;
        public BitmapSource BitmapSource;
    }

    public sealed class ErrorEventArgs : EventArgs
    {
        public string Message { get; set; }
        public int ErrorCode { get; set; }
        public HttpStatusCode HttpResponse { get; set; }
    }
}