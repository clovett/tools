using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel;

namespace XmlNotepad
{   
    /// <summary>
    /// Event arguments for validation exception.
    /// </summary>
    public class ValidationExceptionEventArgs : EventArgs
    {
        string validationExceptionMessage;
        XmlSeverityType validationExceptionSeverity;
        Exception validationExceptionException;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="msg">Text that describes exception</param>
        /// <param name="severity">Warning or Fatal</param>
        /// <param name="exception">Details of exception such as 
        /// LineNumber and LinePosition</param>
        public ValidationExceptionEventArgs(string msg, XmlSeverityType severity, Exception exception)
        {
            this.validationExceptionMessage = msg;
            this.validationExceptionSeverity = severity;
            this.validationExceptionException = exception;
        }
        /// <summary>
        /// Text that describes exception.
        /// </summary>
        public string ValidationExceptionMessage
        {
            get { return this.validationExceptionMessage; }
            set { this.validationExceptionMessage = value; }
        }
        /// <summary>
        /// Warning or Fatal exception severity
        /// </summary>
        public XmlSeverityType ValidationExceptionSeverity
        {
            get { return this.validationExceptionSeverity; }
            set { this.validationExceptionSeverity = value; }
        }
        /// <summary>
        /// Returns detailed information about the schema exception.
        /// </summary>
        public Exception ValidationExceptionException
        {
            get { return this.validationExceptionException; }
            set { this.validationExceptionException = value; }
        }
    }

    /// <summary>
    /// XmlCache wraps an XmlDocument and provides the stuff necessary for an "editor" in terms
    /// of watching for changes on disk, notification when the file has been reloaded, and keeping
    /// track of the current file name and dirty state.
    /// </summary>
    public class XmlCache : IDisposable
    {
        string filename;
        string xsltFilename;
        bool dirty;
        DomLoader loader = new DomLoader();
        XmlDocument doc;
        FileSystemWatcher watcher;
        int retries;
        Timer timer = new Timer();
        ISynchronizeInvoke sync;
        //string namespaceUri = string.Empty;
        SchemaCache schemaCache = new SchemaCache();
        Dictionary<XmlNode, XmlSchemaInfo> typeInfo;

        public event EventHandler FileChanged;
        public event EventHandler<ModelChangedEventArgs> ModelChanged;

        public XmlCache(ISynchronizeInvoke sync)
        {
            this.sync = sync;
            this.Document = new XmlDocument();
            this.timer.Tick += new EventHandler(Reload);
            this.timer.Interval = 1000;
            this.timer.Enabled = false;
        }

        ~XmlCache() {
            Dispose(false);
        }
        public Uri Location {
            get { return new Uri(this.filename); }
        }

        public string FileName
        {
            get { return this.filename; }
        }

        /// <summary>
        /// File path to (optionally user-specified) xslt file.
        /// </summary>
        public string XsltFileName
        {
            get {
                if (string.IsNullOrEmpty(this.xsltFilename)) {
                    this.xsltFilename = this.loader.XsltFileName;
                }
                return this.xsltFilename;
            }
            set { this.xsltFilename = value; }
        }
        public bool Dirty
        {
            get { return this.dirty; }
        }

        public XmlResolver SchemaResolver {
            get {
                return this.schemaCache.Resolver;
            }
        }

        public XPathNavigator Navigator
        {
            get
            {
                XPathDocument xdoc = new XPathDocument(this.filename);
                XPathNavigator nav = xdoc.CreateNavigator();
                return nav;
            }
        }

        public XmlDocument Document
        {
            get { return this.doc; }
            set
            {
                if (this.doc != null)
                {
                    this.doc.NodeChanged -= new XmlNodeChangedEventHandler(OnDocumentChanged);
                    this.doc.NodeInserted -= new XmlNodeChangedEventHandler(OnDocumentChanged);
                    this.doc.NodeRemoved -= new XmlNodeChangedEventHandler(OnDocumentChanged);
                }
                this.doc = value;
                if (this.doc != null)
                {
                    this.doc.NodeChanged += new XmlNodeChangedEventHandler(OnDocumentChanged);
                    this.doc.NodeInserted += new XmlNodeChangedEventHandler(OnDocumentChanged);
                    this.doc.NodeRemoved += new XmlNodeChangedEventHandler(OnDocumentChanged);
                }
            }
        }

        public Dictionary<XmlNode, XmlSchemaInfo> TypeInfoMap {
            get { return this.typeInfo; }
            set { this.typeInfo = value; }
        }

        public XmlSchemaInfo GetTypeInfo(XmlNode node) {
            if (this.typeInfo == null) return null;
            if (this.typeInfo.ContainsKey(node)) {
                return this.typeInfo[node];
            }
            return null;
        }

        private int GetIndexUnderParent(string refName, XmlNode prevSibling, int index) {
            if (prevSibling != null) {
                if (prevSibling.Name == refName) {
                    index++;
                }
                return GetIndexUnderParent(refName, prevSibling.PreviousSibling, index);
            }
            return index;
        }

        /// <summary>
        /// Provides schemas used for validation.
        /// </summary>
        public SchemaCache SchemaCache
        {
            get { return this.schemaCache; }
            set { this.schemaCache = value; }
        }
        
