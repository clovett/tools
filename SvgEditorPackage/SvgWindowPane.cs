using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.SvgEditorPackage.View;
using System.Runtime.InteropServices;
using IServiceProvider = System.IServiceProvider;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.XmlEditor;
using System.Xml.Linq;
using System.Windows.Threading;

namespace Microsoft.SvgEditorPackage
{
    public class SvgWindowPane : WindowPane, IVsDeferredDocView
    {
        SvgEditorPackage package;
        SvgDocument doc;
        SvgPanel panel;

        public SvgWindowPane(SvgEditorPackage package, SvgDocument doc)
        {
            this.package = package;
            this.doc = doc;
            doc.BufferChanged += new EventHandler(OnBufferChanged);
            this.doc.Store.EditingScopeCompleted += new EventHandler<VisualStudio.XmlEditor.XmlEditingScopeEventArgs>(OnEditingScopeCompleted);
            this.doc.Store.UndoRedoCompleted += new EventHandler<VisualStudio.XmlEditor.XmlEditingScopeEventArgs>(OnUndoRedoCompleted);
        }

        private void OnBufferChanged(object sender, EventArgs e)
        {
            OnReparseNeeded();
        }

        public SvgDocument Document { get { return doc; } }

        void OnUndoRedoCompleted(object sender, VisualStudio.XmlEditor.XmlEditingScopeEventArgs e)
        {
            DispatchUpdates(e);
        }

        void OnEditingScopeCompleted(object sender, VisualStudio.XmlEditor.XmlEditingScopeEventArgs e)
        {
            if (e.EditingScope.UserState is SvgElement)
            {
                // then we made the edit!
                return;
            }
            DispatchUpdates(e);
        }

        private void DispatchUpdates(VisualStudio.XmlEditor.XmlEditingScopeEventArgs e)
        {
            List<XmlModelChange> cache = new List<XmlModelChange>(e.EditingScope.Changes(doc.Store));
            panel.Dispatcher.Invoke(new Action(() =>
            {
                panel.MergeChanges(cache);
            }));
        }

        protected override void Initialize()
        {
            base.Initialize();

            this.Content = panel = new SvgPanel(doc);
            SvgElement.SetSvgWindowPane(panel, this);
            panel.Focus();
            panel.PreviewMouseRightButtonUp += new MouseButtonEventHandler(OnMouseRightButtonUp);

            DefineCommandHandler(new CommandID(GuidList.guidSvgEditorPackageCmdSet, (int)PkgCmdIDList.cmdid_ViewXml), this.ViewXml, null, this.CanViewXml);

            DefineCommandHandler(new CommandID(VSConstants.GUID_VSStandardCommandSet97, (int)VSConstants.VSStd97CmdID.Copy), this.Copy, null, this.HasSelection);
            DefineCommandHandler(new CommandID(VSConstants.GUID_VSStandardCommandSet97, (int)VSConstants.VSStd97CmdID.Cut), this.Cut, null, this.HasSelection);
            DefineCommandHandler(new CommandID(VSConstants.GUID_VSStandardCommandSet97, (int)VSConstants.VSStd97CmdID.Paste), this.Paste, null, this.HasClipboard);
            DefineCommandHandler(new CommandID(VSConstants.GUID_VSStandardCommandSet97, (int)VSConstants.VSStd97CmdID.Delete), this.Delete, null, this.HasSelection);
        }
        
        /// <summary>
        /// For selection sensitive commands
        /// </summary>
        void HasSelection(object sender, EventArgs e)
        {
            OleMenuCommand oleMenuCommand = sender as OleMenuCommand;
            if (oleMenuCommand == null)
            {
                return;
            }
            oleMenuCommand.Supported = true;
            bool enabled = this.panel.Selection.Count > 0;
            oleMenuCommand.Enabled = enabled;
            oleMenuCommand.Visible = true;
        }

        private void Copy(object sender, EventArgs e)
        {
            SvgClipboard.Copy(from visual in panel.Selection select visual.Element);
        }

        private void Cut(object sender, EventArgs e)
        {
            using (var scope = CreateEditingScope(Resources.Cut))
            {
                SvgClipboard.Copy(from visual in panel.Selection select visual.Element);
                panel.DeleteSelection();
                scope.Complete();
            }
        }

        private void Delete(object sender, EventArgs e)
        {
            using (var scope = CreateEditingScope(Resources.Delete))
            {
                panel.DeleteSelection();
                scope.Complete();
            }
        }

        /// <summary>
        /// For clipboard sensitive commands
        /// </summary>
        void HasClipboard(object sender, EventArgs e)
        {
            OleMenuCommand oleMenuCommand = sender as OleMenuCommand;
            if (oleMenuCommand == null)
            {
                return;
            }
            oleMenuCommand.Supported = true;
            bool enabled = !SvgClipboard.IsEmpty;
            oleMenuCommand.Enabled = enabled;
            oleMenuCommand.Visible = true;
        }

        private void Paste(object sender, EventArgs args)
        {
            using (var scope = CreateEditingScope(Resources.Paste))
            {
                foreach (XElement e in SvgClipboard.Parse())
                {
                    doc.Document.Root.Add(e);
                    panel.OnAdd(e);
                }
                scope.Complete();
            }
        }

