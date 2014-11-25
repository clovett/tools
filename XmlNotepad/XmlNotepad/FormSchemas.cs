using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml.Schema;
using System.Xml;
using System.Diagnostics;

namespace XmlNotepad {
    /// <summary>
    /// FormSchemas provides a simple grid view interface on top of the SchemaCache and provides
    /// a way to add and remove schemas from the cache.  You can also "disable" certain schemas
    /// from being used in validation by checking the disabled checkbox next to the schema.
    /// All this is persisted in the Settings class so it's remembered across sessions.
    /// </summary>
    public partial class FormSchemas : Form {
        SchemaCache cache;
        UndoManager undoManager = new UndoManager(1000);
        bool inUndoRedo;

        public FormSchemas() {
            InitializeComponent();
            DataGridViewBrowseCell template = new DataGridViewBrowseCell(this.openFileDialog1, this.undoManager);
            this.columnBrowse.CellTemplate = template;
            this.dataGridView1.KeyDown += new KeyEventHandler(dataGridView1_KeyDown);
            this.undoManager.StateChanged += new EventHandler(undoManager_StateChanged);
        }

        void undoManager_StateChanged(object sender, EventArgs e) {
            this.UpdateMenuState();
        }

        protected override void OnLoad(EventArgs e) {            
            HelpProvider hp = this.Site.GetService(typeof(HelpProvider)) as HelpProvider;
            if (hp != null) {
                hp.SetHelpKeyword(this, "Schemas");
                hp.SetHelpNavigator(this, HelpNavigator.KeywordIndex);
            }
            UpdateMenuState();
            LoadSchemas();
            this.dataTableSchemas.ColumnChanging += new DataColumnChangeEventHandler(dataTableSchemas_ColumnChanging);
            this.dataTableSchemas.RowChanging += new DataRowChangeEventHandler(dataTableSchemas_RowChanging);
        }

        void dataTableSchemas_RowChanging(object sender, DataRowChangeEventArgs e) {
            if (e.Action == DataRowAction.Add) {
                SchemaDialogEditCommand cmd = undoManager.Peek() as SchemaDialogEditCommand;
                if (cmd != null) {
                    cmd.IsNewRow = true;
                }
                return;
            }
        }

        void dataTableSchemas_ColumnChanging(object sender, DataColumnChangeEventArgs e) {
            DataRow row = e.Row;
            if (e.Column.Ordinal == 2 && row.RowState != DataRowState.Detached) {
                string filename = e.ProposedValue == null ? "" : e.ProposedValue.ToString();
                if (!inUndoRedo) {
                    Push(new SchemaDialogEditCommand(this.dataGridView1, row, e.ProposedValue as string));
                }
            }
        }

        void LoadSchemas() {
            if (this.cache == null) {
                if (this.Site != null) {
                    this.cache = (SchemaCache)this.Site.GetService(typeof(SchemaCache));
                }
            }
            this.dataTableSchemas.Clear();
            if (this.cache != null) {
                foreach (CacheEntry e in this.cache.GetSchemas()) {
                    Uri uri = e.Location;
                    string filename = uri.IsFile ? uri.LocalPath : uri.AbsoluteUri;
                    this.dataTableSchemas.Rows.Add(new object[] { e.Disabled, e.TargetNamespace, filename });
                }
                this.dataTableSchemas.AcceptChanges();
            }

        }

        protected override void OnClosing(CancelEventArgs e) {
            if (this.undoManager.CanUndo) {
                if (MessageBox.Show(this, SR.DiscardChanges, SR.DiscardChangesCaption, MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) == DialogResult.Cancel) {
                    e.Cancel = true;
                }
            }
        }

        private void buttonOk_Click(object sender, EventArgs e) {
            this.dataTableSchemas.AcceptChanges();
            if (Commit()) {
                this.DialogResult = DialogResult.OK;
            }
        }

