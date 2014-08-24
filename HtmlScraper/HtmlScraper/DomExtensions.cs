using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace HtmlScraper
{
    static class DomExtensions
    {
        public static XmlNode FirstOrDefault(this XmlNodeList list)
        {
            if (list.Count > 0)
                return list.Item(0);
            return null;
        }
    }
}
