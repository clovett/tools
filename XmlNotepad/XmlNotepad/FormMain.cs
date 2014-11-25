//#define WHIDBEY_MENUS

using System;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Xsl;
using System.IO;
using System.Diagnostics;
using System.Text;

using MyContextMenu = System.Windows.Forms.ContextMenu;
using TopLevelMenuItemBaseType = System.Windows.Forms.MenuItem;

namespace XmlNotepad {
    /// <summary>
    /// Summary description for Form1.
    /// </summary>
    public class FormMain : System.Windows.Forms.Form, ISite {
        
        UndoManager undoManager;
        Settings settings;
        string[] args;
        DataFormats.Format urlFormat;
        private System.Windows.Forms.StatusBar statusBar1;
        private System.Windows.Forms.StatusBarPanel statusBarPanelMessage;
        private System.Windows.Forms.StatusBarPanel statusBarPanelBusy;
        RecentFilesMenu recentFiles;        
        TaskList taskList;
        bool loading;
        FormSearch search;
        IIntellisenseProvider ip;
        OpenFileDialog od;
        private ContextMenuStrip contextMenu1;
        private ToolStripSeparator ctxMenuItem20;
        private ToolStripSeparator ctxMenuItem23;
        private ToolStripMenuItem ctxcutToolStripMenuItem;
        private ToolStripMenuItem ctxMenuItemCopy;
        private ToolStripMenuItem ctxMenuItemPaste;
        private ToolStripMenuItem ctxMenuItemExpand;
        private ToolStripMenuItem ctxMenuItemCollapse;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem newToolStripMenuItem;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripMenuItem reloadToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem1;
        private ToolStripMenuItem saveToolStripMenuItem;
        private ToolStripMenuItem saveAsToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem2;
        private ToolStripMenuItem recentFilesToolStripMenuItem;
        private ToolStripMenuItem importToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem3;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem editToolStripMenuItem;
        private ToolStripMenuItem undoToolStripMenuItem;
        private ToolStripMenuItem redoToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem4;
        private ToolStripMenuItem cutToolStripMenuItem;
        private ToolStripMenuItem copyToolStripMenuItem;
        private ToolStripMenuItem pasteToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem5;
        private ToolStripMenuItem deleteToolStripMenuItem;
        private ToolStripMenuItem repeatToolStripMenuItem;
        private ToolStripMenuItem insertToolStripMenuItem;
        private ToolStripMenuItem duplicateToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem6;
        private ToolStripMenuItem nudgeToolStripMenuItem;
        private ToolStripMenuItem upToolStripMenuItem;
        private ToolStripMenuItem downToolStripMenuItem;
        private ToolStripMenuItem leftToolStripMenuItem;
        private ToolStripMenuItem rightToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem7;
        private ToolStripMenuItem findToolStripMenuItem;
        private ToolStripMenuItem replaceToolStripMenuItem;
        private ToolStripMenuItem viewToolStripMenuItem;
        private ToolStripMenuItem expandAllToolStripMenuItem;
        private ToolStripMenuItem collapseAllToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem8;
        private ToolStripMenuItem statusBarToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem9;
        private ToolStripMenuItem sourceToolStripMenuItem;
        private ToolStripMenuItem optionsToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem attributeToolStripMenuItem;
        private ToolStripMenuItem textToolStripMenuItem;
        private ToolStripMenuItem commentToolStripMenuItem;
        private ToolStripMenuItem CDATAToolStripMenuItem;
        private ToolStripMenuItem PIToolStripMenuItem;
        private ToolStripMenuItem contentsToolStripMenuItem;
        private ToolStripMenuItem indexToolStripMenuItem;
        private ToolStripSeparator toolStripMenuItem10;
        private ToolStripMenuItem aboutXMLNotepadToolStripMenuItem;
        private ToolStrip toolStrip1;
        private ToolStripButton toolStripButtonNew;
        private ToolStripButton toolStripButtonOpen;
        private ToolStripButton toolStripButtonSave;
        private ToolStripButton toolStripButtonUndo;
        private ToolStripButton toolStripButtonRedo;
        private ToolStripButton toolStripButtonCut;
        private ToolStripButton toolStripButtonCopy;
        private ToolStripButton toolStripButtonPaste;
        private ToolStripButton toolStripButtonDelete;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripButton toolStripButtonNudgeUp;
        private ToolStripButton toolStripButtonNudgeDown;
        private ToolStripButton toolStripButtonNudgeLeft;
        private ToolStripButton toolStripButtonNudgeRight;
        private HelpProvider helpProvider1;
        private ToolStripMenuItem elementToolStripMenuItem;
        private ToolStripMenuItem elementAfterToolStripMenuItem;
        private ToolStripMenuItem elementBeforeToolStripMenuItem;
        private ToolStripMenuItem elementChildToolStripMenuItem;
        private ToolStripMenuItem attributeBeforeToolStripMenuItem;
        private ToolStripMenuItem attributeAfterToolStripMenuItem;
        private ToolStripMenuItem attributeChildToolStripMenuItem;
        private ToolStripMenuItem textBeforeToolStripMenuItem;
        private ToolStripMenuItem textAfterToolStripMenuItem;
        private ToolStripMenuItem textChildToolStripMenuItem;
        private ToolStripMenuItem commentBeforeToolStripMenuItem;
        private ToolStripMenuItem commentAfterToolStripMenuItem;
        private ToolStripMenuItem commentChildToolStripMenuItem;
        private ToolStripMenuItem cdataBeforeToolStripMenuItem;
        private ToolStripMenuItem cdataAfterToolStripMenuItem;
        private ToolStripMenuItem cdataChildToolStripMenuItem;
        private ToolStripMenuItem PIBeforeToolStripMenuItem;
        private ToolStripMenuItem PIAfterToolStripMenuItem;
        private ToolStripMenuItem PIChildToolStripMenuItem;
        private ToolStripMenuItem ctxElementToolStripMenuItem;
        private ToolStripMenuItem ctxElementBeforeToolStripMenuItem;
        private ToolStripMenuItem ctxElementAfterToolStripMenuItem;
        private ToolStripMenuItem ctxElementChildToolStripMenuItem;
        private ToolStripMenuItem ctxAttributeToolStripMenuItem;
        private ToolStripMenuItem ctxAttributeBeforeToolStripMenuItem;
        private ToolStripMenuItem ctxAttributeAfterToolStripMenuItem;
        private ToolStripMenuItem ctxAttributeChildToolStripMenuItem;
        private ToolStripMenuItem ctxTextToolStripMenuItem;
        private ToolStripMenuItem ctxTextBeforeToolStripMenuItem;
        private ToolStripMenuItem ctxTextAfterToolStripMenuItem;
        private ToolStripMenuItem ctxTextChildToolStripMenuItem;
        private ToolStripMenuItem ctxCommentToolStripMenuItem;
        private ToolStripMenuItem ctxCommentBeforeToolStripMenuItem;
        private ToolStripMenuItem ctxCommentAfterToolStripMenuItem;
        private ToolStripMenuItem ctxCommentChildToolStripMenuItem;
        private ToolStripMenuItem ctxCdataToolStripMenuItem;
        private ToolStripMenuItem ctxCdataBeforeToolStripMenuItem;
        private ToolStripMenuItem ctxCdataAfterToolStripMenuItem;
        private ToolStripMenuItem ctxCdataChildToolStripMenuItem;
        private ToolStripMenuItem ctxPIToolStripMenuItem;
        private ToolStripMenuItem ctxPIBeforeToolStripMenuItem;
        private ToolStripMenuItem ctxPIAfterToolStripMenuItem;
        private ToolStripMenuItem ctxPIChildToolStripMenuItem;
        private ToolStripMenuItem windowToolStripMenuItem;
        private ToolStripMenuItem newWindowToolStripMenuItem;
        private ToolStripMenuItem schemasToolStripMenuItem;
        private System.ComponentModel.IContainer components;
        private ToolStripSeparator toolStripMenuItem11;
        private ToolStripMenuItem nextErrorToolStripMenuItem;
        private XmlCache model;
        private PaneResizer resizer;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem compareXMLFilesToolStripMenuItem;
        private TabControl tabControlLists;
        private TabPage tabPageTaskList;
        private TabControl tabControlViews;
        protected TabPage tabPageTreeView;
        protected TabPage tabPageHtmlView;
        private XmlTreeView xmlTreeView1=null;
        private TextBox defaultXsltFileName = new TextBox();
        private XsltViewer xsltViewer;


        public FormMain() {

            this.settings = new Settings();
            this.model = new XmlCache((ISynchronizeInvoke)this);
            this.undoManager = new UndoManager(1000);
            this.undoManager.StateChanged += new EventHandler(undoManager_StateChanged);

            this.SuspendLayout();
            // Separated out so we can have virtual CreateTreeView without causing WinForms designer to barf.
            InitializeTreeView();

            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            InitializeTabControl();

            this.ResumeLayout();

            InitializeHelp(this.helpProvider1);

            model.FileChanged += new EventHandler(OnFileChanged);
            model.ModelChanged += new EventHandler<ModelChangedEventArgs>(OnModelChanged);

            recentFiles = new RecentFilesMenu(recentFilesToolStripMenuItem);
            this.recentFiles.RecentFileSelected += new EventHandler(OnRecentFileSelected);

            //this.resizer.Pane1 = this.xmlTreeView1;
            this.resizer.Pane1 = this.tabControlViews;
            this.resizer.Pane2 = this.tabControlLists;

            // populate default settings and provide type info.
            this.settings["Font"] = this.Font = new Font("Courier New", 10);
            System.Collections.Hashtable colors = new System.Collections.Hashtable();
            colors["Element"] = Color.FromArgb(0, 64, 128);
            colors["Attribute"] = Color.Maroon;
            colors["Text"] = Color.Black;
            colors["Comment"] = Color.Green;
            colors["PI"] = Color.Purple;
            colors["CDATA"] = Color.Gray;
            colors["Background"] = Color.White;
            this.settings["Colors"] = colors;
            this.settings["FileName"] = new Uri("/",UriKind.RelativeOrAbsolute);
            this.settings["WindowLocation"] = new Point(0,0);
            this.settings["WindowSize"] = new Size(0,0);
            this.settings["TaskListSize"] = 0;
            this.settings["RecentFiles"] = new Uri[0];
            this.settings["SchemaCache"] = this.model.SchemaCache;

            this.settings.Changed += new SettingsEventHandler(settings_Changed);

            // now that we have a font, override the tabControlViews font setting.
            this.xmlTreeView1.Font = this.Font;

            // Event wiring
            this.xmlTreeView1.SetSite(this);
            this.xmlTreeView1.SelectionChanged += new EventHandler(treeView1_SelectionChanged);
            this.xmlTreeView1.ClipboardChanged += new EventHandler(treeView1_ClipboardChanged);
            this.xmlTreeView1.NodeChanged += new EventHandler<NodeChangeEventArgs>(treeView1_NodeChanged);
            this.xmlTreeView1.KeyDown += new KeyEventHandler(treeView1_KeyDown);
            this.taskList.GridKeyDown += new KeyEventHandler(taskList_KeyDown);

            this.toolStripButtonUndo.Enabled = false;
            this.toolStripButtonRedo.Enabled = false;

            this.statusBarToolStripMenuItem.Checked = true;

            this.importToolStripMenuItem.Enabled = false;
            this.duplicateToolStripMenuItem.Enabled = false;
            this.findToolStripMenuItem.Enabled = true;
            this.replaceToolStripMenuItem.Enabled = false;

            this.DragOver += new DragEventHandler(Form1_DragOver);
            this.xmlTreeView1.TreeView.DragOver += new DragEventHandler(Form1_DragOver);
            this.DragDrop += new DragEventHandler(Form1_DragDrop);
            this.xmlTreeView1.TreeView.DragDrop += new DragEventHandler(Form1_DragDrop);
            this.AllowDrop = true;

            this.urlFormat = DataFormats.GetFormat("UniformResourceLocatorW");

            this.recentFilesToolStripMenuItem.Visible = false;

            ctxcutToolStripMenuItem.Click += new EventHandler(this.cutToolStripMenuItem_Click);
            ctxcutToolStripMenuItem.ImageIndex = this.cutToolStripMenuItem.ImageIndex;
            ctxMenuItemCopy.Click += new EventHandler(this.copyToolStripMenuItem_Click);
            ctxMenuItemCopy.ImageIndex = copyToolStripMenuItem.ImageIndex;
            ctxMenuItemPaste.Click += new EventHandler(this.pasteToolStripMenuItem_Click);
            ctxMenuItemPaste.ImageIndex = pasteToolStripMenuItem.ImageIndex;
            ctxMenuItemExpand.Click += new EventHandler(this.expandAllToolStripMenuItem_Click);
            ctxMenuItemCollapse.Click += new EventHandler(this.collapseAllToolStripMenuItem_Click);

            // 
            // helpProvider1
            // 
            this.helpProvider1.HelpNamespace = Application.StartupPath + "\\Help.chm";
            this.helpProvider1.Site = this;

            this.ContextMenuStrip = this.contextMenu1;            
            New();

        }

