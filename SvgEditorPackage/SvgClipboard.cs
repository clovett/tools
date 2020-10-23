using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Windows;

namespace Microsoft.SvgEditorPackage
{
    static class SvgClipboard
    {
        static XNamespace svgNamespace = XNamespace.Get("http://www.w3.org/2000/svg");

        internal static void Copy(IEnumerable<XElement> elements)
        {
            XDocument fragment = new XDocument(
                new XElement(svgNamespace + "svg",
                    elements.ToArray()));

            Clipboard.SetData("SVG", fragment.ToString());
        }

        internal static IEnumerable<XElement> Parse()
        {
            try
            {
                string data = (string)Clipboard.GetData("SVG");
                if (!string.IsNullOrEmpty(data))
                {
                    XDocument fragment = XDocument.Parse(data);
                    return fragment.Root.Elements();
                }
            }
            catch
            {
                // ignore bad clipboard data.
            }

            return new XElement[0];
        }

        public static bool IsEmpty
        {
            get
            {
                try
                {
                    return !Clipboard.ContainsData("SVG");
                } catch {
                    return true;
                }
            }
        }
    }
}
