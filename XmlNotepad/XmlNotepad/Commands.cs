using System;
using System.Xml;
using System.Windows.Forms;
using System.Collections;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.IO;

namespace XmlNotepad {

    public enum InsertPosition { Child, Before, After }

    /// <summary>
    /// This class normalizes the concept of parent for XmlTreeNode and XmlNode node.
    /// The reason for this is that XmlTreeNode.Parent returns null on root level nodes and
    /// XmlNode.ParentNode returns null on XmlAttributes. So this class provides one
    /// uniform way to insert and remove both TreeNodes and their associated XmlNodes. 
    /// </summary>
    class TreeParent {
        //XmlTreeNode node;      // the reference node ("selected" node)
        bool nodeInTree;    // is the reference node in the tree?
        XmlTreeNode parent;
        TreeView view;
        //XmlNode xnode;
        XmlNode xparent;
        XmlNode originalParent;
        XmlDocument doc;

        // There is no reference node, so the parent is the TreeView and XmlDocument.
        public TreeParent(TreeView view, XmlDocument doc) {
            this.view = view;
            this.doc = doc;
            this.xparent = doc;
        }

        public TreeParent(XmlTreeView xview, XmlDocument doc, XmlTreeNode node)
            : this(xview.TreeView, doc, node) {
        }
        

        // This constructor takes the reference node
        public TreeParent(TreeView view, XmlDocument doc, XmlTreeNode node) {
            //this.node = node;
            if (node != null) this.nodeInTree = ((TreeView)node.TreeView == view);
            this.view = view;
            this.doc = doc;
            this.parent = (XmlTreeNode)node.Parent;
 
            //this.xnode = node.Node;
            if (node.Parent != null) {
                this.xparent = ((XmlTreeNode)node.Parent).Node;
            } else if (node.Node != null) {                
                this.xparent = node.Node.ParentNode;
            }
            if (this.xparent == null){
                this.xparent = this.doc;
            }
            originalParent = this.xparent;
            if (this.parent == null) {
                this.xparent = this.originalParent = this.doc;
            }
        }
        public int Count {
            get {
                if (parent != null) return parent.Nodes.Count;
                return view.Nodes.Count;
            }
        }
        public TreeView View {
            get { return this.view; }
        }
        public XmlDocument Document {
            get { return this.doc; }
        }
        public bool IsNodeInTree {
            get { return this.nodeInTree; }
        }
        public XmlTreeNode GetChild(int i) {
            if (parent != null) return (XmlTreeNode)parent.Nodes[i];
            return (XmlTreeNode)view.Nodes[i];
        }
        public void SetParent(XmlTreeNode parent) {
            this.parent = parent;
            if (parent != null && parent.Node != null) {
                this.SetXmlParent(parent.Node);
            }
        }
        void SetXmlParent(XmlNode parent) {
            if (parent != null) {
                this.xparent = parent;
                doc = xparent.OwnerDocument;
            }
        }
        public bool IsRoot {
            get { return xparent == null || xparent is XmlDocument; }
        }
        public bool IsElement {
            get { return xparent is XmlElement; }
        }
        public XmlNode ParentNode {
            get { return this.xparent; }
        }
        public int AttributeCount {
            get {
                if (xparent == null || xparent.Attributes == null) return 0;
                return xparent.Attributes.Count;
            }
        }
        public int ChildCount {
            get {
                if (xparent != null && xparent.HasChildNodes)
                    return xparent.ChildNodes.Count;
                return 0;
            }
        }

        public void Insert(int pos, InsertPosition position, XmlTreeNode n, bool selectIt) {
            
            if (n.Node != null) {
                this.Insert(pos, position, n.Node);
            }
            
            int i = pos;
            if (position == InsertPosition.After) i++;            
            if (parent == null) {
                view.Nodes.Insert(i, n);
            } 
            else {
                parent.Nodes.Insert(i, n);
                if (selectIt && !parent.IsExpanded) {
                    parent.Expand(); // this will change image index of leaf nodes.
                }
            }
            n.Invalidate();
            if (selectIt) view.SelectedNode = n;
        }
        public void Insert(int i, InsertPosition position, XmlNode n) {
            if (n == null) return;
            if (n.NodeType == XmlNodeType.Attribute) {
                Debug.Assert(this.xparent is XmlElement);
                XmlElement pe = (XmlElement)this.xparent;
                if (pe.Attributes != null){
                    XmlNode already = pe.Attributes.GetNamedItem(n.LocalName, n.NamespaceURI);
                    if (already != null){
                        throw new ApplicationException("Attribute with the same name already exists");
                    }
                }
                if (pe.Attributes != null && i < pe.Attributes.Count) {
                    XmlAttribute refNode = this.xparent.Attributes[i];
                    if (position == InsertPosition.After){
                        pe.Attributes.InsertAfter((XmlAttribute)n, refNode);
                    } else {
                        pe.Attributes.InsertBefore((XmlAttribute)n, refNode);
                    }
                } else {
                    pe.Attributes.Append((XmlAttribute)n);
                }
            } else {
                i -= this.AttributeCount;
                if (this.xparent.HasChildNodes && i < this.xparent.ChildNodes.Count) {
                    XmlNode refNode = this.xparent.ChildNodes[i];
                    if (position == InsertPosition.After) {
                        this.xparent.InsertAfter(n, refNode);
                    } else {
                        this.xparent.InsertBefore(n, refNode);
                    }
                } else {
                    this.xparent.AppendChild(n);
                }
            }
        }
        public void Remove(XmlTreeNode n) {
            if (n.Node != null) {
                Remove(n.Node);
            }
            n.Remove();
        }

        void Remove(XmlNode n) {
            if (n != null && this.originalParent != null) {
                if (n.NodeType == XmlNodeType.Attribute) {
                    Debug.Assert(this.originalParent is XmlElement);
                    this.originalParent.Attributes.Remove((XmlAttribute)n);
                } else {
                    this.originalParent.RemoveChild(n);
                }
            }
        }
    }