        bool Commit() {
            int i = 0;
            IList<CacheEntry> oldList = cache.GetSchemas();
            XmlResolver resolver = this.cache.Resolver;
            foreach (DataRow row in this.dataTableSchemas.Rows) {
                string filename = row[2] as string;
                if (!string.IsNullOrEmpty(filename)) {
                    CacheEntry ce = this.cache.FindSchemaByUri(filename);
                    bool isNew = (ce == null);
                    if (ce == null || ce.Schema == null) {
                        try {
                            XmlSchema s = resolver.GetEntity(new Uri(filename), "", typeof(XmlSchema)) as XmlSchema;
                            if (ce == null) {
                                ce = this.cache.Add(s);
                            } else {
                                ce.Schema = s;
                            }
                        } catch (Exception e) {
                            DialogResult rc = MessageBox.Show(this, string.Format(SR.SchemaLoadError, filename, e.Message),
                                SR.SchemaError, MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                            if (rc == DialogResult.Cancel) {
                                this.dataGridView1.Rows[i].Selected = true;
                                return false;
                            }
                        }
                    } 
                    if (!isNew){
                        oldList.Remove(ce);
                    }
                    if (!row.IsNull(0)) {
                        ce.Disabled = (bool)row[0];
                    }
                }                
                i++;
            }            
            // Remove schemas from the cache that have been removed from the grid view.
            foreach (CacheEntry toRemove in oldList) {
                cache.Remove(toRemove);
            }
            this.undoManager.Clear();
            return true;
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e) {
            foreach (DataGridViewRow row in this.dataGridView1.Rows) {
                row.Selected = true;
            }
            Push(new SchemaDialogCutCommand(this.dataGridView1));
        }

        private void addSchemasToolStripMenuItem_Click(object sender, EventArgs e) {
            this.openFileDialog1.Multiselect = true;
            if (this.openFileDialog1.ShowDialog(this) == DialogResult.OK) {
                Push(new SchemaDialogAddFiles(this.dataGridView1, this.openFileDialog1.FileNames));                
            }
        }

        void Push(Command cmd) {
            this.inUndoRedo = true;
            undoManager.Push(cmd);
            UpdateMenuState();
            this.inUndoRedo = false;
        }

        void dataGridView1_KeyDown(object sender, KeyEventArgs e) {
            bool isControl = e.Modifiers == Keys.Control;
            bool isGridEditing = this.dataGridView1.EditingControl != null;
            switch (e.KeyCode & ~Keys.Modifiers){
                case Keys.V:
                    if (isControl && !isGridEditing) {
                        pasteToolStripMenuItem_Click(this, e);
                        e.Handled = true;
                    }
                    break;
                case Keys.X:
                    if (isControl && !isGridEditing) {
                        cutToolStripMenuItem_Click(this, e);
                        e.Handled = true;
                    }
                    break;
                case Keys.C:
                    if (isControl && !isGridEditing) {
                        copyToolStripMenuItem_Click(this, e);
                        e.Handled = true;
                    }
                    break;
                case Keys.Delete:
                    if (!isGridEditing) {
                        deleteToolStripMenuItem_Click(this, e);
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e) {            
            SchemaDialogCutCommand cmd = new SchemaDialogCutCommand(this.dataGridView1);
            Push(cmd);
            if (!string.IsNullOrEmpty(cmd.Clip)) {
                Clipboard.SetText(cmd.Clip);
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e) {
            SchemaDialogCutCommand cmd = new SchemaDialogCutCommand(this.dataGridView1);
            StringBuilder clip = new StringBuilder();
            cmd.ProcessSelectedRows(delegate(DataRow row) {
               cmd.AddEscapedUri(clip, row[2] as string);
            });
            if (clip.Length > 0) {
                Clipboard.SetText(clip.ToString());
            }
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e) {
            string text = Clipboard.GetText();
            if (!string.IsNullOrEmpty(text)) {
                Push(new SchemaDialogAddFiles(this.dataGridView1,
                    text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)));
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e) {
            Push(new SchemaDialogCutCommand(this.dataGridView1));        
        }

        void UpdateMenuState() {
            this.undoToolStripMenuItem.Enabled = this.undoManager.CanUndo;
            this.redoToolStripMenuItem.Enabled = this.undoManager.CanRedo;
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e) {
            inUndoRedo = true;
            this.undoManager.Undo();
            UpdateMenuState();
            inUndoRedo = false;
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e) {
            inUndoRedo = true;
            this.undoManager.Redo();
            UpdateMenuState();
            inUndoRedo = false;
        }
    }

    public class DataGridViewBrowseCell : DataGridViewButtonCell {
        OpenFileDialog fd;
        UndoManager undoManager;

        public DataGridViewBrowseCell(OpenFileDialog od, UndoManager undoManager) {
            this.UseColumnTextForButtonValue = true;
            fd = od;
            this.undoManager = undoManager;
        }

        public DataGridViewBrowseCell() {
            this.UseColumnTextForButtonValue = true;
        }

        public override object Clone() {
            return new DataGridViewBrowseCell(this.fd, this.undoManager);
        }

        protected override void OnClick(DataGridViewCellEventArgs e) {
            DataGridViewRow row = this.OwningRow;
            DataGridView view = row.DataGridView;
            string filename = row.Cells[2].Value as string;
            if (!string.IsNullOrEmpty(filename)) {
                Uri uri = new Uri(filename);
                if (uri.IsFile) {
                    fd.FileName = uri.LocalPath;
                }
            }
            fd.Multiselect = false;
            if (fd.ShowDialog() == DialogResult.OK) {
                DataRowView dv = row.DataBoundItem as DataRowView;
                DataRow dr = dv != null ? dv.Row : null;
                if (dr == null) {
                    undoManager.Push(new SchemaDialogAddFiles(view, fd.FileNames));
                } else {
                    dr[2] = fd.FileName;
                }
            }
        }
    }

    class SchemaDialogCommand : Command {

        DataGridView view;
        DataSet dataSet;

        public SchemaDialogCommand(DataGridView view) {
            this.view = view;
            this.dataSet = view.DataSource as DataSet;
        }

        public DataGridView View {
            get { return view; }
        }

        public DataSet DataSet {
            get { return dataSet; }
        }

        public DataTable Table {
            get { return dataSet.Tables[0]; }
        }

        public override bool IsNoop {
            get {                
                return false;
            }
        }

        public void AcceptChanges() {
            dataSet.AcceptChanges();
        }

        public void InvalidateRow(DataRow row) {
            foreach (DataGridViewRow vr in this.view.Rows) {
                DataRowView dv = vr.DataBoundItem as DataRowView;
                DataRow dr = dv != null ? dv.Row : null;
                if (dr == row) {
                    View.InvalidateRow(vr.Index);
                }
            }
        }

        protected void SelectRows(IList<DataRow> list) {
            foreach (DataGridViewRow vr in this.view.Rows) {
                bool select = false;                
                DataRowView dv = vr.DataBoundItem as DataRowView;
                DataRow dr = dv != null ? dv.Row : null;
                if (dr != null && dr.RowState != DataRowState.Detached && dr.RowState != DataRowState.Deleted) {
                    if (list.Contains(dr)) {
                        select = true;
                    }
                }
                vr.Selected = select;
            }
        }


        public bool IsSamePath(string a, string b) {
            try {
                Uri ua = new Uri(a);
                Uri ub = new Uri(b);
                return ua == ub;
            } catch {
                return a == b;
            }
        }

        public DataRow FindExistingRow(string schema) {
            DataSet ds = this.dataSet;
            DataTable table = this.Table;
            DataRow empty = null;
            foreach (DataRow row in table.Rows) {
                string path = row[2] as string;
                if (string.IsNullOrEmpty(path)) {
                    empty = row;
                } else if (IsSamePath(path, schema)) {
                    // already there!!
                    return row;
                }
            }
            return empty;
        }

        public DataRow InsertRow(string schema) {
            XmlSchema s = LoadSchema(schema);
            if (s != null) {
                DataRow dr = this.Table.Rows.Add(new object[] { false, s.TargetNamespace, schema });
                return dr;
            }
            return null;
        }
        
        internal XmlSchema LoadSchema(string filename) {
            try {
                if (string.IsNullOrEmpty(filename)) return null;
                return XmlSchema.Read(new XmlTextReader(filename, new NameTable()), null);
            } catch (Exception ex) {
                MessageBox.Show(this.View, string.Format(SR.SchemaLoadError, filename, ex.Message), 
                    SR.SchemaError, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return null;
        }

        public delegate void DataRowHandler(DataRow row);

        public void ProcessSelectedRows(DataRowHandler handler) {
            this.view.SuspendLayout();
            foreach (DataGridViewRow row in this.view.SelectedRows) {
                DataRowView dv = row.DataBoundItem as DataRowView;
                DataRow dr = dv != null ? dv.Row : null;
                if (dr != null) {
                    handler(dr);
                }
            }
            this.view.ResumeLayout();
        }

        public void AddEscapedUri(StringBuilder sb, string filename) {
            if (!string.IsNullOrEmpty(filename)) {
                try {
                    Uri uri = new Uri(filename);
                    if (sb.Length > 0) {
                        sb.Append("\r\n");
                    }
                    sb.Append(uri.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped));
                } catch {
                }
            }
        }

        public void AddRows(IList<DataRow> rows) {
            DataTable table = this.Table;
            foreach (DataRow row in rows) {
                table.Rows.Add(row);
            }
            this.AcceptChanges();
            SelectRows(rows);
        }

        public DataRow RemoveRow(DataRow row) {
            DataRow newRow = Table.NewRow();
            newRow.ItemArray = row.ItemArray;            
            row.Delete();
            return newRow;
        }

        public IList<DataRow> RemoveRows(IList<DataRow> rows) {
            IList<DataRow> result = new List<DataRow>();
            DataTable table = this.Table;
            foreach (DataRow row in rows) {
                // Have to create a new row so we can undo this delete operation.
                // You cannot reuse a deleted row unfortunately.
                if (row.RowState == DataRowState.Detached)
                    continue;
                result.Add(RemoveRow(row));                
            }
            this.AcceptChanges();
            return result;
        }

        public override void Do() {
        }

        public override void Undo() {
        }

        public override void Redo() {
        }
    }

    class SchemaDialogEditCommand : SchemaDialogCommand {
        DataRow row;
        string newSchema;
        string oldSchema;
        XmlSchema schema;
        string oldNamespace;
        bool isNewRow;

        public SchemaDialogEditCommand(DataGridView view, DataRow row, string newSchema) : base(view) {
            this.newSchema = newSchema;
            this.row = row;
            if (!row.IsNull(2)) {
                oldSchema = row[2].ToString();
            }
            if (!row.IsNull(1)) {
                oldNamespace = row[1].ToString();
            }
            schema = LoadSchema(newSchema); // make sure we can load it!
        }


        public override bool IsNoop {
            get {
                return newSchema == oldSchema;
            }
        }

        public bool IsNewRow {
            get { return isNewRow; }
            set { isNewRow = value; }
        }

        public override void Do() {
            row[1] = schema == null ? "" : schema.TargetNamespace;
            InvalidateRow(row);
        }

        public override void Undo() {
            if (IsNewRow) {
                row = RemoveRow(row);
                this.AcceptChanges();
            } else {
                row[2] = oldSchema;
                row[1] = oldNamespace;
                InvalidateRow(row);
            }
        }

        public override void Redo() {
            if (IsNewRow) {
                Table.Rows.Add(row);
                this.AcceptChanges();
            } else {
                row[2] = newSchema;
                row[1] = schema == null ? "" : schema.TargetNamespace;
                InvalidateRow(row);
            }
        }
    }

    class SchemaDialogCutCommand : SchemaDialogCommand {
        string clip;
        IList<DataRow> deletedRows = new List<DataRow>();

        public SchemaDialogCutCommand(DataGridView view)
            : base(view) {
        }

        public string Clip {
            get { return clip; }
            set { clip = value; }
        }

        public override void Do() {
            // This builds what should go on clipboard, but doesn't actually
            // mess with the clipboard - the caller does that so this command
            // can also be used for plain delete oepration.
            StringBuilder sb = new StringBuilder();
            DataTable table = this.Table;
            ProcessSelectedRows(delegate(DataRow row) {
                string uri = row[2] as string;
                AddEscapedUri(sb, uri);
                if (!string.IsNullOrEmpty(uri)) {
                    DataRow newRow = table.NewRow();
                    newRow.ItemArray = row.ItemArray;
                    deletedRows.Add(newRow);
                }
                row.Delete();
            });
            this.AcceptChanges();
            this.clip = sb.ToString();
        }

        public override void Undo() {
            AddRows(deletedRows);
        }

        public override void Redo() {
            deletedRows = RemoveRows(deletedRows);            
        }
    }

    class SchemaDialogAddFiles : SchemaDialogCommand {
        string[] files;
        IList<DataRow> newRows = new List<DataRow>();

        public SchemaDialogAddFiles(DataGridView view, string[] files) : base(view)  {
            this.files = files;
        }

        public override void Do() {
            List<DataRow> list = new List<DataRow>();
            foreach (string file in this.files) {
                try {
                    Uri uri = new Uri(file);
                    string path = uri.IsFile ? uri.LocalPath : uri.AbsoluteUri;
                    DataRow row = FindExistingRow(path);
                    if (row != null) {
                        XmlSchema s = LoadSchema(path);
                        if (s != null) {
                            row[1] = s.TargetNamespace;
                            row[2] = path;
                        }
                    } else {
                        row = InsertRow(path);
                        if (row != null) {
                            newRows.Add(row);
                            list.Add(row);
                        }
                    }
                    list.Add(row);
                } catch {
                    Trace.WriteLine("Bad file name:" + file);
                }
            }
            this.AcceptChanges();
            SelectRows(list);
        }

        public override void Undo() {
            newRows = RemoveRows(newRows);
        }

        public override void Redo() {
            AddRows(newRows);
        }
    }

}