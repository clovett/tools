//------------------------------------------------------------------------------
// <copyright file="BaseDocData.cs" company="Microsoft">
//	 Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using System.Xml;
using EnvDTE;

namespace SvgEditor
{
    /// <summary>
    /// The base class for our document data.
    /// </summary>
    abstract class BaseDocData : IDisposable,
                                 IVsFileChangeEvents,
                                 IVsPersistDocData,
                                 IPersistFileFormat,
                                 IVsDocDataFileChangeControl,
                                 IVsFileBackup,
                                 IVsSaveOptionsDlg
    {
        #region Event Handlers
        public event EventHandler<FileChangedEventArgs> FileChanged;
        public event EventHandler<SavedEventArgs> Saved;
        public event EventHandler<SavedEventArgs> SaveFinished;
        public event EventHandler<LoadedEventArgs> Loaded;
        public event EventHandler<EventArgs> Closing;
        public event EventHandler<DirtyChangedEventArgs> DirtyChanged;
        #endregion

        #region Fields
        private IServiceProvider _serviceProvider;
        private bool _isDirty;
        private uint _docCookie;
        private IVsHierarchy _owningVsHierarchy;
        private uint _owningVsItemId;
        private string _fileName = string.Empty;
        private DelayedUITask _fileChangeTask;

        // Counter of the file system changes to ignore.
        private int _changesToIgnore;

        private IVsFileChangeEx fileChangeService;

        // Cookie for the subscription to the file system notification events.
        private uint _fileChangeNotifyCookie;

        // The flag to keep truck if the doc is saved or not.
        // We have to double book keeping here, since VsHierarchy.GetProperty
        // will fail when we closing the doc.  In that case, VsHierarchy.GetProperty
        // can not return the correct value.  We might ends up deleting saved file.
        private bool _isDocSaved = true;

        /// <summary>
        /// The document path that is set when no file is associated with the
        /// editor ("new" document state).
        /// </summary>
        private string _untitledPath;

        private bool disposed;

        #endregion

        #region Construction/Destruction
        protected BaseDocData(IServiceProvider sp)
        {
            this._serviceProvider = sp;
            this._fileChangeNotifyCookie = VSConstants.VSCOOKIE_NIL;

            // Get the VsFileChangeEx service; this service will call us back when a file system
            // event will occur on the file(s) that we register.
            this.fileChangeService = this.GetService<SVsFileChangeEx, IVsFileChangeEx>();
        }

        #endregion

        #region Abstract Properties and Operations
        // All base classes must implement these properties
        public abstract string DocExtension { get; }
        public abstract Guid DocGuid { get; }
        public abstract Guid EditorType { get; }
        public abstract string FormatFilterList { get; }
        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BaseDocData()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Disposes of managed and unmanaged resources.
        /// </summary>
        /// <param name="disposing">
        /// true if we are called from IDisposeable.Dispose and can
        /// get rid of managed resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;

            //*****************************
            // Clean up unmanaged resources
            //*****************************
            this.UnadviseFileChange();
            this.fileChangeService = null;

            // No managed resources to clean up
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns the cookie we were registered with
        /// </summary>
        internal uint Cookie
        {
            get { return _docCookie; }
        }

        /// <summary>
        /// Is this data dirty?
        /// </summary>
        public bool IsDirty
        {
            get { return _isDirty; }
            set
            {
                if (_isDirty != value)
                {
                    _isDirty = value;
                    if (_isDirty)
                    {
                        NotifyDocChanged(__VSRDTATTRIB.RDTA_DocDataIsDirty);
                    }
                    else
                    {
                        NotifyDocChanged(__VSRDTATTRIB.RDTA_DocDataIsNotDirty);
                    }
                    OnDirtyChanged();
                }
            }
        }

        /// <summary>
        /// The name of the file
        /// </summary>
        public virtual string FileName
        {
            get { return _fileName; }
            set { _fileName = value; ResetCanEdit(); }
        }

        /// <summary>
        /// Return hierarchy that owns this document.
        /// </summary>
        public IVsHierarchy OwningHierarchy
        {
            get { return _owningVsHierarchy; }
        }

        /// <summary>
        /// Return item id of this document.
        /// </summary>
        public uint OwningItemId
        {
            get { return _owningVsItemId; }
        }

        /// <summary>
        /// get the DisplayName of the dgml document,if the file is not yet saved the UntitledPath ex: CodeMap1 would be displayed.
        /// </summary>
        public string DisplayName
        {
            get
            {
                var fileName = System.IO.Path.GetFileName(this.FileName);
                if (_untitledPath == String.Empty || _isDocSaved)
                {
                    return fileName;
                }

                return _untitledPath;
            }
        }

        #endregion

        #region Service Provider

        /// <summary>
        /// Parametrized method to get the specified service by Type from the VS service provider
        /// where service type is different from interface you want returned.
        /// </summary>
        /// <typeparam name="ST">Type of the service</typeparam>
        /// <typeparam name="T">Type of the expected instance</typeparam>
        protected T GetService<ST, T>() where ST : class
                             where T : class
        {
            IServiceProvider sp = this._serviceProvider;
            if (typeof(T).IsEquivalentTo(typeof(IServiceProvider)))
            {
                return (T)sp;
            }
            return sp.GetService(typeof(ST)) as T;
        }


        /// <summary>
        /// Parametrized method to get the specified service by Type from the VS service provider.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        protected T GetService<T>() where T : class
        {
            IServiceProvider sp = this._serviceProvider;
            if (typeof(T).IsEquivalentTo(typeof(IServiceProvider)))
            {
                return (T)sp;
            }
            return sp.GetService(typeof(T)) as T;
        }

        #endregion

        #region Source Control

        bool _gettingCheckoutStatus;
        bool? _canEditCache;

        private void ResetCanEdit()
        {
            _canEditCache = null;
        }

        /// <summary>
        /// This function asks the QueryEditQuerySave service if it is possible to edit the file.
        /// This can result in an automatic checkout of the file and may even prompt the user for
        /// permission to checkout the file.  If the user says no or the file cannot be edited 
        /// this returns false.
        /// </summary>
        public bool CanEditFile(out bool cancelledByUser)
        {
            cancelledByUser = false;

            // Check the status of the recursion guard
            if (_gettingCheckoutStatus)
                return false;

            bool canEditFile = false; // assume the worst

            // don't need to check again if the user has already said yes.
            if (_canEditCache.HasValue && _canEditCache.Value)
            {
                return _canEditCache.Value;
            }
            try
            {
                // Set the recursion guard
                _gettingCheckoutStatus = true;

                // Get the QueryEditQuerySave service
                IVsQueryEditQuerySave2 queryEditQuerySave = GetService<SVsQueryEditQuerySave, IVsQueryEditQuerySave2>();

                // todo: some day VS should support for URL's.
                string filename = this.FileName;

                // Now call the QueryEdit method to find the edit status of this file
                string[] documents = { filename };
                uint result;
                uint outFlags;

                // Note that this function can popup a dialog to ask the user to checkout the file.
                // When this dialog is visible, it is possible to receive other request to change
                // the file and this is the reason for the recursion guard
                int hr = queryEditQuerySave.QueryEditFiles(
                    0,              // Flags
                    1,              // Number of elements in the array
                    documents,      // Files to edit
                    null,           // Input flags
                    null,           // Input array of VSQEQS_FILE_ATTRIBUTE_DATA
                    out result,     // result of the checkout
                    out outFlags    // Additional flags
                );
                if (ErrorHandler.Succeeded(hr) && (result == (uint)tagVSQueryEditResult.QER_EditOK))
                {
                    // In this case (and only in this case) we can return true from this function
                    canEditFile = true;
                }
                cancelledByUser = (result == (uint)tagVSQueryEditResult.QER_NoEdit_UserCanceled);
            }
            finally
            {
                _gettingCheckoutStatus = false;
            }
            _canEditCache = canEditFile;
            return canEditFile;
        }

        #endregion 

        #region Overridable Methods

        protected virtual int OnLoad(string fileName, uint grfMode, int readOnly)
        {
            if ((fileName == null) &&
                 ((FileName == null) || (FileName.Length == 0)))
            {
                throw new ArgumentNullException("fileName");
            }

            int hr = VSConstants.S_OK;

            bool isReload = false;

            // If the new file name is null, then this operation is a reload
            if (fileName == null)
            {
                isReload = true;
            }

            // Set the new file name
            if (!isReload)
            {
                // Unsubscribe from the notification of the changes in the previous file.
                this.UnadviseFileChange();
                FileName = fileName;
            }

            // Load the file
            bool documentIsDirty;
            hr = OnLoadHelper(FileName, out documentIsDirty);
            if (hr == VSConstants.S_OK)
            {
                IsDirty = documentIsDirty;

                // Subscribe for the notification on file changes.
                if (!isReload)
                {
                    this.AdviseFileChange();
                }

                // Notify the load or reload
                NotifyDocChanged(__VSRDTATTRIB.RDTA_DocDataReloaded);

                // Send the event that we've been loaded
                OnLoaded(isReload);

                ResetCanEdit();
            }

            return hr;
        }

        protected virtual int OnSave(string fileName, int fRemember, uint nFormatIndex)
        {
            // We don't want to be notify for this change of the file.
            this.UnadviseFileChange();

            int rc = VSConstants.S_OK;
            try
            {
                // If file is null or same --> SAVE
                if (fileName == null || string.Compare(fileName, _fileName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    rc = this.OnSaveHelper(FileName);
                    if (rc == VSConstants.S_OK)
                        IsDirty = false;
                }
                // If remember --> SaveAs
                else if (fRemember != 0)
                {
                    rc = this.OnSaveHelper(fileName);
                    if (rc == VSConstants.S_OK)
                    {
                        IsDirty = false;
                        FileName = fileName;
                    }
                }
                // Else, Save a Copy As
                else
                {
                    rc = this.OnSaveHelper(fileName);
                }
            }
            finally
            {
                // Now that the file is saved (and maybe renamed) we can subscribe again
                // for the file system events.
                this.AdviseFileChange();
            }

            return rc;
        }

        virtual protected int OnSaveHelper(string fileName)
        {
            return VSConstants.S_OK;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2208")]
        virtual protected int OnSaveDocData(VSSAVEFLAGS dwSave, out string pbstrMkDocumentNew, out int piSaveCanceled)
        {
            pbstrMkDocumentNew = null;
            piSaveCanceled = 0;
            int hr = VSConstants.S_OK;
            VSSAVEFLAGS actualFlags = VSSAVEFLAGS.VSSAVE_Save;

            switch (dwSave)
            {
                case VSSAVEFLAGS.VSSAVE_Save:
                case VSSAVEFLAGS.VSSAVE_SilentSave:
                    {
                        IVsQueryEditQuerySave2 queryEditQuerySave = GetService<SVsQueryEditQuerySave, IVsQueryEditQuerySave2>();

                        // Call QueryEditQuerySave
                        uint result = 0;
                        hr = queryEditQuerySave.QuerySaveFile(
                                            this.FileName,        // filename
                                            0,    // flags
                                            null,            // file attributes
                                            out result);    // result
                        if (ErrorHandler.Failed(hr))
                            return hr;

                        // Process according to result from QuerySave
                        switch ((tagVSQuerySaveResult)result)
                        {
                            case tagVSQuerySaveResult.QSR_NoSave_Cancel:
                                // Note that this is also case tagVSQuerySaveResult.QSR_NoSave_UserCanceled because these
                                // two tags have the same value.
                                piSaveCanceled = ~0;
                                return VSConstants.S_OK;

                            case tagVSQuerySaveResult.QSR_SaveOK:
                                actualFlags = GetNewAndUnsaved() ? VSSAVEFLAGS.VSSAVE_SaveAs : dwSave;
                                break;

                            case tagVSQuerySaveResult.QSR_ForceSaveAs:
                                actualFlags = VSSAVEFLAGS.VSSAVE_SaveAs;
                                break;

                            case tagVSQuerySaveResult.QSR_NoSave_Continue:
                                // In this case there is nothing to do.
                                return VSConstants.S_OK;

                        }
                        break;
                    }
                case VSSAVEFLAGS.VSSAVE_SaveAs:
                case VSSAVEFLAGS.VSSAVE_SaveCopyAs:
                    {
                        // Make sure the file name as the right extension
                        if (string.Compare(this.DocExtension, System.IO.Path.GetExtension(this.FileName), StringComparison.OrdinalIgnoreCase) != 0)
                        {
                            this.FileName = System.IO.Path.GetFileNameWithoutExtension(this.FileName) + this.DocExtension;
                        }
                        actualFlags = dwSave;
                        break;
                    }
                default:
                    throw new ArgumentException("dwSave");
            };


            // Call the shell to do the SaveAS for us
            IVsUIShell uiShell = GetService<SVsUIShell, IVsUIShell>();
            hr = uiShell.SaveDocDataToFile(actualFlags, (IPersistFileFormat)this, this.FileName, out pbstrMkDocumentNew, out piSaveCanceled);
            if (ErrorHandler.Failed(hr))
                return hr;

            if (pbstrMkDocumentNew != null)
            {
                this.FileName = pbstrMkDocumentNew;
            }

            // Fire the saved finished event
            OnSaveFinished(this.FileName);

            return VSConstants.S_OK;
        }

        virtual protected int OnLoadHelper(string fileName, out bool documentIsDirty)
        {
            documentIsDirty = false;
            return VSConstants.S_OK;
        }

        virtual protected int OnInitNew(uint nFormatIndex)
        {
            this.SetNewAndUnsaved(true);
            this._isDocSaved = false;

            return VSConstants.S_OK;
        }
        #endregion

        #region Misc VS Shell Helpers
        /// <summary>
        /// Sets the property on the node item such that it is unsaved
        /// and new.
        /// </summary>
        public void SetNewAndUnsaved(bool unsavedAndNew)
        {
            Debug.Assert(_owningVsHierarchy != null);
            if (_owningVsHierarchy == null)
                throw new InvalidOperationException("BaseDocData has invalid state (_owningVsHierarchy is null)");

            // Set ourselves as new and unsaved
            int hr =
                 _owningVsHierarchy.SetProperty(
                    _owningVsItemId,
                    (int)__VSHPROPID.VSHPROPID_IsNewUnsavedItem,
                    unsavedAndNew);

            ResetCanEdit();

            if (hr != VSConstants.E_NOTIMPL)
                ErrorHandler.ThrowOnFailure(hr);
        }

        /// <summary>
        /// Gets the property on the node item and determines if it is new and unsaved.
        /// </summary>
        /// <returns></returns>
        public bool GetNewAndUnsaved()
        {
            bool bNewAndUnsaved = false;

            if (_owningVsHierarchy == null)
                throw new InvalidOperationException();

            object newAndUnusedObject;
            int hr = _owningVsHierarchy.GetProperty(_owningVsItemId,
                (int)__VSHPROPID.VSHPROPID_IsNewUnsavedItem,
                out newAndUnusedObject);
            if (hr != VSConstants.S_OK)
            {
                // We couldn't get the property from the node
                // so assume the node is new and unsaved.
                bNewAndUnsaved = !this._isDocSaved;
            }
            else if (newAndUnusedObject != null)
            {
                bNewAndUnsaved = (bool)newAndUnusedObject;
            }

            return bNewAndUnsaved;
        }

        /// <summary>
        /// Show VS message box with "Yes All" button on it.
        /// </summary>
        /// <param name="caption"></param>
        /// <param name="description"></param>
        /// <param name="f1HelpId"></param>
        /// <returns></returns>
        public OleMessageBoxResult ShowYesNoMessageBox(string caption, string description, string f1HelpId)
        {
            IVsUIShell shell = this.GetService<SVsUIShell, IVsUIShell>();
            int result = 0;
            shell.ShowMessageBox(0, Guid.Empty, caption, description, f1HelpId, 0, OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_QUERY, 1, out result);
            return (OleMessageBoxResult)result;
        }

        #endregion

        #region IVsPersistDocData
        /// <summary>
        /// OnGetGuidEditorType
        /// </summary>
        /// <param name="pClassId"></param>
        int IVsPersistDocData.GetGuidEditorType(out Guid pClassId)
        {
            pClassId = EditorType;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// IsDocDataDirty
        /// </summary>
        /// <param name="iDirty"></param>
        int IVsPersistDocData.IsDocDataDirty(out int iDirty)
        {
            iDirty = IsDirty ? 1 : 0;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// SetUntitledDocPath
        /// </summary>
        /// <param name="untitledPath"></param>
        int IVsPersistDocData.SetUntitledDocPath(string untitledPath)
        {
            _untitledPath = untitledPath;

            if (!System.IO.File.Exists(untitledPath))
                return OnInitNew(0);

            return VSConstants.S_OK;
        }

        /// <summary>
        /// LoadDocData handler through a virtual for override purposes
        /// </summary>
        /// <param name="szMkDocument"></param>
        int IVsPersistDocData.LoadDocData(string szMkDocument)
        {
            return ((IPersistFileFormat)this).Load(szMkDocument, 0, 0);
        }

        int IVsPersistDocData.SaveDocData(VSSAVEFLAGS dwSave, out string pbstrMkDocumentNew, out int piSaveCanceled)
        {
            return OnSaveDocData(dwSave, out pbstrMkDocumentNew, out piSaveCanceled);
        }

        /// <summary>
        /// Close
        /// </summary>
        int IVsPersistDocData.Close()
        {
            return OnClose();
        }

        protected virtual int OnClose()  // IVsPersistDocData.
        {
            OnClosing();

            // Delete the file if it was a new, temporary filename.
            if (this.GetNewAndUnsaved() && String.IsNullOrEmpty(this._fileName) == false)
            {
                System.IO.File.Delete(this._fileName);
            }

            // Unsubscribe from the notification of file system events
            this.UnadviseFileChange();

            //this._loading = true;
            this._fileName = string.Empty;
            return VSConstants.S_OK;
        }


        int IVsPersistDocData.OnRegisterDocData(uint docCookie, IVsHierarchy pHierNew, uint itemidNew)
        {
            return this.OnRegisterDocData(docCookie, pHierNew, itemidNew);
        }

        /// <summary>
        /// Handles IVsPersistDocData.OnRegisterDocData().
        /// </summary>
        protected virtual int OnRegisterDocData(uint docCookie, IVsHierarchy pHierNew, uint itemidNew)
        {
            _docCookie = docCookie;
            _owningVsHierarchy = pHierNew;
            _owningVsItemId = itemidNew;
            return VSConstants.S_OK;
        }

        int IVsPersistDocData.RenameDocData(uint grfAttribs, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
        {
            this._fileName = pszMkDocumentNew;
            this._owningVsHierarchy = pHierNew;
            this._owningVsItemId = itemidNew;
            return VSConstants.S_OK;
        }

        int IVsPersistDocData.IsDocDataReloadable(out int ifReloadable)
        {
            ifReloadable = 1; // true
            return VSConstants.S_OK;
        }

        int IVsPersistDocData.ReloadDocData(uint grfFlags)
        {
            return ((IPersistFileFormat)this).Load(null, grfFlags, 0);
        }

        #endregion

        #region IPersistFileFormat Members

        int IPersistFileFormat.GetClassID(out Guid pClassID)
        {
            pClassID = this.DocGuid;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns the path to an object's current working file, or, if there is not a current working file, the object's default file name prompt. 
        /// </summary>
        /// <param name="ppszFilename">Pointer to the file name. If the object has a valid file name, the file name is returned as the 
        /// ppszFilename out parameter. If the object is in the untitled state, null is returned as the ppszFilename out parameter.</param>
        /// <param name="pnFormatIndex">Value that indicates the current format of the file. This value is interpreted as a zero-based 
        /// index into the list of formats, as returned by a call to GetFormatList. An index value of zero indicates the first format, 
        /// 1 the second format, and so on. If the object supports only a single format, it returns zero. Subsequently, it returns a single 
        /// element in its format list through a call to GetFormatList.</param>
        /// <returns></returns>
        int IPersistFileFormat.GetCurFile(out string ppszFilename, out uint pnFormatIndex)
        {
            ppszFilename = this.FileName;
            pnFormatIndex = 0;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Provides the caller with the information necessary to open the standard common Save As dialog box 
        /// (using the GetSaveFileNameViaDlg function) on behalf of the object.
        /// </summary>
        /// <param name="ppszFormatList">Pointer to a string that contains pairs of format filter strings. </param>
        int IPersistFileFormat.GetFormatList(out string ppszFormatList)
        {
            // Call the abstract routine so each document data type can return
            // their specific format list.
            string list = FormatFilterList;
            // Note: this is used by native save dialog, we must use '\n' instead of '|' to separate the strings
            // for compatibility with \\cpvsbuild\drops\Private\Dev10\pu\lab26vsts\raw\20217.00\sources\env\msenv\core\vsxfmgr.cpp, 6517.
            ppszFormatList = list.Replace('|', '\n');
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Instructs the object to initialize itself in the untitled state.
        /// </summary>
        /// <param name="nFormatIndex">
        /// Index value that indicates the current format of the file. The nFormatIndex parameter 
        /// controls the beginning format of the file. The caller should pass DEF_FORMAT_INDEX if the 
        /// object is to choose its default format. If this parameter is non-zero, then it is interpreted 
        /// as the index into the list of formats, as returned by a call to GetFormatList. An index value 
        /// of 0 indicates the first format, 1 the second format, and so on. </param>
        int IPersistFileFormat.InitNew(uint nFormatIndex)
        {
            return OnInitNew(nFormatIndex);
        }

        /// <summary>
        /// Determines whether an object has changed since being saved to its current file. 
        /// </summary>
        /// <param name="pfIsDirty">true if the document content changed. </param>
        int IPersistFileFormat.IsDirty(out int isDirty)
        {
            isDirty = IsDirty ? 1 : 0;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Opens a specified file and initializes an object from the file contents
        /// </summary>
        /// <param name="pszFilename">Pointer to the name of the file to load, which, for an existing file, should always include the full path.</param>
        /// <param name="grfMode">File format mode. If zero, the object uses the usual defaults as if the user had opened the file</param>
        /// <param name="fReadOnly">true indicates that the file should be opened as read-only</param>
        int IPersistFileFormat.Load(string fileName, uint grfMode, int readOnly)
        {
            return OnLoad(fileName, grfMode, readOnly);
        }

        /// <summary>
        /// Saves a copy of the object into the specified file.
        /// </summary>
        /// <param name="pszFilename">Pointer to the file name. The pszFilename parameter can be null; it instructs the object 
        /// to save using its current file. If the object is in the untitled state and null reference is passed as the pszFilename, 
        /// the object returns E_INVALIDARG. You must specify a valid file name parameter in this situation. </param>
        /// <param name="fRemember">Boolean value that indicates whether the pszFileName parameter is to be used as the current 
        /// working file. If true, pszFileName becomes the current file and the object should clear its dirty flag after the save. 
        /// If false, this save operation is a Save a Copy As operation. In this case, the current file is unchanged and the object 
        /// does not clear its dirty flag. If pszFileName is null, the implementation ignores the fRemember flag.</param>
        /// <param name="nFormatIndex">Value that indicates the format in which the file will be saved. The caller passes DEF_FORMAT_INDEX 
        /// if the object is to choose its default (current) format. If set to non-zero, the value is interpreted as the index into the 
        /// list of formats, as returned by a call to the method GetFormatList. An index value of 0 indicates the first format, 1 the 
        /// second format, and so on. </param>
        int IPersistFileFormat.Save(string fileName, int fRemember, uint nFormatIndex)
        {
            return OnSave(fileName, fRemember, nFormatIndex);
        }

        int IPersistFileFormat.SaveCompleted(string fileName)
        {
            return OnSaveCompleted(fileName);
        }

        protected virtual int OnSaveCompleted(string fileName)
        {
            bool oldNewAndUnsaved = GetNewAndUnsaved();

            SetNewAndUnsaved(false);
            this._isDocSaved = true;

            // Fire the saved event
            OnSaved(fileName);

            // Fire the saved finished event.  We only want to do this in the 
            // case of going from new document since the other cases will be
            // handled by SaveDocData.
            if (oldNewAndUnsaved)
            {
                OnSaveFinished(fileName);
            }

            return VSConstants.S_OK;
        }
        #endregion

        #region IPersist Members

        int Microsoft.VisualStudio.OLE.Interop.IPersist.GetClassID(out Guid pClassID)
        {
            pClassID = this.DocGuid;
            return VSConstants.S_OK;
        }
        #endregion

        #region IVsDocDataFileChangeControl

        int IVsDocDataFileChangeControl.IgnoreFileChanges(int fIgnore)
        {
            return OnIgnoreFileChanges(fIgnore);
        }

        /// <summary>
        /// Called by the shell to notify if a file change must be ignored.
        /// </summary>
        /// <param name="fIgnore">Flag not zero if the file change must be ignored.</param>
        protected virtual int OnIgnoreFileChanges(int fIgnore)
        {
            if (0 != fIgnore)
            {
                // The changes must be ignored, so increase the counter of changes to ignore
                ++_changesToIgnore;
            }
            else
            {
                if (_changesToIgnore > 0)
                {
                    --_changesToIgnore;
                }
            }

            return VSConstants.S_OK;
        }

        #endregion

        #region IVsFileChangeEvents

        int IVsFileChangeEvents.DirectoryChanged(string pszDirectory)
        {
            return OnDirectoryChanged(pszDirectory);
        }

        /// <summary>
        /// Event called when a directory changes.
        /// </summary>
        /// <param name="pszDirectory">Path if the changed directory.</param>
        protected virtual int OnDirectoryChanged(string pszDirectory)
        {
            // Do nothing: we are not interested in this event.
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Event called when there are file changes.
        /// </summary>
        /// <param name="cChanges">Number of files changed.</param>
        /// <param name="rgpszFile">Path of the files.</param>
        /// <param name="rggrfChange">Flags with the kind of changes.</param>
        int IVsFileChangeEvents.FilesChanged(uint cChanges, string[] rgpszFile, uint[] rggrfChange)
        {
            // Check the number of changes.
            if (0 == cChanges || null == rgpszFile || null == rggrfChange)
            {
                return VSConstants.E_INVALIDARG;
            }

            // If the counter of the changes to ignore (set by IVsDocDataFileChangeControl.IgnoreFileChanges)
            // is zero we can process this set of changes, otherwise ignore it.
            if (0 != _changesToIgnore)
                return VSConstants.S_OK;

            // Now scan the list of the changed files to find if the one opened in the editor
            // is one of the changed
            for (int i = 0; i < cChanges; ++i)
            {
                if (!String.IsNullOrEmpty(rgpszFile[i]) && string.Compare(FileName, rgpszFile[i], StringComparison.OrdinalIgnoreCase) == 0)
                {
                    // The file opened in the editor is changed.
                    // The first step now is to find the kind of change.
                    uint contentChange = (uint)_VSFILECHANGEFLAGS.VSFILECHG_Size | (uint)_VSFILECHANGEFLAGS.VSFILECHG_Time;
                    if ((rggrfChange[i] & contentChange) != 0)
                    {
                        // Notify our listeners that the file has changed.
                        OnFileChanged();
                    }
                    // we only have one file associated with our docData.
                    break;
                }
            }

            return VSConstants.S_OK;
        }

        /// <summary>
        /// The file has changed on disk, prompt the user to see if they want to reload it.
        /// </summary>
        public virtual void PromptForReload()
        {
            if (this.ShowYesNoMessageBox(this.FileName, Resources.FileModified, null) == OleMessageBoxResult.IDYES)
            {
                ((IVsPersistDocData)this).ReloadDocData(0);
            }
        }

        #endregion

        #region File And Document Change

        /// <summary>
        /// Subscribe to file system event notifications
        /// </summary>
        private void AdviseFileChange()
        {
            Debug.Assert(fileChangeService != null);
            if (this.fileChangeService != null)
            {
                Debug.Assert(!string.IsNullOrEmpty(this.FileName), "Can't subscribe to file change notifications if the file name is null or empty");

                // Here we want to subscribe for notification when the file opened in the editor changes.
                uint eventsToSubscribe = (uint)_VSFILECHANGEFLAGS.VSFILECHG_Size |
                                         (uint)_VSFILECHANGEFLAGS.VSFILECHG_Time;
                int hr = this.fileChangeService.AdviseFileChange(
                    this.FileName,                        // The file to check
                    eventsToSubscribe,                // Filter to use for the notification
                    (IVsFileChangeEvents)this,        // The callback to call
                    out this._fileChangeNotifyCookie);    // Cookie used to identify this subscription.
                ErrorHandler.ThrowOnFailure(hr);
            }
        }

        /// <summary>
        /// Unsubscribe from file system events notifications
        /// </summary>
        private void UnadviseFileChange()
        {
            Debug.Assert(this.fileChangeService != null);
            if (this.fileChangeService != null)
            {
                // If the goal is to unsubscribe, but there is no subscription active, exit.
                if (this._fileChangeNotifyCookie == VSConstants.VSCOOKIE_NIL)
                {
                    return;
                }

                // Now there is an active subscription, so unsubscribe.
                int hr = this.fileChangeService.UnadviseFileChange(_fileChangeNotifyCookie);
                // No more subscription active, so set the cookie to NIL
                this._fileChangeNotifyCookie = VSConstants.VSCOOKIE_NIL;
                ErrorHandler.ThrowOnFailure(hr);
            }
        }

        /// <summary>
        /// Sends a notification that the document has changed through
        /// the running document table.
        /// </summary>
        protected void NotifyDocChanged(__VSRDTATTRIB attribute)
        {
            if (string.IsNullOrEmpty(this.FileName))
            {
                return;
            }
            IVsRunningDocumentTable rdt = GetService<SVsRunningDocumentTable, IVsRunningDocumentTable>();

            IVsHierarchy hierarchy;
            uint itemid;
            IntPtr docData = IntPtr.Zero;
            try
            {
                uint cookie;
                int hr = rdt.FindAndLockDocument((uint)_VSRDTFLAGS.RDT_ReadLock, this.FileName, out hierarchy, out itemid, out docData, out cookie);
                if (ErrorHandler.Succeeded(hr) && cookie != 0)
                {
                    rdt.NotifyDocumentChanged(cookie, (uint)attribute);
                    rdt.UnlockDocument((uint)_VSRDTFLAGS.RDT_ReadLock, cookie);
                }
            }
            finally
            {
                Marshal.Release(docData);
            }
        }

        #endregion

        #region Raising Events

        void Raise<T>(EventHandler<T> eventProperty, T args) where T : EventArgs
        {
            if (eventProperty != null)
            {
                eventProperty(this, args);
            }
        }

        /// <summary>
        /// Raise the FileChanged event which indicates that the file has changed.
        /// </summary>
        protected virtual void OnFileChanged()
        {
            if (_fileChangeTask == null)
            {
                _fileChangeTask = new DelayedUITask(TimeSpan.FromMilliseconds(250), () =>
                {
                    PromptForReload();
                    _fileChangeTask = null;
                });
            }
            else
            {
                _fileChangeTask.Tick();
            }
            ResetCanEdit();
            Raise<FileChangedEventArgs>(FileChanged, new FileChangedEventArgs());
        }


        /// <summary>
        /// Raise the Saved event which indicates the file has been saved.
        /// </summary>
        protected virtual void OnSaved(String fileName)
        {
            ResetCanEdit();
            Raise<SavedEventArgs>(Saved, new SavedEventArgs(fileName));
        }

        /// <summary>
        /// Raise the SaveFinished event which indicates the save process has
        /// completed.
        /// </summary>
        protected virtual void OnSaveFinished(String fileName)
        {
            ResetCanEdit();
            Raise<SavedEventArgs>(SaveFinished, new SavedEventArgs(fileName));
        }

        /// <summary>
        /// Raise the Loaded event which indicates the file has been loaded.
        /// </summary>
        protected virtual void OnLoaded(bool isReload)
        {
            ResetCanEdit();
            Raise<LoadedEventArgs>(Loaded, new LoadedEventArgs(isReload));
        }

        /// <summary>
        /// Raise the Closing event which indicates the document is being closed.
        /// </summary>
        protected virtual void OnClosing()
        {
            Raise<EventArgs>(Closing, EventArgs.Empty);
        }

        /// <summary>
        /// Raise the DirtyChanged event
        /// </summary>
        protected virtual void OnDirtyChanged()
        {
            Raise<DirtyChangedEventArgs>(DirtyChanged, new DirtyChangedEventArgs());
        }

        #endregion  Raising Events

        #region IVsFileBackup Members

        /// <summary>
        /// To support backup of files. Visual Studio File Recovery backs up all objects in the Running Document Table that 
        /// support IVsFileBackup and have unsaved changes.
        /// 
        /// This method is used to Persist the data to a single file. On a successful backup this 
        /// should clear up the backup dirty bit
        /// 
        /// Our assumption for this base class is that the editor has no
        /// persistence model (which is valid for Data Compare).  Editors can override
        /// to participate fully in this interface.
        /// </summary>
        /// <param name="pszBackupFileName">Name of the file to persist</param>
        /// <returns>S_OK if the data can be successfully persisted.
        /// This should return STG_S_DATALOSS or STG_E_INVALIDCODEPAGE if there is no way to 
        /// persist to a file without data loss
        /// </returns>
        public virtual int BackupFile(string pszBackupFileName)
        {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Used to set the backup dirty bit. This bit should be set when the object is modified 
        /// and cleared on calls to BackupFile and any Save method
        /// 
        /// Our assumption for this base class is that the editor has no
        /// persistence model (which is valid for Data Compare).  Editors can override
        /// to participate fully in this interface.
        /// </summary>
        /// <param name="isObsolete">the dirty bit to be set</param>
        /// <returns>returns 1 if the backup dirty bit is set, 0 otherwise</returns>
        public virtual int IsBackupFileObsolete(out int isObsolete)
        {
            isObsolete = 0;
            return VSConstants.S_OK;
        }

        #endregion

        #region IVsSaveOptionsDlg Members

        public int ShowSaveOptionsDlg(uint dwReserved, IntPtr hwndDlgParent, IntPtr pszFilename)
        {
            string fileName = Marshal.PtrToStringAuto(pszFilename);
            return ShowSaveOptionsDlg(hwndDlgParent, fileName);
        }

        public virtual int ShowSaveOptionsDlg(IntPtr hwndDlgParent, string fileName)
        {
            return VSConstants.S_OK;
        }

        #endregion
    }

    /// <summary>
    /// Event args for file changed events
    /// </summary>
    public class FileChangedEventArgs : EventArgs
    {
    }

    /// <summary>
    /// Event args for dirty change events
    /// </summary>
    public class DirtyChangedEventArgs : EventArgs
    {
    }

    /// <summary>
    /// Event args for saved file events
    /// </summary>
    public class SavedEventArgs : EventArgs
    {
        string _fileName;
        public SavedEventArgs(string fileName) { _fileName = fileName; }
        public string FileName { get { return _fileName; } }
    }

    /// <summary>
    /// Event args for file load events
    /// </summary>
    public class LoadedEventArgs : EventArgs
    {
        bool _isReload;
        public LoadedEventArgs(bool isReload) { _isReload = isReload; }
        public bool IsReload { get { return _isReload; } }
    }

    public enum OleMessageBoxResult
    {
        IDOK = 1,
        IDCANCEL = 2,
        IDABORT = 3,
        IDRETRY = 4,
        IDIGNORE = 5,
        IDYES = 6,
        IDNO = 7
    }
}
