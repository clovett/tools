using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Microsoft.Xml;
using System.Diagnostics;

namespace XmlNotepad {
    public partial class FormSearch : Form {
        XmlTreeView view;
        XmlNamespaceManager nsmgr;
        XmlDocument doc;
        XmlNodeList list;
        IEnumerator selection;
        const int MaxRecentlyUsed = 15;

        public FormSearch() {
            this.SetStyle(ControlStyles.Selectable, true);
            this.KeyPreview = true;
            InitializeComponent();
            this.buttonFindNext.Click += new EventHandler(buttonFindNext_Click);
            this.comboBoxFind.TextChanged += new EventHandler(comboBoxFind_TextChanged);
            this.comboBoxFind.KeyDown += new KeyEventHandler(comboBoxFind_KeyDown);
        }

        public FormSearch(FormSearch old) : this() {
            if (old != null){
                foreach (string s in old.comboBoxFind.Items){
                    this.comboBoxFind.Items.Add(s);
                }
            }
        }

        void comboBoxFind_TextChanged(object sender, EventArgs e) {
            this.list = null;
            this.selection = null;
        }

        void buttonFindNext_Click(object sender, EventArgs e) {
            FindNext();
        }

        void FindNext() {
            if (this.list == null && this.doc != null) {
                string expr = this.Expression;
                if (!this.comboBoxFind.Items.Contains(expr)) {
                    this.comboBoxFind.Items.Add(expr);
                    if (this.comboBoxFind.Items.Count > MaxRecentlyUsed) {
                        this.comboBoxFind.Items.RemoveAt(0);
                    }
                }
                this.list = this.doc.SelectNodes(expr, this.nsmgr);
            }
            if (this.list != null) {
                if (selection == null || !this.selection.MoveNext()) {
                    selection = list.GetEnumerator();
                    this.selection.MoveNext();
                }                
                XmlNode node = this.selection.Current as XmlNode;
                if (node != null) {
                    XmlTreeNode tn = this.view.FindNode(node);
                    if (tn != null) {
                        this.view.SelectedNode = tn;
                    }
                }
            }
        }

        public XmlTreeView View {
            get { return this.view; }
            set { this.view = value;  }
        }

        public string Expression {
            get { return this.comboBoxFind.Text; }
            set { this.comboBoxFind.Text = value; }
        }

        void InitializeExpression() {
            XmlTreeNode node = this.view.SelectedNode as XmlTreeNode;
            this.doc = this.view.Model.Document;
            this.nsmgr = new XmlNamespaceManager(doc.NameTable);
            if (node != null) {
                XmlNode xnode = node.Node;
                if (xnode != null) {
                    XPathGenerator gen = new XPathGenerator();
                    string path = gen.GetXPath(xnode, nsmgr);
                    this.Expression = path;

                    this.dataTableNamespaces.Clear();
                    foreach (string prefix in nsmgr) {
                        if (!string.IsNullOrEmpty(prefix)) {
                            string uri = nsmgr.LookupNamespace(prefix);
                            this.dataTableNamespaces.Rows.Add(new object[] {
                                prefix, uri });
                        }
                    }
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            HandleKeyDown(e);
            if (!e.Handled) {
                base.OnKeyDown(e);
            }
        }

        void comboBoxFind_KeyDown(object sender, KeyEventArgs e) {
            HandleKeyDown(e);
        }

        void HandleKeyDown(KeyEventArgs e) {
            if (e.KeyCode == Keys.Escape) {
                this.Close();
                e.Handled = true;
            } else if (e.KeyCode == Keys.Enter) {
                FindNext();
                e.Handled = true;
            }
        }

        private void checkBoxXPath_CheckedChanged(object sender, EventArgs e) {
            if (checkBoxXPath.Checked && string.IsNullOrEmpty(this.comboBoxFind.Text)) {
                this.InitializeExpression();
            }
            dataGridView1.Visible = checkBoxXPath.Checked;
            checkBoxRegex.Checked = !checkBoxXPath.Checked;
        }

        private void checkBoxRegex_CheckedChanged(object sender, EventArgs e) {
            checkBoxXPath.Checked = !checkBoxRegex.Checked;
        }

    }
}