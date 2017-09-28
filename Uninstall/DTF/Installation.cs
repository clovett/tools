//---------------------------------------------------------------------
// <copyright file="Installation.cs" company="Microsoft">
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
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.Deployment.WindowsInstaller
{
    using System;
    using System.Text;
    using System.Globalization;

    /// <summary>
    /// Subclasses of this abstract class represent a unique instance of a
    /// registered product or patch installation.
    /// </summary>
    public abstract class Installation
    {
        private string installationCode;
        private string userSid;
        private UserContexts context;
        private SourceList sourceList;

        internal Installation(string installationCode, string userSid, UserContexts context)
        {
            if (context == UserContexts.Machine)
            {
                userSid = null;
            }
            this.installationCode = installationCode;
            this.userSid = userSid;
            this.context = context;
        }

        /// <summary>
        /// Gets the user security identifier (SID) under which this product or patch
        /// installation is available.
        /// </summary>
        public string UserSid
        {
            get
            {
                return this.userSid;
            }
        }

        /// <summary>
        /// Gets the user context of this product or patch installation.
        /// </summary>
        public UserContexts Context
        {
            get
            {
                return this.context;
            }
        }

        /// <summary>
        /// Gets the source list of this product or patch installation.
        /// </summary>
        public virtual SourceList SourceList
        {
            get
            {
                if (this.sourceList == null)
                {
                    this.sourceList = new SourceList(this);
                }
                return this.sourceList;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this product or patch is installed on the current system.
        /// </summary>
        public abstract bool IsInstalled
        {
            get;
        }

        internal string InstallationCode
        {
            get
            {
                return this.installationCode;
            }
        }

        internal abstract int InstallationType
        {
            get;
        }

        /// <summary>
        /// Gets a property about the product or patch installation.
        /// </summary>
        /// <param name="propertyName">Name of the property being retrieved.</param>
        /// <returns></returns>
        public abstract string this[string propertyName]
        {
            get;
        }
    }
}
