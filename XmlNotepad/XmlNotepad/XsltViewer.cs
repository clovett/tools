using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Xsl;

namespace XmlNotepad {
    public partial class XsltViewer : UserControl {
        Uri baseUri;
        XslCompiledTransform xslt;
        XsltSettings settings;
        XmlUrlResolver resolver;
        ISite site;
        XmlCache model;
        XmlDocument doc;

        public XsltViewer(ISite site) {
            this.site = site;
            InitializeComponent();
            baseUri = new Uri(Application.StartupPath + Path.DirectorySeparatorChar);
            model = (XmlCache)site.GetService(typeof(XmlCache));
            this.SourceFileName.Text = model.XsltFileName;
            // now that we have a default value for the transform (if there was a PI)
            // wire up the text change event
            this.SourceFileName.TextChanged += new System.EventHandler(this.SourceFileName_TextChanged);

            doc = model.Document;
            xslt = new XslCompiledTransform();
            settings = new XsltSettings(true, false);
            resolver = new XmlUrlResolver();
        }

        public void DisplayXsltResults() {
            if (string.IsNullOrEmpty(this.SourceFileName.Text)) {
                return;
            }
            try {
                Uri xsltUri = new Uri(baseUri, this.SourceFileName.Text);
                Uri relativeUri = new Uri(baseUri, "temp.htm");
                string tempPath = relativeUri.AbsolutePath;

                xslt.Load(xsltUri.AbsolutePath, settings, resolver);
                using (XmlWriter writer = XmlWriter.Create(tempPath, xslt.OutputSettings)) {
                    xslt.Transform(doc, writer);
                }

                Uri uri = new Uri(tempPath);
                this.WebBrowser1.Url = uri;
            } catch (FileNotFoundException x) {
                MessageBox.Show(x.Message, "Error Transforming XML");
            } catch (ApplicationException x) {
                MessageBox.Show(x.Message, "Error Transforming XML");
            } catch (System.Exception x) {
                MessageBox.Show("System Exception of type " + x.GetType().Name + "\n" +
                    x.Message, "Error Transforming XML");
            }
        }

        private void SourceFileName_TextChanged(object sender, EventArgs e) {
            this.DisplayXsltResults();
        }

        private void BrowseButton_Click(object sender, EventArgs e) {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "XSLT files (*.xslt;*.xsl)|*.xslt;*.xsl|All files (*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK) {
                this.SourceFileName.Text = ofd.FileName;
            }
        }
    }
}
