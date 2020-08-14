using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SvgEditor
{
    internal class SvgDocument : BaseDocData
    {
        public SvgDocument(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override string DocExtension
        {
            get { return ".svg"; }
        }

        public override Guid EditorType
        {
            get { return typeof(EditorFactory).GUID; }
        }

        public override Guid DocGuid
        {
            get { return typeof(EditorFactory).GUID; }
        }

        /// <summary>
        /// Get the SaveAs format filter list.
        /// </summary>
        public override string FormatFilterList
        {
            get { return "SVG Files (*.svg)|.svg"; }
        }
    }
}
