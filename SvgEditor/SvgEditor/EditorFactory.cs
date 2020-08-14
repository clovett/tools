using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace SvgEditor
{
    [Guid("13620074-6a80-4a33-a33f-a9c0313e44f7")]
    public sealed class EditorFactory : IVsEditorFactory
    {
        private IServiceProvider _packageProvider;

        public EditorFactory(IServiceProvider packageProvider)
        {
            _packageProvider = packageProvider;
        }

        #region IVsEditorFactory Members

        /// <summary>
        /// Used for initialization of the editor in the environment
        /// </summary>
        /// <param name="psp">Pointer to the service provider. Can be used to obtain instances of other interfaces
        /// </param>
        /// <returns>S_OK</returns>
        public int SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp)
        {
            return VSConstants.S_OK;
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
        //        HKLM\Software\Microsoft\VisualStudio\9.0\Editors\
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

            // we support only a single physical view mapped to LOGVIEWID_Designer.
            if (rguidLogicalView == VSConstants.LOGVIEWID_Primary ||
                rguidLogicalView == VSConstants.LOGVIEWID_Designer)
            {
                return VSConstants.S_OK;        // primary view uses NULL as pbstrPhysicalView
            }
            else
            {
                return VSConstants.E_NOTIMPL;   // you must return E_NOTIMPL for any unrecognized rguidLogicalView values
            }
        }

        /// <summary>
        /// Releases all cached interface pointers and unregisters any event sinks. 
        /// </summary>
        public int Close()
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Used by the editor factory to create an editor instance. the environment first determines the 
        /// editor factory with the highest priority for opening the file and then calls 
        /// IVsEditorFactory.CreateEditorInstance. If the environment is unable to instantiate the document data 
        /// in that editor, it will find the editor with the next highest priority and attempt to so that same 
        /// thing. 
        /// NOTE: The priority of our editor is 32 as mentioned in the attributes on the package class.
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
        /// to use this GUID. This is specified in the .ctc file
        /// </param>
        /// <param name="pgrfCDW">Flags for CreateDocumentWindow</param>
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
            // Initialize to null
            ppunkDocView = IntPtr.Zero;
            ppunkDocData = IntPtr.Zero;
            pguidCmdUI = SvgEditorPackage.CommandSet;// guidEditorFactory;
            pgrfCDW = 0;
            pbstrEditorCaption = null;

            // Validate inputs
            if ((grfCreateDoc & (VSConstants.CEF_OPENFILE | VSConstants.CEF_SILENT)) == 0)
            {
                return VSConstants.E_INVALIDARG;
            }

            if (punkDocDataExisting != IntPtr.Zero)
            {
                // punkDocDataExisting is *not* null which means the file *is* already open. 
                return VSConstants.VS_E_INCOMPATIBLEDOCDATA;
            }

            SvgDocument doc = new SvgDocument(_packageProvider);
            doc.FileName = pszMkDocument;
            SvgWindowPane view = new SvgWindowPane(_packageProvider, doc);

            ppunkDocData = Marshal.GetIUnknownForObject(doc);
            ppunkDocView = Marshal.GetIUnknownForObject(view);
            pbstrEditorCaption = GetEditorCaption(pszMkDocument);

            return VSConstants.S_OK;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Get the caption for the new window frame.
        /// </summary>
        private string GetEditorCaption(string fileName)
        {
            Debug.Assert(!string.IsNullOrEmpty(fileName), "fileName must be specified");
            Debug.Assert(File.Exists(fileName), "Specified file must exist: " + fileName);

            return System.IO.Path.GetFileName(fileName);
        }

        #endregion
    }
}