    public class EditNodeName : Command {
        Command cmd;
        public EditNodeName(XmlTreeNode node, XmlName newName, bool autoGenPrefixes) {
            if (node.Node == null) {
                throw new ArgumentException("Cannot edit name of node, node note created yet!");
            }
            switch (node.NodeType) {
                case XmlNodeType.Element:
                    cmd = new EditElementName(node, newName, autoGenPrefixes);
                    break;
                case XmlNodeType.Attribute:
                    cmd = new EditAttributeName(node, newName, autoGenPrefixes);
                    break;
                case XmlNodeType.ProcessingInstruction:
                    cmd = new EditProcessingInstructionName(node, newName.LocalName);
                    break;
                default:
                    throw new ArgumentException(
                        string.Format("Cannot edit name of node of type '{0}'", node.NodeType.ToString()));
            }
        }
        public EditNodeName(XmlTreeNode node, string newName) {
            if (node.Node == null) {
                throw new ArgumentException("Cannot edit name of node, node note created yet!");
            }
            switch (node.NodeType) {
                case XmlNodeType.Element:
                    cmd = new EditElementName(node, newName);
                    break;
                case XmlNodeType.Attribute:
                    cmd = new EditAttributeName(node, newName);
                    break;
                case XmlNodeType.ProcessingInstruction:
                    cmd = new EditProcessingInstructionName(node, newName);
                    break;                    
                default:
                    throw new ArgumentException(
                        string.Format("Cannot edit name of node of type '{0}'", node.NodeType.ToString()));
            }
        }
        public override void Do() {
            cmd.Do();
        }
        public override void Undo() {
            cmd.Undo();
        }
        public override void Redo() {
            cmd.Redo();
        }
        public override bool IsNoop {
            get { return false; }
        }
    }


    /// <summary>
    /// Change the name of an attribute.
    /// </summary>
    public class EditAttributeName : Command {
        XmlAttribute a;
        XmlAttribute na;
        XmlElement p;
        XmlTreeNode node;
        XmlName name;
        InsertNode xmlns; // generated prefix
        bool autoGenPrefixes = true;

        public EditAttributeName(XmlAttribute attr, NodeLabelEditEventArgs e) {
            this.a = attr;
            this.node = e.Node as XmlTreeNode;
            this.p = this.a.OwnerElement;
            Debug.Assert(this.p != null);
            name = XmlHelpers.ParseName(this.p, e.Label, XmlNodeType.Attribute);
        }
        public EditAttributeName(XmlTreeNode node, string newName) {
            this.a = (XmlAttribute)node.Node;
            this.node = node;
            this.p = this.a.OwnerElement;
            Debug.Assert(this.p != null);
            name = XmlHelpers.ParseName(this.p, newName, XmlNodeType.Attribute);
        }
        public EditAttributeName(XmlTreeNode node, XmlName newName, bool autoGenPrefixes) {
            this.a = (XmlAttribute)node.Node;
            this.node = node;
            name = newName;
            this.autoGenPrefixes = autoGenPrefixes;
        }

        public override bool IsNoop {
            get {
                return this.a.LocalName == name.LocalName && this.a.Prefix == name.Prefix &&
                    this.a.NamespaceURI == name.NamespaceUri;
            }
        }
        public override void Do() {
            XmlAttribute nsa = null;
            this.p = this.a.OwnerElement; // just in case a prior command changed this!
            if (autoGenPrefixes && XmlHelpers.MissingNamespace(name)) {
                nsa  = XmlHelpers.GenerateNamespaceDeclaration(this.p, name);                         
            }
            this.na = a.OwnerDocument.CreateAttribute(name.Prefix, name.LocalName, name.NamespaceUri);
            this.na.Value = this.a.Value; // todo: copy children properly.
            Redo();
            if (nsa != null) {
                xmlns = new InsertNode(node, InsertPosition.After, nsa, false);
                xmlns.Do();
            }
        }
        public override void Undo() {
            if (this.xmlns != null) xmlns.Undo();
            this.p.RemoveAttributeNode(this.na);
            this.p.SetAttributeNode(this.a);
            this.node.Label = this.a.Name;
            this.node.Node = this.a;
        }
        public override void Redo() {
            this.p.RemoveAttributeNode(this.a);
            this.p.SetAttributeNode(this.na);
            this.node.Node = this.na;
            if (this.node.Label != this.na.Name) {
                this.node.Label = this.na.Name;
            }
            if (this.xmlns != null) xmlns.Redo();
        }
        
    }

    /// <summary>
    /// Change the name of a processing instruction.
    /// </summary>
    public class EditProcessingInstructionName : Command {
        XmlProcessingInstruction pi;
        XmlProcessingInstruction newpi;
        XmlNode p;
        XmlTreeNode node;
        string name;

        public EditProcessingInstructionName(XmlProcessingInstruction pi, NodeLabelEditEventArgs e) {
            this.pi = pi;
            this.p = this.pi.ParentNode;
            this.node = e.Node as XmlTreeNode;
            Debug.Assert(this.p != null);
            name = e.Label;
            this.newpi = pi.OwnerDocument.CreateProcessingInstruction(name, pi.Data);
        }
        public EditProcessingInstructionName(XmlTreeNode node, string newName) {
            this.pi = (XmlProcessingInstruction)node.Node;
            this.p = this.pi.ParentNode;
            this.node = node;
            Debug.Assert(this.p != null);
            name = newName;
            this.newpi = pi.OwnerDocument.CreateProcessingInstruction(name, pi.Data);
        }
        public override void Do() {
            Swap(this.pi, this.newpi);
        }
        public override void Undo() {
            Swap(this.newpi, this.pi);
        }
        public override void Redo() {
            Swap(this.pi, this.newpi);
        }
        public override bool IsNoop {
            get {
                return this.pi.Target == name;
            }
        }
        public void Swap(XmlProcessingInstruction op, XmlProcessingInstruction np) {
            this.p.InsertBefore(np, op );
            this.p.RemoveChild(op);
            this.node.Node = np;
            this.node.Label = np.Target;
        }

    }

    /// <summary>
    /// </summary>
    public class InsertNode : Command {
        XmlTreeView view;
        XmlDocument doc;

        //XmlTreeNode n;
        XmlTreeNode newNode;
        TreeParent parent;

        XmlNode theNode;
        int pos;
        XmlNodeType type;
        bool requiresName;
        InsertPosition position;
        bool selectNewNode = true;

        /// <summary>
        /// Insert a new element as a sibling or child of current node. This command can create
        /// new XmlTreeNodes and new XmlNodes to go with it, or it can 
        /// </summary>
        public InsertNode(XmlTreeView view) {
            this.view = view;
            this.newNode = view.CreateTreeNode();
            this.doc = view.Model.Document;
            this.position = InsertPosition.Child;
        }

        /// <summary>
        /// Insert an existing XmlNode into the tree and create a corresponding XmlTreeNode for it.
        /// </summary>
        /// <param name="target">Anchor point for insertion</param>
        /// <param name="position">Where to insert the new node relative to target node</param>
        /// <param name="xnode">Provided XmlNode that the new XmlTreeNode will wrap</param>
        /// <param name="selectNewNode">Whether to select the node in the tree after it's inserted.</param>
        public InsertNode(XmlTreeNode target, InsertPosition position, XmlNode xnode, bool selectNewNode) {
            this.view = target.XmlTreeView;
            this.position = position;
            this.type = xnode.NodeType;
            this.newNode = new XmlTreeNode(this.view, xnode);
            Initialize(newNode, target, position);
            this.selectNewNode = selectNewNode;
        }

