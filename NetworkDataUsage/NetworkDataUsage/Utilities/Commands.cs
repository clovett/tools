using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkDataUsage.Commands
{
    /// <summary>
    /// Shows the main window.
    /// </summary>
    public class QuitCommand : CommandBase<QuitCommand>
    {
        public override void Execute(object parameter)
        {
            App.Current.Shutdown();
        }

        public override bool CanExecute(object parameter)
        {
            //Window win = GetTaskbarWindow(parameter);
            //return win != null && !win.IsVisible;
            return true;
        }
    }

    public class ShowWindowCommand : CommandBase<ShowWindowCommand>
    {
        public override void Execute(object parameter)
        {
            GetTaskbarWindow(parameter).Show();
        }

        public override bool CanExecute(object parameter)
        {
            return true;
        }
    }

}
