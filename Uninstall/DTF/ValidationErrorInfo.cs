//---------------------------------------------------------------------
// <copyright file="ValidationErrorInfo.cs" company="Microsoft">
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
// Microsoft.Deployment.WindowsInstaller.ValidationErrorInfo struct.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.Deployment.WindowsInstaller
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Contains specific information about an error encountered by the <see cref="View.Validate"/>,
    /// <see cref="View.ValidateNew"/>, or <see cref="View.ValidateFields"/> methods of the
    /// <see cref="View"/> class.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
    public struct ValidationErrorInfo
    {
        private ValidationError error;
        private string column;
        
        internal ValidationErrorInfo(ValidationError error, string column)
        {
            this.error = error;
            this.column = column;
        }

        /// <summary>
        /// Gets the type of validation error encountered.
        /// </summary>
        public ValidationError Error
        {
            get
            {
                return this.error;
            }
        }

        /// <summary>
        /// Gets the column containing the error, or null if the error applies to the whole row.
        /// </summary>
        public string Column
        {
            get
            {
                return this.column;
            }
        }
    }
}