        public FormMain(string[] args)
            : this() {
            this.args = args;
        }


        public XmlCache Model {
            get { return model; }
            set { model = value; }
        }

        public PaneResizer Resizer {
            get { return resizer; }
            set { resizer = value; }
        }

        public TabControl TabControlLists {
            get { return tabControlLists; }
            set { tabControlLists = value; }
        }

        public TabControl TabControlViews {
            get { return this.tabControlViews; }
            set { tabControlViews = value; }
        }

        public XmlTreeView XmlTreeView {
            get { return xmlTreeView1; }
            set { xmlTreeView1 = value; }
        }

        void InitializeTabControl() {
            CreateTabControl();
        }

        void InitializeTreeView() {
            this.xmlTreeView1 = CreateTreeView();

            // 
            // xmlTreeView1
            // 
            this.xmlTreeView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.xmlTreeView1.BackColor = System.Drawing.Color.White;
            this.xmlTreeView1.Location = new System.Drawing.Point(0, 52);
            this.xmlTreeView1.Name = "xmlTreeView1";
            this.xmlTreeView1.SelectedNode = null;
            this.xmlTreeView1.Size = new System.Drawing.Size(736, 256);
            this.xmlTreeView1.TabIndex = 1;
            this.xmlTreeView1.Dock = System.Windows.Forms.DockStyle.Fill;

        }

        protected virtual void InitializeHelp(HelpProvider hp) {
            hp.SetHelpNavigator(this, HelpNavigator.TableOfContents);
            hp.Site = this;
        }

        void FocusNextPanel(bool reverse) {
            Control[] panels = new Control[] { this.xmlTreeView1.TreeView, this.xmlTreeView1.NodeTextView, this.tabControlLists.SelectedTab.Controls[0] };
            for (int i = 0; i < panels.Length; i++) {
                Control c = panels[i];
                if (c.ContainsFocus) {
                    int j = i + 1;
                    if (reverse) {
                        j = i - 1;
                        if (j < 0) j = panels.Length - 1;
                    } else if (j >= panels.Length) {
                        j = 0;
                    } 
                    panels[j].Focus();
                    break;
                }
            }            
        }

        void treeView1_KeyDown(object sender, KeyEventArgs e) {
            switch (e.KeyCode) {
                case Keys.Space:
                    if ((e.Modifiers & Keys.Control) == Keys.Control) {
                        Rectangle r = this.xmlTreeView1.TreeView.Bounds;
                        XmlTreeNode node = this.xmlTreeView1.SelectedNode;
                        if (node != null) {
                            r = node.LabelBounds;
                            r.Offset(this.xmlTreeView1.TreeView.ScrollPosition);
                        }
                        r = this.xmlTreeView1.RectangleToScreen(r);
                        this.contextMenu1.Show(r.Left + (r.Width / 2), r.Top + (r.Height / 2));
                    }
                    break;
                case Keys.F6:
                    FocusNextPanel((e.Modifiers & Keys.Shift) != 0);
                    break;
            }
        }

        void taskList_KeyDown(object sender, KeyEventArgs e) {
            switch (e.KeyCode) {
                case Keys.F6:
                    FocusNextPanel((e.Modifiers & Keys.Shift) != 0);
                    break;
                case Keys.Enter:
                    taskList.NavigateSelectedError();
                    break;
            }
        }

        protected override void OnLoad(EventArgs e) {
            LoadConfig();
            base.OnLoad (e);
            // now that we (may) have a model, instantiate the xsltViewer
            this.xsltViewer = new XmlNotepad.XsltViewer(this);
            this.xsltViewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.xsltViewer.Location = new System.Drawing.Point(3, 3);
            this.xsltViewer.Name = "xsltViewer";
            this.xsltViewer.Size = new System.Drawing.Size(722, 328);
            this.xsltViewer.TabIndex = 0;
            // now that we have an xsltViewer, add it to it's tabpage.
            this.tabPageHtmlView.Controls.Add(this.xsltViewer);

        }

        protected override void OnClosing(CancelEventArgs e) {
            this.xmlTreeView1.Commit();
            if (this.model.Dirty){
                DialogResult rc = MessageBox.Show("Do you want to save your changes?", "Save Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);
                if (rc == DialogResult.Yes){
                    this.Save();
                } else if (rc == DialogResult.Cancel){
                    e.Cancel = true;
                    return;
                }
            }
            SaveConfig();
            base.OnClosing (e);
        }

        protected override void OnClosed(EventArgs e) {
            this.xmlTreeView1.Close();
            base.OnClosed(e);
        }

        protected override void OnLayout(LayoutEventArgs levent) {
            Size s = this.ClientSize;
            int w = s.Width;
            int h = s.Height;
            int top = this.menuStrip1.Height;
            this.toolStrip1.Size = new Size(w, 24);
            top += 24;
            int sbHeight = 0;
            if (this.statusBar1.Visible) {
                sbHeight = this.statusBar1.Height;
                this.statusBar1.Size = new Size(w, sbHeight);
            }
            this.tabControlViews.Location = new Point(0, top);
            this.tabControlViews.Size = new Size(w, h - top - sbHeight - this.tabControlLists.Height);
            //this.xmlTreeView1.Location = new Point(0, top);
            //this.xmlTreeView1.Size = new Size(w, h - top - sbHeight - this.tabControl1.Height);
            this.resizer.Size = new Size(w, this.resizer.Height);
            this.resizer.Location = new Point(0, top + this.tabControlViews.Height);
            //this.taskList.Size = new Size(w, this.taskList.Height);
            //this.taskList.Location = new Point(0, top + this.xmlTreeView1.Height + this.resizer.Height);
            this.tabControlLists.Size = new Size(w, this.tabControlLists.Height);
            this.tabControlLists.Location = new Point(0, top + this.tabControlViews.Height + this.resizer.Height);
            base.OnLayout(levent);
        }

        
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing ) {
            if( disposing ) {
                if (components != null) {
                    components.Dispose();
                }
                if (this.settings != null) {
                    this.settings.Dispose();
                    this.settings = null;
                }
                if (this.model != null) {
                    this.model.Dispose();
                    this.model = null;
                }
                IDisposable d = this.ip as IDisposable;
                if (d != null) {
                    d.Dispose();
                }
                this.ip = null;
            }
            base.Dispose( disposing );
        }

        protected virtual XmlTreeView CreateTreeView() {
            return new XmlTreeView();
        }

