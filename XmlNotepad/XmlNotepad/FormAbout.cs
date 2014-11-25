#region Using directives

using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

#endregion

namespace XmlNotepad {
    class FormAbout : Form {
        public FormAbout() {
            InitializeComponent();

            this.labelVersion.Text = string.Format(this.labelVersion.Text, GetVersion());
        }

        string GetVersion(){
            string name = GetType().Assembly.FullName;
            string[] parts = name.Split(',');
            if (parts.Length>1){
                string version = parts[1].Trim();
                parts = version.Split('=');
                if (parts.Length>1){
                    return parts[1];
                }
            }
            return "1.0";
        }
        private Label label2;
        private Label labelVersion;
        private Label label1;
        private Label labelURL;

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormAbout));
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.buttonOK = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.labelVersion = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.labelURL = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(32, 56);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(35, 35);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // linkLabel1
            // 
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.BackColor = System.Drawing.Color.Transparent;
            this.linkLabel1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.linkLabel1.Location = new System.Drawing.Point(102, 126);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(140, 16);
            this.linkLabel1.TabIndex = 4;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "XML Tools Web Site";
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonOK.Location = new System.Drawing.Point(416, 150);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 1;
            this.buttonOK.Text = "&OK";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Location = new System.Drawing.Point(102, 23);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(198, 16);
            this.label2.TabIndex = 7;
            this.label2.Text = "Microsoft XML Notepad 2005";
            // 
            // labelVersion
            // 
            this.labelVersion.AutoSize = true;
            this.labelVersion.BackColor = System.Drawing.Color.Transparent;
            this.labelVersion.Location = new System.Drawing.Point(102, 47);
            this.labelVersion.Name = "labelVersion";
            this.labelVersion.Size = new System.Drawing.Size(85, 16);
            this.labelVersion.TabIndex = 8;
            this.labelVersion.Text = "Version {0}";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Location = new System.Drawing.Point(102, 75);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(350, 16);
            this.label1.TabIndex = 9;
            this.label1.Text = "© 2005 Microsoft Corporation.  All Rights Reserved.";
            // 
            // labelURL
            // 
            this.labelURL.AutoSize = true;
            this.labelURL.BackColor = System.Drawing.Color.Transparent;
            this.labelURL.Location = new System.Drawing.Point(102, 150);
            this.labelURL.Name = "labelURL";
            this.labelURL.Size = new System.Drawing.Size(288, 16);
            this.labelURL.TabIndex = 10;
            this.labelURL.Text = "http://www.gotdotnet.com/team/xmltools";
            this.labelURL.Visible = false;
            // 
            // FormAbout
            // 
            this.AcceptButton = this.buttonOK;
            this.BackgroundImage = global::XmlNotepad.Properties.Resources.texture2;
            this.CancelButton = this.buttonOK;
            this.ClientSize = new System.Drawing.Size(500, 184);
            this.Controls.Add(this.labelURL);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.labelVersion);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.linkLabel1);
            this.Controls.Add(this.pictureBox1);
            this.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "FormAbout";
            this.ShowInTaskbar = false;
            this.Text = "About XML Notepad";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.Button buttonOK;

        private void linkLabel1_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e) {
            string url = labelURL.Text;
            OpenUrl(url);
        }

        void OpenUrl(string url) {
            const int SW_SHOWNORMAL = 1;
            ShellExecute(this.Handle, "open", url, null, Application.StartupPath, SW_SHOWNORMAL);    
        }

        [DllImport("Shell32.dll", EntryPoint="ShellExecuteA",  
             SetLastError=true, CharSet=CharSet.Ansi, ExactSpelling=true,
             CallingConvention=CallingConvention.StdCall)]
        static extern int ShellExecute(IntPtr handle, string verb, string file, 
            string args, string dir, int show);
    }
}