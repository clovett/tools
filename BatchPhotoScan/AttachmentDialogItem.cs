using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BatchPhotoScan
{

    /// <summary>
    ///  This class wraps an item in the attachment dialog and keeps track of it's file name
    ///  and file type.
    /// </summary>
    abstract class AttachmentDialogItem : FrameworkElement
    {
        /// <summary>
        /// The content being rendered in this item.
        /// </summary>
        public abstract FrameworkElement Content { get; }

        /// <summary>
        /// The current file name
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// The default file extension for this kind of item.
        /// </summary>
        public abstract string FileExtension { get; }

        /// <summary>
        /// Whether content supports live resizing.
        /// If this is true Resize is called while user is resizing rather than after resizing gesture is done.
        /// </summary>
        public abstract bool LiveResizable { get; }

        /// <summary>
        /// Return the allowable limit for new bounds sent to Resize method.
        /// </summary>
        public abstract Rect ResizeLimit { get; }

        /// <summary>
        /// Resize the content to this new size
        /// </summary>
        /// <param name="newBounds">The new bounds for the content</param>
        public abstract void Resize(Rect newBounds);

        /// <summary>
        /// Copy content to clipboard.
        /// </summary>
        public abstract void Copy();

        /// <summary>
        /// Create a copy of the content (for printing)
        /// </summary>
        /// <returns></returns>
        public abstract FrameworkElement CloneContent();

        /// <summary>
        /// Save the content to the given file.
        /// </summary>
        /// <param name="filePath"></param>
        public abstract void Save(string filePath);

        /// <summary>
        /// This event is raised if the content is changed.
        /// </summary>
        public event EventHandler ContentChanged;

        protected void OnContentChanged()
        {
            if (ContentChanged != null)
            {
                ContentChanged(this, EventArgs.Empty);
            }
        }
    }

}
