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

namespace WpfGifBuilder.Utilities
{
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
            float r;
            float g;
            float b;
            float a;
        };

        [StructLayout(LayoutKind.Sequential)]
        struct RectF
        {
            float left;
            float top;
            float right;
            float bottom;

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
        List<BitmapFrame> frames = new List<BitmapFrame>();

        public void ReadMetadata(string file)
        {
            metadata = new AnimatedGifMetadata();
            frameMetatadata = new List<AnimatedGifFrameMetadata>();

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
                //Image image = new Image();
                //image.Source = snapshot;
                //image.Width = 200;
                //image.Margin = new Thickness(10);
                var snapshot = BitmapFrame.Create(frame);
                frames.Add(snapshot);
            }
        }

        private BitmapEncoder encoder = new GifBitmapEncoder();

        protected BitmapEncoder Encoder
        {
            get { return encoder; }
        }

        public void AddFrame(BitmapFrame frame)
        {
            Encoder.Frames.Add(frame);
        }

        public void AddFrame(BitmapSource source)
        {
            AddFrame(BitmapFrame.Create(source));
        }

        public void SaveToFile(String fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                Encoder.Save(fs);
            }
        }


        /*
         * Drawing algorithm for animated gif frames
        
        
        HRESULT OverlayNextFrame()
        {
            // Get Frame information
            HRESULT hr = GetRawFrame(m_uNextFrameIndex);
            if (SUCCEEDED(hr))
            {
                // For disposal 3 method, we would want to save a copy of the current
                // composed frame
                if (m_uFrameDisposal == DM_PREVIOUS)
                {
                    hr = SaveComposedFrame();
                }
            }

            if (SUCCEEDED(hr))
            {
                // Start producing the next bitmap
                m_pFrameComposeRT->BeginDraw();

                // If starting a new animation loop
                if (m_uNextFrameIndex == 0)
                {
                    // Draw background and increase loop count
                    m_pFrameComposeRT->Clear(m_backgroundColor);
                    m_uLoopNumber++;
                }

                // Produce the next frame
                m_pFrameComposeRT->DrawBitmap(
                    m_pRawFrame,
                    m_framePosition);

                hr = m_pFrameComposeRT->EndDraw();
            }

            // To improve performance and avoid decoding/composing this frame in the 
            // following animation loops, the composed frame can be cached here in system 
            // or video memory.

            if (SUCCEEDED(hr))
            {
                // Increase the frame index by 1
                m_uNextFrameIndex = (++m_uNextFrameIndex) % m_cFrames;
            }

            return hr;
        }


        HRESULT SaveComposedFrame()
        {
            HRESULT hr = S_OK;

            ID2D1Bitmap* pFrameToBeSaved = NULL;

            hr = m_pFrameComposeRT->GetBitmap(&pFrameToBeSaved);
            if (SUCCEEDED(hr))
            {
                // Create the temporary bitmap if it hasn't been created yet 
                if (m_pSavedFrame == NULL)
                {
                    D2D1_SIZE_U bitmapSize = pFrameToBeSaved->GetPixelSize();
                    D2D1_BITMAP_PROPERTIES bitmapProp;
                    pFrameToBeSaved->GetDpi(&bitmapProp.dpiX, &bitmapProp.dpiY);
                    bitmapProp.pixelFormat = pFrameToBeSaved->GetPixelFormat();

                    hr = m_pFrameComposeRT->CreateBitmap(
                        bitmapSize,
                        bitmapProp,
                        &m_pSavedFrame);
                }
            }

            if (SUCCEEDED(hr))
            {
                // Copy the whole bitmap
                hr = m_pSavedFrame->CopyFromBitmap(NULL, pFrameToBeSaved, NULL);
            }

            SafeRelease(pFrameToBeSaved);

            return hr;
        }


        HRESULT RestoreSavedFrame()
        {
            HRESULT hr = S_OK;

            ID2D1Bitmap* pFrameToCopyTo = NULL;

            hr = m_pSavedFrame ? S_OK : E_FAIL;

            if (SUCCEEDED(hr))
            {
                hr = m_pFrameComposeRT->GetBitmap(&pFrameToCopyTo);
            }

            if (SUCCEEDED(hr))
            {
                // Copy the whole bitmap
                hr = pFrameToCopyTo->CopyFromBitmap(NULL, m_pSavedFrame, NULL);
            }

            SafeRelease(pFrameToCopyTo);

            return hr;
        }

        HRESULT ClearCurrentFrameArea()
        {
            m_pFrameComposeRT->BeginDraw();

            // Clip the render target to the size of the raw frame
            m_pFrameComposeRT->PushAxisAlignedClip(
                &m_framePosition,
                D2D1_ANTIALIAS_MODE_PER_PRIMITIVE);

            m_pFrameComposeRT->Clear(m_backgroundColor);

            // Remove the clipping
            m_pFrameComposeRT->PopAxisAlignedClip();

            return m_pFrameComposeRT->EndDraw();
        }


        HRESULT DisposeCurrentFrame()
        {
            HRESULT hr = S_OK;

            switch (m_uFrameDisposal)
            {
            case DM_UNDEFINED:
            case DM_NONE:
                // We simply draw on the previous frames. Do nothing here.
                break;
            case DM_BACKGROUND:
                // Dispose background
                // Clear the area covered by the current raw frame with background color
                hr = ClearCurrentFrameArea();
                break;
            case DM_PREVIOUS:
                // Dispose previous
                // We restore the previous composed frame first
                hr = RestoreSavedFrame();
                break;
            default:
                // Invalid disposal method
                hr = E_FAIL;
            }

            return hr;
        }


        HRESULT ComposeNextFrame()
        {
            HRESULT hr = S_OK;

            // Check to see if the render targets are initialized
            if (m_pHwndRT && m_pFrameComposeRT)
            {
                // Compose one frame
                hr = DisposeCurrentFrame();
                if (SUCCEEDED(hr))
                {
                    hr = OverlayNextFrame();
                }

                // Keep composing frames until we see a frame with delay greater than
                // 0 (0 delay frames are the invisible intermediate frames), or until
                // we have reached the very last frame.
                while (SUCCEEDED(hr) && m_uFrameDelay == 0 && !IsLastFrame())
                {
                    hr = DisposeCurrentFrame();
                    if (SUCCEEDED(hr))
                    {
                        hr = OverlayNextFrame();
                    }
                }

                // If we have more frames to play, set the timer according to the delay.
                // Set the timer regardless of whether we succeeded in composing a frame
                // to try our best to continue displaying the animation.
                if (!EndOfAnimation() && m_cFrames > 1)
                {
                    // Set the timer according to the delay
                    // SetTimer(m_hWnd, DELAY_TIMER_ID, m_uFrameDelay, NULL);
                }
            }

            return hr;
        }

        BOOL EndOfAnimation()
        {
            return m_fHasLoop && IsLastFrame() && m_uLoopNumber == m_uTotalLoopCount + 1;
        }

        BOOL IsLastFrame()
        {
            return (m_uNextFrameIndex == 0);
        }

    */

    }
}
