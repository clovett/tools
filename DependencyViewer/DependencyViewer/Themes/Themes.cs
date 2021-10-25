
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace DependencyViewer {
    internal class Themes {

        internal static ResourceDictionary GetTheme(string name) {

            Uri uri = new Uri(@".\themes\" + name + ".xaml", UriKind.RelativeOrAbsolute);
            return Application.LoadComponent(uri) as ResourceDictionary;

        }
    }
}