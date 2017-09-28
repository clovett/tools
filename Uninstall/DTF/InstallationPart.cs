//---------------------------------------------------------------------
// <copyright file="InstallationPart.cs" company="Microsoft">
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
    /// <summary>
    /// Subclasses of this abstract class represent an instance
    /// of a registered feature or component.
    /// </summary>
    public abstract class InstallationPart
    {
        private string id;
        private string productCode;

        internal InstallationPart(string id, string productCode)
        {
            this.id = id;
            this.productCode = productCode;
        }

        internal string Id
        {
            get
            {
                return this.id;
            }
        }

        internal string ProductCode
        {
            get
            {
                return this.productCode;
            }
        }

        /// <summary>
        /// Gets the product that this item is a part of.
        /// </summary>
        public ProductInstallation Product
        {
            get
            {
                return this.productCode != null ?
                    new ProductInstallation(this.productCode) : null;
            }
        }

        /// <summary>
        /// Gets the current installation state of the item.
        /// </summary>
        public abstract InstallState State
        {
            get;
        }
    }

}
