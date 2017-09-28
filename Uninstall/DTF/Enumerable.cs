//---------------------------------------------------------------------
// <copyright file="Enumerable.cs" company="Microsoft">
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
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Converts a GetEnumerator() delegate to an IEnumerable.
    /// </summary>
    /// <typeparam name="T">Type of the item being enumerated</typeparam>
    /// <remarks><p>
    /// Returning an IEnumerable from functions and properties is often
    /// more convenient than returning an IEnumerator. Most importantly,
    /// IEnumerable works with LINQ.
    /// </p></remarks>
    internal class Enumerable<T> : IEnumerable<T>
    {
        public delegate IEnumerator<T> GetEnumerator(object[] args);

        private GetEnumerator getEnumerator;
        private object[] args;

        public Enumerable(GetEnumerator getEnumerator, object[] args)
        {
            this.getEnumerator = getEnumerator;
            this.args = args;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this.getEnumerator(args);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>) this).GetEnumerator();
        }
    }
}
