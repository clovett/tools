//---------------------------------------------------------------------
// <copyright file="CustomActionAttribute.cs" company="Microsoft">
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
    using System.Reflection;

    /// <summary>
    /// Marks a method as a custom action entry point.
    /// </summary>
    /// <remarks><p>
    /// A custom action method must be defined as public and static,
    /// take a single <see cref="Session"/> object as a parameter,
    /// and return an <see cref="ActionResult"/> enumeration value.
    /// </p></remarks>
    [Serializable, AttributeUsage(AttributeTargets.Method)]
    public sealed class CustomActionAttribute : Attribute
    {
        /// <summary>
        /// Name of the custom action entrypoint, or null if the same as the method name.
        /// </summary>
        private string name;

        /// <summary>
        /// Marks a method as a custom action entry point.
        /// </summary>
        public CustomActionAttribute()
            : this(null)
        {
        }

        /// <summary>
        /// Marks a method as a custom action entry point.
        /// </summary>
        /// <param name="name">Name of the function to be exported,
        /// defaults to the name of this method</param>
        public CustomActionAttribute(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Gets or sets the name of the custom action entrypoint. A null
        /// value defaults to the name of the method.
        /// </summary>
        /// <value>name of the custom action entrypoint, or null if none was specified</value>
        public string Name
        {
            get
            {
                return this.name;
            }
        }
    }
}
