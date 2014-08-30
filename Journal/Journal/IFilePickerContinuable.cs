using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;

namespace Journal
{
    interface IFilePickerContinuable
    {
        void ContinueFileOpenPicker(FileOpenPickerContinuationEventArgs args);
        void ContinueFileSavePicker(FileSavePickerContinuationEventArgs args);
    }
}