        // Returns false if the given insertion is illegal
        public bool Initialize(XmlTreeNode n, InsertPosition position, XmlNodeType type) {
            this.position = position;
            this.type = type;
            XmlNode xn = null;
            this.newNode.NodeType = type;
            if (n != null) {
                this.parent = new TreeParent(view, doc, n);
                xn = n.Node;
            } else {
                position = InsertPosition.Child; ;
                xn = view.Model.Document;
                this.parent = new TreeParent(view.TreeView, view.Model.Document);
            }
            bool result = CanInsertNode(position, type, xn);
            if (result) {
                if (position == InsertPosition.Child) {
                    if (xn != null) parent.SetParent(n);
                    pos = parent.AttributeCount;
                    if (type != XmlNodeType.Attribute)
                        pos += parent.ChildCount;
                } else {
                    if (type == XmlNodeType.Attribute ^ xn is XmlAttribute) {
                        pos = this.parent.AttributeCount;
                    } else if (n != null) {
                        pos = n.Index;
                    }
                }
            }
            return result;
        }

        bool[][] insertMap = new bool[][] {
        // child -> parent                          None,  Element, Attribute, Text,  CDATA, EntityRef, Entity, PI,    Comment, Doc,   DOCTYPE, Frag,  Notation, WhiteSpace, SigWS, EndElement, EndEntity, XmlDecl                                                
        /* None                     */ new bool[] { false, false,   false,     false, false, false,     false,  false, false,   false, false,   false, false,    false,      false, false,      false,     false },
        /* Element,                 */ new bool[] { false, true,    false,     false, false, true,      false,  false, false,   true,  false,   true,  false,    false,      false, false,      false,     false },
        /* Attribute,               */ new bool[] { false, true,    false,     false, false, false,     false,  false, false,   false, false,   false, false,    false,      false, false,      false,     false },
        /* Text,                    */ new bool[] { false, true,    false,     false, false, true,      false,  false, false,   false, false,   true,  false,    false,      false, false,      false,     false },
        /* CDATA,                   */ new bool[] { false, true,    false,     false, false, true,      false,  false, false,   false, false,   true,  false,    false,      false, false,      false,     false },
        /* EntityReference,         */ new bool[] { false, true,    false,     false, false, true,      false,  false, false,   false, false,   true,  false,    false,      false, false,      false,     false },
        /* Entity,                  */ new bool[] { false, false,   false,     false, false, false,     false,  false, false,   false, false,   false, false,    false,      false, false,      false,     false },
        /* ProcessingInstruction,   */ new bool[] { false, true,    false,     false, false, true,      false,  false, false,   true,  false,   true,  false,    false,      false, false,      false,     false },
        /* Comment,                 */ new bool[] { false, true,    false,     false, false, true,      false,  false, false,   true,  false,   true,  false,    false,      false, false,      false,     false },
        /* Document,                */ new bool[] { false, false,   false,     false, false, false,     false,  false, false,   false, false,   false, false,    false,      false, false,      false,     false },
        /* DocumentType,            */ new bool[] { false, false,   false,     false, false, false,     false,  false, false,   true,  false,   true,  false,    false,      false, false,      false,     false },
        /* DocumentFragment,        */ new bool[] { false, true,    false,     false, false, false,     false,  false, false,   true,  false,   true,  false,    false,      false, false,      false,     false },
        /* Notation,                */ new bool[] { false, false,   false,     false, false, true,      false,  false, false,   false, false,   false, false,    false,      false, false,      false,     false },
        /* Whitespace,              */ new bool[] { false, true,    false,     false, false, true,      false,  false, false,   true,  false,   true,  false,    false,      false, false,      false,     false },
        /* SignificantWhitespace,   */ new bool[] { false, true,    false,     false, false, true,      false,  false, false,   true,  false,   true,  false,    false,      false, false,      false,     false },
        /* EndElement,              */ new bool[] { false, false,   false,     false, false, false,     false,  false, false,   false, false,   false, false,    false,      false, false,      false,     false },
        /* EndEntity,               */ new bool[] { false, false,   false,     false, false, false,     false,  false, false,   false, false,   false, false,    false,      false, false,      false,     false },
        /* XmlDeclaration           */ new bool[] { false, false,   false,     false, false, false,     false,  false, false,   true,  false,   false, false,    false,      false, false,      false,     false }
        };

        private bool CanInsertNode(InsertPosition position, XmlNodeType type, XmlNode xn) {
            if (position == InsertPosition.Before && xn.NodeType == XmlNodeType.XmlDeclaration) {
                return false; // cannot insert anything before xml declaration.
            }
            if (position != InsertPosition.Child) {
                xn = parent.ParentNode;
            }
            XmlNodeType parentType = (xn != null) ? xn.NodeType : XmlNodeType.None;
            bool result = insertMap[(int)type][(int)parentType];
            
            // Check a few extra things...
            switch (type) {
                case XmlNodeType.Attribute:
                    this.requiresName = true;                    
                    break;
                case XmlNodeType.Element:
                    this.requiresName = true;
                    if (position != InsertPosition.Child && parent.IsRoot && parent.Document != null && parent.Document.DocumentElement != null) {
                        result = false; // don't allow multiple root elements.
                    }
                    break;
                case XmlNodeType.ProcessingInstruction:
                    this.requiresName = true;                    
                    break;
            }
            return result;
        }

        public void Initialize(XmlTreeNode newNode, XmlTreeNode target, InsertPosition position) {
            this.newNode = newNode;
            this.position = position;

            if (target == null) {
                this.parent = new TreeParent(this.view.TreeView, this.doc);
            } else {
                this.parent = new TreeParent(this.view, this.doc, target);
                if (CanHaveChildren(target)) {
                    this.parent.SetParent(target);
                } else {
                    // if it's not an element it cannot have children!
                    this.position = InsertPosition.After;
                }
            }
            if (position == InsertPosition.Child) {
                if (target == null) {
                    // inserting at rool level
                    this.pos = this.view.TreeView.Nodes.Count;
                } else {
                    if (!CanHaveChildren(target)) {
                        this.position = InsertPosition.After;
                    }
                    if (newNode.NodeImage == NodeImage.Attribute) {
                        this.pos = this.parent.AttributeCount;
                    } else if (target != null) {
                        this.pos = target.Nodes.Count;
                    }
                }
            }
            if (this.position != InsertPosition.Child) {
                if (target.Node is XmlAttribute ^ newNode.Node is XmlAttribute) {
                    pos = this.parent.AttributeCount;
                } else if (target != null) {
                    this.pos = target.Index + 1;
                }
            }
        }
        bool CanHaveChildren(XmlTreeNode target) {
            return target.NodeType == XmlNodeType.Element ||
                    target.NodeType == XmlNodeType.Document;
        }
        public bool RequiresName {
            get { return this.requiresName; }
        }
        public XmlTreeNode NewNode {
            get { return this.newNode; }
        }

