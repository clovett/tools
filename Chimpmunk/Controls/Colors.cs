using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.UI;
using Windows.UI.Xaml;

namespace Chimpmunk.Controls
{
    public class NamedColor : DependencyObject
    {
        public NamedColor(string name, Color c)
        {
            Name = name;
            Color = c;
        }

        public string Name
        {
            get { return (string)GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Name.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NameProperty =
            DependencyProperty.Register("Name", typeof(string), typeof(NamedColor), new PropertyMetadata(null));

        public Color Color
        {
            get { return (Color)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Color.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(Color), typeof(NamedColor), new PropertyMetadata(null));
    }

    public class ColorTable
    {
        Random random = new Random();
        bool loaded;
        private List<NamedColor> orderedList = new List<NamedColor>();
        private Dictionary<string, NamedColor> map = new Dictionary<string, NamedColor>();

        private static ColorTable instance;

        public static ColorTable Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ColorTable();
                }
                return instance;
            }
        }

        private ColorTable()
        {
        }

        public bool IsLoaded { get { return loaded; } }

        public async Task Load()
        {
            var folder = await Package.Current.InstalledLocation.GetFolderAsync("Assets");
            var file = await folder.GetFileAsync("ColorTable.xml");
            var stream = await file.OpenReadAsync();
            using (var stm = WindowsRuntimeStreamExtensions.AsStream(stream))
            {
                XDocument doc = XDocument.Load(stm);
                foreach (XElement color in doc.Root.Elements())
                {
                    string name = (string)color.Attribute("Name");
                    string colorValue = (string)color.Attribute("Value");
                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(colorValue))
                    {
                        NamedColor c = ParseColor(colorValue);
                        c.Name = name;
                        map[name] = c;
                        orderedList.Add(c);
                    }
                }
            }
            loaded = true;
        }

        internal NamedColor GetRandomColor()
        {
            Color c = Color.FromArgb(0xff, (byte)random.Next(0, 255), (byte)random.Next(0, 255), (byte)random.Next(0, 255));
            return new NamedColor(c.ToString(), c);
        }

        public IEnumerable<NamedColor> OrderedList
        {
            get { return orderedList; }
        }

        NamedColor FindColor(string name)
        {
            NamedColor c;
            if (map.TryGetValue(name, out c))
            {
                return c;
            }
            return new NamedColor(null, Windows.UI.Colors.Transparent);
        }

        public static NamedColor ParseColor(string value)
        {
            string val = value.Trim();
            if (string.IsNullOrEmpty(val))
            {
                return new NamedColor(value, Windows.UI.Colors.Transparent);
            }

            if (val.StartsWith("#"))
            {
                val = val.Replace("#", "");

                byte a = 0xff; // default opaque.
                byte r = 0x00;
                byte g = 0x00;
                byte b = 0x00;
                byte pos = 0;

                if (val.Length == 8)
                {
                    a = System.Convert.ToByte(val.Substring(pos, 2), 16);
                    pos = 2;
                }

                if (val.Length >= pos + 2)
                {
                    r = System.Convert.ToByte(val.Substring(pos, 2), 16);
                    pos += 2;
                }

                if (val.Length >= pos + 2)
                {
                    g = System.Convert.ToByte(val.Substring(pos, 2), 16);
                    pos += 2;
                }

                if (val.Length >= pos + 2)
                {
                    b = System.Convert.ToByte(val.Substring(pos, 2), 16);
                }

                return new NamedColor(value, Color.FromArgb(a, r, g, b));
            }
            else
            {
                ColorTable global = ColorTable.Instance;
                return global.FindColor(value);
            }
        }

    }
}
