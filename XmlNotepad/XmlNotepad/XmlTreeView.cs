using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Schema;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Text;

namespace XmlNotepad {

    public class XmlTreeView : System.Windows.Forms.UserControl {
        XmlCache model;
        Settings settings;
        bool disposed;

        public event EventHandler<NodeChangeEventArgs> NodeChanged;
        public event EventHandler<NodeChangeEventArgs> NodeInserted;
        public event EventHandler SelectionChanged;
        public event EventHandler ClipboardChanged;

        XmlTreeNode dragged;
        XmlTreeViewDropFeedback feedback;

        private NodeTextView nodeTextView;
        private TreeView myTreeView;
        private System.Windows.Forms.ImageList imageList1;
        private System.ComponentModel.IContainer components;
        private PaneResizer resizer;
        private System.Windows.Forms.VScrollBar vScrollBar1;

        public XmlTreeView() {

            this.SetStyle(ControlStyles.ContainerControl, true);
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();
            
            myTreeView.AfterLabelEdit += new EventHandler<NodeLabelEditEventArgs>(myTreeView_AfterLabelEdit);            
            myTreeView.AfterCollapse += new EventHandler<TreeViewEventArgs>(myTreeView_AfterCollapse);
            myTreeView.AfterExpand += new EventHandler<TreeViewEventArgs>(myTreeView_AfterExpand);
            myTreeView.AfterSelect += new EventHandler<TreeViewEventArgs>(myTreeView_AfterSelect);
            myTreeView.MouseWheel += new MouseEventHandler(HandleMouseWheel);
            myTreeView.KeyDown += new KeyEventHandler(myTreeView_KeyDown);

            this.myTreeView.DragDrop += new DragEventHandler(treeViewFeedback_DragDrop);
            this.myTreeView.DragEnter += new DragEventHandler(treeViewFeedback_DragEnter);
            this.myTreeView.DragLeave += new EventHandler(treeViewFeedback_DragLeave);
            this.myTreeView.DragOver += new DragEventHandler(treeViewFeedback_DragOver);
            this.myTreeView.AllowDrop = true;
            this.myTreeView.GiveFeedback += new GiveFeedbackEventHandler(myTreeView_GiveFeedback);
            this.myTreeView.ItemDrag += new ItemDragEventHandler(myTreeView_ItemDrag);

            this.nodeTextView.KeyDown += new KeyEventHandler(nodeTextView_KeyDown);
            this.nodeTextView.MouseWheel += new MouseEventHandler(HandleMouseWheel);
            this.nodeTextView.AfterSelect += new EventHandler<TreeViewEventArgs>(nodeTextView_AfterSelect);
            this.nodeTextView.AccessibleRole=System.Windows.Forms.AccessibleRole.List;

            this.Disposed += new EventHandler(OnDisposed);
        }

        void OnDisposed(object sender, EventArgs e) {
            this.disposed = true;
        }

        public void Close() {
            this.myTreeView.Close();
            this.nodeTextView.Close();
        }

        [Browsable(false)]
        public XmlTreeNode SelectedNode {
            get {
                return this.myTreeView.SelectedNode as XmlTreeNode;
            }
            set {
                this.myTreeView.SelectedNode = value;
            }
        }

        public void ExpandAll() {
            this.myTreeView.ExpandAll();
        }

        public void CollapseAll() {
            this.myTreeView.CollapseAll();
        }

        public void SetSite(ISite site) {
            base.Site = site;
            this.nodeTextView.SetSite(site);
            this.myTreeView.SetSite(site);
            this.model = (XmlCache)this.Site.GetService(typeof(XmlCache));
            if (this.model != null) {
                this.model.FileChanged += new EventHandler(OnFileChanged);
                this.model.ModelChanged += new EventHandler<ModelChangedEventArgs>(OnModelChanged);
            }
            this.settings = (Settings)this.Site.GetService(typeof(Settings));
            if (this.settings != null) {
                this.settings.Changed += new SettingsEventHandler(settings_Changed);
            }            
            if (this.model != null) BindTree();
        }

        [Browsable(false)]
        public XmlCache Model {
            get {
                if (this.Site == null) {
                    throw new ApplicationException("ISite has not been provided, so model cannot be found");
                }
                return this.model; 
            }
        }

        [Browsable(false)]
        public Settings Settings {
            get {
                if (this.Site == null) {
                    throw new ApplicationException("ISite has not been provided, so settings cannot be found");
                }
                return this.settings; 
            }
        }

        public NodeTextView NodeTextView {
            get { return nodeTextView; }
            set { nodeTextView = value; }
        }

        public void CancelEdit() {
            TreeNode n = myTreeView.SelectedNode;
            if (n != null && n.IsEditing) {
                n.EndEdit(true);
            }
            this.nodeTextView.EndEdit(true);
        }


        public virtual XmlTreeNode CreateTreeNode() {
            return new XmlTreeNode(this);
        }

        public virtual XmlTreeNode CreateTreeNode(XmlNode node) {
            return new XmlTreeNode(this, node);
        }

        public virtual XmlTreeNode CreateTreeNode(XmlTreeNode parent, XmlNode node) {
            return new XmlTreeNode(this, parent, node);
        }

        public XmlTreeNode FindNode(XmlNode node) {
            return FindNode(this.myTreeView.Nodes, node);
        }

        XmlTreeNode FindNode(TreeNodeCollection nodes, XmlNode node) {
            foreach (XmlTreeNode xn in nodes) {
                if (xn.Node == node) return xn;
                if (xn.Nodes != null) {
                    XmlTreeNode child = FindNode(xn.Nodes, node);
                    if (child != null) return child;
                }
            }
            return null;
        }
        

        public void CollapseAll(bool everything) {
            this.SuspendLayout();
            TreeNode s = this.myTreeView.SelectedNode;
            if (s != null && !everything) {
                s.CollapseAll();
            } 
            else {
                this.myTreeView.CollapseAll();
            }
            this.ResumeLayout();
        }


        public bool Commit() {
            this.nodeTextView.EndEdit(false);
            TreeNode n = myTreeView.SelectedNode;
            if (n != null && n.IsEditing) {
                return n.EndEdit(false);
            }
            return true;
        }

