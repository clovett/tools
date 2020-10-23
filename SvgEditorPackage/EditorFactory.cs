using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.XmlEditor;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.SvgEditorPackage
{
    /// <summary>
    /// Factory for creating our editor object. Extends from the IVsEditoryFactory interface
    /// </summary>
    [Guid(GuidList.guidSvgEditorPackageEditorFactoryString)]
    public sealed class EditorFactory : IVsEditorFactory, IDisposable
    {
        private SvgEditorPackage editorPackage;
        private ServiceProvider vsServiceProvider;


        public EditorFactory(SvgEditorPackage package)
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering {0} constructor", this.ToString()));

            this.editorPackage = package;
        }

        /// <summary>
        /// Since we create a ServiceProvider which implements IDisposable we
        /// also need to implement IDisposable to make sure that the ServiceProvider's
        /// Dispose method gets called.
        /// </summary>
        public void Dispose()
        {
            if (vsServiceProvider != null)
            {
                vsServiceProvider.Dispose();
            }
        }

        #region IVsEditorFactory Members

        /// <summary>
        /// Used for initialization of the editor in the environment
        /// </summary>
        /// <param name="psp">pointer to the service provider. Can be used to obtain instances of other interfaces
        /// </param>
        /// <returns></returns>
        public int SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp)
        {
            vsServiceProvider = new ServiceProvider(psp);
            return VSConstants.S_OK;
        }

        public T GetService<T>()
        {
            return (T)vsServiceProvider.GetService(typeof(T));
        }

        public T GetService<S,T>()
        {
            return (T)vsServiceProvider.GetService(typeof(S));
        }

        // This method is called by the Environment (inside IVsUIShellOpenDocument::
        // OpenStandardEditor and OpenSpecificEditor) to map a LOGICAL view to a 
        // PHYSICAL view. A LOGICAL view identifies the purpose of the view that is
        // desired (e.g. a view appropriate for Debugging [LOGVIEWID_Debugging], or a 
        // view appropriate for text view manipulation as by navigating to a find
        // result [LOGVIEWID_TextView]). A PHYSICAL view identifies an actual type 
        // of view implementation that an IVsEditorFactory can create. 
        //
        // NOTE: Physical views are identified by a string of your choice with the 
        // one constraint that the default/primary physical view for an editor  
        // *MUST* use a NULL string as its physical view name (*pbstrPhysicalView = NULL).
        //
        // NOTE: It is essential that the implementation of MapLogicalView properly
        // validates that the LogicalView desired is actually supported by the editor.
        // If an unsupported LogicalView is requested then E_NOTIMPL must be returned.
        //
        // NOTE: The special Logical Views supported by an Editor Factory must also 
        // be registered in the local registry hive. LOGVIEWID_Primary is implicitly 
        // supported by all editor types and does not need to be registered.
        // For example, an editor that supports a ViewCode/ViewDesigner scenario
        // might register something like the following:
        //        HKLM\Software\Microsoft\VisualStudio\<version>\Editors\
        //            {...guidEditor...}\
        //                LogicalViews\
        //                    {...LOGVIEWID_TextView...} = s ''
        //                    {...LOGVIEWID_Code...} = s ''
        //                    {...LOGVIEWID_Debugging...} = s ''
        //                    {...LOGVIEWID_Designer...} = s 'Form'
        //
        public int MapLogicalView(ref Guid rguidLogicalView, out string pbstrPhysicalView)
        {
            pbstrPhysicalView = null;    // initialize out parameter

            // we support only a single physical view
            if (VSConstants.LOGVIEWID_Primary == rguidLogicalView || VSConstants.LOGVIEWID_Designer == rguidLogicalView)
            {
                return VSConstants.S_OK;        // primary view uses NULL as pbstrPhysicalView
            }
            //else if (VSConstants.LOGVIEWID_TextView == rguidLogicalView || VSConstants.LOGVIEWID_Code == rguidLogicalView)
            //{
            //    pbstrPhysicalView = "text";
            //    return VSConstants.S_OK; 
            //}
            else
            {
                return VSConstants.E_NOTIMPL;   // you must return E_NOTIMPL for any unrecognized rguidLogicalView values
            }
        }

        public int Close()
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// This is the main method on the IVsEditorFactory interface. It is used to create
        /// instances of our Designer view. Our Designer view is specifically designed to be 
        /// compatible with the XML Editor, thus we use a TextBuffer object as our DocData
        /// 
        /// Since our editor supports opening only a single view for an instance of the document data, if we 
        /// are requested to open document data that is already instantiated in another editor, or even our 
        /// editor, we return a value VS_E_INCOMPATIBLEDOCDATA.
        /// </summary>
        /// <param name="grfCreateDoc">Flags determining when to create the editor. Only open and silent flags 
        /// are valid
        /// </param>
        /// <param name="pszMkDocument">path to the file to be opened</param>
        /// <param name="pszPhysicalView">name of the physical view</param>
        /// <param name="pvHier">pointer to the IVsHierarchy interface</param>
        /// <param name="itemid">Item identifier of this editor instance</param>
        /// <param name="punkDocDataExisting">This parameter is used to determine if a document buffer 
        /// (DocData object) has already been created
        /// </param>
        /// <param name="ppunkDocView">Pointer to the IUnknown interface for the DocView object</param>
        /// <param name="ppunkDocData">Pointer to the IUnknown interface for the DocData object</param>
        /// <param name="pbstrEditorCaption">Caption mentioned by the editor for the doc window</param>
        /// <param name="pguidCmdUI">the Command UI Guid. Any UI element that is visible in the editor has 
        /// to use this GUID. This is specified in the .vsct file
        /// </param>
        /// <param name="pgrfCDW">Flags for CreateDocumentWindow</param>
        /// <returns></returns>
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        public int CreateEditorInstance(
                        uint grfCreateDoc,
                        string pszMkDocument,
                        string pszPhysicalView,
                        IVsHierarchy pvHier,
                        uint itemid,
                        System.IntPtr punkDocDataExisting,
                        out System.IntPtr ppunkDocView,
                        out System.IntPtr ppunkDocData,
                        out string pbstrEditorCaption,
                        out Guid pguidCmdUI,
                        out int pgrfCDW)
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering {0} CreateEditorInstace()", this.ToString()));

            // Initialize to null
            ppunkDocView = IntPtr.Zero;
            ppunkDocData = IntPtr.Zero;
            pguidCmdUI = GuidList.guidSvgEditorPackageEditorFactory;
            pgrfCDW = 0;
            pbstrEditorCaption = null;

            // Validate inputs
            if ((grfCreateDoc & (VSConstants.CEF_OPENFILE | VSConstants.CEF_SILENT)) == 0)
            {
                return VSConstants.E_INVALIDARG;
            }

            IVsTextLines textBuffer = null;

            // QI existing buffer for text lines
            if (punkDocDataExisting != IntPtr.Zero)
            {
                // punkDocDataExisting is *not* null which means the file *is* already open. 
                // We need to verify that the open document is in fact a TextBuffer. If not
                // then we need to return the special error code VS_E_INCOMPATIBLEDOCDATA which
                // causes the user to be prompted to close the open file. If the user closes the
                // file then we will be called again with punkDocDataExisting as null

                // QI existing buffer for text lines
                textBuffer = Marshal.GetObjectForIUnknown(punkDocDataExisting) as IVsTextLines;
                if (textBuffer == null)
                {
                    return VSConstants.VS_E_INCOMPATIBLEDOCDATA;
                }
            }
            else
            {
                // punkDocDataExisting is null which means the file is not yet open.
                // We need to create a new text buffer object 
                // get the ILocalRegistry interface so we can use it to
                // create the text buffer from the shell's local registry
                textBuffer = CreateTextBuffer();
            }

            // create xml store
            XmlEditorService xmlEditor = (XmlEditorService)GetService<XmlEditorService>();
            XmlStore store = xmlEditor.CreateXmlStore();

            // Create the Document (editor)
            SvgWindowPane view = new SvgWindowPane(editorPackage, new SvgDocument(vsServiceProvider, textBuffer, store, pszMkDocument));
            ppunkDocView = Marshal.GetIUnknownForObject(view);
            ppunkDocData = Marshal.GetIUnknownForObject(textBuffer);
            pbstrEditorCaption = "";
            return VSConstants.S_OK;
        }

        internal static Guid guidXmlEditorLanguageService = new Guid("f6819a78-a205-47b5-be1c-675b3c7f0b8e");
        internal static Guid guidFormatFilterString = new Guid("8D88CCA5-7567-4b5c-9CD7-67A3AC136D2D");
        internal static Guid guidVsBufferDetectLangSID = new Guid("17F375AC-C814-11d1-88AD-0000F87579D2");
        /// <summary>
        /// Create a new text buffer
        /// </summary>
        /// <returns></returns>
        internal IVsTextLines CreateTextBuffer()
        {
            IVsTextBuffer buffer = null;
            IOleServiceProvider vssp = GetService<IOleServiceProvider>();

            #region bug 668539: managed editor doesn't bootstrap properly so we have this workaround from Dmitry Goncharenko

            Guid fontService = new Guid("48d069e8-1993-4752-baf3-232236a3ea4f"); // SVsManagedFontAndColorInformation;
            Guid interfaceGuid = new Guid("99f76ac4-8094-4ec7-b60a-ae876ac26a81"); // IVsManagedFontAndColorInformation;
            IntPtr ptr = IntPtr.Zero;
            if (ErrorHandler.Succeeded(vssp.QueryService(ref fontService, ref interfaceGuid, out ptr)))
            {
                Marshal.Release(ptr);
            }

            #endregion

            IComponentModel componentModelHost = GetService<SComponentModel, IComponentModel>();
            IVsEditorAdaptersFactoryService editorAdapterFactory = componentModelHost.GetService<IVsEditorAdaptersFactoryService>();
            if (editorAdapterFactory != null)
            {
                buffer = editorAdapterFactory.CreateVsTextBufferAdapter(vssp);
            }
            if (buffer == null)
            {
                throw new InvalidOperationException();
            }

            IVsTextLines textLines = buffer as IVsTextLines;

            IObjectWithSite ows = textLines as IObjectWithSite;
            if (ows != null)
            {
                ows.SetSite(vssp);
            }

            // We want to set the LanguageService SID explicitly to the XML Language Service.
            // We need turn off GUID_VsBufferDetectLangSID before calling LoadDocData so that the 
            // TextBuffer does not do the work to detect the LanguageService SID from the file extension.
            IVsUserData userData = textLines as IVsUserData;
            if (userData != null)
            {
                Guid VsBufferDetectLangSID = guidVsBufferDetectLangSID;
                ErrorHandler.ThrowOnFailure(userData.SetData(ref VsBufferDetectLangSID, false));
                ErrorHandler.ThrowOnFailure(userData.SetData(ref guidFormatFilterString, Resources.FormatFilterString));
            }

            Guid langSid = guidXmlEditorLanguageService;
            ErrorHandler.ThrowOnFailure(textLines.SetLanguageServiceID(ref langSid));

            return textLines;
        }

        #endregion
    }
}
