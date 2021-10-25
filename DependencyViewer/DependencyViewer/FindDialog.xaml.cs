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
    /// Interaction logic for FindDialog.xaml
    /// </summary>
    public partial class FindDialog : Window {
        IFindTarget target;
        Settings settings;

        public FindDialog(IFindTarget target, Settings settings) {
            this.target = target;
            this.settings = settings;

            InitializeComponent();

            FindButton.Click += new RoutedEventHandler(FindButton_Click);
            FindButton.IsEnabled = SelectAllButton.IsEnabled = false;

            SelectAllButton.Click += new RoutedEventHandler(SelectAllButton_Click);

            FindCombo.AddHandler(TextBox.TextChangedEvent, new RoutedEventHandler(FindCombo_TextChanged));

            List<string> items = settings["FindStrings"] as List<string>;
            foreach (string s in items) {
                FindCombo.Items.Add(s);
            }
            if (FindCombo.Items.Count > 0) {
                FindCombo.SelectedIndex = 0;
            }
        }

        void Remember(string findString) {
            List<string> items = settings["FindStrings"] as List<string>;
            foreach (string s in items) {
                if (s == findString) {
                    items.Remove(s);
                    FindCombo.Items.Remove(s);
                    break;
                }
            }
            items.Insert(0, findString);
            FindCombo.Items.Insert(0, findString);
            while (items.Count > 10) {
                items.RemoveAt(10);
                FindCombo.Items.RemoveAt(10);
            }
            FindCombo.SelectedIndex = 0;
        }

        void FindCombo_TextChanged(object sender, RoutedEventArgs e) {
            bool hasText = !string.IsNullOrEmpty(FindCombo.Text);
            FindButton.IsEnabled = SelectAllButton.IsEnabled = hasText;
        }

        void SelectAllButton_Click(object sender, RoutedEventArgs e) {
            string toFind = this.FindCombo.Text;
            Remember(toFind);
            int c = target.SelectAll(toFind);
            if (c == 0) {
                ShowMessage("No match");
            } else {
                ShowMessage(string.Format("Selected {0} elements", c));
            }
        }

        void FindButton_Click(object sender, RoutedEventArgs e) {
            string toFind = this.FindCombo.Text;
            Remember(toFind);
            string s = target.FindNext(toFind);
            if (string.IsNullOrEmpty(s)) {
                ShowMessage("No match");
            } else {
                ShowMessage(s);
            }
        }

        void ShowMessage(string msg) {
            this.Message.Text = msg;
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            switch (e.Key) {
                case Key.Escape:
                    if (FindCombo.IsFocused) {
                        FindButton.Focus();
                    } else {
                        this.Close();
                    }
                    break;
                case Key.Enter:
                    break;
            }
            base.OnKeyDown(e);
        }
    }

    public interface IFindTarget {
        string FindNext(string toFind);
        string FindNext();
        int SelectAll(string toFind);
    }
}
