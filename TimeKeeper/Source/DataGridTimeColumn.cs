//-----------------------------------------------------------------------
// <copyright file="DataGridTimeColumn.cs" company="Lovett Software">
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

namespace TimeKeeper
{
    public class DataGridTimeColumn : DataGridBoundColumn
    {
        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            Binding binding = this.Binding as Binding;
            var picker = new TimePicker() { DataContext = dataItem };
            picker.SetBinding(TimePicker.TimeOfDayProperty,
                new Binding() { Path = binding.Path, Mode = BindingMode.TwoWay, Source = dataItem });
            return picker;
        }

        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            Binding binding = this.Binding as Binding;
            
            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });

            var block = new TextBlock() { Margin=new Thickness(2,1,2,1) };
            block.SetBinding(TextBlock.TextProperty, new Binding() { Path = binding.Path, Mode = BindingMode.TwoWay, StringFormat = "hh"});
            Grid.SetColumn(block, 0);
            grid.Children.Add(block);
            block = new TextBlock() {Margin = new Thickness(.5) };
            block.SetBinding(TextBlock.TextProperty, new Binding() { Path = binding.Path, Mode = BindingMode.OneWay, Converter = new SmartTimeSeparatorConverter() }); Grid.SetColumn(block, 1);
            Grid.SetColumn(block, 1);
            grid.Children.Add(block);

            block = new TextBlock() { Margin = new Thickness(2, 1, 2, 1) };
            block.SetBinding(TextBlock.TextProperty, new Binding() { Path = binding.Path, Mode = BindingMode.TwoWay, StringFormat = "mm" });
            Grid.SetColumn(block, 2);
            grid.Children.Add(block);
            block = new TextBlock() { Margin = new Thickness(.5) };
            block.SetBinding(TextBlock.TextProperty, new Binding() { Path = binding.Path, Mode = BindingMode.OneWay, Converter = new SmartTimeSeparatorConverter() }); Grid.SetColumn(block, 1);
            Grid.SetColumn(block, 3);
            grid.Children.Add(block);

            block = new TextBlock() { Margin = new Thickness(2, 1, 2, 1) };
            block.SetBinding(TextBlock.TextProperty, new Binding() { Path = binding.Path, Mode = BindingMode.TwoWay, StringFormat = "ss" });
            Grid.SetColumn(block,4);
            grid.Children.Add(block);
            block = new TextBlock() { Text = " ", Margin = new Thickness(.5) };
            Grid.SetColumn(block, 5);
            grid.Children.Add(block);

            block = new TextBlock() { Margin = new Thickness(2, 1, 2, 1) };
            block.SetBinding(TextBlock.TextProperty, new Binding() { Path = binding.Path, Mode = BindingMode.TwoWay, StringFormat = "tt" });
            Grid.SetColumn(block, 6);
            grid.Children.Add(block);

            return grid;
        }

    }
        
    public class DataGridTimeSpanColumn : DataGridBoundColumn 
    {      
        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
        {
            Binding binding = this.Binding as Binding;
            var picker = new TimePicker() { EditTimeSpan = true };
            picker.SetBinding(TimePicker.TimeSpanProperty, 
                new Binding() { Path = binding.Path, Mode = BindingMode.TwoWay, Source = dataItem });
            return picker;
        }

        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
        {
            Binding binding = this.Binding as Binding;

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });

            var block = new TextBlock() { Margin = new Thickness(2, 1, 2, 1) };
            block.SetBinding(TextBlock.TextProperty, new Binding() { Path = binding.Path, Mode = BindingMode.TwoWay, Converter = new TwoDigitsConverter(), ConverterParameter="h" });
            Grid.SetColumn(block, 0);
            grid.Children.Add(block);
            block = new TextBlock() { Margin = new Thickness(1) };
            block.SetBinding(TextBlock.TextProperty, new Binding() { Path = binding.Path, Mode = BindingMode.OneWay, Converter = new SmartTimeSeparatorConverter() }); Grid.SetColumn(block, 1);
            grid.Children.Add(block);

            block = new TextBlock() { Margin = new Thickness(2, 1, 2, 1) };
            block.SetBinding(TextBlock.TextProperty, new Binding() { Path = binding.Path, Mode = BindingMode.TwoWay, Converter = new TwoDigitsConverter(), ConverterParameter = "m" });
            Grid.SetColumn(block, 2);
            grid.Children.Add(block);
            block = new TextBlock() { Margin = new Thickness(1) };
            block.SetBinding(TextBlock.TextProperty, new Binding() { Path = binding.Path, Mode = BindingMode.OneWay, Converter = new SmartTimeSeparatorConverter() });
            Grid.SetColumn(block, 3);
            grid.Children.Add(block);

            block = new TextBlock() { Margin = new Thickness(2, 1, 2, 1) };
            block.SetBinding(TextBlock.TextProperty, new Binding() { Path = binding.Path, Mode = BindingMode.TwoWay, Converter = new TwoDigitsConverter(), ConverterParameter = "s" });
            Grid.SetColumn(block, 4);
            grid.Children.Add(block);
             
            return grid;
        }
    }

    /// <summary>
    /// This class produces a colon ":" if the object is bound to a real DateTime or TimeSpan object, otherwise it returns "" which is what we
    /// need on blank rows of type {NewItemPlaceholder}.
    /// </summary>
    class SmartTimeSeparatorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is TimeSpan || value is DateTime) return ":";
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
