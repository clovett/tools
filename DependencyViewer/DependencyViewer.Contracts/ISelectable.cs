using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Shapes;

namespace DependencyViewer {

    public interface ISelectable {
        bool Selected { get; set; }
    }

}