        [Browsable(false)]
        public UndoManager UndoManager {
            get { return (UndoManager)this.Site.GetService(typeof(UndoManager)); }
        }

        [System.ComponentModel.Browsable(false)]
        public IIntellisenseProvider IntellisenseProvider {
            get { return (IIntellisenseProvider)this.Site.GetService(typeof(IIntellisenseProvider)); }
        }

        [Browsable(false)]
        public TreeView TreeView {
            get { return this.myTreeView; }
        }

        private void myTreeView_AfterLabelEdit(object sender, NodeLabelEditEventArgs e) {
            try {
                XmlTreeNode xn = (XmlTreeNode)e.Node;
                XmlNode n = xn.Node;
                if (e.CancelEdit) return; // it's being cancelled.

                if (e.Label == null || StringHelper.IsNullOrEmpty(e.Label.Trim())) {
                    e.CancelEdit = true;
                    string arg = null;
                    if (xn.NodeImage == NodeImage.Attribute) 
                        arg = "attributes";
                    else if (xn.NodeImage == NodeImage.Element || xn.NodeImage == NodeImage.OpenElement || xn.NodeImage == NodeImage.Leaf) 
                        arg = "elements";

                    if (arg != null && n == null && MessageBox.Show(
                        string.Format("XML {0} must have a non-empty name, are you sure you want to leave this name empty?", arg), "Name Error", 
                        MessageBoxButtons.YesNo, MessageBoxIcon.Error) != DialogResult.Yes) {
                        e.Node.BeginEdit();
                    }
                    return; 
                }
                Command cmd = null;
                if (n == null) {
                    TreeNode parent = e.Node.Parent;
                    XmlNode context = (parent == null) ? this.model.Document : ((XmlTreeNode)parent).Node;
                    cmd = this.UndoManager.Peek();
                    try {
                        InsertNode inode = cmd as InsertNode;
                        if (inode != null) {
                            if (inode.RequiresName) {
                                inode.XmlNode = inode.CreateNode(context, e.Label); 
                                // Cause selection event to be triggered so that menu state
                                // is recalculated.
                                this.myTreeView.SelectedNode = null;
                                this.OnNodeInserted(inode.NewNode);
                            }
                        }
                    } 
                    catch (Exception ex) {
                        MessageBox.Show(this, ex.Message, 
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.myTreeView.SelectedNode = e.Node;
                        e.CancelEdit = true;
                        xn.Label = e.Label.Trim();
                        e.Node.BeginEdit();
                        return;
                    }
                    e.Node.Label = e.Label;
                    this.myTreeView.SelectedNode = e.Node;
                    this.nodeTextView.Invalidate(e.Node);
                    this.nodeTextView.BeginEdit(null);
                    return; // one undoable unit.
                }
                switch (n != null ? n.NodeType : XmlNodeType.None) {
                    case XmlNodeType.Comment:
                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                        e.CancelEdit = true;
                        // actually it would be cool to change the node type at this point.
                        break;
                    case XmlNodeType.Attribute:
                        cmd = new EditAttributeName(n as XmlAttribute, e);
                        break;
                    case XmlNodeType.Element:
                        cmd =  new EditElementName(n as XmlElement, e);
                        break;
                    case XmlNodeType.ProcessingInstruction:
                        cmd = new EditProcessingInstructionName(n as XmlProcessingInstruction, e);
                        break;
                }
                if (cmd != null) {
                    this.UndoManager.Push(cmd);
                }
            } 
            catch (Exception ex) {
                e.CancelEdit = true;
                MessageBox.Show(ex.Message, "Edit Name Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }        

        private void myTreeView_AfterCollapse(object sender, TreeViewEventArgs e) {
            PerformLayout();
            Invalidate();
        }

        private void myTreeView_AfterExpand(object sender, TreeViewEventArgs e) {
            PerformLayout();
            Invalidate();
        }        

        private void myTreeView_AfterSelect(object sender, TreeViewEventArgs e) {            
            TreeNode n = e.Node;
            this.nodeTextView.SelectedNode = n;
            if (n == null) return;
            ScrollIntoView(n);
        }

        void nodeTextView_AfterSelect(object sender, TreeViewEventArgs e) {
            if (this.myTreeView != null) {
                this.myTreeView.SelectedNode = e.Node;
            }
        }

        public virtual void ScrollIntoView(TreeNode n) {
            // Scroll the newly selected node into view.
            Rectangle r = n.LabelBounds;
            int y = r.Top + myTreeView.ScrollPosition.Y;
            if (y > this.Height-myTreeView.ItemHeight) {
                y = y - this.Height + myTreeView.ItemHeight;
            } 
            else if (y > 0) {
                y = 0;
            }
            if (y != 0) {
                myTreeView.ScrollPosition = new Point(myTreeView.ScrollPosition.X, myTreeView.ScrollPosition.Y - y);
                nodeTextView.ScrollPosition = myTreeView.ScrollPosition;
                this.nodeTextView.Invalidate();
                this.vScrollBar1.Value = Math.Max(0,Math.Min(this.vScrollBar1.Maximum, this.vScrollBar1.Value + (y / myTreeView.ItemHeight)));
            }
            nodeTextView.SelectedNode = n;
            if (SelectionChanged != null) SelectionChanged(this, new EventArgs()); 
        }

        internal void OnNodeChanged(XmlTreeNode node) {
            if (NodeChanged != null) NodeChanged(this, new NodeChangeEventArgs(node));
        }

        public virtual void OnNodeInserted(XmlTreeNode node) {
            if (NodeInserted != null) NodeInserted(this, new NodeChangeEventArgs(node));
            // Populate default value.
            if (node.Node != null && !node.Node.HasChildNodes &&
                (node.NodeType == XmlNodeType.Attribute || node.NodeType == XmlNodeType.Element)) {
                SetDefaultValue(node);
            }
        }

        protected virtual void SetDefaultValue(XmlTreeNode node) {
            IIntellisenseProvider provider = this.IntellisenseProvider;
            if (provider != null) {
                provider.SetContextNode(node);
                string defaultValue = provider.GetDefaultValue();
                if (!string.IsNullOrEmpty(defaultValue)) {
                    EditNodeValue cmd = new EditNodeValue(this, node, defaultValue);
                    this.UndoManager.Push(cmd);                    
                }
            }
        }

        private void OnModelChanged(object sender, ModelChangedEventArgs e) {
            if (disposed) return;
            switch (e.ModelChangeType) {
                case ModelChangeType.Reloaded:
                    BindTree();
                    break;
                case ModelChangeType.NamespaceChanged:
                    RecalculateNamespaces(e.Node);
                    break;
            }
            nodeTextView.Invalidate();
        }

        public bool IsEditing {
            get { return this.myTreeView.IsEditing || this.nodeTextView.IsEditing;  }
        }

        private void OnFileChanged(object sender, EventArgs e) {
            BindTree();
        }

        void BindTree() {
            this.myTreeView.ScrollPosition = new Point(0,0);
            this.vScrollBar1.Maximum = 0;
            this.vScrollBar1.Value = 0;
            this.nodeTextView.Top = 0;

            this.SuspendLayout();
            this.myTreeView.BeginUpdate();
            try {
                XmlTreeNodeCollection nodes = new XmlTreeNodeCollection(this, this.model.Document);
                this.myTreeView.Nodes = this.nodeTextView.Nodes = nodes;

                foreach (XmlTreeNode tn in this.myTreeView.Nodes) {
                    tn.Expand();
                }
                this.nodeTextView.Reset();
            } 
            finally {
                this.myTreeView.EndUpdate();
            }
            this.ResumeLayout();
            this.myTreeView.Invalidate();
        }


        int CountVisibleNodes(TreeNodeCollection tc) {
            if (tc == null) return 0;
            int count = 0;
            foreach (TreeNode tn in tc) {
                count++;
                if (tn.IsExpanded) {
                    count += CountVisibleNodes(tn.Nodes);
                }
            }
            return count;
        }

        
        protected override void OnLayout(LayoutEventArgs levent) {
            
            int x = this.resizer.Left;
            this.myTreeView.Width = x;
            int count = CountVisibleNodes(this.myTreeView.Nodes);            
            int h = Math.Max(this.Height, this.myTreeView.ItemHeight * count);
            this.vScrollBar1.Left = this.Right - this.vScrollBar1.Width;
            this.vScrollBar1.Height = this.Height;
            this.myTreeView.Size = new Size(this.resizer.Left, this.Height);
            this.nodeTextView.Size = new Size(this.vScrollBar1.Left - this.resizer.Right, this.Height);
            this.nodeTextView.Left = this.resizer.Right;

            int itemHeight = this.myTreeView.ItemHeight;
            int visibleNodes = this.myTreeView.VirtualHeight / itemHeight;
            this.vScrollBar1.Maximum = visibleNodes - 1;
            this.vScrollBar1.SmallChange = 1;
            this.vScrollBar1.LargeChange = this.myTreeView.VisibleRows;
            this.vScrollBar1.Minimum = 0;

            this.resizer.Height = this.Height;            
            Invalidate();
            this.nodeTextView.Invalidate();
        }


        public void Cut() {
            this.Commit();
            XmlTreeNode selection = (XmlTreeNode)this.myTreeView.SelectedNode;
            if (selection != null) {
                this.UndoManager.Push(new CutCommand(this, selection));
                if (ClipboardChanged != null) ClipboardChanged(this, new EventArgs());
            }
        }

        public void Copy() {
            this.Commit();
            XmlTreeNode selection = (XmlTreeNode)this.myTreeView.SelectedNode;
            if (selection != null) {
                TreeData.SetData(selection);
                if (ClipboardChanged != null) ClipboardChanged(this, new EventArgs());
            }
        }

        public void Paste(InsertPosition position) {
            this.Commit();
            try {
                this.UndoManager.Push(new PasteCommand(this.model.Document, this, position));
            } 
            catch (Exception ex) {
                MessageBox.Show(ex.Message, "Paste Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public virtual bool CanInsertNode(InsertPosition position, XmlNodeType type) {
            XmlTreeNode n = (XmlTreeNode)this.myTreeView.SelectedNode;
            if (n!= null && n.Node == null) {
                // We are still editing this tree node and haven't created XmlNode
                // for it yet - so bail!
                return false;
            }
            InsertNode inode = new InsertNode(this);
            return inode.Initialize(n, position, type);
        }

        public void InsertNode(InsertPosition position, XmlNodeType type) {
            try {
                XmlTreeNode n = (XmlTreeNode)this.myTreeView.SelectedNode;
                InsertNode inode = new InsertNode(this);
                inode.Initialize(n, position, type);
                this.UndoManager.Push(inode);
                this.nodeTextView.Invalidate();
                this.myTreeView.SelectedNode = inode.NewNode;
                if (inode.RequiresName) {
                    inode.NewNode.BeginEdit();
                } else {
                    this.nodeTextView.BeginEdit(null);
                }
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "Insert Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        public bool Delete() {
            if (this.myTreeView.SelectedNode != null) {
                XmlTreeNode t = (XmlTreeNode)this.myTreeView.SelectedNode;                 
                this.UndoManager.Push(new DeleteNode(this.model.Document, t));
                this.nodeTextView.Invalidate();
                return true;
            }
            return false;
        }

        public bool Insert() {
            // Insert empty node of same type as current node right after current node.
            if (this.myTreeView.SelectedNode != null) {
                XmlTreeNode n = (XmlTreeNode)this.myTreeView.SelectedNode;
                InsertNode(InsertPosition.After, n.Node.NodeType);
                return true;
            }
            return false;
        }

        public bool Duplicate() {
            if (this.myTreeView.SelectedNode != null) {
                XmlTreeNode t = (XmlTreeNode)this.myTreeView.SelectedNode;
                this.UndoManager.Push(new MoveNode(this, t, t, InsertPosition.After, true));
                this.nodeTextView.Invalidate();
                return true;
            }
            return false;

        }

        private void HandleMouseWheel(object sender, MouseEventArgs e) {            
            int y = SystemInformation.MouseWheelScrollLines * (e.Delta/120);
            int v = Math.Max(0, Math.Min(this.vScrollBar1.Value - y, this.vScrollBar1.Maximum + 1 - this.vScrollBar1.LargeChange));
            this.vScrollBar1.Value = v;
            vScrollBar1_Scroll(this, new ScrollEventArgs(ScrollEventType.ThumbTrack, v));
        }
        
        private void vScrollBar1_Scroll(object sender, System.Windows.Forms.ScrollEventArgs e) {
            this.myTreeView.ScrollPosition = new Point(this.myTreeView.ScrollPosition.X, -e.NewValue * this.myTreeView.ItemHeight);
            this.nodeTextView.ScrollPosition = this.myTreeView.ScrollPosition;
            this.nodeTextView.Invalidate();
        }        
        
        protected override bool ProcessDialogKey(Keys keyData) {
            Keys modifiers = (keyData & Keys.Modifiers);
            Keys key = keyData & ~modifiers;
            switch (key) {
                case Keys.Tab:
                    if (modifiers == Keys.Shift) {
                        bool editing = this.nodeTextView.IsEditing;
                        if (this.nodeTextView.Focused || editing) {
                            if (this.nodeTextView.EndEdit(false)) {
                                this.myTreeView.Focus();
                            }
                        } else {
                            if (this.myTreeView.SelectedNode != null) {
                                TreeNode previous = this.myTreeView.SelectedNode.PrevVisibleNode;
                                if (previous != null) this.myTreeView.SelectedNode = previous;
                            }
                            this.nodeTextView.Focus();
                        }
                    } else {
                        bool editing = this.myTreeView.IsEditing;
                        if (this.myTreeView.Focused || editing) {
                            if (this.myTreeView.EndEdit(false)) {
                                this.nodeTextView.Focus();
                                if (editing) {
                                    this.nodeTextView.BeginEdit(null);
                                }
                            }
                        } else {
                            if (this.myTreeView.SelectedNode != null) {
                                TreeNode next = this.myTreeView.SelectedNode.NextVisibleNode;
                                if (next != null) this.myTreeView.SelectedNode = next;
                            }
                            this.myTreeView.Focus();
                        }
                    }
                    return true;
            }
            return false;
        }

        private void myTreeView_KeyDown(object sender, KeyEventArgs e) {
            bool ctrlMods = e.Modifiers == Keys.Control || e.Modifiers == (Keys.Control | Keys.Shift);
            bool nudgeMods = e.Modifiers == (Keys.Control | Keys.Shift);
            XmlTreeNode xn = this.SelectedNode;
            TreeNode n = this.myTreeView.SelectedNode;
            switch (e.KeyCode) {
                case Keys.Escape:
                    this.Commit();
                    if (!e.Handled) {
                        this.myTreeView.SelectedNode = null;
                        if (this.SelectionChanged != null)
                            SelectionChanged(this, new EventArgs());
                    }
                    break;
                case Keys.X:
                    if (ctrlMods) {
                        this.Cut();
                        e.Handled = true;
                    }
                    break;
                case Keys.C:
                    if (ctrlMods) {
                        this.Copy();
                        e.Handled = true;
                    }
                    break;
                case Keys.V:
                    if (ctrlMods) {
                        this.Paste(InsertPosition.Child);
                        e.Handled = true;
                    }
                    break;
                case Keys.Right:
                    if (nudgeMods && CanNudgeNode(xn, NudgeDirection.Right)) {
                        this.NudgeNode(xn, NudgeDirection.Right);
                        e.Handled = true;
                    } else if (n != null && n.Nodes.Count == 0) {
                        this.nodeTextView.Focus();
                        e.Handled = true;
                    }
                    break;
                case Keys.Delete:
                    this.Delete();
                    break;
                case Keys.F2:
                case Keys.Enter:
                    if (n != null && n.IsLabelEditable) {
                        n.BeginEdit();
                        e.Handled = true;
                    }
                    break;
                case Keys.Up:
                    if (nudgeMods && CanNudgeNode(xn, NudgeDirection.Up)) {
                        this.NudgeNode(xn, NudgeDirection.Up);
                        e.Handled = true;
                    }
                    break;
                case Keys.Down:
                    if (nudgeMods && CanNudgeNode(xn, NudgeDirection.Down)) {
                        this.NudgeNode(xn, NudgeDirection.Down);
                        e.Handled = true;
                    }
                    break;
                case Keys.Left:
                    if (nudgeMods && CanNudgeNode(xn, NudgeDirection.Left)) {
                        this.NudgeNode(xn, NudgeDirection.Left);
                        e.Handled = true;
                    }
                    break;
                default:
                    if (!e.Handled ){
                        nodeTextView_KeyDown(sender, e);
                    }
                    break;
            }            
        }

        private void nodeTextView_KeyDown(object sender, KeyEventArgs e) {
            bool ctrlMods = e.Modifiers == Keys.Control || e.Modifiers == (Keys.Control | Keys.Shift);
            Keys key = (e.KeyData & ~Keys.Modifiers);
            switch (key) {
                case Keys.Left:
                    if (nodeTextView.Focused) {
                        this.myTreeView.Focus();
                        e.Handled = true;
                    }
                    break;
                case Keys.Delete:
                    this.Delete();
                    e.Handled = true;
                    return;
                case Keys.X:
                    if (ctrlMods) {
                        this.Cut();
                        e.Handled = true;
                    }
                    break;
                case Keys.C:
                    if (ctrlMods) {
                        this.Copy();
                        e.Handled = true;
                    }
                    break;
                case Keys.V:
                    if (ctrlMods) {
                        this.Paste(InsertPosition.Child);
                        e.Handled = true;
                    }
                    break;
                default:
                    if (!e.Handled) {
                        this.myTreeView.HandleKeyDown(e);
                    }
                    break;
            }
            if (!e.Handled) {
                base.OnKeyDown(e);
            }
        }

        private void settings_Changed(object sender, string name) {
            // change the node colors.
            System.Collections.Hashtable colors = (System.Collections.Hashtable)this.settings["Colors"];
            Color backColor = (Color)colors["Background"];
            this.BackColor = backColor;
            this.myTreeView.BackColor = backColor;
            this.nodeTextView.BackColor = backColor;
            this.myTreeView.BeginUpdate();
            InvalidateNodes(this.myTreeView.Nodes); // force nodes to pick up new colors.
            this.myTreeView.EndUpdate();
        }

        void InvalidateNodes(TreeNodeCollection nodes) {
            if (nodes == null) return;
            foreach (XmlTreeNode xn in nodes) {
                if (xn.IsVisible) {
                    xn.Invalidate();
                    if (xn.IsExpanded) {
                        InvalidateNodes(xn.Nodes);
                    }
                }
            }
        }

        enum DragKeyState {
            LeftButton = 1,
            RightButton = 2,
            Shift = 4,
            Control = 8,
            MiddleButton = 16,
            Alt = 32
        }

        static TreeData CheckDragEvent(DragEventArgs e) {
            TreeData data = null;
            string name = DataFormats.GetFormat(typeof(TreeData).FullName).Name;
            try {
                if (e.Data.GetDataPresent(name, false)) {
                    data = (TreeData)e.Data.GetData(name);
                }
            } 
            catch (Exception ex) {
                Trace.WriteLine("Exception:" + ex.ToString());
            }
            if (data == null && e.Data.GetDataPresent(DataFormats.Text)) {
                string xml = (string)e.Data.GetData(DataFormats.Text);
                data = new TreeData(xml);
            }
            if (data != null) {
                DragKeyState ks = (DragKeyState)e.KeyState;
                // Copy when the control key is down.
                e.Effect = ((ks & DragKeyState.Control) != DragKeyState.Control) ?
                    DragDropEffects.Move : DragDropEffects.Copy;
            } 
            else {
                e.Effect = DragDropEffects.None;
            }
            return data;
        }

        private void treeViewFeedback_DragDrop(object sender, DragEventArgs e) {
            TreeData data = CheckDragEvent(e);
            FinishDragDrop(data, e.Effect);
        }

        private void treeViewFeedback_DragEnter(object sender, DragEventArgs e) {
            TreeData data = CheckDragEvent(e);
            if (data != null && this.feedback == null) {
                this.feedback = new XmlTreeViewDropFeedback();
                if (this.dragged == null) {
                    // dragging from another app, so we have to import the node at this point.
                    XmlTreeNode target = (XmlTreeNode)this.myTreeView.FindNodeAt(e.X, e.Y);
                    this.dragged = data.GetTreeNode(this.Model.Document, target, this);
                }
                this.feedback.Item = this.dragged;
                this.feedback.TreeView = this.myTreeView;
            }            
            if (this.feedback != null) {
                this.feedback.Position = new Point(e.X, e.Y);
            }
        }

        private void treeViewFeedback_DragLeave(object sender, EventArgs e) {
            RemoveFeedback();
        }

        private void treeViewFeedback_DragOver(object sender, DragEventArgs e) {
            // find the node under the X,Y position and draw feedback as to where the new node will
            // be dropped. 
            if (this.feedback != null) {
                CheckDragEvent(e);                
                this.feedback.Position = new Point(e.X, e.Y);
            }
        }

        private void myTreeView_GiveFeedback(object sender, GiveFeedbackEventArgs e) {
            e.UseDefaultCursors = true;
        }

        private void myTreeView_ItemDrag(object sender, ItemDragEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                this.dragged = (XmlTreeNode)e.Item;
                this.myTreeView.SelectedNode = this.dragged;
                TreeData data = new TreeData(this.dragged);

                DragDropEffects effect = this.DoDragDrop(data, DragDropEffects.All);
                if (this.dragged != null && effect != DragDropEffects.None) {
                    FinishDragDrop(data, effect);
                }
                RemoveFeedback();

            }
        }
        void RemoveFeedback() {
            if (this.feedback != null) {
                this.feedback.Finish(this.dragged != null);
                this.feedback.Dispose();
                this.feedback = null;
            }
        }

        protected void FinishDragDrop(TreeData data, DragDropEffects effect) {
            if (data != null && effect != DragDropEffects.None && this.dragged != null) {
                bool copy = (effect == DragDropEffects.Copy);
                if (this.feedback != null) {
                    // Then we are also the drop site
                    MoveNode cmd = null;
                    if (this.feedback.Before != null) {
                        cmd = MoveNode(this.dragged, (XmlTreeNode)this.feedback.Before, InsertPosition.Before, copy);
                    } 
                    else if (this.feedback.After != null) {
                        cmd = MoveNode(this.dragged, (XmlTreeNode)this.feedback.After, InsertPosition.After, copy);
                    }
                    // Now we can expand it because it is now in the tree
                    if (cmd != null && cmd.Source.Nodes.Count > 1) {
                        cmd.Source.Expand();
                    }
                } 
                else if (!copy) {
                    // Then this was a move to another process, so now we have to remove it
                    // from this process.
                    Debug.Assert(this.myTreeView.SelectedNode == this.dragged);
                    this.Delete();
                }
            }
            this.dragged = null;
            RemoveFeedback();
        }

        private MoveNode MoveNode(XmlTreeNode source, XmlTreeNode dest, InsertPosition where, bool copy) {
            try {
                MoveNode cmd = new MoveNode(this, source, dest, where, copy);
                this.UndoManager.Push(cmd);
                return cmd;
            } 
            catch (Exception ex) {
                MessageBox.Show(ex.Message, "Move Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return null;
        }

        public bool CanNudgeNode(XmlTreeNode node, NudgeDirection dir) {
            if (node == null) return false;
            NudgeNode n = new NudgeNode(this, node, dir);
            return n.IsEnabled;
        }

        public void NudgeNode(XmlTreeNode node, NudgeDirection dir) {
            try {
                NudgeNode cmd = new NudgeNode(this, node, dir);
                this.UndoManager.Push(cmd);
            } 
            catch (Exception ex) {
                MessageBox.Show(ex.Message, "Nudge Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        bool reentrantLock = false;
        public void RecalculateNamespaces(XmlNode node) {
            if (node is XmlText) node = node.ParentNode;
            if (node.NodeType != XmlNodeType.Element && node.NodeType != XmlNodeType.Attribute) {
                return;
            }
            if (reentrantLock) return;
            Command exec = this.UndoManager.Executing;
            if (exec != null && exec.Name == "RecalculateNamespaces")
                return; // don't "redo" this during Commnad.Redo()! 

            // Do not re-enter this when we are processing the recalcNamespaces compound command!
            reentrantLock = true;
            try {
                // This xmlns attribute has changed, so we need to recalculate the NamespaceURI
                // property on the scoped element and it's children so that validation works
                // as expected.  
                XmlElement scope;
                if (node is XmlAttribute) {
                    scope = ((XmlAttribute)node).OwnerElement;
                } else {
                    scope = (XmlElement)node;
                }
                if (scope == null) return;
                XmlTreeNode tnode = FindNode(scope);
                if (tnode == null) return;
                if (tnode.Node == null) return;

                XmlNamespaceManager nsmgr = XmlHelpers.GetNamespaceScope(scope);
                CompoundCommand cmd = new CompoundCommand("RecalculateNamespaces");
                tnode.RecalculateNamespaces(nsmgr, cmd);
                if (cmd.Count > 0) {
                    this.UndoManager.Merge(cmd);
                }
            } finally {
                reentrantLock = false;
            }
        }

        //=======================================================================================
        // DO NOT EDIT BELOW THIS LINE
        //=======================================================================================

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing) {
            if (disposing) {
                if (components != null) {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(XmlTreeView));
            this.myTreeView = new TreeView();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.nodeTextView = new XmlNotepad.NodeTextView();
            this.resizer = new XmlNotepad.PaneResizer();
            this.vScrollBar1 = new System.Windows.Forms.VScrollBar();
            this.SuspendLayout();
            // 
            // myTreeView
            // 
            this.myTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)));
            this.myTreeView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.myTreeView.ImageList = this.imageList1;
            this.myTreeView.LabelEdit = true;
            this.myTreeView.Location = new System.Drawing.Point(0, 0);
            this.myTreeView.Name = "TreeView";
            //this.myTreeView.Scrollable = false;
            this.myTreeView.Size = new System.Drawing.Size(216, 224);
            this.myTreeView.TabIndex = 1;
            // 
            // imageList1
            // 
            this.imageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth24Bit;
            this.imageList1.ImageSize = new System.Drawing.Size(16, 16);
            object imgStream = resources.GetObject("imageList1.ImageStream");
            this.imageList1.ImageStream = (System.Windows.Forms.ImageListStreamer)imgStream;
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // nodeTextView
            // 
            this.nodeTextView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.nodeTextView.BackColor = System.Drawing.Color.White;
            this.nodeTextView.Location = new System.Drawing.Point(301, 0);
            this.nodeTextView.Name = "nodeTextView";
            this.nodeTextView.SelectedNode = null;
            this.nodeTextView.Size = new System.Drawing.Size(179, 224);
            this.nodeTextView.TabIndex = 4;
            // 
            // resizer
            // 
            this.resizer.Border3DStyle = System.Windows.Forms.Border3DStyle.Raised;
            this.resizer.Location = new System.Drawing.Point(296, 0);
            this.resizer.Name = "resizer";
            this.resizer.Pane1 = this.myTreeView;
            this.resizer.Pane2 = this.nodeTextView;
            this.resizer.PaneWidth = 5;
            this.resizer.Size = new System.Drawing.Size(5, 408);
            this.resizer.TabIndex = 3;
            this.resizer.Vertical = true;
            // 
            // vScrollBar1
            // 
            this.vScrollBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.vScrollBar1.Location = new System.Drawing.Point(480, 0);
            this.vScrollBar1.Name = "VScrollBar";
            this.vScrollBar1.Size = new System.Drawing.Size(17, 224);
            this.vScrollBar1.TabIndex = 0;
            this.vScrollBar1.Scroll += new System.Windows.Forms.ScrollEventHandler(this.vScrollBar1_Scroll);
            // 
            // XmlTreeView
            // 
            this.BackColor = System.Drawing.SystemColors.Window;
            this.Controls.Add(this.vScrollBar1);
            this.Controls.Add(this.resizer);
            this.Controls.Add(this.nodeTextView);
            this.Controls.Add(this.myTreeView);
            this.Size = new System.Drawing.Size(496, 224);
            this.ResumeLayout(false);

        }
        #endregion
    }

    public enum NodeImage {
        None,
        Element,
        Attribute,
        Leaf,
        Text,
        Comment,
        PI,
        OpenElement,
        CData,
    }

    public class XmlTreeNode : TreeNode {
        Settings settings;
        NodeImage img;
        Color foreColor;
        internal List<XmlTreeNode> children;
        XmlTreeView view;
        XmlNode node;
        XmlSchemaType type;
        XmlNodeType nodeType;
        string editLabel;

        public XmlTreeNode(XmlTreeView view) {
            this.view = view;
        }

        public XmlTreeNode(XmlTreeView view, XmlNode node) {
            this.view = view;
            this.node = node;
            Init();
        }

        public XmlTreeNode(XmlTreeView view, XmlTreeNode parent, XmlNode node)
            : base(parent) {
            this.view = view;
            this.node = node;
            Init();
        }

        [Browsable(false)]
        public XmlNodeType NodeType {
            get { return (this.node != null) ? this.node.NodeType : this.nodeType; }
            set { this.nodeType = value; }
        }

        [Browsable(false)]
        public XmlTreeView XmlTreeView {
            get { return this.view; }
            set {
                this.view = value;
                this.TreeView = value == null ? null : value.TreeView;
                PropagateView(value, children);
                Init();
            }
        }

        public override void Remove() {
            base.Remove();
            XmlTreeNode xp = this.Parent as XmlTreeNode;
            if (xp != null) {
                xp.OnChildRemoved();
            }
        }

        void OnChildRemoved() {
            if (this.img == NodeImage.Element && this.Nodes.Count == 0) {
                MakeLeaf();
            }
        }

        void MakeLeaf() {
            this.Collapse();
            this.img = NodeImage.Leaf;
            this.Invalidate();
        }

        [Browsable(false)]
        public XmlSchemaType SchemaType {
            get { return this.type; }
            set { this.type = value; }
        }

        void PropagateView(XmlTreeView view, List<XmlTreeNode> children) {
            if (children != null) {
                foreach (XmlTreeNode child in children) {
                    child.XmlTreeView = view;
                    PropagateView(view, child.children);
                }
            }
        }

        void Init() {
            if (this.view != null) {
                this.settings = view.Settings;
                this.img = CalculateNodeImage(this.Node);
                this.foreColor = this.GetForeColor(this.img);
            }
        }


        public override void Invalidate() {
            base.Invalidate();
            Init();
        }

        public Settings Settings { get { return this.settings; } }

        public XmlNode Node {
            get { return this.node; }
            set {
                int count = this.Nodes.Count;
                this.node = value;
                Init();
                if (this.Nodes.Count != count) {
                    this.Invalidate();
                } else if (this.TreeView != null) {
                    this.TreeView.InvalidateLayout(); // LabelBounds needs recalculating.                
                    this.TreeView.InvalidateNode(this);
                }
            }
        }
        public override string Label {
            get {
                return this.Node == null ? editLabel : this.Node.Name;
            }
            set {
                editLabel = value;
                this.Invalidate();
            }
        }
        public override bool IsLabelEditable {
            get {
                return (this.node == null || this.node is XmlProcessingInstruction ||
                    ((this.node is XmlAttribute || this.node is XmlElement)));
            }
        }
        public override string Text {
            get {
                XmlNode n = this.Node;
                string text = null;
                if (n is XmlElement) {
                    NodeImage i = this.NodeImage;
                    if (NodeImage.Element != i && NodeImage.OpenElement != i) {
                        text = n.InnerText;
                    }
                } else if (n is XmlProcessingInstruction) {
                    text = ((XmlProcessingInstruction)n).Data;
                } else if (n != null) {
                    text = n.Value;
                }
                return text;
            }
            set {
                EditNodeValue ev = new EditNodeValue(this.XmlTreeView, this, value);
                UndoManager undo = this.XmlTreeView.UndoManager;
                undo.Push(ev);
            }
        }

        public override Color ForeColor {
            get {
                return this.foreColor;
            }
        }

        public override TreeNodeCollection Nodes {
            get {
                return new XmlTreeNodeCollection(this);
            }
        }

        public NodeImage NodeImage {
            get {
                if (this.IsExpanded) {
                    return NodeImage.OpenElement;
                }
                return this.img;
            }
        }

        public override int ImageIndex {
            get {
                return (int)this.NodeImage - 1;
            }
        }

        public Color GetForeColor(NodeImage img) {
            System.Collections.Hashtable colors = (System.Collections.Hashtable)this.settings["Colors"];
            switch (img) {
                case NodeImage.Element:
                case NodeImage.OpenElement:
                case NodeImage.Leaf:
                    return (Color)colors["Element"];
                case NodeImage.Attribute:
                    return (Color)colors["Attribute"];
                case NodeImage.PI:
                    return (Color)colors["PI"];
                case NodeImage.CData:
                    return (Color)colors["CDATA"];
                case NodeImage.Comment:
                    return (Color)colors["Comment"];
                default:
                    return (Color)colors["Text"];
            }
        }

        NodeImage CalculateNodeImage(XmlNode n) {
            XmlNodeType nt = (n == null) ? this.nodeType : n.NodeType;
            switch (nt) {
                case XmlNodeType.Attribute:
                    return NodeImage.Attribute;
                case XmlNodeType.Comment:
                    return NodeImage.Comment;
                case XmlNodeType.ProcessingInstruction:
                case XmlNodeType.XmlDeclaration:
                    return NodeImage.PI;
                case XmlNodeType.Text:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    return NodeImage.Text;
                case XmlNodeType.CDATA:
                    return NodeImage.CData;
                case XmlNodeType.Element:
                    XmlElement e = (XmlElement)n;
                    if (e != null && IsContainer(e)) {
                        return NodeImage.Element;
                    }
                    return NodeImage.Leaf;
                default:
                    return NodeImage.PI;
            }
        }

        static bool IsContainer(XmlElement e) {
            if (e.HasChildNodes) {
                int count = 0;
                foreach (XmlNode c in e.ChildNodes) {
                    if (c is XmlComment || c is XmlProcessingInstruction || c is XmlElement)
                        return true;
                    if (c is XmlText || c is XmlCDataSection) {
                        count++;
                        if (count > 1) return true;
                    }
                }
            }
            return HasSpecifiedAttributes(e);
        }

        static bool HasSpecifiedAttributes(XmlElement e) {
            if (e.HasAttributes) {
                foreach (XmlAttribute a in e.Attributes) {
                    if (a.Specified) return true;
                }
            }
            return false;
        }

        public void RecalculateNamespaces(XmlNamespaceManager nsmgr, CompoundCommand cmd) {
            Debug.Assert(this.NodeType == XmlNodeType.Element || this.NodeType == XmlNodeType.Attribute);
            XmlNode e = this.Node;
            if (e == null) return; // user is still editing this tree node!
            bool hasXmlNs = false;
            if (e.Attributes != null) {
                XmlAttributeCollection acol = e.Attributes;
                for (int i = 0, n = acol.Count; i < n; i++) {
                    XmlAttribute a = acol[i];
                    string value = a.Value;
                    if (a.NamespaceURI == XmlHelpers.XmlnsUri) {
                        if (!hasXmlNs) {
                            nsmgr.PushScope();
                            hasXmlNs = true;
                        }
                        XmlNameTable nt = nsmgr.NameTable;
                        string prefix = nt.Add(a.LocalName);
                        if (prefix == "xmlns") prefix = "";
                        if (!nsmgr.HasNamespace(prefix)) {
                            try {
                                nsmgr.AddNamespace(prefix, nt.Add(value));
                            } catch (Exception ex) {
                                // illegal namespace declaration, perhaps user has not finished editing it yet.
                                Trace.WriteLine(ex.Message);
                            }
                        }
                    }
                }
            }

            XmlName name = XmlHelpers.ParseName(nsmgr, e.Name, this.NodeType);
            if (name.NamespaceUri != e.NamespaceURI &&
                (string.IsNullOrEmpty(name.Prefix) || !string.IsNullOrEmpty(name.NamespaceUri))) {
                // Node has bound to a different namespace!
                // Note that XmlNode doesn't let you change the NamespaceURI property
                // so we have to recreate the XmlNode objects, and so we have to create
                // a command for this since it is editing the tree!
                EditNodeName rename = new EditNodeName(this, name, false);
                cmd.Add(rename);
            }

            foreach (XmlTreeNode child in this.Nodes) {
                switch (child.NodeType) {
                    case XmlNodeType.Attribute:
                    case XmlNodeType.Element:
                        child.RecalculateNamespaces(nsmgr, cmd);
                        break;
                    default:
                        // no change required on text nodes.
                        break;
                }
            }

            if (hasXmlNs) {
                nsmgr.PopScope();
            }
        }

        public override bool CanExpandAll {
            get {
                return (this.NodeImage != NodeImage.Leaf); // don't expand the leaves
            }
        }
    }

    public class XmlTreeNodeCollection : TreeNodeCollection, IEnumerable<XmlTreeNode> {
        XmlTreeNode parent;
        XmlNode node;
        XmlTreeView treeView;
        List<XmlTreeNode> children;

        public XmlTreeNodeCollection(XmlTreeView treeView, XmlNode parent) {
            this.node = parent;
            this.treeView = treeView;
        }

        public XmlTreeNodeCollection(XmlTreeNode parent) {
            this.treeView = parent.XmlTreeView;
            this.parent = parent;
            if (parent != null) this.children = parent.children;
            this.node = parent.Node;
        }
        
        IEnumerator<XmlTreeNode> IEnumerable<XmlTreeNode>.GetEnumerator() {
            Populate();
            if (this.children != null)
                return this.children.GetEnumerator();
            return null;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            IEnumerable<XmlTreeNode> ie = (IEnumerable<XmlTreeNode>)this;
            return ie.GetEnumerator();
        }

        public override IEnumerator<TreeNode> GetEnumerator() {
            System.Collections.IEnumerable e = (System.Collections.IEnumerable)this;
            foreach (XmlTreeNode xn in e) {
                yield return xn;
            }           
        }

        public override int Count {
            get {
                Populate();
                return this.children == null ? 0 : this.children.Count;
            }
        }

        public override int GetIndex(TreeNode node) {
            Populate();
            XmlTreeNode xn = ((XmlTreeNode)node);
            return this.children.IndexOf(xn);
        }

        public override void Add(TreeNode node) {
            node.Parent = this.parent;
            XmlTreeNode xn = ((XmlTreeNode)node);
            xn.XmlTreeView = this.treeView;
            Populate();
            this.children.Add(xn);
        }

        public override void Insert(int i, TreeNode node) {
            node.Parent = this.parent;
            XmlTreeNode xn = ((XmlTreeNode)node);
            xn.XmlTreeView = this.treeView;
            Populate();
            if (i > this.children.Count) i = this.children.Count;
            this.children.Insert(i, xn);
        }

        public override void Remove(TreeNode child) {
            if (this.children != null) {
                XmlTreeNode xn = ((XmlTreeNode)child);            
                if (this.children.Contains(xn)) {
                    this.children.Remove(xn);
                    return;
                }
            }
            throw new InvalidOperationException("Child is not a child");
        }

        public override TreeNode this[int i] {
            get {
                Populate();
                return this.children[i] as TreeNode;
            }
        }

        void Populate() {
            if (this.children == null && node != null) {
                List<XmlTreeNode> children = new List<XmlTreeNode>();
                if (!(node is XmlAttribute)) {
                    if (node.Attributes != null) {
                        foreach (XmlAttribute a in node.Attributes) {
                            if (a.Specified) {
                                XmlTreeNode c = treeView.CreateTreeNode(parent, a);
                                c.XmlTreeView = this.treeView;
                                children.Add(c);
                            }
                        }
                    }
                    if (node.HasChildNodes) {
                        foreach (XmlNode n in node.ChildNodes) {
                            XmlTreeNode c = treeView.CreateTreeNode(parent, n);
                            c.XmlTreeView = this.treeView;
                            children.Add(c);
                        }
                    }
                }
                this.children = children;
                if (parent != null) parent.children = children;
            }
        }


    }

    public sealed class StringHelper {
        StringHelper() { }

        public static bool IsNullOrEmpty(string s) {
            return (s == null || s.Length == 0);
        }
    }

    public class NodeChangeEventArgs : EventArgs {
        XmlTreeNode node;

        public XmlTreeNode Node {
            get { return node; }
            set { node = value; }
        }

        public NodeChangeEventArgs(XmlTreeNode node) {
            this.node = node;
        }
    }

    public class XmlTreeViewDropFeedback : TreeViewDropFeedback {
        int autoScrollCount;

        public override Point Position {
            get { return base.Position; }
            set {
                base.Position = value;
                CheckAutoScroll(value);
            }
        }

        void CheckAutoScroll(Point pt) {
            TreeView tv = this.TreeView;
            Point local = tv.PointToClient(pt);
            Point pos = tv.ApplyScrollOffset(local);
            XmlTreeView parent = (XmlTreeView)tv.Parent;
            int height = tv.ItemHeight;
            int halfheight = height / 2;
            TreeNode node = null;
            bool nearEnd = false;
            if (local.Y - height < tv.Top) {
                node = tv.FindNodeAt(pos.X, pos.Y - height);
                nearEnd = (local.Y - halfheight < tv.Top);
            } else if (local.Y + height > tv.Bottom) {
                node = tv.FindNodeAt(pos.X, pos.Y + height);
                nearEnd = (local.Y + halfheight > tv.Bottom);
            }
            if (node != null) {
                if (nearEnd || autoScrollCount > 1) {
                    parent.ScrollIntoView(node);
                    autoScrollCount = 0;
                } else {
                    autoScrollCount++;
                }
                ResetToggleCount();
            }
        }

    }

}