        public XmlNode CreateNode(XmlNode context, string name) {
            XmlNode n = null;            
            switch (type) {
                case XmlNodeType.Attribute: {
                        XmlName qname = XmlHelpers.ParseName(context, name, type);
                        if (qname.Prefix == null) {
                            n = doc.CreateAttribute(qname.LocalName);
                        } else {
                            n = doc.CreateAttribute(qname.Prefix, qname.LocalName, qname.NamespaceUri);
                        }
                    }
                    break;
                case XmlNodeType.CDATA:
                    n = doc.CreateCDataSection("");
                    break;
                case XmlNodeType.Comment:
                    n = doc.CreateComment("");
                    break;
                case XmlNodeType.DocumentType:
                    XmlConvert.VerifyName(name);
                    n = doc.CreateDocumentType(name, null, null, null);
                    break;
                case XmlNodeType.Element: {
                        XmlName qname = XmlHelpers.ParseName(context, name, type);
                        n = doc.CreateElement(qname.Prefix, qname.LocalName, qname.NamespaceUri);
                        break;
                    }
                case XmlNodeType.ProcessingInstruction:
                    XmlConvert.VerifyName(name); 
                    n = doc.CreateProcessingInstruction(name, "");
                    break;
                case XmlNodeType.Text:
                    n = doc.CreateTextNode("");
                    break;
                default:
                    throw new ApplicationException("Unsupported node type:" + type.ToString());
            }
            return n;
        }
        public XmlNode CreateDocumentElement(string namespaceUri, string name) {
            XmlNode n = null;
            n = doc.CreateElement(name, namespaceUri);
            return n;
        }
        public XmlNode XmlNode {
            get { return newNode.Node; }
            set {
                parent.Insert(this.pos, this.position, value);
                newNode.Node = this.theNode = value;
                view.TreeView.OnSelectionChanged();
            }
        }
        public override bool IsNoop {
            get {
                return false;
            }
        }
        public override void Do() {
            Debug.Assert(parent != null);
            parent.Insert(this.pos, this.position, newNode, this.selectNewNode);
            if (this.RequiresName) {
                Debug.Assert(newNode != null);
                if (this.theNode != null && newNode.Node == null) {
                    this.XmlNode = this.theNode;
                }
                Debug.Assert(view != null);
                if (selectNewNode) this.view.SelectedNode = newNode;
            } else if (newNode.Node == null) {
                this.XmlNode = CreateNode(null, null);
                this.view.OnNodeInserted(newNode);
            }
        }
        public override void Undo() {
            if (newNode.IsEditing) {
                newNode.EndEdit(true);
            }
            TreeParent np = new TreeParent(this.view, this.doc, newNode);
            np.Remove(newNode);
        }
        public override void Redo() {
            Do();
        }

    }

    public class EditElementName : Command {

        XmlElement xe;
        XmlElement ne;
        XmlNode p;
        XmlTreeNode node;
        XmlName name;
        InsertNode xmlns; // generated prefix
        bool autoGenPrefixes = true;

        public EditElementName(XmlElement n, NodeLabelEditEventArgs e) {
            this.xe = n;
            this.node = e.Node as XmlTreeNode;
            this.name = XmlHelpers.ParseName(n, e.Label, n.NodeType);
        }
        public EditElementName(XmlTreeNode node, string newName) {
            this.xe = (XmlElement)node.Node; ;
            this.node = node;
            this.name = XmlHelpers.ParseName(this.xe, newName, node.NodeType);
        }
        public EditElementName(XmlTreeNode node, XmlName newName, bool autoGenPrefixes) {
            this.xe = (XmlElement)node.Node; ;
            this.node = node;
            this.name = newName;
            this.autoGenPrefixes = autoGenPrefixes;
        }
        public override bool IsNoop {
            get {
                return this.xe.LocalName == name.LocalName && this.xe.Prefix == name.Prefix &&
                    this.xe.NamespaceURI == name.NamespaceUri;
            }
        }
        public override void Do() {
            this.p = xe.ParentNode; // in case a prior command changed this!
            XmlAttribute a = null;
            if (autoGenPrefixes && XmlHelpers.MissingNamespace(name)) {
                a = XmlHelpers.GenerateNamespaceDeclaration(xe, name);                
            }
            this.ne = xe.OwnerDocument.CreateElement(name.Prefix, name.LocalName, name.NamespaceUri); 
            Redo();
            if (a != null) {
                xmlns = new InsertNode(node, InsertPosition.Child, a, false);
                xmlns.Do();
            }            
        }

        public override void Undo() {
            if (this.xmlns != null) xmlns.Undo();
            Move(ne, xe);            
            this.p.ReplaceChild(xe, ne);
            node.Node = xe;
        }

        public override void Redo() {
            // Since you cannot rename an element using the DOM, create new element 
            // and copy all children over.
            Move(xe, ne);
            this.p.ReplaceChild(ne, xe);
            node.Node = ne;
            if (this.xmlns != null) xmlns.Redo();             
        }

        static void Move(XmlElement from, XmlElement to) {
            ArrayList move = new ArrayList();
            foreach (XmlAttribute a in from.Attributes) {
                if (a.Specified) {
                    move.Add(a);
                }
            }
            foreach (XmlAttribute a in move) {
                from.Attributes.Remove(a);
                to.Attributes.Append(a);
            }
            while (from.HasChildNodes) {
                to.AppendChild(from.FirstChild);
            }
        }

    }

    /// <summary>
    /// Change the value of a node.
    /// </summary>
    public class EditNodeValue : Command {
        XmlTreeNode n;
        XmlNode xn;
        string newValue;
        string oldValue;
        XmlTreeView view;

        public EditNodeValue(XmlTreeView view, XmlTreeNode n, string newValue) {
            this.view = view;
            this.n = n;
            this.xn = n.Node;
            this.newValue = newValue;

            if (xn is XmlElement) {
                this.oldValue = xn.InnerText;                
            } else if (xn is XmlProcessingInstruction) {
                XmlProcessingInstruction pi = ((XmlProcessingInstruction)xn);
                this.oldValue = pi.Data;
            } else if (xn != null) {
                this.oldValue = xn.Value;
            }
        }

        public override bool IsNoop {
            get {
                return this.oldValue == this.newValue; 
            }
        }

        void SetValue(string value) {
            if (xn is XmlElement) {
                xn.InnerText = value;
                n.RemoveChildren();
                if (!string.IsNullOrEmpty(value)){
                   // Add text node child.
                    XmlTreeNode text = view.CreateTreeNode();
                    text.Node = xn.FirstChild;
                    n.Nodes.Add(text);                    
                }
            } else if (xn is XmlProcessingInstruction) {
                XmlProcessingInstruction pi = ((XmlProcessingInstruction)xn);
                pi.Data = value;
            } else if (xn != null) {
                xn.Value = value;
            }
        }
        public override void Do() {
            SetValue(newValue);
        }

