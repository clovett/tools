using System.ComponentModel;

namespace LovettSoftware.Utilities
{
    public class NotifyingObject : INotifyPropertyChanged
    {
        protected void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
