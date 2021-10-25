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
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : Window {
        Settings settings;

        public Options(Settings settings) {
            InitializeComponent();
            this.settings = settings;

            this.OkButton.Click += new RoutedEventHandler(OkButton_Click);
            this.CancelButton.Click += new RoutedEventHandler(CancelButton_Click);
            this.LayerSeparationSlider.Tag = this.LayerSeparationSlider.Value = (double)this.settings["LayerSeparation"];
            this.LayerSeparationSlider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(LayerSeparationSlider_ValueChanged);
            this.NodeSeparationSlider.Tag = this.NodeSeparationSlider.Value = (double)this.settings["NodeSeparation"];
            this.NodeSeparationSlider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(NodeSeparationSlider_ValueChanged);
            object ratio = settings["AspectRatio"];
            this.AspectRatioCheckBox.Tag = ratio;
            this.AspectRatioCheckBox.Checked += new RoutedEventHandler(AspectRatioCheckBox_Checked);
            this.AspectRatioCheckBox.Unchecked += new RoutedEventHandler(AspectRatioCheckBox_Unchecked);
            this.AspectRatioCheckBox.IsChecked = (ratio != null) ? (bool)ratio : false;
            this.EdgeThicknessSlider.Tag = this.EdgeThicknessSlider.Value = (double)this.settings["EdgeThickness"];
            this.EdgeThicknessSlider.ValueChanged += new RoutedPropertyChangedEventHandler<double>(EdgeThicknessSlider_ValueChanged);
        }

        void EdgeThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (LiveCheckBox.IsChecked == true)
            {
                settings["EdgeThickness"] = EdgeThicknessSlider.Value;
            }
        }

        void AspectRatioCheckBox_Unchecked(object sender, RoutedEventArgs e) {
            if (LiveCheckBox.IsChecked == true)
            {
                settings["AspectRatio"] = false;
            }
        }

        void AspectRatioCheckBox_Checked(object sender, RoutedEventArgs e) {
            if (LiveCheckBox.IsChecked == true)
            {
                settings["AspectRatio"] = true;
            }
        }

        void NodeSeparationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (LiveCheckBox.IsChecked == true)
            {
                settings["NodeSeparation"] = Math.Floor(NodeSeparationSlider.Value);
            }
        }

        void LayerSeparationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (LiveCheckBox.IsChecked == true)
            {
                settings["LayerSeparation"] = Math.Floor(LayerSeparationSlider.Value);
            }
        }

        void CancelButton_Click(object sender, RoutedEventArgs e) {
            Cancel();
        }

        void OkButton_Click(object sender, RoutedEventArgs e) {
            Apply();
        }

        void Cancel() {
            settings["NodeSeparation"] = this.NodeSeparationSlider.Tag;
            settings["LayerSeparation"] = this.LayerSeparationSlider.Tag;
            settings["AspectRatio"] = this.AspectRatioCheckBox.Tag;
            settings["EdgeThickness"] = EdgeThicknessSlider.Tag;
            this.Close();
        }

        void Apply() {
            settings["NodeSeparation"] = Math.Floor(NodeSeparationSlider.Value);
            settings["LayerSeparation"] = Math.Floor(LayerSeparationSlider.Value);
            settings["AspectRatio"] = AspectRatioCheckBox.IsChecked == true;
            settings["EdgeThickness"] = EdgeThicknessSlider.Value;
            this.Close();          
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            switch (e.Key) {
                case Key.Escape:
                    Cancel();
                    break;
                case Key.Enter:
                    Apply();
                    break;
            }
            base.OnKeyDown(e);
        }
    }
}