        public override void Undo() {
            SetValue(oldValue);
        }
        public override void Redo() {
            SetValue(newValue);
        }

    }

    public class DeleteNode : Command {

        XmlTreeNode e;
        TreeParent parent;
        int pos;

        public DeleteNode(XmlDocument doc, XmlTreeNode e) {
            this.e = e;
            this.pos = e.Index;
            this.parent = new TreeParent(e.TreeView, doc, e);
        }
        public override bool IsNoop {
            get { return false; }
        }
        public override void Do() {
            parent.Remove(e);
        }

        public override void Undo() {
            parent.Insert(this.pos, InsertPosition.Before, this.e, true);
        }

        public override void Redo() {
            Do();
        }

    }

    public class MoveNode : Command {
        XmlTreeNode source;
        XmlTreeNode target;
        TreeParent tp;
        InsertPosition where;
        TreeParent sourceParent;
        int sourcePosition;
        bool copy;
        bool bound;
        bool wasExpanded;
        XmlTreeView view;

        /// <summary>
        /// Move or copy a node from one place to another place in the tree.
        /// </summary>
        /// <param name="view">The MyTreeView that we are inserting into</param>
        /// <param name="source">The node that we are moving.  This node may not be in the tree
        /// and that is ok, so it might be a node that is being cut&paste from another process
        /// for example</param>
        /// <param name="target">The existing node that establishes where in the tree we want
        /// to move the source node to</param>
        /// <param name="where">The position relative to the target node (before or after)</param>
        /// <param name="copy">Whether we are moving or copying the source node</param>
        public MoveNode(XmlTreeView view, XmlTreeNode source, XmlTreeNode target, InsertPosition where, bool copy) {
            XmlNode sn = source.Node;
            XmlNode dn = target.Node;

            this.copy = copy;
            TreeView tv = view.TreeView;
            XmlDocument doc = view.Model.Document;
            this.view = view;
            this.sourcePosition = source.Index;
            
            if (copy) {
                this.wasExpanded = source.IsExpanded;
                XmlTreeNode newSource = view.CreateTreeNode();
                if (sn != null) {
                    sn = sn.CloneNode(true);
                    newSource.Node = sn;
                }
                source = newSource;
            }

            this.sourceParent = new TreeParent(tv, doc, source);
            this.tp = new TreeParent(tv, doc, target);

            // normalize destination based on source node type.
            // for example, if source is an attribute, then it can only be
            // inserted amongst attributes of another node.
            if (tp.IsRoot && where != InsertPosition.Child) {
                if (sn is XmlAttribute)
                    throw new Exception("Cannot insert attributes at the root level");
                if (sn is XmlText || sn is XmlCDataSection)
                    throw new Exception("Cannot insert text at the root level");
                if (sn is XmlElement && sn.OwnerDocument.DocumentElement != null && sn.OwnerDocument.DocumentElement != sn)
                    throw new Exception("Cannot have two top level elements");
                if (dn is XmlDeclaration && where == InsertPosition.Before)
                    throw new Exception("Cannot have anything before the XML declaration");
            }
            if (where != InsertPosition.Child) {
                if (sn is XmlAttribute) {
                    if (!(dn is XmlAttribute)) {
                        if (tp.AttributeCount != 0) {
                            // move target to valid location for attributes.
                            target = tp.GetChild(tp.AttributeCount - 1);
                            where = InsertPosition.After;
                        } else {
                            // append the attribute.
                            where = InsertPosition.Child;
                            target = (XmlTreeNode)target.Parent;
                        }

                    }
                } else if (dn is XmlAttribute) {
                    if (!(sn is XmlAttribute)) {
                        int skip = tp.AttributeCount;
                        if (tp.Count > skip) {
                            // Move non-attribute down to beginning of child elements.
                            target = tp.GetChild(skip);
                            where = InsertPosition.Before;
                        } else {
                            // append the node.
                            where = InsertPosition.Child;
                            target = (XmlTreeNode)target.Parent;
                        }
                    }
                }
            }
            this.source = source;
            this.target = target;
            this.where = where;
            this.tp = new TreeParent(tv, doc, target);

            if (where == InsertPosition.Child) {
                this.tp.SetParent(target);                
            }
        }

        public XmlTreeNode Source { get { return this.source; }}

        public override bool IsNoop {
            get { return this.source == this.target; }
        }

        public override void Do() {
            XmlNode sn = source.Node;
            //XmlNode dn = target.Node;

            XmlTreeNode sp = (XmlTreeNode)source.Parent;
            
            int sindex = this.sourcePosition;
            TreeView tv = source.TreeView;
            if (tv != null){
                this.sourceParent.Remove(source);
            }
            int index = target.Index;

            if (where == InsertPosition.Child) {
                if (sn is XmlAttribute) {
                    index = this.tp.AttributeCount;
                } else {
                    index = this.tp.AttributeCount + this.tp.ChildCount;
                }
            }
            try {
                this.tp.Insert(index, where, source, true);
            } catch (Exception){
                if (sp != null){
                    sp.Nodes.Insert(sindex, source);
                } else if (tv != null) {
                    tv.Nodes.Insert(sindex, source);
                }
                if (tv != null) {
                    source.TreeView.SelectedNode = source;
                }
                throw;
            }
            
            if (this.copy && !bound){
                bound = true;
                this.view.Invalidate(); // Bind(source.Nodes, (XmlNode)source.Tag);
            }
            if (this.wasExpanded ){
                source.Expand();
            }
            
            source.TreeView.SelectedNode = source;
        }

        public override void Undo() {
            // Cannot use this.sourceParent because this points to the old source position
            // not the current position.
            TreeParent parent = new TreeParent(this.sourceParent.View, this.sourceParent.Document, this.source);
            parent.Remove(this.source);

            // If the node was not in the tree, then undo just removes it, it does not
            // have to re-insert back in a previous position, because it was a new node
            // (probably inserted via drag/drop).
            if (this.sourceParent.IsNodeInTree) {                
                this.sourceParent.Insert(this.sourcePosition, InsertPosition.Before, source, true);
                if (source.Parent != null && source.Parent.Nodes.Count == 1) {
                    source.Parent.Expand();
                }
                source.TreeView.SelectedNode = source;
            } else {
                this.target.TreeView.SelectedNode = target;
            }
        }
        public override void Redo() {
            Do();
        }

    }

    public enum NudgeDirection { Up, Down, Left, Right }

    public class NudgeNode : Command {

        XmlTreeNode node;
        NudgeDirection dir;
        MoveNode mover;
        XmlTreeView view;

