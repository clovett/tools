//---------------------------------------------------------------------
// <copyright file="PatchInstallation.cs" company="Microsoft">
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
    using System.Collections.Generic;
    using System.Globalization;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// The Patch object represents a unique instance of a patch that has been
    /// registered or applied.
    /// </summary>
    public class PatchInstallation : Installation
    {
        /// <summary>
        /// Enumerates all patch installations on the system.
        /// </summary>
        /// <returns>Enumeration of patch objects.</returns>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msienumpatches.asp">MsiEnumPatches</a>
        /// </p></remarks>
        public static IEnumerable<PatchInstallation> AllPatches
        {
            get
            {
                return new Enumerable<PatchInstallation>(
                    PatchInstallation.EnumPatches, new object[] { null, null, null, UserContexts.All, PatchStates.All });
            }
        }

        /// <summary>
        /// Enumerates patch installations based on certain criteria.
        /// </summary>
        /// <param name="patchCode">PatchCode (GUID) of the patch to be enumerated. Only
        /// instances of patches within the scope of the context specified by the
        /// <paramref name="userSid"/> and <paramref name="context"/> parameters will be
        /// enumerated. This parameter may be set to null to enumerate all patches in the specified
        /// context.</param>
        /// <param name="targetProductCode">ProductCode (GUID) product whose patches are to be
        /// enumerated. If non-null, patch enumeration is restricted to instances of this product
        /// within the specified context. If null, the patches for all products under the specified
        /// context are enumerated.</param>
        /// <param name="userSid">Specifies a security identifier (SID) that restricts the context
        /// of enumeration. A SID value other than s-1-1-0 is considered a user SID and restricts
        /// enumeration to the current user or any user in the system. The special SID string
        /// s-1-1-0 (Everyone) specifies enumeration across all users in the system. This parameter
        /// can be set to null to restrict the enumeration scope to the current user. When
        /// <paramref name="installContext"/> is set to the machine context only,
        /// <paramref name="userSid"/> must be null.</param>
        /// <param name="context">Specifies the user context.</param>
        /// <param name="states">The <see cref="PatchStates"/> of patches to return.</param>
        /// <remarks><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msienumpatchesex.asp">MsiEnumPatchesEx</a>
        /// </p></remarks>
        public static IEnumerable<PatchInstallation> GetPatches(
            string patchCode,
            string targetProductCode,
            string userSid,
            UserContexts context,
            PatchStates states)
        {
            return new Enumerable<PatchInstallation>(
                PatchInstallation.EnumPatches, new object[] {
                patchCode, targetProductCode, userSid, context, states });
        }

        private static IEnumerator<PatchInstallation> EnumPatches(object[] args)
        {
            string patchCode = (string) args[0];
            string targetProductCode = (string) args[1];
            string userSid = (string) args[2];
            UserContexts context = (UserContexts) args[3];
            PatchStates states = (PatchStates) args[4];

            StringBuilder buf = new StringBuilder(40);
            StringBuilder targetProductBuf = new StringBuilder(40);
            int targetContext;
            StringBuilder targetSidBuf = new StringBuilder(40);
            for (uint i = 0; ; i++)
            {
                uint targetSidBufSize = (uint) targetSidBuf.Capacity;
                uint ret = NativeMethods.MsiEnumPatchesEx(
                    targetProductCode,
                    userSid,
                    (int) context,
                    (uint) states,
                    i,
                    buf,
                    targetProductBuf,
                    out targetContext,
                    targetSidBuf,
                    ref targetSidBufSize);
                if (ret == (uint) NativeMethods.Error.MORE_DATA)
                {
                    targetSidBuf.Capacity = (int) ++targetSidBufSize;
                    ret = NativeMethods.MsiEnumPatchesEx(
                        targetProductCode,
                        userSid,
                        (int) context,
                        (uint) states,
                        i,
                        buf,
                        targetProductBuf,
                        out targetContext,
                        targetSidBuf,
                        ref targetSidBufSize);
                }

                if (ret == (uint) NativeMethods.Error.NO_MORE_ITEMS)
                {
                    break;
                }

                if (ret != 0)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }

                string thisPatchCode = buf.ToString();
                if (patchCode == null || patchCode == thisPatchCode)
                {
                    yield return new PatchInstallation(
                        buf.ToString(),
                        targetProductBuf.ToString(),
                        targetSidBuf.ToString(),
                        (UserContexts) targetContext);
                }
            }
        }

        private string productCode;

        /// <summary>
        /// Creates a new object for accessing information about a patch installation on the current system.
        /// </summary>
        /// <param name="patchCode">Patch code (GUID) of the patch.</param>
        /// <param name="productCode">ProductCode (GUID) the patch has been applied to.
        /// This parameter may be null for patches that are registered only and not yet
        /// applied to any product.</param>
        /// <remarks><p>
        /// All available user contexts will be queried.
        /// </p></remarks>
        public PatchInstallation(string patchCode, string productCode)
            : this(patchCode, productCode, null, UserContexts.All)
        {
        }

        /// <summary>
        /// Creates a new object for accessing information about a patch installation on the current system.
        /// </summary>
        /// <param name="patchCode">Registered patch code (GUID) of the patch.</param>
        /// <param name="productCode">ProductCode (GUID) the patch has been applied to.
        /// This parameter may be null for patches that are registered only and not yet
        /// applied to any product.</param>
        /// <param name="userSid">The specific user, when working in a user context.  This
        /// parameter may be null to indicate the current user.  The parameter must be null
        /// when working in a machine context.</param>
        /// <param name="context">The user context. The calling process must have administrative
        /// privileges to get information for a product installed for a user other than the
        /// current user.</param>
        /// <remarks><p>
        /// If the <paramref name="productCode"/> is null, the Patch object may
        /// only be used to read and update the patch's SourceList information.
        /// </p></remarks>
        public PatchInstallation(string patchCode, string productCode, string userSid, UserContexts context)
            : base(patchCode, userSid, context)
        {
            if (String.IsNullOrEmpty(patchCode))
            {
                throw new ArgumentNullException("patchCode");
            }

            this.productCode = productCode;
        }

        /// <summary>
        /// Gets the patch code (GUID) of the patch.
        /// </summary>
        public string PatchCode
        {
            get
            {
                return this.InstallationCode;
            }
        }

        /// <summary>
        /// Gets the ProductCode (GUID) of the product.
        /// </summary>
        public string ProductCode
        {
            get
            {
                return this.productCode;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this patch is currently installed.
        /// </summary>
        public override bool IsInstalled
        {
            get
            {
                return (this.State & PatchStates.Applied) != 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this patch is marked as obsolte.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Obsoleted")]
        public bool IsObsoleted
        {
            get
            {
                return (this.State & PatchStates.Obsoleted) != 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this patch is present but has been
        /// superseded by a more recent installed patch.
        /// </summary>
        public bool IsSuperseded
        {
            get
            {
                return (this.State & PatchStates.Superseded) != 0;
            }
        }

        internal override int InstallationType
        {
            get
            {
                const int MSICODE_PATCH = 0x40000000;
                return MSICODE_PATCH;
            }
        }

        /// <summary>
        /// Gets the installation state of this instance of the patch.
        /// </summary>
        /// <exception cref="ArgumentException">An unknown patch was requested</exception>
        /// <exception cref="InstallerException">The installer configuration data is corrupt</exception>
        public PatchStates State
        {
            get
            {
                string stateString = this["State"];
                return (PatchStates) Int32.Parse(stateString, CultureInfo.InvariantCulture.NumberFormat);
            }
        }

        /// <summary>
        /// Gets the cached patch file that the product uses.
        /// </summary>
        public string LocalPackage
        {
            get
            {
                return this["LocalPackage"];
            }
        }

        /// <summary>
        /// Gets the set of patch transforms that the last patch
        /// installation applied to the product.
        /// </summary>
        /// <remarks><p>
        /// This value may not be available for per-user, non-managed applications
        /// if the user is not logged on.
        /// </p></remarks>
        public string Transforms
        {
            get
            {
                // TODO: convert to IList<string>?
                return this["Transforms"];
            }
        }

        /// <summary>
        /// Gets the date and time when the patch is applied to the product.
        /// </summary>
        public DateTime InstallDate
        {
            get
            {
                try
                {
                    return DateTime.ParseExact(
                        this["InstallDate"], "yyyyMMdd", CultureInfo.InvariantCulture);
                }
                catch (FormatException)
                {
                    return DateTime.MinValue;
                }
            }
        }

        /// <summary>
        /// True patch is marked as possible to uninstall from the product.
        /// </summary>
        /// <remarks><p>
        /// Even if this property is true, the installer can still block the
        /// uninstallation if this patch is required by another patch that
        /// cannot be uninstalled.
        /// </p></remarks>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Uninstallable")]
        public bool Uninstallable
        {
            get
            {
                return this["Uninstallable"] == "1";
            }
        }

        /// <summary>
        /// Get the registered display name for the patch.
        /// </summary>
        public string DisplayName
        {
            get
            {
                return this["DisplayName"];
            }
        }

        /// <summary>
        /// Gets the registered support information URL for the patch.
        /// </summary>
        public Uri MoreInfoUrl
        {
            get
            {
                string value = this["MoreInfoURL"];
                if (!String.IsNullOrEmpty(value))
                {
                    try
                    {
                        return new Uri(value);
                    }
                    catch (UriFormatException) { }
                }

                return null;
            }
        }

        /// <summary>
        /// Gets information about a specific patch installation. 
        /// </summary>
        /// <param name="propertyName">The property being retrieved; see remarks for valid properties.</param>
        /// <returns>The property value, or an empty string if the property is not set for the patch.</returns>
        /// <exception cref="ArgumentOutOfRangeException">An unknown patch or property was requested</exception>
        /// <exception cref="InstallerException">The installer configuration data is corrupt</exception>
        /// <remarks><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetpatchinfo.asp">MsiGetPatchInfo</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetpatchinfoex.asp">MsiGetPatchInfoEx</a>
        /// </p></remarks>
        public override string this[string propertyName]
        {
            get
            {
                StringBuilder buf = new StringBuilder("");
                uint bufSize = 0;
                uint ret;

                if (this.Context == UserContexts.UserManaged ||
                    this.Context == UserContexts.UserUnmanaged ||
                    this.Context == UserContexts.Machine)
                {
                    ret = NativeMethods.MsiGetPatchInfoEx(
                        this.PatchCode,
                        this.ProductCode,
                        this.UserSid,
                        (int) this.Context,
                        propertyName,
                        buf,
                        ref bufSize);
                    if (ret == (uint) NativeMethods.Error.MORE_DATA)
                    {
                        buf.Capacity = (int) ++bufSize;
                        ret = NativeMethods.MsiGetPatchInfoEx(
                            this.PatchCode,
                            this.ProductCode,
                            this.UserSid,
                            (int) this.Context,
                            propertyName,
                            buf,
                            ref bufSize);
                    }
                }
                else
                {
                    ret = NativeMethods.MsiGetPatchInfo(
                        this.PatchCode,
                        propertyName,
                        buf,
                        ref bufSize);
                    if (ret == (uint) NativeMethods.Error.MORE_DATA)
                    {
                        buf.Capacity = (int) ++bufSize;
                        ret = NativeMethods.MsiGetPatchInfo(
                            this.PatchCode,
                            propertyName,
                            buf,
                            ref bufSize);
                    }
                }

                if (ret != 0)
                {
                    return null;
                }

                return buf.ToString();
            }
        }
    }
}
