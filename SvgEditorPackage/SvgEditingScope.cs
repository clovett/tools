using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.XmlEditor;

namespace Microsoft.SvgEditorPackage
{

    /// <summary>
    /// This class wraps the underlying XmlEditingScope to work around a bug in the XML Editor.
    /// It has to ensure that the change is parsed by the XML editor when the XML view is not open.
    /// </summary>
    public class SvgEditingScope : IDisposable
    {
        XmlEditingScope scope;
        SvgWindowPane pane;

        public SvgEditingScope(SvgWindowPane pane, string caption)
        {
            this.pane = pane;
            this.scope = pane.Document.Model.Store.BeginEditingScope(caption, pane);
        }

        public void Dispose()
        {
            scope.Dispose();
            pane.OnReparseNeeded();
            pane = null;
            scope = null;
        }

        public void Complete()
        {
            scope.Complete();
        }
    }

}
