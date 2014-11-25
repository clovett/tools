using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace XmlNotepad
{
	/// <summary>
	/// Summary description for FormOptions.
	/// </summary>
	public class FormOptions : System.Windows.Forms.Form
	{
        private Settings settings;
        Font font;
        
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.FontDialog fontDialog1;
        private System.Windows.Forms.ColorDialog colorDialog1;
        private System.Windows.Forms.GroupBox groupBox1;
        private TableLayoutPanel tableLayoutPanel1;
        private Button buttonFont;
        private Label labelFont;
        private Label label1;
        private TableLayoutPanel tableLayoutPanel2;
        private Label label2;
        private Button buttonElementColor;
        private Label label5;
        private Button buttonCommentColor;
        private Label label3;
        private Button buttonAttributeColor;
        private Label label6;
        private Button buttonPiColor;
        private Label label4;
        private Button buttonTextColor;
        private Label label7;
        private Button buttonCDATAColor;
        private Label label8;
        private Button buttonBackgroundColor;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public FormOptions()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
		}

        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
        
            HelpProvider hp = this.Site.GetService(typeof(HelpProvider)) as HelpProvider;
            if (hp != null) {
                hp.SetHelpKeyword(this, "Options");
                hp.SetHelpNavigator(this, HelpNavigator.KeywordIndex);
            }
        }

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormOptions));
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.fontDialog1 = new System.Windows.Forms.FontDialog();
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.labelFont = new System.Windows.Forms.Label();
            this.buttonFont = new System.Windows.Forms.Button();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonElementColor = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.buttonCommentColor = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.buttonAttributeColor = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.buttonPiColor = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.buttonTextColor = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.buttonCDATAColor = new System.Windows.Forms.Button();
            this.label8 = new System.Windows.Forms.Label();
            this.buttonBackgroundColor = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonOK
            // 
            resources.ApplyResources(this.buttonOK, "buttonOK");
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            resources.ApplyResources(this.buttonCancel, "buttonCancel");
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Name = "buttonCancel";
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.tableLayoutPanel2);
            this.groupBox1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.labelFont, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.buttonFont, 2, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Name = "label1";
            // 
            // labelFont
            // 
            resources.ApplyResources(this.labelFont, "labelFont");
            this.labelFont.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.labelFont.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelFont.Name = "labelFont";
            // 
            // buttonFont
            // 
            resources.ApplyResources(this.buttonFont, "buttonFont");
            this.buttonFont.Name = "buttonFont";
            this.buttonFont.Click += new System.EventHandler(this.buttonFont_Click);
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
            this.tableLayoutPanel2.Controls.Add(this.label2, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.buttonElementColor, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.label5, 2, 0);
            this.tableLayoutPanel2.Controls.Add(this.buttonCommentColor, 3, 0);
            this.tableLayoutPanel2.Controls.Add(this.label3, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.buttonAttributeColor, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.label6, 2, 1);
            this.tableLayoutPanel2.Controls.Add(this.buttonPiColor, 3, 1);
            this.tableLayoutPanel2.Controls.Add(this.label4, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.buttonTextColor, 1, 2);
            this.tableLayoutPanel2.Controls.Add(this.label7, 2, 2);
            this.tableLayoutPanel2.Controls.Add(this.buttonCDATAColor, 3, 2);
            this.tableLayoutPanel2.Controls.Add(this.label8, 0, 3);
            this.tableLayoutPanel2.Controls.Add(this.buttonBackgroundColor, 1, 3);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Name = "label2";
            // 
            // buttonElementColor
            // 
            resources.ApplyResources(this.buttonElementColor, "buttonElementColor");
            this.buttonElementColor.Name = "buttonElementColor";
            this.buttonElementColor.Click += new EventHandler(buttonElementColor_Click);
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label5.Name = "label5";
            // 
            // buttonCommentColor
            // 
            resources.ApplyResources(this.buttonCommentColor, "buttonCommentColor");
            this.buttonCommentColor.Name = "buttonCommentColor";
            this.buttonCommentColor.Click += new EventHandler(buttonCommentColor_Click);
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label3.Name = "label3";
            // 
            // buttonAttributeColor
            // 
            resources.ApplyResources(this.buttonAttributeColor, "buttonAttributeColor");
            this.buttonAttributeColor.Name = "buttonAttributeColor";
            this.buttonAttributeColor.Click += new EventHandler(buttonAttributeColor_Click);
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label6.Name = "label6";
            // 
            // buttonPiColor
            // 
            resources.ApplyResources(this.buttonPiColor, "buttonPiColor");
            this.buttonPiColor.Name = "buttonPiColor";
            this.buttonPiColor.Click += new EventHandler(buttonPiColor_Click);
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label4.Name = "label4";
            // 
            // buttonTextColor
            // 
            resources.ApplyResources(this.buttonTextColor, "buttonTextColor");
            this.buttonTextColor.Name = "buttonTextColor";
            this.buttonTextColor.Click += new EventHandler(buttonTextColor_Click);
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label7.Name = "label7";
            // 
            // buttonCDATAColor
            // 
            resources.ApplyResources(this.buttonCDATAColor, "buttonCDATAColor");
            this.buttonCDATAColor.Name = "buttonCDATAColor";
            this.buttonCDATAColor.Click += new EventHandler(buttonCDATAColor_Click);
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            // 
            // buttonBackgroundColor
            // 
            resources.ApplyResources(this.buttonBackgroundColor, "buttonBackgroundColor");
            this.buttonBackgroundColor.Name = "buttonBackgroundColor";
            this.buttonBackgroundColor.Click += new EventHandler(buttonBackgroundColor_Click);
            // 
            // FormOptions
            // 
            resources.ApplyResources(this, "$this");
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "FormOptions";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
		#endregion

        public Settings Settings {
            get { return this.settings; }
            set { 
                this.settings = value;
                this.font = (Font)this.settings["Font"];
                this.labelFont.Text = font.Name + " " + font.SizeInPoints + " " + font.Style.ToString();
                Hashtable colors = (Hashtable)this.settings["Colors"];
                this.buttonElementColor.BackColor = (Color)colors["Element"];
                this.buttonCommentColor.BackColor = (Color)colors["Comment"];
                this.buttonCDATAColor.BackColor = (Color)colors["CDATA"];
                this.buttonAttributeColor.BackColor = (Color)colors["Attribute"];
                this.buttonPiColor.BackColor = (Color)colors["PI"];
                this.buttonTextColor.BackColor = (Color)colors["Text"];
                this.buttonBackgroundColor.BackColor = (Color)colors["Background"];
            }
        }
        private void buttonFont_Click(object sender, System.EventArgs e) {
            this.fontDialog1.Font = font;
            if (fontDialog1.ShowDialog() == DialogResult.OK){
                this.font = this.fontDialog1.Font;
                this.labelFont.Text = font.Name + " " + font.SizeInPoints + " " + font.Style.ToString();                
            }
        }

        private void buttonElementColor_Click(object sender, System.EventArgs e) {
            this.colorDialog1.Color = this.buttonElementColor.BackColor;
            if (colorDialog1.ShowDialog() == DialogResult.OK){
                this.buttonElementColor.BackColor = this.colorDialog1.Color;
            }
        }

        private void buttonAttributeColor_Click(object sender, System.EventArgs e) {
            this.colorDialog1.Color = this.buttonAttributeColor.BackColor;
            if (colorDialog1.ShowDialog() == DialogResult.OK){
                this.buttonAttributeColor.BackColor = this.colorDialog1.Color;
            }
        
        }

        private void buttonTextColor_Click(object sender, System.EventArgs e) {
            this.colorDialog1.Color = this.buttonTextColor.BackColor;
            if (colorDialog1.ShowDialog() == DialogResult.OK){
                this.buttonTextColor.BackColor = this.colorDialog1.Color;
            }
        
        }

        private void buttonCommentColor_Click(object sender, System.EventArgs e) {
            this.colorDialog1.Color = this.buttonCommentColor.BackColor;
            if (colorDialog1.ShowDialog() == DialogResult.OK){
                this.buttonCommentColor.BackColor = this.colorDialog1.Color;
            }
        
        }

        private void buttonPiColor_Click(object sender, System.EventArgs e) {
            this.colorDialog1.Color = this.buttonPiColor.BackColor;
            if (colorDialog1.ShowDialog() == DialogResult.OK){
                this.buttonPiColor.BackColor = this.colorDialog1.Color;
            }
        }

        private void buttonCDATAColor_Click(object sender, System.EventArgs e) {
            this.colorDialog1.Color = this.buttonCDATAColor.BackColor;
            if (colorDialog1.ShowDialog() == DialogResult.OK){
                this.buttonCDATAColor.BackColor = this.colorDialog1.Color;
            }
        }

        private void buttonBackgroundColor_Click(object sender, System.EventArgs e) {
            this.colorDialog1.Color = this.buttonBackgroundColor.BackColor;
            if (colorDialog1.ShowDialog() == DialogResult.OK){
                this.buttonBackgroundColor.BackColor = this.colorDialog1.Color;
            }
        }

        private void buttonOK_Click(object sender, System.EventArgs e) {
            this.settings["Font"] = this.font;
            
            Hashtable colors = (Hashtable)this.settings["Colors"];
            colors["Element"] = this.buttonElementColor.BackColor;
            colors["Comment"] = this.buttonCommentColor.BackColor;
            colors["CDATA"] = this.buttonCDATAColor.BackColor;
            colors["Attribute"] = this.buttonAttributeColor.BackColor;
            colors["PI"] = this.buttonPiColor.BackColor;
            colors["Text"] = this.buttonTextColor.BackColor;
            colors["Background"] = this.buttonBackgroundColor.BackColor;
            this.settings.OnChanged("Colors");

            this.Close();
        }
	}              

}
