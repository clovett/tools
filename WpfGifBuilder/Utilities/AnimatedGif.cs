using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace WpfGifBuilder.Utilities
{
    class AnimatedGifFrame
    {
        public BitmapSource Bitmap;
        public uint Delay; // in milliseconds.
    }

    class AnimatedGif
    {
        // See https://github.com/microsoft/Windows-classic-samples/blob/main/Samples/Win7Samples/multimedia/wic/wicanimatedgif/WicAnimatedGif.vcproj

        enum DisposalMethods
        {
            DM_UNDEFINED = 0,
            DM_NONE = 1,
            DM_BACKGROUND = 2,
            DM_PREVIOUS = 3
        };

        [StructLayout(LayoutKind.Sequential)]
        struct ColorF
        {
            public float r;
            public float g;
            public float b;
            public float a;
        };

        [StructLayout(LayoutKind.Sequential)]
        struct RectF
        {
            public float left;
            public float top;
            public float right;
            public float bottom;
        };

        [StructLayout(LayoutKind.Sequential)]
        struct AnimatedGifMetadata
        {
            public ColorF backgroundColor;
            public int numFrames;
            public uint totalLoopCount;
            public uint cxGifImage;
            public uint cyGifImage;
            public uint cxGifImagePixel;  // Width of the displayed image in pixel calculated using pixel aspect ratio
            public uint cyGifImagePixel;  // Height of the displayed image in pixel calculated using pixel aspect ratio
            public bool hasLoop;      // Whether the gif has a loop
            public IntPtr handle;
        };

        [StructLayout(LayoutKind.Sequential)]
        struct AnimatedGifFrameMetadata
        {
            public uint frameDelay;
            public RectF framePosition;
            public DisposalMethods frameDisposal;
        };


        [DllImport("AnimatedGif", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        static extern int OpenAnimatedGif(String fileName, ref AnimatedGifMetadata gif);

        [DllImport("AnimatedGif", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        static extern int ReadFrameMetadata(ref AnimatedGifMetadata gif, uint frameIndex, ref AnimatedGifFrameMetadata frame);

        [DllImport("AnimatedGif", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        static extern void ReleaseAnimatedGif(ref AnimatedGifMetadata gif);

        AnimatedGifMetadata metadata;
        List<AnimatedGifFrameMetadata> frameMetatadata;
        List<BitmapFrame> originalFrames = new List<BitmapFrame>();
        BitmapSource previousFrame = null;
        List<BitmapSource> renderedFrames;
        int position = 0;
        int loopIndex = 0;


        public IEnumerable<BitmapFrame> OriginalFrames => this.originalFrames;

        public Size Size => new Size(metadata.cxGifImage, metadata.cyGifImage);

        public void ReadMetadata(string file)
        {
            this.metadata = new AnimatedGifMetadata();
            this.frameMetatadata = new List<AnimatedGifFrameMetadata>();
            this.originalFrames = new List<BitmapFrame>();
            this.previousFrame = null;
            this.position = 0;
            this.loopIndex = 0;

            int hr = OpenAnimatedGif(file, ref metadata);
            if(hr == 0)
            {
                for (uint i = 0; i < metadata.numFrames; i++)
                {
                    var frame = new AnimatedGifFrameMetadata();
                    hr = ReadFrameMetadata(ref metadata, i, ref frame);
                    if (hr == 0)
                    {
                        frameMetatadata.Add(frame);
                    }
                }
            }
            ReleaseAnimatedGif(ref metadata);

            BitmapDecoder bitmapDecoder = BitmapDecoder.Create(new Uri(file), BitmapCreateOptions.None, BitmapCacheOption.None);            
            foreach (var frame in bitmapDecoder.Frames)
            {
                var snapshot = BitmapFrame.Create(frame);
                originalFrames.Add(snapshot);
            }

            renderedFrames = new List<BitmapSource>();
            for (int i = 0; i < frameMetatadata.Count; i++)
            {
                var frame = this.RenderNextFrame(i);
                renderedFrames.Add(frame);
            }
        }

        public Color GetBackgroundColor()
        {
            return Color.FromArgb((byte)(metadata.backgroundColor.a * 255), (byte)(metadata.backgroundColor.r * 255), (byte)(metadata.backgroundColor.g * 255), (byte)(metadata.backgroundColor.b * 255));
        }

        /// <summary>
        /// Get the next frame in the 
        /// </summary>
        /// <returns></returns>
        public AnimatedGifFrame GetNextFrame()
        {
            if (renderedFrames.Count == 0 || frameMetatadata == null  || position >= renderedFrames.Count)
            {
                return null;
            }

            var metadata = this.frameMetatadata[position];
            var frame = this.renderedFrames[position];

            position++;
            if (position >= renderedFrames.Count && (!this.metadata.hasLoop || this.loopIndex < this.metadata.totalLoopCount))
            {
                position = 0;
                this.loopIndex++;
            }

            return new AnimatedGifFrame() { Bitmap = frame, Delay = metadata.frameDelay };
        }


        public void SaveToFile(String fileName)
        {
            BitmapEncoder encoder = new GifBitmapEncoder();
            foreach(var frame in this.originalFrames)
            {
                encoder.Frames.Add(frame);
            }

            // todo: create a metadata writer in C++.

            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                encoder.Save(fs);
            }
        }

        private BitmapSource RenderNextFrame(int position)
        {
            var frame = this.originalFrames[position];
            var metadata = this.frameMetatadata[position];

            int width = (int)this.metadata.cxGifImage;
            int height = (int)this.metadata.cyGifImage;
            RenderTargetBitmap newFrame = new RenderTargetBitmap(width, height, 96.0, 96.0, PixelFormats.Pbgra32);
            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();

            var frameRect = new Rect(new Point(metadata.framePosition.left, metadata.framePosition.top),
                new Size(metadata.framePosition.right - metadata.framePosition.left, metadata.framePosition.bottom - metadata.framePosition.top));

            // Debug.WriteLine("Frame {0} x {1} delay {2}, disposal {3}", frameRect.Width, frameRect.Height, metadata.frameDelay, metadata.frameDisposal);
            this.RestoreSavedFrame(drawingContext, frameRect);
            this.DisposeCurrentFrame(drawingContext, metadata, frameRect);
            this.OverlayNextFrame(drawingContext, frame, frameRect);
            drawingContext.Close();

            newFrame.Render(drawingVisual);
            this.previousFrame = newFrame;

            return newFrame;
        }

        private void OverlayNextFrame(DrawingContext drawingContext, BitmapFrame frame, Rect frameRect)
        {
            drawingContext.DrawImage(frame, frameRect);
        }

        private void RestoreSavedFrame(DrawingContext drawingContext, Rect frameRect)
        {
            if (this.previousFrame != null)
            {
                drawingContext.DrawImage(this.previousFrame, new Rect(new Point(0, 0),
                    new Size(this.previousFrame.Width, this.previousFrame.Height)));
            }
            else
            {
                // or clear the background color.
                drawingContext.DrawRectangle(new SolidColorBrush(this.GetBackgroundColor()), null, frameRect);
            }
        }

        private void DisposeCurrentFrame(DrawingContext drawingContext, AnimatedGifFrameMetadata metadata, Rect frameRect)
        {
            switch (metadata.frameDisposal)
            {
                case DisposalMethods.DM_UNDEFINED:
                case DisposalMethods.DM_NONE:
                    // We simply build on the previous frame, so nothing to do here.                    
                    break;
                case DisposalMethods.DM_BACKGROUND:
                    // Clear the area covered by the current raw frame with background color
                    this.ClearCurrentFrameArea(drawingContext, frameRect);
                    break;
                case DisposalMethods.DM_PREVIOUS:
                    // We restore the previous composed frame first (already done!)
                    break;
            }
        }

        private void ClearCurrentFrameArea(DrawingContext drawingContext, Rect frameRect)
        {
            drawingContext.DrawRectangle(new SolidColorBrush(this.GetBackgroundColor()), null, frameRect);
        }
    }
}