        public NudgeNode(XmlTreeView view, XmlTreeNode node, NudgeDirection dir) {
            this.node = node;
            this.dir = dir;
            this.view = view;
        }

        public override bool IsNoop {
            get {
                MoveNode cmd = GetMover();
                return (cmd == null || cmd.IsNoop);
            }
        }
        public bool IsEnabled {
            get {
                switch (dir) {
                    case NudgeDirection.Up:
                        return CanNudgeUp;
                    case NudgeDirection.Down:
                        return CanNudgeDown;
                    case NudgeDirection.Left:
                        return CanNudgeLeft;
                    case NudgeDirection.Right:
                        return CanNudgeRight;
                }
                return false;
            }
        }

        public override void Do() {
            MoveNode cmd = GetMover();
            if (cmd != null)
                cmd.Do();
        }

        public override void Undo() {
            MoveNode cmd = GetMover();
            if (cmd != null)
                cmd.Undo();
        }

        public override void Redo() {
            Do();
        }

        MoveNode GetMover() {
            if (this.mover == null) {
                switch (dir) {
                    case NudgeDirection.Up:
                        this.mover = GetNudgeUp();
                        break;
                    case NudgeDirection.Down:
                        this.mover = GetNudgeDown();
                        break;
                    case NudgeDirection.Left:
                        this.mover = GetNudgeLeft();
                        break;
                    case NudgeDirection.Right:
                        this.mover = GetNudgeRight();
                        break;
                }
            }
            return this.mover;
        }

