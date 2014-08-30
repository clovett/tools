using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;

namespace Microsoft.Phone.BatteryStretcher.Common
{
    interface IFileSavePickerContinuable
    {
        void ContinueFileSavePicker(FileSavePickerContinuationEventArgs args);
    }
    interface IFileOpenPickerContinuable
    {
        void ContinueFileOpenPicker(FileOpenPickerContinuationEventArgs args);
    }
    interface IFolderPickerContinuable
    {
        void ContinueFolderPicker(FolderPickerContinuationEventArgs folderPickerContinuationEventArgs);
    }
}
