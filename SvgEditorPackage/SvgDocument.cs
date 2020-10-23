using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.XmlEditor;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Package;
using IServiceProvider = System.IServiceProvider;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.SvgEditorPackage
{
    /// <summary>
    /// This class wraps the IVsTextLines docData and provides handy methods for managing
    /// the synchroinzation with this buffer.  This document is NOT the docData, it is just a 
    /// temporary wrapper.
    /// </summary>
    public class SvgDocument : IDisposable, IVsTextBufferDataEvents
    {
        IServiceProvider serviceProvider;
        IVsTextLines buffer;
        XmlStore store;
        XmlModel model;
        ITextBuffer newBuffer;

        string fileName;
        IConnectionPoint cp;
        uint cookie;

        public SvgDocument(IServiceProvider serviceProvider, IVsTextLines buffer, XmlStore store, string fileName)
        {
            this.serviceProvider = serviceProvider;
            this.buffer = buffer;
            this.store = store;
            this.fileName = fileName;

            newBuffer = GetNewBuffer();
            if (newBuffer != null)
            {
                // already loaded (probably had punkDocDataExisting)
                OnLoadCompleted(1);
                newBuffer.PostChanged += new EventHandler(OnBufferChanged);
            }
            else
            {
                // need to wait for load
                IConnectionPointContainer cpc = buffer as IConnectionPointContainer;
                Guid g = typeof(IVsTextBufferDataEvents).GUID;
                cpc.FindConnectionPoint(g, out cp);
                cp.Advise(this, out cookie);
            }
        }

        public event EventHandler BufferChanged;

        void OnBufferChanged(object sender, EventArgs e)
        {
            if (BufferChanged != null)
            {
                BufferChanged(this, EventArgs.Empty);
            }
        }

        public bool IsParsing
        {
            get
            {
                Guid xmlLangSvc = new Guid("f6819a78-a205-47b5-be1c-675b3c7f0b8e");
                Guid iunknown = new Guid("00000000-0000-0000-C000-000000000046");
                IOleServiceProvider sp = (IOleServiceProvider)serviceProvider.GetService(typeof(IOleServiceProvider));
                IntPtr pvar;
                sp.QueryService(ref xmlLangSvc, ref iunknown, out pvar);
                object xmlLanguageService;
                try
                {
                    xmlLanguageService = Marshal.GetObjectForIUnknown(pvar);
                }
                finally
                {
                    Marshal.Release(pvar);
                }

                LanguageService langsvc = (LanguageService)xmlLanguageService;
                return langsvc.IsParsing;
            }
        }

        public string FileName
        {
            get { return fileName; }
        }

        public XmlModel Model
        {
            get { return model; }
        }

        public XmlStore Store
        {
            get { return store; }
        }

        public void Dispose()
        {
            if (cp != null)
            {
                IConnectionPointContainer cpc = buffer as IConnectionPointContainer;
                Guid g = typeof(IVsTextBufferDataEvents).GUID;
                cpc.FindConnectionPoint(g, out cp);
                cp.Unadvise(cookie);
                cp = null;
            }

            using (store)
            {
                store = null;
            }
            using (model)
            {
                model = null;
            }
            if (newBuffer != null)
            {
                newBuffer.PostChanged -= new EventHandler(OnBufferChanged);
                newBuffer = null;
            }
            buffer = null;
        }

        public XDocument Document { get { return model == null ? null : model.Document; } }

        void OnBufferReloaded(object sender, EventArgs e)
        {
            OnReloaded();
        }

        public event EventHandler Reloaded;

        void OnReloaded()
        {
            if (Reloaded != null)
            {
                Reloaded(this, EventArgs.Empty);
            }
        }

        #region IVsTextBufferDataEvents
        public void OnFileChanged(uint grfChange, uint dwFileAttrs)
        {
        }

        public int OnLoadCompleted(int fReload)
        {
            // get new text buffer
            model = store.OpenXmlModel(new Uri(this.FileName));
            model.BufferReloaded += new EventHandler(OnBufferReloaded);

            if (newBuffer == null)
            {
                newBuffer = GetNewBuffer();
                // already loaded (probably had punkDocDataExisting)
                newBuffer.PostChanged += new EventHandler(OnBufferChanged);
            }

            OnReloaded();
            return 0;
        }

        private ITextBuffer GetNewBuffer()
        {
            IComponentModel componentModelHost = serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            if (componentModelHost != null)
            {
                IVsEditorAdaptersFactoryService editorAdapterFactory = componentModelHost.GetService<IVsEditorAdaptersFactoryService>();
                if (editorAdapterFactory != null)
                {
                    return editorAdapterFactory.GetDataBuffer(this.buffer);
                }
            }
            return null;
        }
        #endregion 

    }
}
