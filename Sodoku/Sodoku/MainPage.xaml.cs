using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Sodoku
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            SodokuGrid.Board = new Board(new int[] {             
               0,0,0,1,0,6,0,0,4,
               0,1,0,0,0,8,7,3,0,
               0,0,0,0,9,4,0,0,5,
               5,0,0,0,1,0,0,8,6,
               7,8,1,0,2,0,9,4,3,
               6,9,0,0,8,0,0,0,1,
               2,0,0,5,4,0,0,0,0,
               0,5,9,8,0,0,0,6,0,
               3,0,0,9,0,2,0,0,0
            });

        }

        private void OnSolve(object sender, RoutedEventArgs e)
        {
            SodokuGrid.Board.Solve();
        }
    }
}
