using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Windows.Forms;
using System.IO;

namespace XmlNotepad
{
    public partial class WebBrowserForm : Form
    {
        #region ctors
        public WebBrowserForm(Uri uri, string formName)
        {
            InitializeComponent();

            this.Text = formName;
            this.webBrowser1.Url = uri;
        }
        public WebBrowserForm(string formName)
        {
            InitializeComponent();
            this.Text = formName;
            this.webBrowser1.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(webBrowser1_DocumentCompleted);

        }

        public event EventHandler DocumentCompleted; 
        void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (this.DocumentCompleted!=null)
                this.DocumentCompleted(this, new EventArgs());
        }
        public string DocumentText
        {
            set { webBrowser1.DocumentText = value; }
        }
        public WebBrowserForm(Stream transform, Stream doc, string formName)
        {
            InitializeComponent();
            XmlDocument xdoc = new XmlDocument();
            xdoc.Load(doc);
            Run(transform, xdoc.CreateNavigator(), formName);
        }        public WebBrowserForm(Stream transform, XPathNavigator xdoc, string formName)
        {
            InitializeComponent();
            Run(transform, xdoc, formName);
        }
        #endregion

        private void Run(Stream transform, XPathNavigator xdoc, string formName)
        {
            string baseUri = xdoc.BaseURI;
            if (baseUri == null || baseUri == string.Empty)
            {
                baseUri = Environment.CurrentDirectory + Path.DirectorySeparatorChar + "temp.htm";
            }
            else
            {
                baseUri = Path.ChangeExtension(baseUri, ".htm");
            }
            Uri uri = new Uri(baseUri);
            XmlWriter writer = null;
            XslCompiledTransform xslt = new XslCompiledTransform();
            using (XmlTextReader rdr = new XmlTextReader(transform))
            {
                xslt.Load(rdr);
                rdr.Close();
                // this is a total hack. if we remove the try/catch we
                // almost always get an exception on the second try at
                // rendering the doc. with the try/catch, we've gone 16
                // attempts without an exception. go figure.
                try
                {
                    using (writer = XmlWriter.Create(uri.LocalPath, xslt.OutputSettings))
                    {
                        xslt.Transform(xdoc, writer);
                        writer.Close();
                    }
                }
                catch{}
            }
            this.Text = formName;
            this.webBrowser1.Url = uri;
        }
    }
}