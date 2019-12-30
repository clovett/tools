using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MergePhotos
{
    class Metadata
    {
        string filename;
        XDocument doc;
        static XNamespace xmpNS = XNamespace.Get("adobe:ns:meta/");
        static XNamespace rdfNS = XNamespace.Get("http://www.w3.org/1999/02/22-rdf-syntax-ns#");
        static XNamespace exifNS = XNamespace.Get("http://ns.adobe.com/exif/1.0/");
        static XNamespace xapNS = XNamespace.Get("http://ns.adobe.com/xap/1.0/");
        static XNamespace mylioNS = XNamespace.Get("http://ns.mylollc.com/MyloEdit/");
        static XNamespace tiffNS = XNamespace.Get("http://ns.adobe.com/tiff/1.0/");
        static XNamespace xapmmNS = XNamespace.Get("http://ns.adobe.com/xap/1.0/mm/");
        static XNamespace dcNS = XNamespace.Get("http://purl.org/dc/elements/1.1/");
        static XNamespace mwgrsNS = XNamespace.Get("http://www.metadataworkinggroup.com/schemas/regions/");
        static XNamespace stDimNS = XNamespace.Get("http://ns.adobe.com/xap/1.0/sType/Dimensions#");
        static XNamespace stAreaNS = XNamespace.Get("http://ns.adobe.com/xmp/sType/Area#");
        static XNamespace iptc4xmpExtNS = XNamespace.Get("http://iptc.org/std/Iptc4xmpExt/2008-02-29/");

        static HashSet<XName> whitelist = new HashSet<XName>(new XName[] {
                exifNS + "DateTimeOriginal", exifNS + "ModifyDate", exifNS + "GPSLatitude", exifNS + "GPSLongitude",
                xapNS + "CreateDate", xapNS + "MetadataDate", mylioNS + "MetadataDate", xapmmNS + "DocumentID",
                xapmmNS + "OriginalDocumentID", xapmmNS + "InstanceID", xmpNS + "xmptk", mylioNS + "processVersion",
                mylioNS + "MetadataDate", mylioNS + "DateConfidence"
            });

        internal Metadata(string filename)
        {
            this.filename = filename;
            this.doc = XDocument.Load(filename);
        }

        public static Metadata Load(string filename)
        {
            return new Metadata(filename);
        }

        public bool IsSame(Metadata other)
        {
            return IsSame(this.doc.Root, other.doc.Root);
        }

        private static bool IsSame(XElement e1, XElement e2)
        {
            if (e1.Name != e2.Name)
            {
                Console.WriteLine("Element mismatch {0} != {1}", e1.Name.LocalName, e2.Name.LocalName);
                return false;
            }
            List<XAttribute> other = new List<XAttribute>(e2.Attributes());
            foreach (XAttribute a1 in e1.Attributes())
            {
                XAttribute a2 = e2.Attribute(a1.Name);
                if (a2 == null)
                {
                    // missing attribute means e1 has more which is ok
                    continue;
                }
                other.Remove(a2);
                if (!IsSame(a1, a2))
                {
                    return false;
                }
            }

            if (other.Count > 0)
            {
                foreach (var item in other.ToArray())
                {
                    if (whitelist.Contains(item.Name) || item.Name.Namespace == XNamespace.Xmlns)
                    {
                        other.Remove(item);
                    }
                }
                if (other.Count > 0)
                {
                    Console.Write("found additional attributes: ");
                    foreach (var item in other)
                    {
                        Console.Write(item.ToString());
                    }
                    Console.WriteLine();
                    return false;
                }
            }

            IEnumerable<XNode> c1 = e1.Nodes();
            IEnumerable<XNode> c2 = e2.Nodes();

            if (!c1.Any() && !c2.Any())
            {
                return true;
            }
            IEnumerator<XNode> cenum1 = c1.GetEnumerator();
            IEnumerator<XNode> cenum2 = c2.GetEnumerator();
            while (cenum1.MoveNext() && cenum2.MoveNext())
            {
                XNode cn1 = cenum1.Current;
                XNode cn2 = cenum2.Current;
                if (cn2 == null)
                {
                    // e1 has more
                    return true;
                }
                if (cn1.NodeType != cn2.NodeType) 
                {
                    return false;
                }
                if (cn1 is XElement)
                {
                    XElement ce1 = (XElement)cn1;
                    XElement ce2 = (XElement)cn2;
                    if (!IsSame(ce1, ce2))
                    {
                        return false;
                    }
                }
                else if (cn1 is XText)
                {
                    XText t1 = (XText)cn1;
                    XText t2 = (XText)cn2;
                    if (t1.Value != t2.Value)
                    {
                        return false;
                    }
                }
                else if (cn1 is XCData)
                {
                    XCData t1 = (XCData)cn1;
                    XCData t2 = (XCData)cn2;
                    if (t1.Value != t2.Value)
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }

        private static bool IsSame(XAttribute a1, XAttribute a2)
        {
            if (whitelist.Contains(a1.Name))
            {
                return true;
            }
            if (a1.Value != a2.Value)
            {
                return false;
            }
            return true;
        }
    }
}
