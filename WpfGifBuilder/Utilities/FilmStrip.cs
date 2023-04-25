using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace WpfGifBuilder.Utilities
{
    public class FilmStrip
    {
        // TODO: Add GIF metadata to control frame timing and looping behavior.
        // See https://github.com/microsoft/Windows-classic-samples/blob/main/Samples/Win7Samples/multimedia/wic/wicanimatedgif/WicAnimatedGif.vcproj

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
    }
}
