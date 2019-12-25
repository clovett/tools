using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace MergePhotos
{
    class Metadata
    {
        public string xmptk;
        public string about;
        public string DateTimeOriginal;
        public string ModifyDate;
        public string GPSVersionID;
        public string GPSLatitude;
        public string GPSLongitude;
        public string SubSecTime;
        public string SubSecTimeOriginal;
        public string SubSecTimeDigitized;
        public string CreateDate;
        public string Rating;
        public string MetadataDate;
        public string DateConfidence;
        public string Flag;
        public string ProcessVersion;
        public string MylioMetadataDate;
        public string DateRangeStart;
        public string DateRangeEnd;
        public string DateRangeScope;
        public string Undated;
        public string TiffOrientation;
        public string DocumentID;
        public string OriginalDocumentID;
        public string InstanceID;
        public List<string> ISOSpeedRatings;
        public List<string> Subjects;
        public Regions Regions;
        public List<string> PersonInImage;

        static XNamespace xmpNS = XNamespace.Get("adobe:ns:meta/");
        static XNamespace rdfNS = XNamespace.Get("http://www.w3.org/1999/02/22-rdf-syntax-ns#");
        static XNamespace exifNS  = XNamespace.Get("http://ns.adobe.com/exif/1.0/");
        static XNamespace xapNS = XNamespace.Get("http://ns.adobe.com/xap/1.0/");
        static XNamespace mylioNS = XNamespace.Get("http://ns.mylollc.com/MyloEdit/");
        static XNamespace tiffNS = XNamespace.Get("http://ns.adobe.com/tiff/1.0/");
        static XNamespace xapmmNS = XNamespace.Get("http://ns.adobe.com/xap/1.0/mm/");
        static XNamespace dcNS = XNamespace.Get("http://purl.org/dc/elements/1.1/");
        static XNamespace mwgrsNS = XNamespace.Get("http://www.metadataworkinggroup.com/schemas/regions/");
        static XNamespace stDimNS = XNamespace.Get("http://ns.adobe.com/xap/1.0/sType/Dimensions#");
        static XNamespace stAreaNS = XNamespace.Get("http://ns.adobe.com/xmp/sType/Area#");
        static XNamespace iptc4xmpExtNS = XNamespace.Get("http://iptc.org/std/Iptc4xmpExt/2008-02-29/");


        public static Metadata Load(string filename)
        {
            Metadata result = new Metadata();
            XDocument source = XDocument.Load(filename);
            if (source.Root.Name.Namespace == xmpNS && source.Root.Name.LocalName == "xmpmeta")
            {
                result.xmptk = (string)source.Root.Attribute(xmpNS + "xmptk");
                foreach (XElement rdf in source.Root.Elements())
                {
                    if (rdf.Name.Namespace == rdfNS && rdf.Name.LocalName == "RDF")
                    {
                        foreach (XElement description in rdf.Elements())
                        {
                            if (description.Name.Namespace == rdfNS && description.Name.LocalName == "Description")
                            {
                                foreach (XAttribute a in description.Attributes())
                                {
                                    XName name = a.Name;
                                    if (name.Namespace == XNamespace.Xmlns)
                                    {
                                        // skip xmlns attributes
                                    }
                                    else if (name == rdfNS + "about")
                                    {
                                        result.about = a.Value;
                                    }
                                    else if(name == exifNS + "DateTimeOriginal")
                                    {
                                        result.DateTimeOriginal = a.Value;
                                    }
                                    else if (name == exifNS + "ModifyDate")
                                    {
                                        result.ModifyDate = a.Value;
                                    }
                                    else if (name == exifNS + "GPSVersionID")
                                    {
                                        result.GPSVersionID = a.Value;
                                    }
                                    else if (name == exifNS + "GPSLatitude")
                                    {
                                        result.GPSLatitude = a.Value;
                                    }
                                    else if (name == exifNS + "GPSLongitude")
                                    {
                                        result.GPSLongitude = a.Value;
                                    }
                                    else if (name == exifNS + "SubSecTime")
                                    {
                                        result.SubSecTime = a.Value;
                                    }
                                    else if (name == exifNS + "SubSecTimeOriginal")
                                    {
                                        result.SubSecTimeOriginal = a.Value;
                                    }
                                    else if (name == exifNS + "SubSecTimeDigitized")
                                    {
                                        result.SubSecTimeDigitized = a.Value;
                                    }
                                    else if (name == xapNS + "CreateDate")
                                    {
                                        result.CreateDate = a.Value;
                                    }
                                    else if (name == xapNS + "ModifyDate")
                                    {
                                        if (result.ModifyDate == null)
                                        {
                                            result.ModifyDate = a.Value;
                                        }
                                        else if (result.ModifyDate != a.Value)
                                        {
                                            throw new Exception("Conflicting metadata ModifyDate attributes");
                                        }
                                    }
                                    else if (name == xapNS + "Rating")
                                    {
                                        result.Rating = a.Value;
                                    }
                                    else if (name == xapNS + "MetadataDate")
                                    {
                                        result.MetadataDate = a.Value;
                                    }
                                    else if (name == mylioNS + "DateConfidence")
                                    {
                                        result.DateConfidence = a.Value;
                                    }
                                    else if (name == mylioNS + "flag")
                                    {
                                        result.Flag = a.Value;
                                    }
                                    else if (name == mylioNS + "processVersion")
                                    {
                                        result.ProcessVersion = a.Value;
                                    }
                                    else if (name == mylioNS + "MetadataDate")
                                    {
                                        result.MylioMetadataDate = a.Value;
                                    }
                                    else if (name == mylioNS + "DateRangeStart")
                                    {
                                        result.DateRangeStart = a.Value;
                                    }
                                    else if (name == mylioNS + "DateRangeEnd")
                                    {
                                        result.DateRangeEnd = a.Value;
                                    }
                                    else if (name == mylioNS + "DateRangeScope")
                                    {
                                        result.DateRangeScope = a.Value;
                                    }
                                    else if (name == mylioNS + "Undated")
                                    {
                                        result.Undated = a.Value;
                                    }
                                    else if (name == tiffNS + "Orientation")
                                    {
                                        result.TiffOrientation = a.Value;
                                    }
                                    else if (name == xapmmNS + "DocumentID")
                                    {
                                        result.DocumentID = a.Value;
                                    }
                                    else if (name == xapmmNS + "OriginalDocumentID")
                                    {
                                        result.OriginalDocumentID = a.Value;
                                    }
                                    else if (name == xapmmNS + "InstanceID")
                                    {
                                        result.InstanceID = a.Value;
                                    }
                                    else
                                    {
                                        throw new Exception("Unknown metadata attribute");
                                    }
                                }

                                foreach (XElement child in description.Elements())
                                {
                                    if (child.Name == exifNS + "ISOSpeedRatings")
                                    {
                                        ParseRdfSequence(ref result.ISOSpeedRatings, child);
                                    }
                                    else if (child.Name == dcNS + "subject")
                                    {
                                        ParseRdfBag(ref result.Subjects, child);
                                    }
                                    else if (child.Name == iptc4xmpExtNS + "PersonInImage")
                                    {
                                        ParseRdfSequence(ref result.PersonInImage, child);
                                    }
                                    else if (child.Name == mwgrsNS + "Regions")
                                    {
                                        Regions regions = new Regions();
                                        regions.ParseRegions(child);
                                    }
                                    else
                                    {
                                        throw new Exception("Unknown metadata element");
                                    }
                                }
                            }
                            else
                            {
                                throw new Exception("Unknown metadata element: " + description.Name);
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("Unknown metadata element: " + rdf.Name);
                    }
     
                }
            }

            return result;
        }

        private static void ParseRdfSequence(ref List<string> result, XElement child)
        {
            var seq = child.Elements(rdfNS + "Seq");
            if (seq != null)
            {
                foreach (XElement e in seq.Elements())
                {
                    if (e.Name == rdfNS + "li")
                    {
                        if (result == null)
                        {
                            result = new List<string>();
                        }
                        result.Add(e.Value);
                    }
                }
            }
            else
            {
                throw new Exception("Expecting rdf:Seq element");
            }
        }

        private static void ParseRdfBag(ref List<string> result, XElement child)
        {
            var bag = child.Elements(rdfNS + "Bag");
            if (bag != null)
            {
                foreach (XElement e in bag.Elements())
                {
                    if (e.Name == rdfNS + "li")
                    {
                        if (result == null)
                        {
                            result = new List<string>();
                        }
                        result.Add(e.Value);
                    }
                }
            }
            else
            {
                throw new Exception("Expecting rdf:Bag element");
            }
        }

        /// <summary>
        /// Merge other metdata into this metadata, and throw exception if there is an unreconcilable
        /// merge conflict.
        /// </summary>
        /// <param name="other">Another Metadata object</param>
        /// <returns>True if a substantial change was found</returns>        
        public bool Merge(Metadata other)
        {
            return MergeDateValue(DateTimeOriginal, other.DateTimeOriginal) &&
                MergeDateValue(ModifyDate, other.ModifyDate) &&
                MergeValue(ref GPSVersionID, other.GPSVersionID) &&
                MergeValue(ref GPSLatitude, other.GPSLatitude) &&
                MergeValue(ref GPSLongitude, other.GPSLongitude) &&
                MergeValue(ref SubSecTime, other.SubSecTime) &&
                MergeValue(ref SubSecTimeOriginal, other.SubSecTimeOriginal) &&
                MergeValue(ref SubSecTimeDigitized, other.SubSecTimeDigitized) &&
                MergeDateValue(CreateDate, other.CreateDate) &&
                MergeValue(ref Rating, other.Rating) &&
                //MergeIfNull(ref MetadataDate, other.MetadataDate) &&
                MergeValue(ref DateConfidence, other.DateConfidence) &&
                MergeValue(ref Flag, other.Flag) &&
                MergeValue(ref ProcessVersion, other.ProcessVersion) &&
                //MergeIfNull(ref MylioMetadataDate, other.MylioMetadataDate) &&
                MergeValue(ref DateRangeStart, other.DateRangeStart) &&
                MergeValue(ref DateRangeEnd, other.DateRangeEnd) &&
                MergeValue(ref DateRangeScope, other.DateRangeScope) &&
                MergeValue(ref Undated, other.Undated) &&
                MergeValue(ref TiffOrientation, other.TiffOrientation) &&
                MergeIfNull(ref DocumentID, other.DocumentID) &&
                MergeIfNull(ref OriginalDocumentID, other.OriginalDocumentID) &&
                MergeIfNull(ref InstanceID, other.InstanceID) &&
                MergeList(ref ISOSpeedRatings, other.ISOSpeedRatings) &&
                MergeList(ref PersonInImage, other.PersonInImage) &&
                MergeList(ref Subjects, other.Subjects) &&
                MergeRegions(ref Regions, other.Regions);
        }

        private bool MergeDateValue(string master, string other)
        {
            if (master == other)
            {
                return true;
            }
            DateTime d1;
            DateTime d2;
            if (DateTime.TryParse(master, out d1) && DateTime.TryParse(other, out d2) && 
                d1.Year == d2.Year && d1.Month == d2.Month && d1.Day == d2.Day && d1.Hour == d2.Hour && d1.Minute == d2.Minute && d1.Second == d2.Second)
            {
                // good enough!
                return true;
            }
            return false;
        }

        private bool MergeRegions(ref Regions r1, Regions r2)
        {
            if (r2 == null)
            {
                return true;
            }
            if (r1 == null)
            {
                r1 = new Regions(r2);
                return true;
            }
            return r1.Merge(r2);
        }

        private bool MergeList(ref List<string> a, List<string> b)
        {
            if (b == null)
            {
                return true;
            }
            if (a == null)
            {
                a = new List<string>(b);
                return true;
            }

            // hmmm, well for keywords at least, we generally know the more the merrier...
            // but if it was a typo fix or something, this is will be very bad...
            foreach(string x in a)
            {
                if (!b.Contains(x))
                {
                    b.Add(x);
                }
            }
            return true;
        }

        bool MergeValue(ref string target, string source)
        {
            if (target == source)
            {
                return true;
            }
            if (target == null)
            {
                target = source;
                return true;
            }
            return false;
        }

        bool MergeIfNull(ref string target, string source)
        {
            if (target == null)
            {
                target = source;
            }
            return true;
        }

        internal void Save(string targetMetadata)
        {
            var root = new XElement(xmpNS + "x");
            root.SetAttributeValue(XNamespace.Xmlns + "x", xmpNS.ToString());
            if (!string.IsNullOrEmpty(xmptk)) {
                root.SetAttributeValue(xmpNS + "xmptk", this.xmptk);
            }
            var rdf = new XElement(rdfNS + "RDF");
            rdf.SetAttributeValue(XNamespace.Xmlns + "rdf", rdfNS.ToString());
            root.Add(rdf);

            var description = new XElement(rdfNS + "Description");
            description.SetAttributeValue(rdfNS + "about", this.about);
            rdf.Add(description);

            if (!string.IsNullOrEmpty(this.DateTimeOriginal))
            {
                description.SetAttributeValue(exifNS + "DateTimeOriginal", this.DateTimeOriginal);
            }
            if (!string.IsNullOrEmpty(this.ModifyDate))
            {
                description.SetAttributeValue(exifNS + "ModifyDate", this.ModifyDate);
            }
            if (!string.IsNullOrEmpty(this.GPSVersionID))
            {
                description.SetAttributeValue(exifNS + "GPSVersionID", this.GPSVersionID);
            }
            if (!string.IsNullOrEmpty(this.GPSLatitude))
            {
                description.SetAttributeValue(exifNS + "GPSLatitude", this.GPSLatitude);
            }
            if (!string.IsNullOrEmpty(this.GPSLongitude))
            {
                description.SetAttributeValue(exifNS + "GPSLongitude", this.GPSLongitude);
            }
            if (!string.IsNullOrEmpty(this.SubSecTime))
            {
                description.SetAttributeValue(exifNS + "SubSecTime", this.SubSecTime);
            }
            if (!string.IsNullOrEmpty(this.SubSecTimeOriginal))
            {
                description.SetAttributeValue(exifNS + "SubSecTimeOriginal", this.SubSecTimeOriginal);
            }
            if (!string.IsNullOrEmpty(this.SubSecTimeDigitized))
            {
                description.SetAttributeValue(exifNS + "SubSecTimeDigitized", this.SubSecTimeDigitized);
            }
            if (!string.IsNullOrEmpty(this.CreateDate))
            {
                description.SetAttributeValue(xapNS + "CreateDate", this.CreateDate);
            }
            if (!string.IsNullOrEmpty(this.MetadataDate))
            {
                description.SetAttributeValue(xapNS + "MetadataDate", this.MetadataDate);
            }
            if (!string.IsNullOrEmpty(this.Rating))
            {
                description.SetAttributeValue(xapNS + "Rating", this.Rating);
            }
            if (!string.IsNullOrEmpty(this.DateConfidence))
            {
                description.SetAttributeValue(mylioNS + "MylioDateConfidence", this.DateConfidence);
            }
            if (!string.IsNullOrEmpty(this.Flag))
            {
                description.SetAttributeValue(mylioNS + "MylioFlag", this.Flag);
            }
            if (!string.IsNullOrEmpty(this.ProcessVersion))
            {
                description.SetAttributeValue(mylioNS + "MylopProcessVersion", this.ProcessVersion);
            }
            if (!string.IsNullOrEmpty(this.MylioMetadataDate))
            {
                description.SetAttributeValue(mylioNS + "MylioMetadataDate", this.MylioMetadataDate);
            }
            if (!string.IsNullOrEmpty(this.DateRangeStart))
            {
                description.SetAttributeValue(mylioNS + "DateRangeStart", this.DateRangeStart);
            }

            if (!string.IsNullOrEmpty(this.DateRangeEnd))
            {
                description.SetAttributeValue(mylioNS + "DateRangeEnd", this.DateRangeEnd);
            }

            if (!string.IsNullOrEmpty(this.DateRangeScope))
            {
                description.SetAttributeValue(mylioNS + "DateRangeScope", this.DateRangeScope);
            }

            if (!string.IsNullOrEmpty(this.Undated))
            {
                description.SetAttributeValue(mylioNS + "Undated", this.Undated);
            }
            if (!string.IsNullOrEmpty(this.TiffOrientation))
            {
                description.SetAttributeValue(tiffNS + "TiffOrientation", this.TiffOrientation);
            }
            if (!string.IsNullOrEmpty(this.DocumentID))
            {
                description.SetAttributeValue(xapmmNS + "DocumentID", this.DocumentID);
            }
            if (!string.IsNullOrEmpty(this.OriginalDocumentID))
            {
                description.SetAttributeValue(xapmmNS + "OriginalDocumentID", this.OriginalDocumentID);
            }
            if (!string.IsNullOrEmpty(this.InstanceID))
            {
                description.SetAttributeValue(xapmmNS + "InstanceID", this.InstanceID);
            }
            if (this.ISOSpeedRatings != null)
            {
                var seq = new XElement(rdfNS + "Seq");
                foreach(var speed in this.ISOSpeedRatings)
                {
                    seq.Add(new XElement(rdfNS + "li", speed));
                }
                description.Add(new XElement(exifNS + "ISOSpeedRatings", seq));
            }
            if (this.Subjects != null)
            {
                var bag = new XElement(rdfNS + "Bag");
                foreach (var speed in this.Subjects)
                {
                    bag.Add(new XElement(rdfNS + "li", speed));
                }
                description.Add(new XElement(dcNS + "subject", bag));
            }
            if (this.Regions != null)
            {
                description.Add(this.Regions.ToXElement());
            }
            if (this.PersonInImage != null)
            {
                var seq = new XElement(rdfNS + "Seq");
                foreach (var name in this.PersonInImage)
                {
                    seq.Add(new XElement(rdfNS + "li", name));
                }
                description.Add(new XElement(iptc4xmpExtNS + "PersonInImage", seq));
            }

            XDocument doc = new XDocument(root);
            doc.Save(targetMetadata);
        }
    }

    class Region
    {
        public string Type;
        public string Name;
        public string Description;
        public string x;
        public string y;
        public string w;
        public string h;
        public string unit;

        static XNamespace rdfNS = XNamespace.Get("http://www.w3.org/1999/02/22-rdf-syntax-ns#");
        static XNamespace mwgrsNS = XNamespace.Get("http://www.metadataworkinggroup.com/schemas/regions/");
        static XNamespace stAreaNS = XNamespace.Get("http://ns.adobe.com/xmp/sType/Area#");

        internal void Parse(XElement li)
        {
            foreach (XElement e in li.Elements())
            {
                if (e.Name == rdfNS + "Description")
                {
                    Type = (string)e.Attribute(mwgrsNS + "Type");
                    Name = (string)e.Attribute(mwgrsNS + "Name");
                    Description = (string)e.Attribute(mwgrsNS + "Description");

                    foreach (XElement f in e.Elements())
                    {
                        if (f.Name == mwgrsNS + "Area")
                        {
                            x = (string)e.Attribute(stAreaNS + "x");
                            y = (string)e.Attribute(stAreaNS + "y");
                            w = (string)e.Attribute(stAreaNS + "w");
                            h = (string)e.Attribute(stAreaNS + "h");
                            unit = (string)e.Attribute(stAreaNS + "unit");
                        }
                        else
                        {
                            throw new Exception("Unknown metadata element: " + f.Name);
                        }
                    }
                }
                else
                {
                    throw new Exception("Unknown metadata element: " + e.Name);
                }
            }
        }

        public override bool Equals(object obj)
        {
            Region r1 = this;
            Region r2 = obj as Region;
            if (r2 == null)
            {
                return false;
            }
            return r1.Type == r2.Type && r1.Name == r2.Name && r1.Description == r2.Description &&
                   r1.x == r2.x && r1.y == r2.y && r1.w == r2.w && r1.unit == r2.unit;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        internal object ToXElement()
        {
            return new XElement(rdfNS + "Description",
                        new XAttribute(mwgrsNS + "Type", this.Type),
                        new XAttribute(mwgrsNS + "Name", this.Name),
                        new XAttribute(mwgrsNS + "Description", this.Description),
                        new XElement(mwgrsNS + "Area",
                            new XAttribute(stAreaNS + "x", this.x),
                            new XAttribute(stAreaNS + "y", this.y),
                            new XAttribute(stAreaNS + "w", this.w),
                            new XAttribute(stAreaNS + "h", this.h),
                            new XAttribute(stAreaNS + "unit", this.unit)
                        )
                    );
        }
    }

    class Regions
    {
        public string width;
        public string height;
        public string unit;
        public List<Region> regions;

        static XNamespace rdfNS = XNamespace.Get("http://www.w3.org/1999/02/22-rdf-syntax-ns#");
        static XNamespace mwgrsNS = XNamespace.Get("http://www.metadataworkinggroup.com/schemas/regions/");
        static XNamespace stDimNS = XNamespace.Get("http://ns.adobe.com/xap/1.0/sType/Dimensions#");

        public Regions() { }

        public Regions(Regions other)
        {
            width = other.width;
            height = other.height;
            unit = other.unit;
            regions = new List<Region>(other.regions);
        }

        public bool Merge(Regions other)
        {
            if (other == null)
            {
                return true;
            }
            if (this.width == other.width && this.height == other.height && this.unit == other.unit)
            {
                if (this.regions == null)
                {
                    this.regions = new List<Region>(other.regions);
                    return true;
                }
                if (other.regions == null)
                {
                    return true;
                }
                if (this.regions.Count == other.regions.Count)
                {
                    for (int i = 0; i < this.regions.Count; i++)
                    {
                        Region r1 = this.regions[i];
                        Region r2 = other.regions[i];
                        if (!r1.Equals(r2))
                        {
                            throw new Exception("Cannot merge conflicting region info");
                        }
                    }
                }
            }

            throw new Exception("Cannot merge conflicting region info");
        }

        internal void ParseRegions(XElement element)
        {
            var parseType = (string)element.Attribute(rdfNS + "parseType");
            if (parseType != "Resource")
            {
                throw new Exception("Unknown metadata parseType: " + parseType);
            }
            foreach (var child in element.Elements())
            {
                if (child.Name == mwgrsNS + "AppliedToDimensions")
                {
                    width = (string)child.Attribute(stDimNS + "w");
                    height = (string)child.Attribute(stDimNS + "h");
                    unit= (string)child.Attribute(stDimNS + "unit");
                }
                else if (child.Name == mwgrsNS + "RegionList")
                {
                    var seq = child.Elements(rdfNS + "Seq");
                    if (seq != null)
                    {
                        foreach (XElement e in seq.Elements())
                        {
                            if (e.Name == rdfNS + "li")
                            {
                                if (regions == null)
                                {
                                    regions = new List<Region>();
                                }
                                var region = new Region();
                                region.Parse(e);
                                regions.Add(region);
                            }
                        }
                    }
                    else
                    {
                        throw new Exception("Expecting rdf:Seq element");
                    }
                }
                else
                {
                    throw new Exception("Unknown metadata element: " + child.Name);
                }
            }
        }

        internal object ToXElement()
        {
            return new XElement(mwgrsNS + "Regions", new XAttribute(rdfNS + "parseType", "Resource"),
                new XElement(mwgrsNS + "AppliedToDimensions",
                    new XAttribute(stDimNS + "w", this.width),
                    new XAttribute(stDimNS + "h", this.height),
                    new XAttribute(stDimNS + "unit", this.unit),
                    new XElement(mwgrsNS + "RegionList",
                        new XElement(rdfNS + "Seq",
                            from i in this.regions select new XElement(rdfNS + "li",
                                i.ToXElement()
                                )
                            )
                        )
                    )
                );
        }
    }
}
