//---------------------------------------------------------------------
// <copyright file="MediaDisk.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//    
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//    
//    You must not remove this notice, or any other, from this software.
// </copyright>
// <summary>
// Microsoft.Deployment.WindowsInstaller.MediaDisk struct.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.Deployment.WindowsInstaller
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Represents a media disk source of a product or a patch.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
    public struct MediaDisk
    {
        private int diskId;
        private string volumeLabel;
        private string diskPrompt;

        /// <summary>
        /// Creates a new media disk.
        /// </summary>
        /// <param name="diskId"></param>
        /// <param name="volumeLabel"></param>
        /// <param name="diskPrompt"></param>
        public MediaDisk(int diskId, string volumeLabel, string diskPrompt)
        {
            this.diskId = diskId;
            this.volumeLabel = volumeLabel;
            this.diskPrompt = diskPrompt;
        }

        /// <summary>
        /// Gets or sets the disk id of the media disk.
        /// </summary>
        public int DiskId
        {
            get { return this.diskId; }
            set { this.diskId = value; }
        }

        /// <summary>
        /// Gets or sets the volume label of the media disk.
        /// </summary>
        public string VolumeLabel
        {
            get { return this.volumeLabel; }
            set { this.volumeLabel = value; }
        }

        /// <summary>
        /// Gets or sets the disk prompt of the media disk.
        /// </summary>
        public string DiskPrompt
        {
            get { return this.diskPrompt; }
            set { this.diskPrompt = value; }
        }
    }
}
