using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DependencyViewer {
    /// <summary>
    /// Interaction logic for NamespaceDialog.xaml
    /// </summary>
    public partial class NamespaceDialog : Window {
        IList<NamespaceInfo> list;

        public readonly static RoutedUICommand OkCommand;

        static NamespaceDialog() {
            OkCommand = new RoutedUICommand("Ok", "OkCommand", typeof(NamespaceDialog));
        }

        public NamespaceDialog() {
            InitializeComponent();            
        }

        public IList<NamespaceInfo> Namespaces {
            get { return list; }
            set {
                list = value;
                List.ItemsSource = list;                
            }
        }

        public void OnOkClick(object sender, RoutedEventArgs e) {
            this.DialogResult = true;
        }

        public void OnCancelClick(object sender, RoutedEventArgs e) {
            this.DialogResult = false;
            this.Close();
        }
    }

    public class NamespaceInfo {
        bool selected; 
        string nsuri;

        public bool Checked {
            get { return selected; }
            set { selected = value; }
        }

        public string Namespace {
            get { return nsuri; }
            set { nsuri = value; }
        }

        public NamespaceInfo() { 
        }

        public NamespaceInfo(bool selected, string name) {
            this.selected = selected;
            this.nsuri = name;
        }
    }
}
