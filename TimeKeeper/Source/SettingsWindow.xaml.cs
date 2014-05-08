//-----------------------------------------------------------------------
// <copyright file="SettingsWindow.cs" company="Lovett Software">
//   (c) Lovett Software.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TimeKeeper
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

            Directory.Text = TimeKeeper.Properties.Settings.Default.Directory;
            Directory.TextChanged += new TextChangedEventHandler(Directory_TextChanged);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OnAccept(this, null);
            }
            else if (e.Key == Key.Escape)
            {
                OnCancel(this, null);
            }
            else
            {
                base.OnPreviewKeyDown(e);
            }
        }

        void Directory_TextChanged(object sender, TextChangedEventArgs e)
        {
            ButtonOk.IsEnabled = !string.IsNullOrEmpty(Directory.Text);
        }

        void OnAccept(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Directory.Text) && System.IO.Directory.Exists(Directory.Text))
            {
                TimeKeeper.Properties.Settings.Default.Directory = Directory.Text;
                TimeKeeper.Properties.Settings.Default.Save();
                DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show(string.Format("Directory '{0}' does not exist", Directory.Text), "No such directory",
                     MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}
