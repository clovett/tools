// PkgCmdID.cs
// MUST match PkgCmdID.h
using System;

namespace Microsoft.SvgEditorPackage
{
    static class PkgCmdIDList
    {



        // Menus
        public const int menu_SvgEditorContext = 0x1000; // context menu

        // Menu Groups
        public const int group_View = 0x2000;
        public const int group_CutCopy = 0x2020;
        public const int group_Properties = 0x2030;

        // Command IDs

        public const int cmdid_ViewXml = 0x0100;
        public const int cmdid_Properties = 0x120;

    };
}