        /// <summary>
        /// Set the state of a ViewXml command
        /// </summary>
        void CanViewXml(object sender, EventArgs e)
        {
            OleMenuCommand oleMenuCommand = sender as OleMenuCommand;
            if (oleMenuCommand == null)
            {
                return;
            }
            oleMenuCommand.Supported = true;
            bool enabled = true;            
            oleMenuCommand.Enabled = enabled;
            oleMenuCommand.Visible = enabled;
        }

        private void ViewXml(object sender, EventArgs e)
        {
            EnvDTE.DTE dte = this.GetService(typeof(EnvDTE._DTE)) as EnvDTE.DTE;
            if (dte != null)
            {                
                EnvDTE.Window w = dte.ItemOperations.OpenFile(this.doc.FileName, EnvDTE.Constants.vsViewKindTextView);

                // todo: jump to a selected node in the file...
                //if (w != null && obj != null)
                //{
                //    IsActive = false;
                //    EnvDTE.TextDocument text = w.Document.Object("") as EnvDTE.TextDocument;
                //    if (text != null)
                //    {
                //        TextSpan? span = null;
                //        if (doc != null)
                //        {
                //            span = doc.GetSpan(obj);
                //        }
                //        if (span.HasValue)
                //        {
                //            TextSpan ts = span.Value;
                //            text.Selection.MoveToLineAndOffset(ts.iStartLine + 1, ts.iStartIndex + 1, false);
                //            text.Selection.MoveToLineAndOffset(ts.iEndLine + 1, ts.iEndIndex + 1, true);
                //        }
                //    }
                //}
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.package = null;
            using (this.doc)
            {
                this.doc = null;
            }
            this.Content = null;
        }


        /// <summary>
        /// Create the command for the command ID and associates the handler methods, if specified
        /// </summary>
        /// <param name="id">The Id of the command with which to associate the handlers</param>
        /// <param name="invokeHandler">Method that should be called to execute the command</param>
        /// <param name="changeStatusHandler">Method that should be called when the status of the command changes</param>
        /// <param name="beforeQueryStatusHandler">Method that should be called before VS queries for the status of the command</param>
        /// <returns>The OleMenuCommand object representing the command and the handlers associated with the command</returns>
        internal OleMenuCommand DefineCommandHandler(CommandID id, EventHandler invokeHandler, EventHandler changeStatusHandler, EventHandler beforeQueryStatusHandler)
        {
            // Get the OleCommandService object provided by the MPF; this object is the one
            // responsible for handling the collection of commands implemented by the package.
            OleMenuCommandService menuService = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            OleMenuCommand command = null;
            if (null != menuService)
            {
                // Add the command handler
                command = new OleMenuCommand(invokeHandler, changeStatusHandler, beforeQueryStatusHandler, id);

                menuService.AddCommand(command);
            }
            return command;
        }

        void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!e.Handled)
            {
                if (ShowContextMenu(Win32.GetMousePosition()))
                {
                    e.Handled = true;
                }
            }
        }

        private Point GetContextMenuPosition()
        {
            try
            {
                Point pos = new Point(panel.ActualWidth / 2, panel.ActualHeight / 2); // default is center
                return panel.PointToScreen(pos);
            }
            catch
            {
                // means the position was offscreen, so use the mouse position.
                return Win32.GetMousePosition();
            }
        }

        private bool ShowContextMenu(Point pos)
        {
            VsShell.ShowContextMenu((IServiceProvider)this, this, GuidList.guidSvgEditorPackageCmdSet, PkgCmdIDList.menu_SvgEditorContext, pos);
            return true;
        }

        #region Special Win32 Handling for Context Menu
        
        const int WM_CONTEXTMENU = 0x007B;
        const int WM_SYSKEYDOWN = 0x0104;
        const int VK_F10 = 0x79;

        protected override bool PreProcessMessage(ref System.Windows.Forms.Message m)
        {
            switch (m.Msg)
            {
                case WM_SYSKEYDOWN:
                    short key = (short)(int)m.WParam;
                    if (key == VK_F10 && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
                    {
                        goto case WM_CONTEXTMENU;
                    }
                    break;
                case WM_CONTEXTMENU:
                    {
                        Point pos = GetContextMenuPosition();
                        ShowContextMenu(pos);
                        return true;
                    }
            }
            return base.PreProcessMessage(ref m);
        }

        #endregion

        #region Custom Editing Scope

        public SvgEditingScope CreateEditingScope(string caption)
        {
            return new SvgEditingScope(this, caption);
        }

        DispatcherTimer parseDelay;

        internal void OnReparseNeeded()
        {
            if (parseDelay == null)
            {
                parseDelay = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.Background, OnParseTick, panel.Dispatcher);
                parseDelay.Start();
            }
        }

        private void OnParseTick(object sender, EventArgs e)
        {
            parseDelay.Stop();
            parseDelay = null;
            if (!doc.IsParsing)
            {
                // trigger reparse.
                using (doc.Store.BeginEditingScope("Trigger", null)) { }
            }
        }

        #endregion 
    
        #region IVsDeferredDocView

        public int get_CmdUIGuid(out Guid pGuidCmdId)
        {
            pGuidCmdId = GuidList.guidSvgEditorPackageCmdSet;
            return VSConstants.S_OK;
        }

        public int get_DocView(out IntPtr ppUnkDocView)
        {
            ppUnkDocView = Marshal.GetIUnknownForObject(this);
            return VSConstants.S_OK;
        }
        #endregion 

    }
}
