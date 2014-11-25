using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace XmlNotepad {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            MyForm form = new MyForm();
            form.Show();
            Application.DoEvents();
            if (args.Length > 0) {
                form.Open(args[0]);
            }
            Application.Run(form);
        }
    }

}