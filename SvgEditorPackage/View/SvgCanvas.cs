using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace Microsoft.SvgEditorPackage.View
{
    public class SvgCanvas : Canvas
    {
        XDocument doc;

        public SvgCanvas(XDocument doc)
        {
            // assert doc.Root.name == "svg"
            this.doc = doc;
            // todo: any global svg tag stuff...
        }

    }
}