        protected virtual void CreateTabControl() {
                        // 
            // tabPage1
            // 
            this.tabPageTaskList.Controls.Add(this.taskList);
            this.tabPageTaskList.Location = new System.Drawing.Point(4, 24);
            this.tabPageTaskList.Name = "tabPage1";
            this.tabPageTaskList.Padding = new System.Windows.Forms.Padding(0);
            this.tabPageTaskList.Size = new System.Drawing.Size(728, 68);
            this.tabPageTaskList.TabIndex = 0;
            this.tabPageTaskList.Text = "Task List";
            this.tabPageTaskList.UseVisualStyleBackColor = true;
            // 
            // taskList
            // 
            this.taskList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.taskList.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.taskList.Location = new System.Drawing.Point(3, 3);
            this.taskList.Margin = new System.Windows.Forms.Padding(0);
            this.taskList.Name = "taskList";
            this.taskList.Size = new System.Drawing.Size(722, 62);
            this.taskList.TabIndex = 2;
            this.taskList.Navigate += new XmlNotepad.NavigateEventHandler(this.taskList_Navigate);

            // 
            // tabControl1
            // 
            this.tabControlLists = new TabControl();
            this.tabControlLists.Padding = new Point(0, 0);
            this.tabControlLists.Controls.Add(this.tabPageTaskList);
            this.tabControlLists.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.tabControlLists.Location = new System.Drawing.Point(0, 300);
            this.tabControlLists.Name = "tabControl1";
            this.tabControlLists.SelectedIndex = 0;
            this.tabControlLists.Size = new System.Drawing.Size(736, 96);
            this.tabControlLists.TabIndex = 9;
            this.tabControlLists.Selected += new TabControlEventHandler(TabControlLists_Selected);

            this.Controls.Add(this.tabControlLists);

        }
        protected virtual void TabControlLists_Selected(object sender, TabControlEventArgs e) {
        }


        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        [STAThread]
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.statusBar1 = new System.Windows.Forms.StatusBar();
            this.statusBarPanelMessage = new System.Windows.Forms.StatusBarPanel();
            this.statusBarPanelBusy = new System.Windows.Forms.StatusBarPanel();
            this.contextMenu1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ctxcutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxMenuItemCopy = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxMenuItemPaste = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxMenuItem20 = new System.Windows.Forms.ToolStripSeparator();
            this.ctxElementToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxElementBeforeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxElementAfterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxElementChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxAttributeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxAttributeBeforeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxAttributeAfterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxAttributeChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxTextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxTextBeforeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxTextAfterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxTextChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxCommentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxCommentBeforeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxCommentAfterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxCommentChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxCdataToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxCdataBeforeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxCdataAfterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxCdataChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxPIToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxPIBeforeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxPIAfterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxPIChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxMenuItem23 = new System.Windows.Forms.ToolStripSeparator();
            this.ctxMenuItemExpand = new System.Windows.Forms.ToolStripMenuItem();
            this.ctxMenuItemCollapse = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.reloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.recentFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.redoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
            this.cutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripSeparator();
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.repeatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.duplicateToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem6 = new System.Windows.Forms.ToolStripSeparator();
            this.nudgeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.upToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.downToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.leftToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.rightToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem7 = new System.Windows.Forms.ToolStripSeparator();
            this.findToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.replaceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.expandAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.collapseAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem8 = new System.Windows.Forms.ToolStripSeparator();
            this.statusBarToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem9 = new System.Windows.Forms.ToolStripSeparator();
            this.sourceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.schemasToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem11 = new System.Windows.Forms.ToolStripSeparator();
            this.nextErrorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.compareXMLFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.insertToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.elementToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.elementAfterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.elementBeforeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.elementChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.attributeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.attributeBeforeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.attributeAfterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.attributeChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.textToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.textBeforeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.textAfterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.textChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.commentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.commentBeforeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.commentAfterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.commentChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CDATAToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cdataBeforeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cdataAfterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cdataChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.PIToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.PIBeforeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.PIAfterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.PIChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.windowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newWindowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contentsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.indexToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem10 = new System.Windows.Forms.ToolStripSeparator();
            this.aboutXMLNotepadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButtonNew = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonOpen = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonSave = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonUndo = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonRedo = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonCut = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonCopy = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonPaste = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonDelete = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonNudgeUp = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonNudgeDown = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonNudgeLeft = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonNudgeRight = new System.Windows.Forms.ToolStripButton();
            this.helpProvider1 = new System.Windows.Forms.HelpProvider();
            this.tabPageTaskList = new System.Windows.Forms.TabPage();
            this.resizer = new XmlNotepad.PaneResizer();
            this.taskList = new XmlNotepad.TaskList();
            this.tabControlViews = new System.Windows.Forms.TabControl();
            this.tabPageTreeView = new System.Windows.Forms.TabPage();
            this.tabPageHtmlView = new System.Windows.Forms.TabPage();
            ((System.ComponentModel.ISupportInitialize)(this.statusBarPanelMessage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.statusBarPanelBusy)).BeginInit();
            this.contextMenu1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.tabControlViews.SuspendLayout();
            this.tabPageHtmlView.SuspendLayout();
            this.SuspendLayout();
            // 
            // statusBar1
            // 
            this.statusBar1.Location = new System.Drawing.Point(0, 411);
            this.statusBar1.Name = "statusBar1";
            this.statusBar1.Panels.AddRange(new System.Windows.Forms.StatusBarPanel[] {
            this.statusBarPanelMessage,
            this.statusBarPanelBusy});
            this.statusBar1.ShowPanels = true;
            this.statusBar1.Size = new System.Drawing.Size(736, 22);
            this.statusBar1.TabIndex = 5; 
            this.statusBar1.Font = this.Font;
            // 
            // statusBarPanelMessage
            // 
            this.statusBarPanelMessage.Name = "statusBarPanelMessage";
            this.statusBarPanelMessage.Width = 250;
            // 
            // statusBarPanelBusy
            // 
            this.statusBarPanelBusy.Alignment = System.Windows.Forms.HorizontalAlignment.Right;
            this.statusBarPanelBusy.Name = "statusBarPanelBusy";
            // 
            // contextMenu1
            // 
            this.contextMenu1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ctxcutToolStripMenuItem,
            this.ctxMenuItemCopy,
            this.ctxMenuItemPaste,
            this.ctxMenuItem20,
            this.ctxElementToolStripMenuItem,
            this.ctxAttributeToolStripMenuItem,
            this.ctxTextToolStripMenuItem,
            this.ctxCommentToolStripMenuItem,
            this.ctxCdataToolStripMenuItem,
            this.ctxPIToolStripMenuItem,
            this.ctxMenuItem23,
            this.ctxMenuItemExpand,
            this.ctxMenuItemCollapse});
            this.contextMenu1.Name = "contextMenuStrip1";
            this.contextMenu1.Size = new System.Drawing.Size(131, 258);
            // 
            // ctxcutToolStripMenuItem
            // 
            this.ctxcutToolStripMenuItem.Name = "ctxcutToolStripMenuItem";
            this.ctxcutToolStripMenuItem.Size = new System.Drawing.Size(130, 22);
            this.ctxcutToolStripMenuItem.Text = "&Cut";
            // 
            // ctxMenuItemCopy
            // 
            this.ctxMenuItemCopy.Name = "ctxMenuItemCopy";
            this.ctxMenuItemCopy.Size = new System.Drawing.Size(130, 22);
            this.ctxMenuItemCopy.Text = "&Copy";
            this.ctxMenuItemCopy.ToolTipText = "Copy the selected node onto the clipboard";
            // 
            // ctxMenuItemPaste
            // 
            this.ctxMenuItemPaste.Name = "ctxMenuItemPaste";
            this.ctxMenuItemPaste.Size = new System.Drawing.Size(130, 22);
            this.ctxMenuItemPaste.Text = "&Paste";
            this.ctxMenuItemPaste.ToolTipText = "Paste the clipboard contents as a child of the selected node";
            // 
            // ctxMenuItem20
            // 
            this.ctxMenuItem20.Name = "ctxMenuItem20";
            this.ctxMenuItem20.Size = new System.Drawing.Size(127, 6);
            // 
            // ctxElementToolStripMenuItem
            // 
            this.ctxElementToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ctxElementBeforeToolStripMenuItem,
            this.ctxElementAfterToolStripMenuItem,
            this.ctxElementChildToolStripMenuItem});
            this.ctxElementToolStripMenuItem.Name = "ctxElementToolStripMenuItem";
            this.ctxElementToolStripMenuItem.Size = new System.Drawing.Size(130, 22);
            this.ctxElementToolStripMenuItem.Text = "&Element";
            // 
            // ctxElementBeforeToolStripMenuItem
            // 
            this.ctxElementBeforeToolStripMenuItem.Name = "ctxElementBeforeToolStripMenuItem";
            this.ctxElementBeforeToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.ctxElementBeforeToolStripMenuItem.Text = "&Before";
            this.ctxElementBeforeToolStripMenuItem.Click += new System.EventHandler(this.elementBeforeToolStripMenuItem_Click);
            // 
            // ctxElementAfterToolStripMenuItem
            // 
            this.ctxElementAfterToolStripMenuItem.Name = "ctxElementAfterToolStripMenuItem";
            this.ctxElementAfterToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.ctxElementAfterToolStripMenuItem.Text = "&After";
            this.ctxElementAfterToolStripMenuItem.Click += new System.EventHandler(this.elementAfterToolStripMenuItem_Click);
            // 
            // ctxElementChildToolStripMenuItem
            // 
            this.ctxElementChildToolStripMenuItem.Name = "ctxElementChildToolStripMenuItem";
            this.ctxElementChildToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.ctxElementChildToolStripMenuItem.Text = "&Child";
            this.ctxElementChildToolStripMenuItem.Click += new System.EventHandler(this.elementChildToolStripMenuItem_Click);
            // 
            // ctxAttributeToolStripMenuItem
            // 
            this.ctxAttributeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ctxAttributeBeforeToolStripMenuItem,
            this.ctxAttributeAfterToolStripMenuItem,
            this.ctxAttributeChildToolStripMenuItem});
            this.ctxAttributeToolStripMenuItem.Name = "ctxAttributeToolStripMenuItem";
            this.ctxAttributeToolStripMenuItem.Size = new System.Drawing.Size(130, 22);
            this.ctxAttributeToolStripMenuItem.Text = "&Attribute";
            // 
            // ctxAttributeBeforeToolStripMenuItem
            // 
            this.ctxAttributeBeforeToolStripMenuItem.Name = "ctxAttributeBeforeToolStripMenuItem";
            this.ctxAttributeBeforeToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.ctxAttributeBeforeToolStripMenuItem.Text = "&Before";
            this.ctxAttributeBeforeToolStripMenuItem.Click += new System.EventHandler(this.attributeBeforeToolStripMenuItem_Click);
            // 
            // ctxAttributeAfterToolStripMenuItem
            // 
            this.ctxAttributeAfterToolStripMenuItem.Name = "ctxAttributeAfterToolStripMenuItem";
            this.ctxAttributeAfterToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.ctxAttributeAfterToolStripMenuItem.Text = "&After";
            this.ctxAttributeAfterToolStripMenuItem.Click += new System.EventHandler(this.attributeAfterToolStripMenuItem_Click);
            // 
            // ctxAttributeChildToolStripMenuItem
            // 
            this.ctxAttributeChildToolStripMenuItem.Name = "ctxAttributeChildToolStripMenuItem";
            this.ctxAttributeChildToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.ctxAttributeChildToolStripMenuItem.Text = "&Child";
            this.ctxAttributeChildToolStripMenuItem.Click += new System.EventHandler(this.attributeChildToolStripMenuItem_Click);
            // 
            // ctxTextToolStripMenuItem
            // 
            this.ctxTextToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ctxTextBeforeToolStripMenuItem,
            this.ctxTextAfterToolStripMenuItem,
            this.ctxTextChildToolStripMenuItem});
            this.ctxTextToolStripMenuItem.Name = "ctxTextToolStripMenuItem";
            this.ctxTextToolStripMenuItem.Size = new System.Drawing.Size(130, 22);
            this.ctxTextToolStripMenuItem.Text = "&Text";
            // 
            // ctxTextBeforeToolStripMenuItem
            // 
            this.ctxTextBeforeToolStripMenuItem.Name = "ctxTextBeforeToolStripMenuItem";
            this.ctxTextBeforeToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.ctxTextBeforeToolStripMenuItem.Text = "&Before";
            this.ctxTextBeforeToolStripMenuItem.Click += new System.EventHandler(this.textBeforeToolStripMenuItem_Click);
            // 
            // ctxTextAfterToolStripMenuItem
            // 
            this.ctxTextAfterToolStripMenuItem.Name = "ctxTextAfterToolStripMenuItem";
            this.ctxTextAfterToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.ctxTextAfterToolStripMenuItem.Text = "&After";
            this.ctxTextAfterToolStripMenuItem.Click += new System.EventHandler(this.textAfterToolStripMenuItem_Click);
            // 
            // ctxTextChildToolStripMenuItem
            // 
            this.ctxTextChildToolStripMenuItem.Name = "ctxTextChildToolStripMenuItem";
            this.ctxTextChildToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.ctxTextChildToolStripMenuItem.Text = "&Child";
            this.ctxTextChildToolStripMenuItem.Click += new System.EventHandler(this.textChildToolStripMenuItem_Click);
            // 
            // ctxCommentToolStripMenuItem
            // 
            this.ctxCommentToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ctxCommentBeforeToolStripMenuItem,
            this.ctxCommentAfterToolStripMenuItem,
            this.ctxCommentChildToolStripMenuItem});
            this.ctxCommentToolStripMenuItem.Name = "ctxCommentToolStripMenuItem";
            this.ctxCommentToolStripMenuItem.Size = new System.Drawing.Size(130, 22);
            this.ctxCommentToolStripMenuItem.Text = "&Comment";
            // 
            // ctxCommentBeforeToolStripMenuItem
            // 
            this.ctxCommentBeforeToolStripMenuItem.Name = "ctxCommentBeforeToolStripMenuItem";
            this.ctxCommentBeforeToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.ctxCommentBeforeToolStripMenuItem.Text = "&Before";
            this.ctxCommentBeforeToolStripMenuItem.Click += new System.EventHandler(this.commentBeforeToolStripMenuItem_Click);
            // 
            // ctxCommentAfterToolStripMenuItem
            // 
            this.ctxCommentAfterToolStripMenuItem.Name = "ctxCommentAfterToolStripMenuItem";
            this.ctxCommentAfterToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.ctxCommentAfterToolStripMenuItem.Text = "&After";
            this.ctxCommentAfterToolStripMenuItem.Click += new System.EventHandler(this.commentAfterToolStripMenuItem_Click);
            // 
            // ctxCommentChildToolStripMenuItem
            // 
            this.ctxCommentChildToolStripMenuItem.Name = "ctxCommentChildToolStripMenuItem";
            this.ctxCommentChildToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.ctxCommentChildToolStripMenuItem.Text = "&Child";
            this.ctxCommentChildToolStripMenuItem.Click += new System.EventHandler(this.commentChildToolStripMenuItem_Click);
            // 
            // ctxCdataToolStripMenuItem
            // 
            this.ctxCdataToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ctxCdataBeforeToolStripMenuItem,
            this.ctxCdataAfterToolStripMenuItem,
            this.ctxCdataChildToolStripMenuItem});
            this.ctxCdataToolStripMenuItem.Name = "ctxCdataToolStripMenuItem";
            this.ctxCdataToolStripMenuItem.Size = new System.Drawing.Size(130, 22);
            this.ctxCdataToolStripMenuItem.Text = "C&DATA";
            // 
            // ctxCdataBeforeToolStripMenuItem
            // 
            this.ctxCdataBeforeToolStripMenuItem.Name = "ctxCdataBeforeToolStripMenuItem";
            this.ctxCdataBeforeToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.ctxCdataBeforeToolStripMenuItem.Text = "&Before";
            this.ctxCdataBeforeToolStripMenuItem.Click += new System.EventHandler(this.cdataBeforeToolStripMenuItem_Click);
            // 
            // ctxCdataAfterToolStripMenuItem
            // 
            this.ctxCdataAfterToolStripMenuItem.Name = "ctxCdataAfterToolStripMenuItem";
            this.ctxCdataAfterToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.ctxCdataAfterToolStripMenuItem.Text = "&After";
            this.ctxCdataAfterToolStripMenuItem.Click += new System.EventHandler(this.cdataAfterToolStripMenuItem_Click);
            // 
            // ctxCdataChildToolStripMenuItem
            // 
            this.ctxCdataChildToolStripMenuItem.Name = "ctxCdataChildToolStripMenuItem";
            this.ctxCdataChildToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.ctxCdataChildToolStripMenuItem.Text = "&Child";
            this.ctxCdataChildToolStripMenuItem.Click += new System.EventHandler(this.cdataChildToolStripMenuItem_Click);
            // 
            // ctxPIToolStripMenuItem
            // 
            this.ctxPIToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ctxPIBeforeToolStripMenuItem,
            this.ctxPIAfterToolStripMenuItem,
            this.ctxPIChildToolStripMenuItem});
            this.ctxPIToolStripMenuItem.Name = "ctxPIToolStripMenuItem";
            this.ctxPIToolStripMenuItem.Size = new System.Drawing.Size(130, 22);
            this.ctxPIToolStripMenuItem.Text = "&PI";
            // 
            // ctxPIBeforeToolStripMenuItem
            // 
            this.ctxPIBeforeToolStripMenuItem.Name = "ctxPIBeforeToolStripMenuItem";
            this.ctxPIBeforeToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.ctxPIBeforeToolStripMenuItem.Text = "&Before";
            this.ctxPIBeforeToolStripMenuItem.Click += new System.EventHandler(this.PIBeforeToolStripMenuItem_Click);
            // 
            // ctxPIAfterToolStripMenuItem
            // 
            this.ctxPIAfterToolStripMenuItem.Name = "ctxPIAfterToolStripMenuItem";
            this.ctxPIAfterToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.ctxPIAfterToolStripMenuItem.Text = "&After";
            this.ctxPIAfterToolStripMenuItem.Click += new System.EventHandler(this.PIAfterToolStripMenuItem_Click);
            // 
            // ctxPIChildToolStripMenuItem
            // 
            this.ctxPIChildToolStripMenuItem.Name = "ctxPIChildToolStripMenuItem";
            this.ctxPIChildToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.ctxPIChildToolStripMenuItem.Text = "&Child";
            this.ctxPIChildToolStripMenuItem.Click += new System.EventHandler(this.PIChildToolStripMenuItem_Click);
            // 
            // ctxMenuItem23
            // 
            this.ctxMenuItem23.Name = "ctxMenuItem23";
            this.ctxMenuItem23.Size = new System.Drawing.Size(127, 6);
            // 
            // ctxMenuItemExpand
            // 
            this.ctxMenuItemExpand.Name = "ctxMenuItemExpand";
            this.ctxMenuItemExpand.Size = new System.Drawing.Size(130, 22);
            this.ctxMenuItemExpand.Text = "E&xpand";
            // 
            // ctxMenuItemCollapse
            // 
            this.ctxMenuItemCollapse.Name = "ctxMenuItemCollapse";
            this.ctxMenuItemCollapse.Size = new System.Drawing.Size(130, 22);
            this.ctxMenuItemCollapse.Text = "&Collapse";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.viewToolStripMenuItem,
            this.insertToolStripMenuItem,
            this.windowToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(736, 24);
            this.menuStrip1.TabIndex = 7;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem,
            this.openToolStripMenuItem,
            this.reloadToolStripMenuItem,
            this.toolStripMenuItem1,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.toolStripMenuItem2,
            this.recentFilesToolStripMenuItem,
            this.importToolStripMenuItem,
            this.toolStripMenuItem3,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // newToolStripMenuItem
            // 
            this.newToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("newToolStripMenuItem.Image")));
            this.newToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            this.newToolStripMenuItem.Name = "newToolStripMenuItem";
            this.newToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.newToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.newToolStripMenuItem.Text = "&New";
            this.newToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("openToolStripMenuItem.Image")));
            this.openToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.openToolStripMenuItem.Text = "&Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // reloadToolStripMenuItem
            // 
            this.reloadToolStripMenuItem.Name = "reloadToolStripMenuItem";
            this.reloadToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.reloadToolStripMenuItem.Text = "&Reload";
            this.reloadToolStripMenuItem.Click += new System.EventHandler(this.reloadToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(172, 6);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("saveToolStripMenuItem.Image")));
            this.saveToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.saveToolStripMenuItem.Text = "&Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.saveAsToolStripMenuItem.Text = "Save &As...";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(172, 6);
            // 
            // recentFilesToolStripMenuItem
            // 
            this.recentFilesToolStripMenuItem.Enabled = false;
            this.recentFilesToolStripMenuItem.Name = "recentFilesToolStripMenuItem";
            this.recentFilesToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.recentFilesToolStripMenuItem.Text = "Recent &Files";
            // 
            // importToolStripMenuItem
            // 
            this.importToolStripMenuItem.Enabled = false;
            this.importToolStripMenuItem.Name = "importToolStripMenuItem";
            this.importToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.importToolStripMenuItem.Text = "&Import...";
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(172, 6);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(175, 22);
            this.exitToolStripMenuItem.Text = "&Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.undoToolStripMenuItem,
            this.redoToolStripMenuItem,
            this.toolStripMenuItem4,
            this.cutToolStripMenuItem,
            this.copyToolStripMenuItem,
            this.pasteToolStripMenuItem,
            this.toolStripMenuItem5,
            this.deleteToolStripMenuItem,
            this.repeatToolStripMenuItem,
            this.duplicateToolStripMenuItem,
            this.toolStripMenuItem6,
            this.nudgeToolStripMenuItem,
            this.toolStripMenuItem7,
            this.findToolStripMenuItem,
            this.replaceToolStripMenuItem});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.editToolStripMenuItem.Text = "&Edit";
            // 
            // undoToolStripMenuItem
            // 
            this.undoToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("undoToolStripMenuItem.Image")));
            this.undoToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            this.undoToolStripMenuItem.Name = "undoToolStripMenuItem";
            this.undoToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z)));
            this.undoToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.undoToolStripMenuItem.Text = "&Undo";
            this.undoToolStripMenuItem.Click += new System.EventHandler(this.undoToolStripMenuItem_Click);
            // 
            // redoToolStripMenuItem
            // 
            this.redoToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("redoToolStripMenuItem.Image")));
            this.redoToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            this.redoToolStripMenuItem.Name = "redoToolStripMenuItem";
            this.redoToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Y)));
            this.redoToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.redoToolStripMenuItem.Text = "&Redo";
            this.redoToolStripMenuItem.Click += new System.EventHandler(this.redoToolStripMenuItem_Click);
            // 
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            this.toolStripMenuItem4.Size = new System.Drawing.Size(165, 6);
            // 
            // cutToolStripMenuItem
            // 
            this.cutToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("cutToolStripMenuItem.Image")));
            this.cutToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            this.cutToolStripMenuItem.Name = "cutToolStripMenuItem";
            this.cutToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.cutToolStripMenuItem.Text = "&Cut";
            this.cutToolStripMenuItem.Click += new System.EventHandler(this.cutToolStripMenuItem_Click);
            // 
            // copyToolStripMenuItem
            // 
            this.copyToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("copyToolStripMenuItem.Image")));
            this.copyToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            this.copyToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.copyToolStripMenuItem.Text = "C&opy";
            this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
            // 
            // pasteToolStripMenuItem
            // 
            this.pasteToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("pasteToolStripMenuItem.Image")));
            this.pasteToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
            this.pasteToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.pasteToolStripMenuItem.Text = "&Paste";
            this.pasteToolStripMenuItem.Click += new System.EventHandler(this.pasteToolStripMenuItem_Click);
            // 
            // toolStripMenuItem5
            // 
            this.toolStripMenuItem5.Name = "toolStripMenuItem5";
            this.toolStripMenuItem5.Size = new System.Drawing.Size(165, 6);
            // 
            // deleteToolStripMenuItem
            // 
            this.deleteToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("deleteToolStripMenuItem.Image")));
            this.deleteToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
            this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            this.deleteToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.deleteToolStripMenuItem.Text = "&Delete";
            this.deleteToolStripMenuItem.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
            // 
            // repeatToolStripMenuItem
            // 
            this.repeatToolStripMenuItem.Name = "repeatToolStripMenuItem";
            this.repeatToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Insert;
            this.repeatToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.repeatToolStripMenuItem.Text = "&Insert";
            this.repeatToolStripMenuItem.Click += new System.EventHandler(this.repeatToolStripMenuItem_Click);
            // 
            // duplicateToolStripMenuItem
            // 
            this.duplicateToolStripMenuItem.Name = "duplicateToolStripMenuItem";
            this.duplicateToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
            this.duplicateToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.duplicateToolStripMenuItem.Text = "&Duplicate";
            this.duplicateToolStripMenuItem.Click += new System.EventHandler(this.duplicateToolStripMenuItem_Click);
            // 
            // toolStripMenuItem6
            // 
            this.toolStripMenuItem6.Name = "toolStripMenuItem6";
            this.toolStripMenuItem6.Size = new System.Drawing.Size(165, 6);
            // 
            // nudgeToolStripMenuItem
            // 
            this.nudgeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.upToolStripMenuItem,
            this.downToolStripMenuItem,
            this.leftToolStripMenuItem,
            this.rightToolStripMenuItem});
            this.nudgeToolStripMenuItem.Name = "nudgeToolStripMenuItem";
            this.nudgeToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.nudgeToolStripMenuItem.Text = "Nudge";
            // 
            // upToolStripMenuItem
            // 
            this.upToolStripMenuItem.Name = "upToolStripMenuItem";
            this.upToolStripMenuItem.Size = new System.Drawing.Size(112, 22);
            this.upToolStripMenuItem.Text = "&Up";
            this.upToolStripMenuItem.Click += new System.EventHandler(this.upToolStripMenuItem_Click);
            // 
            // downToolStripMenuItem
            // 
            this.downToolStripMenuItem.Name = "downToolStripMenuItem";
            this.downToolStripMenuItem.Size = new System.Drawing.Size(112, 22);
            this.downToolStripMenuItem.Text = "&Down";
            this.downToolStripMenuItem.Click += new System.EventHandler(this.downToolStripMenuItem_Click);
            // 
            // leftToolStripMenuItem
            // 
            this.leftToolStripMenuItem.Name = "leftToolStripMenuItem";
            this.leftToolStripMenuItem.Size = new System.Drawing.Size(112, 22);
            this.leftToolStripMenuItem.Text = "&Left";
            this.leftToolStripMenuItem.Click += new System.EventHandler(this.leftToolStripMenuItem_Click);
            // 
            // rightToolStripMenuItem
            // 
            this.rightToolStripMenuItem.Name = "rightToolStripMenuItem";
            this.rightToolStripMenuItem.Size = new System.Drawing.Size(112, 22);
            this.rightToolStripMenuItem.Text = "&Right";
            this.rightToolStripMenuItem.Click += new System.EventHandler(this.rightToolStripMenuItem_Click);
            // 
            // toolStripMenuItem7
            // 
            this.toolStripMenuItem7.Name = "toolStripMenuItem7";
            this.toolStripMenuItem7.Size = new System.Drawing.Size(165, 6);
            // 
            // findToolStripMenuItem
            // 
            this.findToolStripMenuItem.Name = "findToolStripMenuItem";
            this.findToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F)));
            this.findToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.findToolStripMenuItem.Text = "&Find...";
            this.findToolStripMenuItem.Click += new System.EventHandler(this.findToolStripMenuItem_Click);
            // 
            // replaceToolStripMenuItem
            // 
            this.replaceToolStripMenuItem.Name = "replaceToolStripMenuItem";
            this.replaceToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            this.replaceToolStripMenuItem.Text = "&Replace...";
            this.replaceToolStripMenuItem.Click += new System.EventHandler(this.replaceToolStripMenuItem_Click);
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.expandAllToolStripMenuItem,
            this.collapseAllToolStripMenuItem,
            this.toolStripMenuItem8,
            this.statusBarToolStripMenuItem,
            this.toolStripMenuItem9,
            this.sourceToolStripMenuItem,
            this.optionsToolStripMenuItem,
            this.schemasToolStripMenuItem,
            this.toolStripMenuItem11,
            this.nextErrorToolStripMenuItem,
            this.toolStripSeparator2,
            this.compareXMLFilesToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(41, 20);
            this.viewToolStripMenuItem.Text = "&View";
            // 
            // expandAllToolStripMenuItem
            // 
            this.expandAllToolStripMenuItem.Name = "expandAllToolStripMenuItem";
            this.expandAllToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            this.expandAllToolStripMenuItem.Text = "&Expand All";
            this.expandAllToolStripMenuItem.Click += new System.EventHandler(this.expandAllToolStripMenuItem_Click);
            // 
            // collapseAllToolStripMenuItem
            // 
            this.collapseAllToolStripMenuItem.Name = "collapseAllToolStripMenuItem";
            this.collapseAllToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            this.collapseAllToolStripMenuItem.Text = "&Collapse All";
            this.collapseAllToolStripMenuItem.Click += new System.EventHandler(this.collapseAllToolStripMenuItem_Click);
            // 
            // toolStripMenuItem8
            // 
            this.toolStripMenuItem8.Name = "toolStripMenuItem8";
            this.toolStripMenuItem8.Size = new System.Drawing.Size(183, 6);
            // 
            // statusBarToolStripMenuItem
            // 
            this.statusBarToolStripMenuItem.Name = "statusBarToolStripMenuItem";
            this.statusBarToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            this.statusBarToolStripMenuItem.Text = "S&tatus Bar";
            this.statusBarToolStripMenuItem.Click += new System.EventHandler(this.statusBarToolStripMenuItem_Click);
            // 
            // toolStripMenuItem9
            // 
            this.toolStripMenuItem9.Name = "toolStripMenuItem9";
            this.toolStripMenuItem9.Size = new System.Drawing.Size(183, 6);
            // 
            // sourceToolStripMenuItem
            // 
            this.sourceToolStripMenuItem.Name = "sourceToolStripMenuItem";
            this.sourceToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            this.sourceToolStripMenuItem.Text = "&Source";
            this.sourceToolStripMenuItem.Click += new System.EventHandler(this.sourceToolStripMenuItem_Click);
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            this.optionsToolStripMenuItem.Text = "&Options...";
            this.optionsToolStripMenuItem.Click += new System.EventHandler(this.optionsToolStripMenuItem_Click);
            // 
            // schemasToolStripMenuItem
            // 
            this.schemasToolStripMenuItem.Name = "schemasToolStripMenuItem";
            this.schemasToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            this.schemasToolStripMenuItem.Text = "Sche&mas...";
            this.schemasToolStripMenuItem.Click += new System.EventHandler(this.schemasToolStripMenuItem_Click);
            // 
            // toolStripMenuItem11
            // 
            this.toolStripMenuItem11.Name = "toolStripMenuItem11";
            this.toolStripMenuItem11.Size = new System.Drawing.Size(183, 6);
            // 
            // nextErrorToolStripMenuItem
            // 
            this.nextErrorToolStripMenuItem.Name = "nextErrorToolStripMenuItem";
            this.nextErrorToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F4;
            this.nextErrorToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            this.nextErrorToolStripMenuItem.Text = "&Next Error";
            this.nextErrorToolStripMenuItem.Click += new System.EventHandler(this.nextErrorToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(183, 6);
            // 
            // compareXMLFilesToolStripMenuItem
            // 
            this.compareXMLFilesToolStripMenuItem.Name = "compareXMLFilesToolStripMenuItem";
            this.compareXMLFilesToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            this.compareXMLFilesToolStripMenuItem.Text = "Compare XML Files...";
            this.compareXMLFilesToolStripMenuItem.Click += new System.EventHandler(this.compareXMLFilesToolStripMenuItem_Click);
            // 
            // insertToolStripMenuItem
            // 
            this.insertToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.elementToolStripMenuItem,
            this.attributeToolStripMenuItem,
            this.textToolStripMenuItem,
            this.commentToolStripMenuItem,
            this.CDATAToolStripMenuItem,
            this.PIToolStripMenuItem});
            this.insertToolStripMenuItem.Name = "insertToolStripMenuItem";
            this.insertToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
            this.insertToolStripMenuItem.Text = "&Insert";
            // 
            // elementToolStripMenuItem
            // 
            this.elementToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.elementAfterToolStripMenuItem,
            this.elementBeforeToolStripMenuItem,
            this.elementChildToolStripMenuItem});
            this.elementToolStripMenuItem.Name = "elementToolStripMenuItem";
            this.elementToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.elementToolStripMenuItem.Text = "Element";
            // 
            // elementAfterToolStripMenuItem
            // 
            this.elementAfterToolStripMenuItem.Name = "elementAfterToolStripMenuItem";
            this.elementAfterToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.elementAfterToolStripMenuItem.Text = "&After";
            this.elementAfterToolStripMenuItem.Click += new System.EventHandler(this.elementAfterToolStripMenuItem_Click);
            // 
            // elementBeforeToolStripMenuItem
            // 
            this.elementBeforeToolStripMenuItem.Name = "elementBeforeToolStripMenuItem";
            this.elementBeforeToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.elementBeforeToolStripMenuItem.Text = "&Before";
            this.elementBeforeToolStripMenuItem.Click += new System.EventHandler(this.elementBeforeToolStripMenuItem_Click);
            // 
            // elementChildToolStripMenuItem
            // 
            this.elementChildToolStripMenuItem.Name = "elementChildToolStripMenuItem";
            this.elementChildToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.elementChildToolStripMenuItem.Text = "&Child";
            this.elementChildToolStripMenuItem.Click += new System.EventHandler(this.elementChildToolStripMenuItem_Click);
            // 
            // attributeToolStripMenuItem
            // 
            this.attributeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.attributeBeforeToolStripMenuItem,
            this.attributeAfterToolStripMenuItem,
            this.attributeChildToolStripMenuItem});
            this.attributeToolStripMenuItem.Name = "attributeToolStripMenuItem";
            this.attributeToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.attributeToolStripMenuItem.Text = "&Attribute";
            // 
            // attributeBeforeToolStripMenuItem
            // 
            this.attributeBeforeToolStripMenuItem.Name = "attributeBeforeToolStripMenuItem";
            this.attributeBeforeToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.attributeBeforeToolStripMenuItem.Text = "&Before";
            this.attributeBeforeToolStripMenuItem.Click += new System.EventHandler(this.attributeBeforeToolStripMenuItem_Click);
            // 
            // attributeAfterToolStripMenuItem
            // 
            this.attributeAfterToolStripMenuItem.Name = "attributeAfterToolStripMenuItem";
            this.attributeAfterToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.attributeAfterToolStripMenuItem.Text = "&After";
            this.attributeAfterToolStripMenuItem.Click += new System.EventHandler(this.attributeAfterToolStripMenuItem_Click);
            // 
            // attributeChildToolStripMenuItem
            // 
            this.attributeChildToolStripMenuItem.Name = "attributeChildToolStripMenuItem";
            this.attributeChildToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.attributeChildToolStripMenuItem.Text = "&Child";
            this.attributeChildToolStripMenuItem.Click += new System.EventHandler(this.attributeChildToolStripMenuItem_Click);
            // 
            // textToolStripMenuItem
            // 
            this.textToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.textBeforeToolStripMenuItem,
            this.textAfterToolStripMenuItem,
            this.textChildToolStripMenuItem});
            this.textToolStripMenuItem.Name = "textToolStripMenuItem";
            this.textToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.textToolStripMenuItem.Text = "&Text";
            // 
            // textBeforeToolStripMenuItem
            // 
            this.textBeforeToolStripMenuItem.Name = "textBeforeToolStripMenuItem";
            this.textBeforeToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.textBeforeToolStripMenuItem.Text = "&Before";
            this.textBeforeToolStripMenuItem.Click += new System.EventHandler(this.textBeforeToolStripMenuItem_Click);
            // 
            // textAfterToolStripMenuItem
            // 
            this.textAfterToolStripMenuItem.Name = "textAfterToolStripMenuItem";
            this.textAfterToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.textAfterToolStripMenuItem.Text = "&After";
            this.textAfterToolStripMenuItem.Click += new System.EventHandler(this.textAfterToolStripMenuItem_Click);
            // 
            // textChildToolStripMenuItem
            // 
            this.textChildToolStripMenuItem.Name = "textChildToolStripMenuItem";
            this.textChildToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.textChildToolStripMenuItem.Text = "&Child";
            this.textChildToolStripMenuItem.Click += new System.EventHandler(this.textChildToolStripMenuItem_Click);
            // 
            // commentToolStripMenuItem
            // 
            this.commentToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.commentBeforeToolStripMenuItem,
            this.commentAfterToolStripMenuItem,
            this.commentChildToolStripMenuItem});
            this.commentToolStripMenuItem.Name = "commentToolStripMenuItem";
            this.commentToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.commentToolStripMenuItem.Text = "&Comment";
            // 
            // commentBeforeToolStripMenuItem
            // 
            this.commentBeforeToolStripMenuItem.Name = "commentBeforeToolStripMenuItem";
            this.commentBeforeToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.commentBeforeToolStripMenuItem.Text = "&Before";
            this.commentBeforeToolStripMenuItem.Click += new System.EventHandler(this.commentBeforeToolStripMenuItem_Click);
            // 
            // commentAfterToolStripMenuItem
            // 
            this.commentAfterToolStripMenuItem.Name = "commentAfterToolStripMenuItem";
            this.commentAfterToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.commentAfterToolStripMenuItem.Text = "&After";
            this.commentAfterToolStripMenuItem.Click += new System.EventHandler(this.commentAfterToolStripMenuItem_Click);
            // 
            // commentChildToolStripMenuItem
            // 
            this.commentChildToolStripMenuItem.Name = "commentChildToolStripMenuItem";
            this.commentChildToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.commentChildToolStripMenuItem.Text = "&Child";
            this.commentChildToolStripMenuItem.Click += new System.EventHandler(this.commentChildToolStripMenuItem_Click);
            // 
            // CDATAToolStripMenuItem
            // 
            this.CDATAToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cdataBeforeToolStripMenuItem,
            this.cdataAfterToolStripMenuItem,
            this.cdataChildToolStripMenuItem});
            this.CDATAToolStripMenuItem.Name = "CDATAToolStripMenuItem";
            this.CDATAToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.CDATAToolStripMenuItem.Text = "C&DATA";
            // 
            // cdataBeforeToolStripMenuItem
            // 
            this.cdataBeforeToolStripMenuItem.Name = "cdataBeforeToolStripMenuItem";
            this.cdataBeforeToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.cdataBeforeToolStripMenuItem.Text = "&Before";
            this.cdataBeforeToolStripMenuItem.Click += new System.EventHandler(this.cdataBeforeToolStripMenuItem_Click);
            // 
            // cdataAfterToolStripMenuItem
            // 
            this.cdataAfterToolStripMenuItem.Name = "cdataAfterToolStripMenuItem";
            this.cdataAfterToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.cdataAfterToolStripMenuItem.Text = "&After";
            this.cdataAfterToolStripMenuItem.Click += new System.EventHandler(this.cdataAfterToolStripMenuItem_Click);
            // 
            // cdataChildToolStripMenuItem
            // 
            this.cdataChildToolStripMenuItem.Name = "cdataChildToolStripMenuItem";
            this.cdataChildToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.cdataChildToolStripMenuItem.Text = "&Child";
            this.cdataChildToolStripMenuItem.Click += new System.EventHandler(this.cdataChildToolStripMenuItem_Click);
            // 
            // PIToolStripMenuItem
            // 
            this.PIToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.PIBeforeToolStripMenuItem,
            this.PIAfterToolStripMenuItem,
            this.PIChildToolStripMenuItem});
            this.PIToolStripMenuItem.Name = "PIToolStripMenuItem";
            this.PIToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.PIToolStripMenuItem.Text = "&Processing Instruction";
            // 
            // PIBeforeToolStripMenuItem
            // 
            this.PIBeforeToolStripMenuItem.Name = "PIBeforeToolStripMenuItem";
            this.PIBeforeToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.PIBeforeToolStripMenuItem.Text = "&Before";
            this.PIBeforeToolStripMenuItem.Click += new System.EventHandler(this.PIBeforeToolStripMenuItem_Click);
            // 
            // PIAfterToolStripMenuItem
            // 
            this.PIAfterToolStripMenuItem.Name = "PIAfterToolStripMenuItem";
            this.PIAfterToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.PIAfterToolStripMenuItem.Text = "&After";
            this.PIAfterToolStripMenuItem.Click += new System.EventHandler(this.PIAfterToolStripMenuItem_Click);
            // 
            // PIChildToolStripMenuItem
            // 
            this.PIChildToolStripMenuItem.Name = "PIChildToolStripMenuItem";
            this.PIChildToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.PIChildToolStripMenuItem.Text = "&Child";
            this.PIChildToolStripMenuItem.Click += new System.EventHandler(this.PIChildToolStripMenuItem_Click);
            // 
            // windowToolStripMenuItem
            // 
            this.windowToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newWindowToolStripMenuItem});
            this.windowToolStripMenuItem.Name = "windowToolStripMenuItem";
            this.windowToolStripMenuItem.Size = new System.Drawing.Size(57, 20);
            this.windowToolStripMenuItem.Text = "&Window";
            // 
            // newWindowToolStripMenuItem
            // 
            this.newWindowToolStripMenuItem.Name = "newWindowToolStripMenuItem";
            this.newWindowToolStripMenuItem.Size = new System.Drawing.Size(147, 22);
            this.newWindowToolStripMenuItem.Text = "&New Window";
            this.newWindowToolStripMenuItem.Click += new System.EventHandler(this.newWindowToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.contentsToolStripMenuItem,
            this.indexToolStripMenuItem,
            this.toolStripMenuItem10,
            this.aboutXMLNotepadToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(40, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // contentsToolStripMenuItem
            // 
            this.contentsToolStripMenuItem.Name = "contentsToolStripMenuItem";
            this.contentsToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.contentsToolStripMenuItem.Text = "&Contents";
            this.contentsToolStripMenuItem.Click += new System.EventHandler(this.contentsToolStripMenuItem_Click);
            // 
            // indexToolStripMenuItem
            // 
            this.indexToolStripMenuItem.Name = "indexToolStripMenuItem";
            this.indexToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.indexToolStripMenuItem.Text = "&Index";
            this.indexToolStripMenuItem.Click += new System.EventHandler(this.indexToolStripMenuItem_Click);
            // 
            // toolStripMenuItem10
            // 
            this.toolStripMenuItem10.Name = "toolStripMenuItem10";
            this.toolStripMenuItem10.Size = new System.Drawing.Size(177, 6);
            // 
            // aboutXMLNotepadToolStripMenuItem
            // 
            this.aboutXMLNotepadToolStripMenuItem.Name = "aboutXMLNotepadToolStripMenuItem";
            this.aboutXMLNotepadToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.aboutXMLNotepadToolStripMenuItem.Text = "&About XML Notepad";
            this.aboutXMLNotepadToolStripMenuItem.Click += new System.EventHandler(this.aboutXMLNotepadToolStripMenuItem_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonNew,
            this.toolStripButtonOpen,
            this.toolStripButtonSave,
            this.toolStripButtonUndo,
            this.toolStripButtonRedo,
            this.toolStripButtonCut,
            this.toolStripButtonCopy,
            this.toolStripButtonPaste,
            this.toolStripButtonDelete,
            this.toolStripSeparator1,
            this.toolStripButtonNudgeUp,
            this.toolStripButtonNudgeDown,
            this.toolStripButtonNudgeLeft,
            this.toolStripButtonNudgeRight});
            this.toolStrip1.Location = new System.Drawing.Point(0, 24);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(736, 25);
            this.toolStrip1.TabIndex = 8;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButtonNew
            // 
            this.toolStripButtonNew.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonNew.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonNew.Image")));
            this.toolStripButtonNew.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonNew.Name = "toolStripButtonNew";
            this.toolStripButtonNew.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonNew.Text = "&New";
            this.toolStripButtonNew.ToolTipText = "New file";
            this.toolStripButtonNew.Click += new System.EventHandler(this.toolStripButtonNew_Click);
            // 
            // toolStripButtonOpen
            // 
            this.toolStripButtonOpen.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonOpen.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonOpen.Image")));
            this.toolStripButtonOpen.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonOpen.Name = "toolStripButtonOpen";
            this.toolStripButtonOpen.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonOpen.Text = "&Open";
            this.toolStripButtonOpen.ToolTipText = "Open a file";
            this.toolStripButtonOpen.Click += new System.EventHandler(this.toolStripButtonOpen_Click);
            // 
            // toolStripButtonSave
            // 
            this.toolStripButtonSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonSave.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonSave.Image")));
            this.toolStripButtonSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSave.Name = "toolStripButtonSave";
            this.toolStripButtonSave.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonSave.Text = "Save";
            this.toolStripButtonSave.ToolTipText = "Save current file";
            this.toolStripButtonSave.Click += new System.EventHandler(this.toolStripButtonSave_Click);
            // 
            // toolStripButtonUndo
            // 
            this.toolStripButtonUndo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonUndo.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonUndo.Image")));
            this.toolStripButtonUndo.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonUndo.Name = "toolStripButtonUndo";
            this.toolStripButtonUndo.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonUndo.Text = "Undo";
            this.toolStripButtonUndo.Click += new System.EventHandler(this.toolStripButtonUndo_Click);
            // 
            // toolStripButtonRedo
            // 
            this.toolStripButtonRedo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonRedo.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonRedo.Image")));
            this.toolStripButtonRedo.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonRedo.Name = "toolStripButtonRedo";
            this.toolStripButtonRedo.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonRedo.Text = "Redo";
            this.toolStripButtonRedo.Click += new System.EventHandler(this.toolStripButtonRedo_Click);
            // 
            // toolStripButtonCut
            // 
            this.toolStripButtonCut.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonCut.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonCut.Image")));
            this.toolStripButtonCut.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonCut.Name = "toolStripButtonCut";
            this.toolStripButtonCut.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonCut.Text = "Cut the selected node onto the clipboard";
            this.toolStripButtonCut.Click += new System.EventHandler(this.toolStripButtonCut_Click);
            // 
            // toolStripButtonCopy
            // 
            this.toolStripButtonCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonCopy.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonCopy.Image")));
            this.toolStripButtonCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonCopy.Name = "toolStripButtonCopy";
            this.toolStripButtonCopy.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonCopy.Text = "Copy the selected node onto the clipboard";
            this.toolStripButtonCopy.Click += new System.EventHandler(this.toolStripButtonCopy_Click);
            // 
            // toolStripButtonPaste
            // 
            this.toolStripButtonPaste.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonPaste.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonPaste.Image")));
            this.toolStripButtonPaste.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonPaste.Name = "toolStripButtonPaste";
            this.toolStripButtonPaste.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonPaste.Text = "Paste the clipboard XML fragment as children of the selected node";
            this.toolStripButtonPaste.Click += new System.EventHandler(this.toolStripButtonPaste_Click);
            // 
            // toolStripButtonDelete
            // 
            this.toolStripButtonDelete.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonDelete.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonDelete.Image")));
            this.toolStripButtonDelete.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonDelete.Name = "toolStripButtonDelete";
            this.toolStripButtonDelete.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonDelete.Text = "Delete the selected node";
            this.toolStripButtonDelete.Click += new System.EventHandler(this.toolStripButtonDelete_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonNudgeUp
            // 
            this.toolStripButtonNudgeUp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonNudgeUp.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonNudgeUp.Image")));
            this.toolStripButtonNudgeUp.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonNudgeUp.Name = "toolStripButtonNudgeUp";
            this.toolStripButtonNudgeUp.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonNudgeUp.Text = "Nudge up - move the selected node before it\'s previous sibling or before it\'s par" +
                "ent if it has no previous sibling";
            this.toolStripButtonNudgeUp.Click += new System.EventHandler(this.toolStripButtonNudgeUp_Click);
            // 
            // toolStripButtonNudgeDown
            // 
            this.toolStripButtonNudgeDown.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonNudgeDown.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonNudgeDown.Image")));
            this.toolStripButtonNudgeDown.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonNudgeDown.Name = "toolStripButtonNudgeDown";
            this.toolStripButtonNudgeDown.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonNudgeDown.Text = "Nudge down - move the selected node after it\'s next sibling, or after it\'s parent" +
                " if it has no next sibling";
            this.toolStripButtonNudgeDown.Click += new System.EventHandler(this.toolStripButtonNudgeDown_Click);
            // 
            // toolStripButtonNudgeLeft
            // 
            this.toolStripButtonNudgeLeft.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonNudgeLeft.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonNudgeLeft.Image")));
            this.toolStripButtonNudgeLeft.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonNudgeLeft.Name = "toolStripButtonNudgeLeft";
            this.toolStripButtonNudgeLeft.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonNudgeLeft.Text = "Nudge left - move the selected node out to the same level as it\'s parent";
            this.toolStripButtonNudgeLeft.Click += new System.EventHandler(this.toolStripButtonNudgeLeft_Click);
            // 
            // toolStripButtonNudgeRight
            // 
            this.toolStripButtonNudgeRight.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonNudgeRight.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonNudgeRight.Image")));
            this.toolStripButtonNudgeRight.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonNudgeRight.Name = "toolStripButtonNudgeRight";
            this.toolStripButtonNudgeRight.Size = new System.Drawing.Size(23, 22);
            this.toolStripButtonNudgeRight.Text = "Nudge right - move the selected node to become the last child of it\'s previous si" +
                "bling";
            this.toolStripButtonNudgeRight.Click += new System.EventHandler(this.toolStripButtonNudgeRight_Click);
            // 
            // tabPageTaskList
            // 
            this.tabPageTaskList.Location = new System.Drawing.Point(0, 0);
            this.tabPageTaskList.Name = "tabPageTaskList";
            this.tabPageTaskList.Size = new System.Drawing.Size(200, 100);
            this.tabPageTaskList.TabIndex = 0;
            // 
            // resizer
            // 
            this.resizer.Border3DStyle = System.Windows.Forms.Border3DStyle.Raised;
            this.resizer.Location = new System.Drawing.Point(0, 289);
            this.resizer.Name = "resizer";
            this.resizer.Pane1 = null;
            this.resizer.Pane2 = null;
            this.resizer.PaneWidth = 5;
            this.resizer.Size = new System.Drawing.Size(736, 5);
            this.resizer.TabIndex = 6;
            this.resizer.Vertical = false;
            // 
            // taskList
            // 
            this.taskList.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.taskList.Location = new System.Drawing.Point(0, 0);
            this.taskList.Margin = new System.Windows.Forms.Padding(0);
            this.taskList.Name = "taskList";
            this.taskList.Size = new System.Drawing.Size(682, 150);
            this.taskList.TabIndex = 0;
            // 
            // tabControlViews
            // 
            this.tabControlViews.Controls.Add(this.tabPageTreeView);
            this.tabControlViews.Controls.Add(this.tabPageHtmlView);
            this.tabControlViews.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.tabControlViews.Location = new System.Drawing.Point(0, 49);
            this.tabControlViews.Name = "tabControlViews";
            this.tabControlViews.SelectedIndex = 0;
            this.tabControlViews.Size = new System.Drawing.Size(736, 362);
            this.tabControlViews.TabIndex = 9;
            this.tabControlViews.Selected += new System.Windows.Forms.TabControlEventHandler(this.TabControlViews_Selected);
            // 
            // tabPageTreeView
            // 
            this.tabPageTreeView.Location = new System.Drawing.Point(4, 24);
            this.tabPageTreeView.Name = "tabPageTreeView";
            this.tabPageTreeView.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageTreeView.Size = new System.Drawing.Size(728, 334);
            this.tabPageTreeView.TabIndex = 0;
            this.tabPageTreeView.Text = "Tree View";
            this.tabPageTreeView.UseVisualStyleBackColor = true;
            this.tabPageTreeView.Controls.Add(this.xmlTreeView1);

            // 
            // tabPageHtmlView
            // 
            this.tabPageHtmlView.Location = new System.Drawing.Point(4, 24);
            this.tabPageHtmlView.Name = "tabPageHtmlView";
            this.tabPageHtmlView.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageHtmlView.Size = new System.Drawing.Size(728, 334);
            this.tabPageHtmlView.TabIndex = 1;
            this.tabPageHtmlView.Text = "HTML View";
            this.tabPageHtmlView.UseVisualStyleBackColor = true;
            // 
            // FormMain
            // 
            this.ClientSize = new System.Drawing.Size(736, 433);
            this.Controls.Add(this.tabControlViews);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.statusBar1);
            this.Controls.Add(this.resizer);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "FormMain";
            this.Text = "XML Notepad";
            ((System.ComponentModel.ISupportInitialize)(this.statusBarPanelMessage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.statusBarPanelBusy)).EndInit();
            this.contextMenu1.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.tabControlViews.ResumeLayout(false);
            this.tabPageHtmlView.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        protected virtual void TabControlViews_Selected(object sender, TabControlEventArgs e) {
            if (e.TabPage == this.tabPageHtmlView) {
                this.DisplayXsltResults();
            }
        }

        #endregion

        void EnableFileMenu(){
            bool hasFile = (model.FileName != null);
            this.toolStripButtonSave.Enabled = this.saveToolStripMenuItem.Enabled = hasFile;
            this.reloadToolStripMenuItem.Enabled = hasFile;
            this.saveAsToolStripMenuItem.Enabled = true;
        }
        public virtual void DisplayXsltResults() {
                this.xsltViewer.DisplayXsltResults();
        }
        public virtual void New(){
            if (!SaveIfDirty(true))
                return;  
            model.Clear();            
            EnableFileMenu();
            this.settings["FileName"] = new Uri("/", UriKind.RelativeOrAbsolute);
            UpdateMenuState();
        }

        protected virtual IIntellisenseProvider CreateIntellisenseProvider(XmlCache model, ISite site) {
            return new XmlIntellisenseProvider(this.model, site);
        }

        protected override object GetService(Type service) {
            if (service == typeof(UndoManager)){
                return this.undoManager;
            } else if (service == typeof(SchemaCache)) {
                return this.model.SchemaCache;
            } else if (service == typeof(TreeView)) {
                XmlTreeView view = (XmlTreeView)GetService(typeof(XmlTreeView));
                return view.TreeView;
            } else if (service == typeof(XmlTreeView)) {
                if (this.xmlTreeView1 == null) {
                    this.xmlTreeView1 = this.CreateTreeView();
                }
                return this.xmlTreeView1;
            } else if (service == typeof(XmlCache)) {
                return this.model;
            } else if (service == typeof(Settings)){
                return this.settings;
            } else if (service == typeof(IIntellisenseProvider)) {
                if (this.ip == null) this.ip = CreateIntellisenseProvider(this.model, this);
                return this.ip;
            } else if (service == typeof(HelpProvider)) {
                return this.helpProvider1;
            }
            return base.GetService (service);
        }

        public OpenFileDialog OpenFileDialog {
            get { return this.od; }
        }

        public virtual void Open() {
            if (!SaveIfDirty(true))
                return;
            if (od == null) od = new OpenFileDialog();
            if (model.FileName != null) {
                Uri uri = new Uri(model.FileName);
                if (uri.Scheme == "file"){
                    od.FileName = model.FileName;
                }
            }
            od.Filter = "All files (*.*)|*.*";
            if (od.ShowDialog(this) == DialogResult.OK){
                Open(od.FileName);
            }
        }

        public virtual void ShowStatus(string msg) {
            this.statusBarPanelMessage.Text = msg;
        }

        public virtual void Open(string filename) {
            try {
                InternalOpen(filename);             
            } catch (Exception e){
                MessageBox.Show(this, "Error loading:" + filename + "\n" + e.Message, "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);                
            }
        }

        private void InternalOpen(string filename) {
            DateTime start = DateTime.Now;
            this.model.Load(filename);
            this.settings["FileName"] = this.model.Location;
            DateTime finish = DateTime.Now;
            TimeSpan diff = finish - start;
            string s = diff.ToString();
            ShowStatus("Loaded in " + s);
            this.UpdateCaption();
            EnableFileMenu();
            this.recentFiles.AddRecentFile(this.model.Location);
        }

        public virtual bool SaveIfDirty(bool prompt) {
            if (model.Dirty){
                if (prompt){
                    DialogResult rc = MessageBox.Show(this, "Save changes to "+model.FileName+"?" ,"Save Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);                
                    if (rc == DialogResult.Cancel){
                        return false;
                    } else if (rc == DialogResult.Yes){
                        Save();
                    }
                } else {
                    Save();
                }
            }
            return true;
        }
        public virtual void Save() {
            this.xmlTreeView1.Commit();
            if (model.FileName == null){
                SaveAs();
            } else {
                try {
                    model.Save();
                    ShowStatus("Saved");
                } catch (Exception e){
                    MessageBox.Show(this, e.Message, "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }     
            }           
        }
        public virtual void Save(string newName) {
            this.xmlTreeView1.Commit();
            try {
                model.Save(newName);
                ShowStatus("Saved");
                this.settings["FileName"] = model.Location;
                UpdateCaption();
                EnableFileMenu();
            } catch (Exception e){
                MessageBox.Show(this, e.Message, "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);                
            }
        }
        public virtual void SaveAs() {
            SaveFileDialog sd = new SaveFileDialog();
            if (model.FileName != null) sd.FileName = model.FileName;
            sd.Filter = "All files (*.*)|*.*";
            if (sd.ShowDialog(this) == DialogResult.OK){
                Save(sd.FileName);
            }
        }

        string caption = "XML Notepad - {0}";

        public string Caption {
            get { return caption; }
            set { caption = value; }
        }

        public virtual void UpdateCaption() {
            string caption = string.Format(this.Caption, model.FileName);
            if (this.model.Dirty){
                caption += "*";
            }            
            this.Text = caption;
            sourceToolStripMenuItem.Enabled = this.model.FileName != null;
        }

        void OnFileChanged(object sender, EventArgs e) {
            OnFileChanged();
        }

        protected virtual void OnFileChanged() {
            if (MessageBox.Show("The file you are editing has been changed on disk.  Would you like to reload this file?", "File Changed", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes){
                try {
                    this.model.Reload();
                }  catch (Exception ex){
                    MessageBox.Show(this, "Error loading:" + this.model.FileName + "\n" + ex.Message, "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);                
                }
                this.undoManager.Clear();
                this.taskList.Clear();
                this.UpdateCaption(); // may have been renamed.
            }
        }

        private void undoManager_StateChanged(object sender, EventArgs e) {
            this.ShowStatus("");
            this.undoToolStripMenuItem.Enabled = toolStripButtonUndo.Enabled = this.undoManager.CanUndo;
            this.redoToolStripMenuItem.Enabled = toolStripButtonRedo.Enabled = this.undoManager.CanRedo;
            
        }

        public virtual string ConfigFile {
            get { 
                string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                Debug.Assert(!string.IsNullOrEmpty(path));
                return path + @"\Microsoft\Xml Notepad\XmlNotepad.settings";
            }
        }

        public virtual void LoadConfig() {
            string path = null;
            this.loading = true;
            if (this.args != null && this.args.Length > 0) {
                // When user passes arguments we skip the config file
                // This is for unit testing where we need consistent config!
                path = this.args[0];
                this.settings.FileName = this.ConfigFile;
            } else {
                if (File.Exists(this.ConfigFile)) {
                    settings.Load(this.ConfigFile);
                    this.OnSettingsChanged(this, "WindowLocation"); // do this right away since we know it just changed.
                    this.OnSettingsChanged(this, "WindowSize");
                    this.OnSettingsChanged(this, "TaskListSize"); 
                    
                    Uri location = (Uri)this.settings["FileName"];
                    if (location != null && location.OriginalString != "/") {
                        path = location.IsFile ? location.LocalPath : location.AbsoluteUri;
                    }
                }
            }
            if (path != null) {
                try {
                    InternalOpen(path);
                } catch (Exception) {
                    // Oh well, at least we tried!
                    // This probably also means that the schem paths are bogus also.
                    this.model.SchemaCache.Clear();
                }
            }
            this.loading = false;
        }

        public virtual void SaveConfig() {
            this.settings.StopWatchingFileChanges();
            this.settings["WindowLocation"] = this.Location;
            this.settings["WindowSize"] = this.ClientSize;
            this.settings["Font"] = this.Font;
            this.settings["TaskListSize"] = this.tabControlLists.Height;
            this.settings["RecentFiles"] = this.recentFiles.ToArray();
            this.settings.Save(this.ConfigFile);
        }

        #region  ISite implementation
        IComponent ISite.Component{
            get { return this; }
        }

        string ISite.Name {
            get { return this.Name; }
            set { this.Name = value; } 
        }

        IContainer ISite.Container {
            get { return this.Container; }
        }

        bool ISite.DesignMode {
            get { return this.DesignMode;}
        }
        object IServiceProvider.GetService(Type serviceType) {
            return this.GetService(serviceType);
        }
        #endregion

        void OnModelChanged(object sender, ModelChangedEventArgs e) {
            OnModelChanged();
        }

        protected virtual void OnModelChanged() {
            ValidateModel();
        }

        void ValidateModel() {
            TaskHandler handler = new TaskHandler(this.taskList);
            handler.Start();
            Checker checker = new Checker(handler);
            checker.Validate(this.model);
            handler.Finish();
            UpdateCaption();
        }
       
        private void settings_Changed(object sender, string name) {
            // Make sure it's on the right thread...
            ISynchronizeInvoke si = (ISynchronizeInvoke)this;
            if (si.InvokeRequired) {
                si.BeginInvoke(new SettingsEventHandler(OnSettingsChanged),
                    new object[] { sender, name });
            } else {
                OnSettingsChanged(sender, name);
            }
        }

        protected virtual void OnSettingsChanged(object sender, string name) {        
            switch (name){
                case "File":
                    this.settings.Reload(); // just do it!!                    
                    break;
                case "WindowLocation":
                    if (loading) { // only if loading first time!
                        Point pt = (Point)this.settings["WindowLocation"];
                        if (!pt.IsEmpty) {
                            this.Location = pt;
                            this.StartPosition = FormStartPosition.Manual;
                        }
                    }
                    break;
                case "WindowSize":
                    if (loading) { // only if loading first time!
                        Size size = (Size)this.settings["WindowSize"];
                        if (!size.IsEmpty) {
                            this.ClientSize = size;
                        }
                    }
                    break;
                case "TaskListSize":
                    int height = (int)this.settings["TaskListSize"];
                    if (height != 0) {
                        this.tabControlLists.Height = height;
                    } 
                    break;
                case "Font":
                    this.Font = (Font)this.settings["Font"];
                    break;
                case "RecentFiles":
                    Uri[] files = (Uri[])this.settings["RecentFiles"];
                    if (files != null) {
                        this.recentFiles.SetFiles(files);
                    }
                    break;
            }
        }

        void OnRecentFileSelected(object sender, EventArgs e) {
            ToolStripItem item = (ToolStripItem)sender;
            string fileName = item.Text;
            Open(fileName);
        }

        private void treeView1_SelectionChanged(object sender, EventArgs e) {
            UpdateMenuState();
        }

        private void treeView1_NodeChanged(object sender, NodeChangeEventArgs e) {
            UpdateMenuState();
        }

        protected virtual void UpdateMenuState() {

            XmlTreeNode node = this.xmlTreeView1.SelectedNode as XmlTreeNode;
            XmlNode xnode = (node != null) ? node.Node : null;
            bool hasSelection = node != null;
            bool hasXmlNode = xnode != null;

            this.toolStripButtonCut.Enabled = this.cutToolStripMenuItem.Enabled = this.ctxcutToolStripMenuItem.Enabled = hasXmlNode;
            this.toolStripButtonDelete.Enabled = this.deleteToolStripMenuItem.Enabled = hasSelection;
            this.toolStripButtonCopy.Enabled = this.copyToolStripMenuItem.Enabled = this.ctxMenuItemCopy.Enabled = hasXmlNode;
            this.duplicateToolStripMenuItem.Enabled = hasXmlNode;

            this.toolStripButtonNudgeUp.Enabled = upToolStripMenuItem.Enabled = this.xmlTreeView1.CanNudgeNode((XmlTreeNode)this.xmlTreeView1.SelectedNode, NudgeDirection.Up);
            this.toolStripButtonNudgeDown.Enabled = downToolStripMenuItem.Enabled = this.xmlTreeView1.CanNudgeNode((XmlTreeNode)this.xmlTreeView1.SelectedNode, NudgeDirection.Down);
            this.toolStripButtonNudgeLeft.Enabled = leftToolStripMenuItem.Enabled = this.xmlTreeView1.CanNudgeNode((XmlTreeNode)this.xmlTreeView1.SelectedNode, NudgeDirection.Left);
            this.toolStripButtonNudgeRight.Enabled = rightToolStripMenuItem.Enabled = this.xmlTreeView1.CanNudgeNode((XmlTreeNode)this.xmlTreeView1.SelectedNode, NudgeDirection.Right);

            this.repeatToolStripMenuItem.Enabled = hasSelection && xnode != null && this.xmlTreeView1.CanInsertNode(InsertPosition.After, xnode.NodeType);
            this.undoToolStripMenuItem.Enabled = toolStripButtonUndo.Enabled = this.undoManager.CanUndo;
            this.redoToolStripMenuItem.Enabled = toolStripButtonRedo.Enabled = this.undoManager.CanRedo;

            EnableNodeItems(XmlNodeType.Element, this.ctxElementBeforeToolStripMenuItem, this.elementBeforeToolStripMenuItem,
                this.ctxElementAfterToolStripMenuItem, this.elementAfterToolStripMenuItem,
                this.ctxElementChildToolStripMenuItem, this.elementChildToolStripMenuItem);
            EnableNodeItems(XmlNodeType.Attribute, this.ctxAttributeBeforeToolStripMenuItem, this.attributeBeforeToolStripMenuItem,
                this.ctxAttributeAfterToolStripMenuItem, this.attributeAfterToolStripMenuItem,
                this.ctxAttributeChildToolStripMenuItem, this.attributeChildToolStripMenuItem);
            EnableNodeItems(XmlNodeType.Text, this.ctxTextBeforeToolStripMenuItem, this.textBeforeToolStripMenuItem,
                this.ctxTextAfterToolStripMenuItem, this.textAfterToolStripMenuItem,
                this.ctxTextChildToolStripMenuItem, this.textChildToolStripMenuItem);
            EnableNodeItems(XmlNodeType.CDATA, this.ctxCdataBeforeToolStripMenuItem, this.cdataBeforeToolStripMenuItem,
                this.ctxCdataAfterToolStripMenuItem, this.cdataAfterToolStripMenuItem,
                this.ctxCdataChildToolStripMenuItem, this.cdataChildToolStripMenuItem);
            EnableNodeItems(XmlNodeType.Comment, this.ctxCommentBeforeToolStripMenuItem, this.commentBeforeToolStripMenuItem,
                this.ctxCommentAfterToolStripMenuItem, this.commentAfterToolStripMenuItem,
                this.ctxCommentChildToolStripMenuItem, this.commentChildToolStripMenuItem);
            EnableNodeItems(XmlNodeType.ProcessingInstruction, this.ctxPIBeforeToolStripMenuItem, this.PIBeforeToolStripMenuItem,
                this.ctxPIAfterToolStripMenuItem, this.PIAfterToolStripMenuItem,
                this.ctxPIChildToolStripMenuItem, this.PIChildToolStripMenuItem);
        }

        void EnableNodeItems(XmlNodeType nt, ToolStripMenuItem c1, ToolStripMenuItem m1, ToolStripMenuItem c2, ToolStripMenuItem m2, ToolStripMenuItem c3, ToolStripMenuItem m3){
            c1.Enabled = m1.Enabled = this.xmlTreeView1.CanInsertNode(InsertPosition.Before, nt);
            c2.Enabled = m2.Enabled = this.xmlTreeView1.CanInsertNode(InsertPosition.After, nt);
            c3.Enabled = m3.Enabled = this.xmlTreeView1.CanInsertNode(InsertPosition.Child, nt);
        }

        protected virtual void OpenNotepad(string path) {
            if (this.SaveIfDirty(true)){
                string sysdir = Environment.SystemDirectory;
                string notepad = sysdir + Path.DirectorySeparatorChar + "notepad.exe";
                if (File.Exists(notepad)){
                    ProcessStartInfo pi = new ProcessStartInfo(notepad, path);
                    Process.Start(pi);
                }
            }
        }

		void treeView1_ClipboardChanged(object sender, EventArgs e) {
			CheckClipboard();
		}

		void CheckClipboard() {
            this.toolStripButtonPaste.Enabled = this.pasteToolStripMenuItem.Enabled = this.ctxMenuItemPaste.Enabled = TreeData.HasData;
		}

		protected override void OnActivated(EventArgs e) {
			CheckClipboard();
		}

        void taskList_Navigate(object sender, Task task) {
            XmlNode node = task.Data as XmlNode;
            if (node != null) {
                XmlTreeNode tn = this.xmlTreeView1.FindNode(node);
                if (tn != null) {
                    this.xmlTreeView1.SelectedNode = tn;
                    this.xmlTreeView1.Focus();
                }
            }
        }

        private void Form1_DragOver(object sender, DragEventArgs e) {
            IDataObject data = e.Data;
            if (data.GetDataPresent(DataFormats.FileDrop) || data.GetDataPresent(this.urlFormat.Name)){
                e.Effect = DragDropEffects.Copy;
            }
            return;
        }


        private void Form1_DragDrop(object sender, DragEventArgs e) {
            IDataObject data = e.Data;
            if (data.GetDataPresent(DataFormats.FileDrop)){
                Array a = data.GetData(DataFormats.FileDrop) as Array;
                if (a != null){
                    if (a.Length>0 && a.GetValue(0) is string){
                        string filename = (string)a.GetValue(0);
                        this.Open(filename);
                    }
                }
            } else if (data.GetDataPresent(this.urlFormat.Name)){
                Stream stm = data.GetData(this.urlFormat.Name) as Stream;
                if (stm != null) {
                    try {
                        // Note: for some reason sr.ReadToEnd doesn't work right.
                        StreamReader sr = new StreamReader(stm, Encoding.Unicode);
                        StringBuilder sb = new StringBuilder();
                        while (true) {
                            int i = sr.Read();
                            if (i != 0) {
                                sb.Append(Convert.ToChar(i));
                            } else {
                                break;
                            }
                        }
                        string url = sb.ToString();
                        this.Open(url);
                    } catch (Exception){
                    }
                }
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e) {
            this.xmlTreeView1.CancelEdit();
            New();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e) {
            this.xmlTreeView1.CancelEdit();
            Open();
        }

        private void reloadToolStripMenuItem_Click(object sender, EventArgs e) {
            Open(this.model.FileName);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
            this.xmlTreeView1.Commit();
            Save();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e) {
            this.xmlTreeView1.Commit();
            SaveAs();
        }

        private void menuItemRecentFiles_Click(object sender, EventArgs e) {

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            this.xmlTreeView1.Commit();
            this.Close();
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e) {
            this.xmlTreeView1.CancelEdit();
            this.undoManager.Undo();
            UpdateMenuState();
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit()) 
                this.undoManager.Redo();
            UpdateMenuState();
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit()) 
                this.xmlTreeView1.Cut();
            UpdateMenuState();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e) {
            this.xmlTreeView1.Commit();
            this.xmlTreeView1.Copy();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.Paste(InsertPosition.Child);
            UpdateMenuState();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e) {
            this.xmlTreeView1.Commit();
            this.xmlTreeView1.Delete();
            UpdateMenuState();
        }
        private void repeatToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.Insert();
            UpdateMenuState();
        }

        private void duplicateToolStripMenuItem_Click(object sender, EventArgs e) {
            try {
                if (this.xmlTreeView1.Commit())
                    this.xmlTreeView1.Duplicate();
                UpdateMenuState();
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "Duplicate Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }            
        }

        private void upToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())                    
                this.xmlTreeView1.NudgeNode(this.xmlTreeView1.SelectedNode, NudgeDirection.Up);        
        }

        private void downToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.NudgeNode(this.xmlTreeView1.SelectedNode, NudgeDirection.Down);
        }

        private void leftToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.NudgeNode(this.xmlTreeView1.SelectedNode, NudgeDirection.Left);
        }

        private void rightToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.NudgeNode(this.xmlTreeView1.SelectedNode, NudgeDirection.Right);
        }

        private void findToolStripMenuItem_Click(object sender, EventArgs e) {
            if (search == null || !search.Visible ) {
                search = new FormSearch(search);
            }
            search.View = this.xmlTreeView1;
            search.Show(); // modeless
        }

        private void replaceToolStripMenuItem_Click(object sender, EventArgs e) {

        }

        private void expandAllToolStripMenuItem_Click(object sender, EventArgs e) {
            this.xmlTreeView1.ExpandAll();
        }

        private void collapseAllToolStripMenuItem_Click(object sender, EventArgs e) {
            this.xmlTreeView1.CollapseAll();
        }

        private void statusBarToolStripMenuItem_Click(object sender, EventArgs e) {
            bool visible = !statusBarToolStripMenuItem.Checked;
            statusBarToolStripMenuItem.Checked = visible;
            int h = this.ClientSize.Height - this.toolStrip1.Bottom - 2;
            if (visible) {
                h -= this.statusBar1.Height;
            }
            this.tabControlViews.Height = h;
            this.statusBar1.Visible = visible;
            this.PerformLayout();
        }

        private void sourceToolStripMenuItem_Click(object sender, EventArgs e) {
            OpenNotepad(this.model.FileName);
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e) {
            FormOptions options = new FormOptions();
            options.Site = this;
            options.Settings = this.settings;
            options.ShowDialog();
        }


        private void contentsToolStripMenuItem_Click(object sender, EventArgs e) {
            Help.ShowHelp(this, this.helpProvider1.HelpNamespace, HelpNavigator.TableOfContents);
        }

        private void indexToolStripMenuItem_Click(object sender, EventArgs e) {
            Help.ShowHelp(this, this.helpProvider1.HelpNamespace, HelpNavigator.Index);
        }

        private void aboutXMLNotepadToolStripMenuItem_Click(object sender, EventArgs e) {
            FormAbout frm = new FormAbout();
            frm.ShowDialog();
        }

        private void toolStripButtonNew_Click(object sender, EventArgs e) {
            this.xmlTreeView1.CancelEdit();
            this.New();
        }

        private void toolStripButtonOpen_Click(object sender, EventArgs e) {
            this.xmlTreeView1.CancelEdit();
            this.Open();
        }

        private void toolStripButtonSave_Click(object sender, EventArgs e) {
            this.xmlTreeView1.Commit();
            this.Save();
        }

        private void toolStripButtonUndo_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.undoManager.Undo();
            UpdateMenuState();
        }

        private void toolStripButtonRedo_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.undoManager.Redo();
            UpdateMenuState();
        }

        private void toolStripButtonCut_Click(object sender, EventArgs e) {
            this.xmlTreeView1.Cut();
            UpdateMenuState();
        }

        private void toolStripButtonCopy_Click(object sender, EventArgs e) {
            this.xmlTreeView1.Copy();
        }

        private void toolStripButtonPaste_Click(object sender, EventArgs e) {
            this.xmlTreeView1.Paste(InsertPosition.Child);
            UpdateMenuState();
        }

        private void toolStripButtonDelete_Click(object sender, EventArgs e) {
            this.xmlTreeView1.CancelEdit();
            this.xmlTreeView1.Delete();
            UpdateMenuState();
        }

        private void toolStripButtonNudgeUp_Click(object sender, EventArgs e) {
            this.upToolStripMenuItem_Click(sender, e);
        }

        private void toolStripButtonNudgeDown_Click(object sender, EventArgs e) {
            this.downToolStripMenuItem_Click(sender, e);
        }

        private void toolStripButtonNudgeLeft_Click(object sender, EventArgs e) {
            this.leftToolStripMenuItem_Click(sender, e);
        }

        private void toolStripButtonNudgeRight_Click(object sender, EventArgs e) {
            this.rightToolStripMenuItem_Click(sender, e);
        }

        // Insert Menu Items.

        private void elementAfterToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.After, XmlNodeType.Element);
        }

        private void elementBeforeToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Before, XmlNodeType.Element);
        }

        private void elementChildToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Child, XmlNodeType.Element);
        }

        private void attributeBeforeToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Before, XmlNodeType.Attribute);
        }

        private void attributeAfterToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.After, XmlNodeType.Attribute);
        }

        private void attributeChildToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Child, XmlNodeType.Attribute);
        }

        private void textBeforeToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Before, XmlNodeType.Text);
        }

        private void textAfterToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.After, XmlNodeType.Text);
        }

        private void textChildToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Child, XmlNodeType.Text);
        }

        private void commentBeforeToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Before, XmlNodeType.Comment);
        }

        private void commentAfterToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.After, XmlNodeType.Comment);
        }

        private void commentChildToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Child, XmlNodeType.Comment);
        }

        private void cdataBeforeToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Before, XmlNodeType.CDATA);
        }

        private void cdataAfterToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.After, XmlNodeType.CDATA);
        }

        private void cdataChildToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Child, XmlNodeType.CDATA);
        }

        private void PIBeforeToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Before, XmlNodeType.ProcessingInstruction);
        }

        private void PIAfterToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.After, XmlNodeType.ProcessingInstruction);
        }

        private void PIChildToolStripMenuItem_Click(object sender, EventArgs e) {
            if (this.xmlTreeView1.Commit())
                this.xmlTreeView1.InsertNode(InsertPosition.Child, XmlNodeType.ProcessingInstruction);
        }

        void Launch(string exeFileName, string args) {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = exeFileName;
            info.Arguments = args;
            Process p = new Process();
            p.StartInfo = info;
            if (!p.Start()) {
                MessageBox.Show(this, "Error creating new process.", "Process Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
            }            
        }

        private void newWindowToolStripMenuItem_Click(object sender, EventArgs e) {
            this.SaveIfDirty(true);
            string path = Application.ExecutablePath;
            if (!path.EndsWith("XmlNotepad.exe", StringComparison.CurrentCultureIgnoreCase)) {
                // must be running UnitTest.dll!
                Uri baseUri = new Uri(this.GetType().Assembly.Location);
                Uri resolved = new Uri(baseUri, @"..\..\..\Application\bin\debug\XmlNotepad.exe");
                path = resolved.LocalPath;
            }
            Launch(path, "\"" + this.model.FileName + "\"");
        }

        private void schemasToolStripMenuItem_Click(object sender, EventArgs e) {
            FormSchemas frm = new FormSchemas();
            frm.Site = this;
            if (frm.ShowDialog(this) == DialogResult.OK) {
                ValidateModel();
            }
        }

        private void nextErrorToolStripMenuItem_Click(object sender, EventArgs e) {
            this.taskList.NavigateNextError();
        }

        private void compareXMLFilesToolStripMenuItem_Click(object sender, EventArgs e) {
            string secondFile = null;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter =
                "XML files (*.xml;*.xsd)|*.xml;*.xsd;|" +
                "All files (*.*)|*.*";
            ofd.ShowDialog();
            if (ofd.FileName != string.Empty) {
                secondFile = ofd.FileName;
                DoCompare(this.model.FileName, secondFile);
            }

        }
        private void DoCompare(string file1, string file2) {
            Microsoft.OperationsManager.Test.Infrastructure.XmlDiffView diff = new
                Microsoft.OperationsManager.Test.Infrastructure.XmlDiffView();
            string startupPath = Application.StartupPath;
            //output diff file.
            string diffFile = startupPath + Path.DirectorySeparatorChar + "vxd.out";
            // delete an old diff file (but leave it behind so user can examine it
            if (File.Exists(diffFile)) {
                File.Delete(diffFile);
            }
            XmlTextWriter tw = new XmlTextWriter(new StreamWriter(diffFile));
            tw.Formatting = Formatting.Indented;

            bool isEqual = false;
            string tempFile = startupPath + Path.DirectorySeparatorChar +
                Path.GetRandomFileName();

            //Now compare the two files.
            try {
                isEqual = diff.DifferencesSideBySideAsHtml(
                    file1,
                    file2,
                    tempFile,
                    true,
                    Microsoft.OperationsManager.Test.Infrastructure.MomXmlDiffOptions.None);
            } catch (XmlException xe) {
                MessageBox.Show("An exception occured while comparing\n" + xe.StackTrace);
            } finally {
                tw.Close();
            }

            if (isEqual) {
                //This means the files were identical for given options.
                MessageBox.Show("Files Identical for the given options");
                return; //dont need to show the differences.
            }

            Uri uri = new Uri(tempFile);
            WebBrowserForm browserForm = new WebBrowserForm(uri, "XmlDiff");
            browserForm.Show();
        }

    }

}