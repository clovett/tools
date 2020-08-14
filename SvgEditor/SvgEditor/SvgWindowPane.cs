using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SvgEditor
{
    class SvgWindowPane : WindowPane
    {
        private IServiceProvider _packageProvider;
        SvgDocument _doc;
        SvgControl _control;

        public SvgWindowPane(IServiceProvider packageProvider, SvgDocument doc)
        {
            _packageProvider = packageProvider;
            _doc = doc;
            _control = new SvgControl();
        }

        /// <summary>
        /// Return the GraphControl contained in this window.
        /// </summary>
        public SvgControl SvgControl
        {
            get { return _control; }
        }

        /// <summary>
        /// Returns the graph control as the content of the window.
        /// </summary>
        public override object Content
        {
            get
            {
                return _control;
            }
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        /// <summary>
        /// Called when the window is created.
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();
        }
    }
}
