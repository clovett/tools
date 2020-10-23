// Guids.cs
// MUST match guids.h
using System;

namespace Microsoft.SvgEditorPackage
{
    static class GuidList
    {
        public const string guidSvgEditorPackagePkgString = "3ea129a7-aaf0-4af3-b258-2b13bf64bbd2";
        public const string guidSvgEditorPackageCmdSetString = "f6fb9b12-2921-467a-8cb0-ce05a2f50bf2";
        public const string guidSvgEditorPackageEditorFactoryString = "a371c3f1-d865-409a-98bb-760349a159ce";

        public static readonly Guid guidSvgEditorPackageCmdSet = new Guid(guidSvgEditorPackageCmdSetString);
        public static readonly Guid guidSvgEditorPackageEditorFactory = new Guid(guidSvgEditorPackageEditorFactoryString);
    };
}