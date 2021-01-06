using MyFitness.Model;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MyFitness.Controls
{
    /// <summary>
    /// Interaction logic for CalendarControl.xaml
    /// </summary>
    public partial class CalendarControl : UserControl
    {
        List<MonthViewDayTile> tiles = new List<MonthViewDayTile>();

        public CalendarControl()
        {
            InitializeComponent();
            this.DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            foreach (var tile in tiles)
            {
                CalendarGrid.Children.Remove(tile);
            }
            tiles.Clear();

            if (e.NewValue is CalendarMonth m)
            {
                m.PopulateIfEmpty();
                int row = 1;
                int column = 0;
                foreach (var day in m.Days) 
                {
                    var tile = new MonthViewDayTile() { DataContext = day };
                    int c = (int)day.Date.DayOfWeek;
                    Grid.SetColumn(tile, c);
                    if (column > c)
                    {
                        // wrapped around.
                        row++;
                    }
                    Grid.SetRow(tile, row);
                    column = c;
                    CalendarGrid.Children.Add(tile);
                }
            }

            return;
        }
    }
}
