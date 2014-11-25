using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace XmlNotepad {

    public enum Severity { None, Hint, Warning, Error }

    public delegate void NavigateEventHandler(object sender, Task task);

    public partial class TaskList : UserControl {

        public event NavigateEventHandler Navigate;
        public event KeyEventHandler GridKeyDown;

        ImageList imageList = new ImageList();
        IList<Task> tasks = new List<Task>();
        DataGridViewRow navigated;

        public TaskList() {
            InitializeComponent();
            Stream stream = this.GetType().Assembly.GetManifestResourceStream("XmlNotepad.Resources.errorlist.bmp");
            Image strip = Image.FromStream(stream);
            imageList.Images.AddStrip(strip);
            imageList.TransparentColor = Color.FromArgb(0, 255, 0);
            dataGridView1.DoubleClick += new EventHandler(dataGridView1_DoubleClick);
            dataGridView1.KeyDown += new KeyEventHandler(dataGridView1_KeyDown);
        }

        void dataGridView1_KeyDown(object sender, KeyEventArgs e) {
            if (GridKeyDown != null) GridKeyDown(sender, e);
        }

        public Task this[int index] {
            get {
                return this.tasks[index];
            }
            set {
                this.tasks[index] = value;
                DataGridViewRow row = this.dataGridView1.Rows[index];
                row.SetValues(GetValues(value));
            }
        }

        public ImageList Images {
            get { return this.imageList; }
            set { this.imageList = value; }
        }

        static object[] GetValues(Task t) {
            return new object[] { t.SeverityImage, t.Description, t.FileName, t.Line, t.Column };
        }

        public int Add(Task t) {
            this.tasks.Add(t);
            t.Parent = this;
            return this.dataGridView1.Rows.Add(GetValues(t));
        }

        public int GetTaskIndex(Task t) {
            for (int i = 0, n = this.tasks.Count; i < n; i++) {
                if (this.tasks[i] == t) {
                    return i;
                }
            }
            return -1;
        }

        public bool Remove(Task t) {
            int i = GetTaskIndex(t);
            if (i >= 0) {
                t.Parent = null;
                this.tasks.RemoveAt(i);
                if (navigated != null && navigated.Index == i)
                    navigated = null;
                this.dataGridView1.Rows.RemoveAt(i);
                return true;
            }
            return false;
        }

        public bool Contains(Task t) {
            return this.tasks.Contains(t);
        }

        public void Insert(int index, Task t) {
            this.tasks.Insert(index, t);
            t.Parent = this;
            this.dataGridView1.Rows.Insert(index, GetValues(t));
        }

        public void Clear() {
            navigated = null;
            foreach (Task t in this.tasks) {
                t.Parent = null;
            }
            this.tasks.Clear();
            this.dataGridView1.Rows.Clear();
        }

        public int Count {
            get { return this.tasks.Count; }
        }

        internal void OnTaskChanged(Task t) {
            int i = GetTaskIndex(t);
            if (i >= 0) {
                t.Parent = this;
                this.dataGridView1.Rows[i].SetValues(GetValues(t));
            }
        }

        public bool NavigateNextError() {
            int index = -1;
            if (navigated != null) {
                index = navigated.Index;
            }
            if (index + 1 >= this.dataGridView1.Rows.Count) {
                index = -1; // wrap around
            }
            if (index + 1 < this.dataGridView1.Rows.Count) {
                this.dataGridView1.ClearSelection();
                this.dataGridView1.Rows[index + 1].Selected = true;
                NavigateSelectedError();
                return true;
            } 
            return false;
        }

        public void NavigateSelectedError() {
            if (this.dataGridView1.SelectedRows.Count > 0) {
                DataGridViewRow row = this.dataGridView1.SelectedRows[0];
                Task task = this[row.Index];
                if (task != null && this.Navigate != null) {
                    Navigate(this, task);
                    navigated = row;
                }
            }
        }

        void dataGridView1_DoubleClick(object sender, EventArgs e) {
            NavigateSelectedError();
        }
    }

    public class Task {
        Severity severity;
        string description;
        string fileName;
        int line;
        int column;
        TaskList parent;
        object data;
        Image img;

        internal Task() {
        }

        public Task(Severity sev, string description, string fileName, int line, int column, object data) {
            this.severity = sev;
            this.description = description;
            this.fileName = fileName;
            this.line = line;
            this.column = column;
            this.data = data;
        }

        public TaskList Parent {
            get { return this.parent; }
            set { this.parent = value; }
        }

        [System.ComponentModel.Browsable(false)]
        public Severity Severity {
            get { return this.severity; }
            set {
                if (this.severity != value) {
                    this.severity = value;
                    OnChanged();
                }
            }
        }

        public Image SeverityImage {
            get {
                if (img == null) {
                    if (this.parent != null && this.parent.Images != null) {
                        return this.parent.Images.Images[(int)this.Severity];
                    }
                }
                return img; 
            }
            set {
                this.img = value;
            }
        }

        public string Description {
            get { return this.description; }
            set {
                if (this.description != value) {
                    this.description = value;
                    OnChanged();
                }
            }
        }
        public string FileName {
            get { return this.fileName; }
            set {
                if (this.fileName != value) {
                    this.fileName = value;
                    OnChanged();
                }
            }
        }
        public int Line {
            get { return this.line; }
            set {
                if (this.line != value) {
                    this.line = value;
                    OnChanged();
                }
            }
        }
        public int Column {
            get { return this.column; }
            set {
                if (this.column != value) {
                    this.column = value;
                    OnChanged();
                } 
            }
        }

        public object Data {
            get { return this.data; }
            set { this.data = value; }
        }

        private void OnChanged() {
            if (this.parent != null)
                this.parent.OnTaskChanged(this);
        }

        public override bool Equals(object obj) {
            if (obj is Task) {
                Task other = (Task)obj;
                return this.severity == other.severity && this.description == other.description &&
                    this.fileName == other.fileName && this.line == other.line && this.column == other.column;
            }
            return false;
        }

        public override int GetHashCode() {
            int hash1 = 0;
            int hash2 = 0;
            if (this.description != null) hash1 = this.description.GetHashCode();
            if (this.fileName != null) hash2 = this.fileName.GetHashCode();
            int hash = hash1 ^ hash2 ^ (int)this.severity ^ this.line ^ this.column;
            return hash;
        }

    }

    public class TaskHandler : ErrorHandler {
        TaskList list;
        System.Collections.Hashtable unique;
        IList<Task> errors;
        public TaskHandler(TaskList list){
            this.list = list;
        }

        public void Start() {
            this.unique = new System.Collections.Hashtable();
            this.errors = new List<Task>();
        }

        public override void HandleError(Severity sev, string reason, string filename, int line, int col, object data) {
            Task nt = new Task(sev, reason, filename, line, col, data);
            if (!unique.Contains(nt)) {
                unique[nt] = nt;
                errors.Add(nt); // preserve order
            }
        }

        public void Finish() {
            // Now merge the lists.
            IList<Task> copy = new List<Task>();
            for (int i = 0, n = list.Count; i < n; i++) {
                Task t = list[i];
                copy.Add(t);
            }
            // Remove tasks that are no longer reported.
            foreach (Task t in copy){
                if (!unique.Contains(t)) {
                    this.list.Remove(t);
                }                
            }
            // Remove any new tasks that have appeared.
            for (int i = 0, n = errors.Count; i < n; i++) {
                Task t = errors[i];
                if (!list.Contains(t)) {
                    this.list.Insert(i, t);
                }
            }
        }
    }
}
