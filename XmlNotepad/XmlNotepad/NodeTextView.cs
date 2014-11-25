using System;
using System.Collections;
using System.Xml;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.ComponentModel;
using System.Text;

namespace XmlNotepad {
    //========================================================================================
    /// <summary>
    /// Displays the text of the attributes, comments, text, cdata and leaf element nodes and 
    /// provides type-to-find and editing of those values.
    /// </summary>
    public class NodeTextView : UserControl {

        private TreeNode selectedNode;
        Settings settings;
        private TypeToFindHandler ttf;
        Point scrollPosition;
        private TextEditorOverlay editor;
        private TreeNodeCollection nodes;
        public event EventHandler<TreeViewEventArgs> AfterSelect;

        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        public NodeTextView() {
            this.SetStyle(ControlStyles.ResizeRedraw,true);
            this.SetStyle(ControlStyles.UserPaint,true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint,true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.Selectable,true);
          
            InitializeComponent();

            this.SuspendLayout();
            this.editor = new TextEditorOverlay(this);
            this.editor.MultiLine = true;
            this.editor.CommitEdit += new EventHandler<TextEditorEventArgs>(OnCommitEdit);
            this.editor.LayoutEditor += new EventHandler<TextEditorLayoutEventArgs>(OnLayoutEditor);

            this.ttf = new TypeToFindHandler(this, 2000);
            this.ttf.FindString += new TypeToFindEventHandler(FindString);
            this.ResumeLayout();
            this.AccessibleRole=System.Windows.Forms.AccessibleRole.List;
        }

        #region Component Designer generated code

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            if (this.ttf != null) {
                this.ttf.Dispose();
                this.ttf = null;
            }
            base.Dispose(disposing);
        }

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            this.SuspendLayout();

            this.AccessibleRole = AccessibleRole.List;
            this.Name = this.AccessibleName = "NodeTextView";