        public bool CanNudgeUp {
            get {
                if (node != null) {
                    XmlTreeNode prev = (XmlTreeNode)node.PrevNode;
                    if (prev != null) {
                        if (prev.Node is XmlAttribute && !(node.Node is XmlAttribute)) {
                            return false;
                        } else {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public MoveNode GetNudgeUp() {
            if (node != null) {
                XmlTreeNode prev = (XmlTreeNode)node.PrevNode;
                if (prev != null) {
                    if (prev.Node is XmlAttribute && !(node.Node is XmlAttribute)) {
                        prev = (XmlTreeNode)node.Parent;
                    }
                    return new MoveNode(this.view, node, prev, InsertPosition.Before, false);
                }
            }
            return null;
        }

        public bool CanNudgeDown {
            get {
                if (node != null) {
                    XmlTreeNode next = (XmlTreeNode)node.NextSiblingNode;
                    if (next != null) {
                        return true;
                    }
                }
                return false;
            }
        }
        public MoveNode GetNudgeDown() {
            if (node != null) {
                XmlTreeNode next = (XmlTreeNode)node.NextSiblingNode;
                if (next != null) {
                    if (node.Parent != next.Parent) {
                        return new MoveNode(this.view, node, next, InsertPosition.Before, false);
                    } else {
                        return new MoveNode(this.view, node, next, InsertPosition.After, false);
                    }
                }
            }
            return null;
        }
        public bool CanNudgeLeft {
            get {
                if (node != null) {
                    XmlTreeNode xn = (XmlTreeNode)node;
                    XmlTreeNode parent = (XmlTreeNode)node.Parent;
                    if (parent != null) {
                        if (xn.Node is XmlAttribute) {
                            XmlAttribute a = (XmlAttribute)xn.Node;
                            if (IsDocumentElement(parent)) {
                                // cannot move attributes outside of document element
                                return false;
                            }
                            if (parent.Node != null && parent.Node.ParentNode != null) {
                                XmlAttributeCollection ac = parent.Node.ParentNode.Attributes;
                                if (ac != null && ac.GetNamedItem(a.Name, a.NamespaceURI) != null) {
                                    // parent already has this attribute!
                                    return false;
                                }
                            }
                        }
                        return true;
                    }
                }
                return false;
            }
        }

        static bool IsDocumentElement(XmlTreeNode node) {
            return node.Node != null && node.Node == node.Node.OwnerDocument.DocumentElement;
        }

        public MoveNode GetNudgeLeft() {
            if (node != null) {
                XmlTreeNode parent = (XmlTreeNode)node.Parent;
                if (parent != null) {
                    if (node.Index == 0 && node.Index != parent.Nodes.Count-1) {
                        return new MoveNode(this.view, node, parent, InsertPosition.Before, false);
                    } else {
                        return new MoveNode(this.view, node, parent, InsertPosition.After, false);
                    }
                }
            }
            return null;
        }
        public bool CanNudgeRight {
            get {
                if (node != null) {
                    XmlTreeNode prev = (XmlTreeNode)node.PrevNode;
                    if (prev != null && prev != node.Parent) {
                        if (prev.Node is XmlElement) {
                            return true;
                        }
                    }
                }
                return false;
            }
        }
        public MoveNode GetNudgeRight() {
            if (node != null) {
                XmlTreeNode prev = (XmlTreeNode)node.PrevNode;
                if (prev != null && prev != node.Parent) {
                    if (prev.Node is XmlElement) {
                        return new MoveNode(this.view, node, prev, InsertPosition.Child, false);
                    }
                }
            }
            return null;
        }

    }

    /// <summary>
    /// The TreeData class encapsulates the process of copying the XmlTreeNode to the
    /// clipboard and back. This is a custom IDataObject that supports the custom
    /// TreeData format, and string.
    /// </summary>
    [Serializable]
    public class TreeData : IDataObject {
        int img;
        string xml;
        int nodeType;
        MemoryStream stm;

        public TreeData(MemoryStream stm){
            this.stm = stm;
            this.img = -1;
        }

        public TreeData(string xml ) {
            this.xml = xml;
            this.img = -1;
        }

        public TreeData(XmlTreeNode node) {
            img = node.ImageIndex;
            XmlNode x = node.Node;
            if (x != null) {
                nodeType = (int)x.NodeType;
                this.xml = x.OuterXml;
            }
        }

        public static void SetData(XmlTreeNode node) {
            if (node.Node != null) {
                Clipboard.SetDataObject(new TreeData(node));
            }
        }

        public static bool HasData {
            get {
                try {
                    IDataObject data = Clipboard.GetDataObject();
                    if (data.GetDataPresent(typeof(string)))
                        return true;

                    foreach (string format in data.GetFormats()){
                        if (format == typeof(TreeData).FullName)
                            return true;
                        if (format.ToUpper().StartsWith("XML"))
                            return true;
                    }                    
                } catch (System.Runtime.InteropServices.ExternalException){
                } catch (System.Threading.ThreadStateException){
                }
                return false;
            }
        }

        public static TreeData GetData() {
            try {
                IDataObject data = Clipboard.GetDataObject();
                try {
                    if (data.GetDataPresent(typeof(TreeData))) {
                        return data.GetData(typeof(TreeData)) as TreeData;
                    }
                    foreach (string format in data.GetFormats()){
                        if (format.ToUpper().StartsWith("XML")){
                            MemoryStream raw = data.GetData(format) as MemoryStream;
                            return new TreeData(raw);
                        }
                    }   
                } catch (Exception ){
                }
                if (data.GetDataPresent(typeof(string))) {
                    string xml = data.GetData(typeof(string)).ToString();
                    // We don't want to actually parse the XML at this point, 
                    // because we don't have the namespace context yet.
                    // So we just sniff the XML to determine the node type.
                    return new TreeData(xml);
                }
            } catch (System.Runtime.InteropServices.ExternalException){
            } catch (System.Threading.ThreadStateException){
            }            
            return null;
        }

        public XmlTreeNode GetTreeNode(XmlDocument owner, XmlTreeNode target, XmlTreeView view) {
            XmlTreeNode node = view.CreateTreeNode();
            
            if (this.img == -1 && this.xml != null){
                Regex regex = new Regex(@"[:_.\w]+\s*=\s*(""[^""]*"")|('[^']*')\s*");
                Match m = regex.Match(xml);
                string trimmed = xml.Trim();
                if (m.Success && m.Index == 0 && m.Length == xml.Length) {
                    nodeType = (int)XmlNodeType.Attribute;
                    img = (int)NodeImage.Attribute - 1;
                } else if (trimmed.StartsWith("<?")) {
                    nodeType = (int)XmlNodeType.ProcessingInstruction;
                    img = (int)NodeImage.PI - 1;
                } else if (trimmed.StartsWith("<!--")) {
                    nodeType = (int)XmlNodeType.Comment;
                    img = (int)NodeImage.Comment - 1;
                } else if (trimmed.StartsWith("<![CDATA[")) {
                    nodeType = (int)XmlNodeType.CDATA;
                    img = (int)NodeImage.CData - 1;
                } else if (trimmed.StartsWith("<")) {
                    nodeType = (int)XmlNodeType.Element;
                    img = (int)NodeImage.Element - 1;
                } else {
                    nodeType = (int)XmlNodeType.Text;
                    img = (int)NodeImage.Text - 1;
                }
            }

            XmlNode xn = null;
            XmlNode context = (target != null) ? target.Node : owner;

            if (this.nodeType == (int)XmlNodeType.Attribute) {
                int i = this.xml.IndexOf('=');
                if (i > 0) {
                    string name = this.xml.Substring(0, i).Trim();
                    XmlName qname = XmlHelpers.ParseName(context, name, XmlNodeType.Attribute);
                    xn = owner.CreateAttribute(qname.Prefix, qname.LocalName, qname.NamespaceUri);
                    string s = this.xml.Substring(i + 1).Trim();
                    if (s.Length > 2) {
                        xn.Value = s.Substring(1, s.Length - 2); // strip off quotes
                    }
                }

            } else {
                XmlNamespaceManager nsmgr = XmlHelpers.GetNamespaceScope(context);
                XmlParserContext pcontext = new XmlParserContext(owner.NameTable, nsmgr, null, XmlSpace.None);
                XmlTextReader r = null;
                if (this.xml != null) {
                    r = new XmlTextReader(this.xml, XmlNodeType.Element, pcontext);
                } else {
                    r = new XmlTextReader(this.stm, XmlNodeType.Element, pcontext);
                }
                r.WhitespaceHandling = WhitespaceHandling.Significant;

                // TODO: add multi-select support, so we can insert multiple nodes also.
                // And find valid nodes (for example, don't attempt to insert Xml declaration
                // if target node is not at the top of the document, etc).
                // For now we just favor XML elements over other node types.
                ArrayList list = new ArrayList();
                while (true){
                    XmlNode rn = owner.ReadNode(r);
                    if (rn == null)
                        break;
                    if (rn is XmlElement){
                        xn = rn;
                        NormalizeNamespaces((XmlElement)rn, nsmgr);
                        break;
                    }
                    list.Add(rn);
                }
                if (xn == null && list.Count>0)
                    xn = list[0] as XmlNode;
            }
            node.Node = xn;            
            
            if (!(xn is XmlAttribute)) {
                //view.Bind(node.Nodes, xn);
                view.Invalidate();
                if (xn is XmlElement){
                    if (node.Nodes.Count <= 1){
                        this.img = ((int)NodeImage.Leaf -1);
                    }
                }
            }
            return node;
        }

        static void NormalizeNamespaces(XmlElement node, XmlNamespaceManager mgr) {
            if (node.HasAttributes) {
                ArrayList toRemove = null;
                foreach (XmlAttribute a in node.Attributes) {
                    string prefix = null;
                    string nsuri = null;
                    if (a.Prefix == "xmlns") {
                        prefix = a.LocalName;
                        nsuri = a.Value;
                    } else if (a.LocalName == "xmlns") {
                        prefix = a.Prefix;
                        nsuri = a.Value;
                    }
                    if (prefix != null && mgr.LookupNamespace(prefix) == nsuri){
                        if (toRemove == null) toRemove = new ArrayList();
                        toRemove.Add(a);
                    }
                }
                if (toRemove != null) {
                    foreach (XmlAttribute a in toRemove) {
                        node.Attributes.Remove(a);
                    }
                }
            }
        }

        #region IDataObject Members

        public object GetData(Type format) {
            if (format == typeof(string)) {
                return this.xml;
            } else if (format == typeof(TreeData)) {
                return this;
            }
            return null;
        }

        public object GetData(string format) {
            if (format == DataFormats.Text || format == DataFormats.UnicodeText || 
                format == DataFormats.GetFormat("XML").Name) {
                return this.xml;
            } else if (format == DataFormats.GetFormat(typeof(TreeData).FullName).Name) {
                return this;
            }
            return null;
        }

        public object GetData(string format, bool autoConvert) {
            return GetData(format);
        }

        public bool GetDataPresent(Type format) {
            return (format == typeof(string) ||
                format == typeof(XmlTreeNode));
        }

        public bool GetDataPresent(string format) {
            return (format == DataFormats.Text || format == DataFormats.UnicodeText ||
                format == DataFormats.GetFormat("XML").Name ||
                format == DataFormats.GetFormat(typeof(TreeData).FullName).Name);
        }

        public bool GetDataPresent(string format, bool autoConvert) {
            return GetDataPresent(format);
        }

        public string[] GetFormats() {
            return new string[] {
                 DataFormats.Text,
                 DataFormats.UnicodeText,
                 DataFormats.GetFormat("XML").Name,
                 DataFormats.GetFormat(typeof(TreeData).FullName).Name
             };
        }

        public string[] GetFormats(bool autoConvert) {
            return GetFormats();
        }

        public void SetData(object data) {
            throw new NotImplementedException();
        }

        public void SetData(Type format, object data) {
            throw new NotImplementedException();
        }

        public void SetData(string format, object data) {
            throw new NotImplementedException();
        }

        public void SetData(string format, bool autoConvert, object data) {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class CutCommand : Command {
        XmlTreeNode node;
        TreeParent parent;
        int index;

        public CutCommand(XmlTreeView view, XmlTreeNode node) {
            this.node = node;
            this.parent = new TreeParent(view, view.Model.Document, node);
            index = node.Index;
        }

        public override bool IsNoop {
            get {
                return false;
            }
        }
        public override void Do() {
            TreeData.SetData(node);
            this.parent.Remove(node);
        }

        public override void Undo() {
            parent.Insert(this.index, InsertPosition.Before, node, true);
        }

        public override void Redo() {
            Do();
        }

    }

    public class PasteCommand : Command {
        XmlTreeNode target;
        //int index;
        XmlDocument doc;
        XmlTreeNode source;
        Command cmd;
        XmlTreeView view;
        InsertPosition position;

        public PasteCommand(XmlDocument doc, XmlTreeView view, InsertPosition position) {
            this.doc = doc;
            this.position = position;
            this.target = (XmlTreeNode)view.SelectedNode;
            if (this.target == null && view.TreeView.Nodes.Count > 0) {
                this.target = (XmlTreeNode)view.TreeView.Nodes[0];
            }
            if (this.target != null && this.target.NodeType != XmlNodeType.Element &&
                this.target.NodeType != XmlNodeType.Document) {
                position = InsertPosition.After;
            }
            this.view = view;
        }

        public override bool IsNoop {
            get {
                return false;
            }
        }
        public override void Do() {
            TreeData td = TreeData.GetData();
            if (td != null) {
                this.source = td.GetTreeNode(this.doc, this.target, this.view);
                InsertNode icmd = new InsertNode(this.view);
                icmd.Initialize(source, this.target, position);
                this.cmd = icmd;
                this.cmd.Do();
                if (this.source.Nodes.Count > 1) {
                    this.source.Expand();
                }
            }
        }

        public override void Undo() {
            if (this.cmd != null) {
                this.cmd.Undo();
            }
        }

        public override void Redo() {
            if (this.cmd != null) {
                this.cmd.Do();
            }
        }

    }

    public class XmlName {
        private string prefix;
        private string localName;
        private string namespaceUri;

        public string Prefix {
            get { return prefix; }
            set { prefix = value; }
        }

        public string LocalName {
            get { return localName; }
            set { localName = value; }
        }

        public string NamespaceUri {
            get { return namespaceUri; }
            set { namespaceUri = value; }
        }
    }

    public sealed class XmlHelpers {
        private XmlHelpers() { }

        public const string XmlnsUri = "http://www.w3.org/2000/xmlns/";
        public const string XmlUri = "http://www.w3.org/XML/1998/namespace";

        public static XmlName ParseName(XmlNode context, string name, XmlNodeType nt) {
            XmlName result = new XmlName();
            XmlConvert.VerifyName(name);
            int i = name.IndexOf(':');
            if (i>0){
                string prefix = result.Prefix = name.Substring(0,i);
                result.LocalName = name.Substring(i + 1);
                if (prefix == "xml") {
                    result.NamespaceUri = XmlUri; 
                } else if (prefix == "xmlns") {
                    result.NamespaceUri = XmlnsUri;
                } else {
                    result.NamespaceUri = context.GetNamespaceOfPrefix(prefix);
                }
            } else {
                result.LocalName = name;
                if (name == "xmlns") {
                    result.NamespaceUri = XmlnsUri;
                } else if (nt == XmlNodeType.Attribute) {
                    result.NamespaceUri = ""; // non-prefixed attributes are empty namespace by definition
                } else {
                    result.NamespaceUri = context.GetNamespaceOfPrefix("");
                }
            }
            return result;
        }

        public static XmlName ParseName(XmlNamespaceManager nsmgr, string name, XmlNodeType nt) {
            XmlName result = new XmlName();
            XmlConvert.VerifyName(name);
            int i = name.IndexOf(':');
            if (i > 0) {
                string prefix = result.Prefix = name.Substring(0, i);
                result.LocalName = name.Substring(i + 1);
                if (prefix == "xml") {
                    result.NamespaceUri = XmlUri;
                } else if (prefix == "xmlns") {
                    result.NamespaceUri = XmlnsUri;
                } else {
                    result.NamespaceUri = nsmgr.LookupNamespace(prefix);
                }
            } else {
                result.LocalName = name;
                if (name == "xmlns") {
                    result.NamespaceUri = XmlnsUri;
                } else if (nt == XmlNodeType.Attribute) {
                    result.NamespaceUri = ""; // non-prefixed attributes are empty namespace by definition
                } else {
                    result.NamespaceUri = nsmgr.LookupNamespace("");
                }
            }
            return result;
        }

        public static XmlNamespaceManager GetNamespaceScope(XmlNode context){
            XmlNameTable nt = context.OwnerDocument.NameTable;
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(nt);
            XmlNode parent = context;
            while (parent != null) {
                if (parent is XmlElement){
                    if (parent.Attributes != null) {
                        foreach (XmlAttribute a in parent.Attributes) {
                            if (a.NamespaceURI == XmlnsUri) {
                                string prefix = nt.Add(a.LocalName);
                                if (prefix == "xmlns") prefix = "";
                                if (!nsmgr.HasNamespace(prefix)) {
                                    nsmgr.AddNamespace(prefix, nt.Add(a.Value));
                                }
                            }
                        }
                    }
                }
                parent = parent.ParentNode;
            }
            return nsmgr;
        }

        public static bool MissingNamespace(XmlName name) {
            return !string.IsNullOrEmpty(name.Prefix) && string.IsNullOrEmpty(name.NamespaceUri) &&
                name.Prefix != "xmlns" && name.LocalName != "xmlns" && name.Prefix != "xml";
        }

        public static XmlAttribute GenerateNamespaceDeclaration(XmlElement context, XmlName name) {
            int count = 1;
            while (!string.IsNullOrEmpty(context.GetPrefixOfNamespace("uri:" + count))) {
                count++;
            }
            name.NamespaceUri = "uri:" + count;
            XmlAttribute xmlns = context.OwnerDocument.CreateAttribute("xmlns", name.Prefix, XmlHelpers.XmlnsUri);
            if (context.HasAttribute(xmlns.Name)) {
                // already have an attribute with this name! This is a tricky case where
                // user is deleting a namespace declaration.  We don't want to reinsert it
                // automatically in that case!
                return null;
            }
            xmlns.Value = name.NamespaceUri;
            return xmlns;
        }

        public static bool IsXmlnsNode(XmlNode node) {
            if (node == null) return false;
            return node.NodeType == XmlNodeType.Attribute &&
                (node.LocalName == "xmlns" || node.Prefix == "xmlns");
        }

    }
}