        /// <summary>
        /// Loads an instance of xml.
        /// Load updated to handle validation when instance doc refers to schema.
        /// </summary>
        /// <param name="file">Xml instance document</param>
        /// <returns></returns>
        public void Load(string file)
        {
            this.Clear();
            loader = new DomLoader();
            StopFileWatch();
            this.filename = file;
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ProhibitDtd = false;
            settings.ValidationEventHandler += new ValidationEventHandler(OnValidationEvent);
            XmlReader reader = XmlReader.Create(file, settings);            
            using (reader) {
                this.Document = loader.Load(reader);
            }
            this.dirty = false;
            // calling this event will cause the XmlTreeView to populate
            FireModelChanged(ModelChangeType.Reloaded, this.doc);
            StartFileWatch();
        }

        public LineInfo GetLineInfo(XmlNode node) {
            return loader.GetLineInfo(node);
        }

        void OnValidationEvent(object sender, ValidationEventArgs e)
        {
            // todo: log errors in error list window.
        }                

        public void Reload()
        {
            string filename = this.filename;
            Clear();
            Load(filename);
        }

        public void Clear()
        {
            this.Document = new XmlDocument();
            StopFileWatch();
            this.filename = null;
            FireModelChanged(ModelChangeType.Reloaded, this.doc);
        }

        public void Save()
        {
            Save(this.filename);
        }

        public void Save(string name)
        {
            try
            {
                StopFileWatch();
                doc.Save(name);
                this.dirty = false;
                this.filename = name;
                FireModelChanged(ModelChangeType.Saved, this.doc);
            }
            finally
            {
                StartFileWatch();
            }
        }

        void StopFileWatch()
        {
            if (this.watcher != null)
            {
                this.watcher.Dispose();
                this.watcher = null;
            }
        }
        private void StartFileWatch()
        {
            if (this.filename != null && File.Exists(this.filename))
            {
                string dir = Path.GetDirectoryName(this.filename) + "\\";
                this.watcher = new FileSystemWatcher(dir, "*.*");
                this.watcher.Changed += new FileSystemEventHandler(watcher_Changed);
                this.watcher.Renamed += new RenamedEventHandler(watcher_Renamed);
                this.watcher.EnableRaisingEvents = true;
            }
            else
            {
                StopFileWatch();
            }
        }
        void StartReload(object sender, EventArgs e)
        {
            // Apart from retrying, the timer has the nice side effect of also 
            // collapsing multiple file system events into one timer event.
            retries = 3;
            timer.Enabled = true;
            timer.Start();
        }

        void Reload(object sender, EventArgs e)
        {
            try
            {
                // Test if we can open the file (it might still be locked).
                FileStream fs = new FileStream(this.filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                fs.Close();

                timer.Enabled = false;
                FireFileChanged();

            }
            finally
            {
                retries--;
                if (retries == 0)
                {
                    timer.Enabled = false;
                }
            }
        }

        private void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed &&
                IsSamePath(this.filename, e.FullPath))
            {
                sync.BeginInvoke(new EventHandler(StartReload), new object[] { this, new EventArgs() });
            }
        }

        private void watcher_Renamed(object sender, RenamedEventArgs e)
        {
            if (IsSamePath(this.filename, e.OldFullPath))
            {
                StopFileWatch();
                this.filename = e.FullPath;
                StartFileWatch();
                sync.BeginInvoke(new EventHandler(StartReload), new object[] { this, new EventArgs() });
            }
        }

        static bool IsSamePath(string a, string b)
        {
            return string.Compare(a, b, true) == 0;
        }

        void FireFileChanged()
        {
            if (this.FileChanged != null)
            {
                FileChanged(this, new EventArgs());
            }
        }

        void FireModelChanged(ModelChangeType t, XmlNode node)
        {
            if (this.ModelChanged != null)
                this.ModelChanged(this, new ModelChangedEventArgs(t, node));
        }

        private void OnDocumentChanged(object sender, XmlNodeChangedEventArgs e)
        {
            // initialize t
            ModelChangeType t = ModelChangeType.NodeChanged;

            this.dirty = true;

            if (XmlHelpers.IsXmlnsNode(e.NewParent) || XmlHelpers.IsXmlnsNode(e.Node)) {
                // we flag a namespace change whenever an xmlns attribute changes.
                t = ModelChangeType.NamespaceChanged;
                XmlNode node = e.Node;
                if (e.Action == XmlNodeChangedAction.Remove) {
                    node = e.OldParent; // since node.OwnerElement link has been severed!
                }
                FireModelChanged(t, node);
            } else {
                switch (e.Action) {
                    case XmlNodeChangedAction.Change:
                        t = ModelChangeType.NodeChanged;
                        break;
                    case XmlNodeChangedAction.Insert:
                        t = ModelChangeType.NodeInserted;
                        break;
                    case XmlNodeChangedAction.Remove:
                        t = ModelChangeType.NodeRemoved;
                        break;
                }
                FireModelChanged(t, e.Node);
            }
        }

        public void Dispose() {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing) {
            if (timer != null) {
                timer.Dispose();
                timer = null;
            }
            StopFileWatch();
            GC.SuppressFinalize(this);
        }
    }

    public enum ModelChangeType
    {
        Reloaded,
        Saved,
        NodeChanged,
        NodeInserted,
        NodeRemoved,
        NamespaceChanged
    }

    public class ModelChangedEventArgs : EventArgs
    {
        ModelChangeType type;
        XmlNode node;

        public ModelChangedEventArgs(ModelChangeType t, XmlNode node)
        {
            this.type = t;
            this.node = node;
        }

        public XmlNode Node {
            get { return node; }
            set { node = value; }
        }

        public ModelChangeType ModelChangeType
        {
            get { return this.type; }
            set { this.type = value; }
        }

    }
}