            this.ResumeLayout(false);
        }

        #endregion

        public void Close() {
            this.editor.Dispose();
        }

        // The nodes to display.
        public TreeNodeCollection Nodes {
            get { return this.nodes; }
            set {
                this.selectedNode = null;
                this.nodes = value;
                PerformLayout();
            }
        }

        [System.ComponentModel.Browsable(false)]
        public UndoManager UndoManager {
            get { return (UndoManager)this.Site.GetService(typeof(UndoManager)); }
        }

        [System.ComponentModel.Browsable(false)]
        public IIntellisenseProvider IntellisenseProvider {
            get { return (IIntellisenseProvider)this.Site.GetService(typeof(IIntellisenseProvider)); }
        }

        public void SetSite(ISite site) {
            // Overriding the Site property directly breaks the WinForms designer.
            this.Site = site;
            this.settings = (Settings)site.GetService(typeof(Settings));
            if (this.settings != null) {
                this.settings.Changed += new SettingsEventHandler(settings_Changed);
            }
            settings_Changed(this, "");
            this.editor.Site = site;
        }

        public Point ScrollPosition {
            get { return this.scrollPosition; }
            set { this.scrollPosition = value; }
        }

        public Point ApplyScrollOffset(int x, int y) {
            return new Point(x - this.scrollPosition.X, y - this.scrollPosition.Y);
        }

        public Point ApplyScrollOffset(Point pt) {
            return new Point(pt.X - this.scrollPosition.X, pt.Y - this.scrollPosition.Y);
        }

        private void settings_Changed(object sender, string name) {
            // change the colors.
            Invalidate();
        }

        protected override void OnGotFocus(EventArgs e) {
            if (this.selectedNode != null){
                Invalidate(this.selectedNode);
            }
            base.OnGotFocus (e);
        }

        protected override void OnLostFocus(EventArgs e) {
            if (this.selectedNode != null){
                Invalidate(this.selectedNode);
            }
            base.OnLostFocus (e);
        }

        public void Reset(){
            this.editor.EndEdit(false);
            this.selectedNode = null;
            Invalidate();
        }

        public TreeNode SelectedNode {
            get { return this.selectedNode; }
            set {
                if (this.selectedNode != value) {
                    this.editor.EndEdit(false);
                    Invalidate();
                    this.selectedNode = value;
                    if (AfterSelect != null) {
                        AfterSelect(this, new TreeViewEventArgs(value, TreeViewAction.None));
                    }
                }
            }
        }

        static bool IsTextEditable(TreeNode node) {
            NodeImage img = (NodeImage)(node.ImageIndex+1);
            return !(img == NodeImage.Element || img == NodeImage.OpenElement);

        }

        protected override void OnMove(EventArgs e) {
            this.editor.EndEdit(false);
            base.OnMove (e);
        }
        
        protected override void OnLayout(LayoutEventArgs levent) {
            base.OnLayout (levent);
            if (this.editor.IsEditing) {
                this.editor.PerformLayout();
            }
        }

        void OnLayoutEditor(object sender, TextEditorLayoutEventArgs args) {
            Rectangle r = this.GetTextBounds(this.selectedNode);
            r.Offset(this.scrollPosition);
            args.PreferredBounds = r;
            args.MaxBounds = r;
        }

        public bool BeginEdit(string value) {
            if (this.selectedNode != null) {
                if (this.selectedNode.Label == null) {
                    MessageBox.Show(this, 
                        "You cannot edit the value of a node until you provide a name", 
                        "Node has no name",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                } 
                else if (IsTextEditable(this.selectedNode)) {
                    // see if control has possible values that cannot be known in xsd
                    IIntellisenseProvider provider = this.IntellisenseProvider;
                    string text = value != null ? value : GetNodeText(this.selectedNode);
                    if (provider != null) {
                        provider.SetContextNode(this.selectedNode);
                        if (!provider.IsValueEditable) {
                            return false;
                        }                        
                    }
                    this.editor.BeginEdit(text, provider, EditMode.Value, this.selectedNode.ForeColor);
                    return true;
                }
            }
            return false;
        }

        public bool IsEditing {
            get { return this.editor.IsEditing; }
        }

        public bool EndEdit(bool cancel) {
            return this.editor.EndEdit(cancel);
        }           

        void OnCommitEdit(object sender, TextEditorEventArgs args) {
            if (!args.Cancelled) {
                SetNodeText(this.selectedNode, args.Text);
            }
        }
       
        protected override bool IsInputKey(Keys keyData) {
            Keys key = (keyData & ~Keys.Modifiers);
            switch (key){
                case Keys.F2:
                case Keys.Enter:
                case Keys.Home:
                case Keys.End:
                case Keys.Up:
                case Keys.PageDown:
                case Keys.PageUp:
                case Keys.Right:
                case Keys.Left:
                    return true;
            }
            return base.IsInputKey (keyData);
        }
        
        protected override void OnKeyDown(KeyEventArgs e) {
            bool isLetterOrDigit = ((e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z) ||
                       (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9)) &&
                       (e.Modifiers == Keys.Shift || e.Modifiers == 0);

            switch (e.KeyCode) {
                case Keys.F2:
                case Keys.Enter:
                    BeginEdit(null);
                    e.Handled = true;
                    break;
                case Keys.Left:
                    // do not give to tree view, this will do a shift-tab instead.
                    break;
                default:
                    if (isLetterOrDigit && !e.Handled && this.ContainsFocus) {
                        if (ttf.Started) {
                            e.Handled = true; // let ttf handle it!
                        } else {
                            char ch = Convert.ToChar(e.KeyValue);
                            if (!e.Shift) ch = Char.ToLower(ch);
                            if (this.BeginEdit(ch.ToString())) {
                                this.editor.SelectEnd();
                                e.Handled = true;
                            }
                        }
                    }
                    break;
            }
            base.OnKeyDown (e);
        }


        protected override void OnPaint(PaintEventArgs e) {
            Rectangle clip = e.ClipRectangle;
            Graphics g = e.Graphics;
            Matrix m = g.Transform;
            m.Translate(this.scrollPosition.X, this.scrollPosition.Y);
            g.Transform = m;

            base.OnPaint(e);
            if (this.nodes != null){
                clip = new Rectangle(this.ApplyScrollOffset(clip.X, clip.Y), new Size(clip.Width, clip.Height));
                PaintNodes(this.nodes, g, ref clip);
            }
        }

        void PaintNodes(TreeNodeCollection nodes, Graphics g, ref Rectangle clip) {
            if (nodes == null) return;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            foreach (TreeNode n in nodes) {
                Rectangle r = GetTextBounds(n);
                if (r.Top > clip.Bottom) return;
                if (r.IntersectsWith(clip)){
                    DrawItem(r, n, g);
                }
                if (n.IsExpanded){
                    PaintNodes(n.Nodes, g, ref clip);
                }
            }
        }

        static string GetNodeText(TreeNode n) {
            string text = n.Text;
            return NormalizeNewLines(text);
        }

        static string NormalizeNewLines(string text){
            if (text == null) return null;
            StringBuilder sb = new StringBuilder();
            for (int i = 0, n = text.Length; i<n; i++){
                char ch = text[i];
                if (ch == '\r'){
                    if (i+1<n && text[i+1] == '\n')
                        i++;
                    sb.Append("\r\n");
                } else if (ch == '\n'){
                    sb.Append("\r\n");
                } else {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }

        static void SetNodeText(TreeNode n, string value) {
            n.Text = value;
        }

        public void Invalidate(TreeNode n) {
            if (n != null) {
                Rectangle r = this.GetTextBounds(n);
                r.Offset(this.scrollPosition);
                Invalidate(r);
            }
        }

        private void DrawItem(Rectangle bounds, TreeNode tn, Graphics g) {

            //g.SmoothingMode = SmoothingMode.AntiAlias;
            Color c = tn.ForeColor;

            Brush myBrush = null;            
            bool focusSelected = false;
            if (this.Focused && tn == this.SelectedNode){
                focusSelected= true;
                g.FillRectangle(SystemBrushes.Highlight, bounds);
                myBrush = Utilities.HighlightTextBrush(c);
            } else {
                myBrush = new SolidBrush(c);
            }
                   
            string value = GetNodeText(tn);
            if (value == null && !focusSelected) {
                g.FillRectangle(Brushes.LightGray, bounds);
            }

            if (value != null && value.Length > 0) {

                Font font = this.Font;
                Rectangle inset = new Rectangle(bounds.Left+3,bounds.Top, bounds.Width-3, bounds.Height);
                //inset.Inflate(-3, -2);

                char ellipsis = Convert.ToChar(0x2026);

                value = value.Trim();
            
                int i = value.IndexOfAny(new char[] { '\r', '\n'});
                if (i>0){
                    value = value.Substring(0, i) + ellipsis;
                }

                // Figure out how much of the text we can display                
                int width = inset.Width;;
                //int height = inset.Height;
                string s = value;
                if (width <0) return;
                int length = value.Length;
                int j = value.Length;
                SizeF size = g.MeasureString(s, font, width + 1000, StringFormat.GenericTypographic);
                int dy = (font.Height - (int)Math.Ceiling(size.Height))/2;
                if (dy<0) dy = 0;
                char[] ws = new char[]{' ','\t'};
                if ((int)size.Width > width && j > 1){ // line wrap?
                    int start = 0;
                    int w = 0;
                    int k = value.IndexOfAny(ws);
                    while (k>0){
                        s = value.Substring(0, k) + ellipsis;
                        size = g.MeasureString(s, font, width + 1000, StringFormat.GenericTypographic);
                        if ((int)size.Width < width && k < length){
                            start = k;
                            w = (int)size.Width;
                            while (start<length && (value[start] == ' ' || value[start] == '\t')){
                                start++;
                            }
                            k = value.IndexOfAny(ws, start);
                        } else {
                            break;
                        }
                    }
                    j = start;
                    if (w < width / 2){ 
                        // if we have a really long word (e.g. binhex) then just take characters
                        // up to the end of the line.                        
                        while ((int)w < width && j < length){
                            j++;
                            s = value.Substring(0, j) + ellipsis;
                            size = g.MeasureString(s, font, width + 1000, StringFormat.GenericTypographic);
                            w = (int)size.Width;
                        }                        
                    }
                    if (j <= 0) {
                        s = "";
                    } else if (j < length){
                        s = value.Substring(0, j-1) + ellipsis;
                    }
                }             
                // Draw the current item text based on the current Font and the custom brush settings.
                g.DrawString(s, font, myBrush, inset.Left, dy + inset.Top, StringFormat.GenericTypographic);
            }        

            // If the ListBox has focus, draw a focus rectangle around the selected item.
            if (tn == this.SelectedNode){
                g.SmoothingMode = SmoothingMode.Default;
                Pen p = new Pen(Color.Black, 1);
                p.DashStyle = DashStyle.Dot;
                p.Alignment = PenAlignment.Inset;
                p.LineJoin = LineJoin.Round;
                bounds.Width--;
                bounds.Height--;
                g.DrawRectangle(p, bounds);
                p.Dispose();
            }        

            myBrush.Dispose();

        }


        protected override void OnMouseDown(MouseEventArgs e) {
            this.editor.EndEdit(false);
            if (this.nodes != null) {
                Point p = this.ApplyScrollOffset(e.X, e.Y);
                TreeNode tn = this.FindNodeAt(this.nodes, p.X, p.Y);
                if (tn != null) {
                    if (this.SelectedNode == tn && this.Focused) {
                        if (e.Button == MouseButtons.Left) {
                            this.BeginEdit(null);
                        }
                        return;
                    } else {
                        this.SelectedNode = tn;
                    }
                }
            }
            this.Focus();        
        }

        public TreeNode FindNodeAt(TreeNodeCollection nodes, int x, int y) {
            if (nodes == null) return null;
            foreach (TreeNode n in nodes) {
                Rectangle r = GetTextBounds(n);
                if (r.Contains(x, y)) {
                    return n;
                }
                if (n.IsExpanded && n.LabelBounds.Top <= y && n.bottom >= y) {
                    TreeNode result = FindNodeAt(n.Nodes, x, y);
                    if (result != null) return result;
                }
            }
            return null;
        }

        
        public Rectangle GetTextBounds(TreeNode n) {
            Rectangle r = new Rectangle(0, n.LabelBounds.Top, this.Width, n.LabelBounds.Height);
            return r;
        }

        void FindString(object sender, string toFind) {
            TreeNode node = this.SelectedNode;
        
            if (node == null) node = this.FirstVisibleNode;
            TreeNode start = node;
            while (node != null) {
                string s = GetNodeText(node);
                if (s != null && s.StartsWith(toFind, StringComparison.CurrentCultureIgnoreCase)) {
                    this.SelectedNode = node;
                    return;
                }
                node = node.NextVisibleNode;
                if (node == null) node = this.FirstVisibleNode;
                if (node == start)
                    break;
            }
        }

        public TreeNode FirstVisibleNode {
            get {
                if (this.nodes == null) return null;
                foreach (TreeNode node in this.nodes) {
                    if (node.IsVisible) {
                        return node;
                    }
                }
                return null;
            }
        }

        AccessibleNodeTextView acc;
        protected override AccessibleObject CreateAccessibilityInstance() {
            if (this.acc == null) this.acc = new AccessibleNodeTextView(this);
            return this.acc;
        }
    }

    class AccessibleNodeTextView : Control.ControlAccessibleObject {
        NodeTextView view;
        Hashtable cache = new Hashtable(); // TreeNode -> AccessibleNodeTextViewNode.

        public AccessibleNodeTextView(NodeTextView view)
            : base(view) {
            this.view = view;
        }

        public NodeTextView View { get { return this.view; } }

        public override Rectangle Bounds {
            get {
                return view.RectangleToScreen(view.ClientRectangle);
            }
        }
        public override string DefaultAction {
            get {
                return "Edit";
            }
        }
        public override void DoDefaultAction() {
            if (view.SelectedNode != null) {
                view.BeginEdit(null);
            }
        }
        public override int GetChildCount() {
            return view.Nodes.Count;
        }
        public override AccessibleObject GetChild(int index) {
            TreeNode node = view.Nodes[index];
            return Wrap(node);
        }

        public AccessibleObject Wrap(TreeNode node) {
            if (node == null) return null;
            AccessibleObject a =(AccessibleObject)cache[node];
            if (a == null){
                a = new AccessibleNodeTextViewNode(this, node);
                cache[node] = a;
            }
            return a;
        }

        public override AccessibleObject GetFocused() {
            return GetSelected();
        }
        public override int GetHelpTopic(out string fileName) {
            fileName = "TBD";
            return 0;
        }
        public override AccessibleObject GetSelected() {
            if (view.SelectedNode != null) {
                return Wrap(view.SelectedNode);
            }
            return this;
        }
        public override AccessibleObject HitTest(int x, int y) {
            Point pt = view.PointToClient(new Point(x, y));
            pt = view.ApplyScrollOffset(pt);
            TreeNode node = view.FindNodeAt(view.Nodes, pt.X, pt.Y);
            if (node != null) {
                return Wrap(node);
            }
            return this;
        }

        public override AccessibleObject Navigate(AccessibleNavigation navdir) {
            TreeNode node = null;
            TreeNodeCollection children = view.Nodes;
            int count = children.Count;
            switch (navdir) {
                case AccessibleNavigation.Left:
                case AccessibleNavigation.Down:
                case AccessibleNavigation.FirstChild:
                    if (count > 0) node = children[0];
                    break;
                case AccessibleNavigation.LastChild:
                case AccessibleNavigation.Next:
                case AccessibleNavigation.Previous:
                case AccessibleNavigation.Right:
                case AccessibleNavigation.Up:
                    if (count > 0) node = children[count - 1];
                    break;
            }
            if (node != null) {
                return Wrap(node);
            }
            return this;
        }
        public override AccessibleObject Parent {
            get {
                return view.Parent.AccessibilityObject;
            }
        }
        public override AccessibleRole Role {
            get {
                return view.AccessibleRole;
            }
        }
        public override void Select(AccessibleSelection flags) {
            this.view.Focus();
        }
        public override AccessibleStates State {
            get {
                AccessibleStates result = AccessibleStates.Focusable | AccessibleStates.Selectable |
                    AccessibleStates.Sizeable;
                if (view.Focused) result |= AccessibleStates.Focused;
                if (!view.Visible) result |= AccessibleStates.Invisible;
                return result;
            }
        }
        public override string Value {
            get {
                return "";
            }
            set {
                //???
            }
        }
    }

    class AccessibleNodeTextViewNode : AccessibleObject {
        TreeNode node;
        NodeTextView view;
        AccessibleNodeTextView acc;

        public AccessibleNodeTextViewNode(AccessibleNodeTextView acc, TreeNode node) {
            this.acc = acc;
            this.view = acc.View;
            this.node = node;
        }
        public override Rectangle Bounds {
            get {
                Rectangle bounds = view.GetTextBounds(node);
                bounds.Offset(view.ScrollPosition);
                return view.RectangleToScreen(bounds);
            }
        }
        public override string DefaultAction {
            get {
                return "Toggle";
            }
        }
        public override string Description {
            get {
                return "TextNode";
            }
        }
        public override void DoDefaultAction() {
            node.Toggle();
        }
        public override int GetChildCount() {
            return node.Nodes.Count;
        }
        public override AccessibleObject GetChild(int index) {
            TreeNode child = this.node.Nodes[index];
            return acc.Wrap(child);
        }
        public override AccessibleObject GetFocused() {
            return GetSelected();
        }
        public override int GetHelpTopic(out string fileName) {
            fileName = "TBD";
            return 0;
        }
        public override AccessibleObject GetSelected() {
            if (node.Selected) {
                return this;
            }
            return acc.GetSelected();
        }
        public override string Help {
            get {
                return "TBD";
            }
        }
        public override AccessibleObject HitTest(int x, int y) {
            return acc.HitTest(x, y);
        }
        public override string KeyboardShortcut {
            get {
                return "TBD";
            }
        }
        public override string Name {
            get {
                return node.Label;
            }
            set {
                // hack alert - this is breaking architectural layering!
                XmlTreeNode xnode = (XmlTreeNode)node;
                view.UndoManager.Push(new EditNodeName(xnode, value));
            }
        }
        public override AccessibleObject Navigate(AccessibleNavigation navdir) {
            TreeNode result = null;
            TreeNodeCollection children = node.Nodes;
            int count = children.Count;
            switch (navdir) {
                case AccessibleNavigation.Down:
                case AccessibleNavigation.Next:
                    result = node.NextVisibleNode;
                    break;
                case AccessibleNavigation.FirstChild:
                    if (count > 0) result = children[0];
                    if (!node.IsExpanded) node.Expand();
                    break;
                case AccessibleNavigation.Left:
                    return node.AccessibleObject;
                case AccessibleNavigation.Right:
                    break;
                case AccessibleNavigation.LastChild:
                    if (count > 0) result = children[count - 1];
                    if (!node.IsExpanded) node.Expand();
                    break;
                case AccessibleNavigation.Previous:
                case AccessibleNavigation.Up:
                    result = node.PrevVisibleNode;
                    break;
            }
            if (result != null) {
                return acc.Wrap(result);
            }
            return this;
        }
        public override AccessibleObject Parent {
            get {
                if (node.Parent != null) {
                    return acc.Wrap(node.Parent);
                } else {
                    return acc;
                }
            }
        }
        public override AccessibleRole Role {
            get {
                return AccessibleRole.ListItem;
            }
        }
        public override void Select(AccessibleSelection flags) {
            view.Focus();
            if ((flags & AccessibleSelection.TakeSelection) != 0 ||
                (flags & AccessibleSelection.AddSelection) != 0) {
                view.SelectedNode = node;
            } else if ((flags & AccessibleSelection.RemoveSelection) != 0) {
                if (view.SelectedNode == this.node) {
                    view.SelectedNode = null;
                }
            }
        }
        public override AccessibleStates State {
            get {
                AccessibleStates result = AccessibleStates.Focusable | AccessibleStates.Selectable;
                if (node.Selected) result |= AccessibleStates.Focused | AccessibleStates.Selected;
                if (!node.IsVisible) result |= AccessibleStates.Invisible | AccessibleStates.Collapsed;
                else result |= AccessibleStates.Expanded;
                return result;
            }
        }
        public override string Value {
            get {
                string s = this.node.Text;
                if (s == null) s = "";
                return s;
            }
            set {
                // hack alert - this is breaking architectural layering!
                XmlTreeNode xnode = (XmlTreeNode)node;
                XmlTreeView xview = xnode.XmlTreeView;
                view.UndoManager.Push(new EditNodeValue(xview, xnode, value));
            }
        }
    }

}