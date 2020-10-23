using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using IServiceProvider = System.IServiceProvider;

namespace Microsoft.SvgEditorPackage
{
    internal static class VsShell
    {
        /// <summary>
        /// Show the specified VS context menu at the given location.
        /// </summary>
        /// <param name="groupGuid">Guid of the menu</param>
        /// <param name="menuId">Id of the menu</param>
        /// <param name="position">Location in screen coordinates</param>
        public static void ShowContextMenu(IServiceProvider provider, IOleCommandTarget target, Guid menuGuid, int menuId, Point position)
        {
            IVsUIShell uiShell = provider.GetService(typeof(SVsUIShell)) as IVsUIShell;
            if (uiShell != null) 
            {
                POINTS[] pnts = new POINTS[1];
                pnts[0].x = (short)position.X;
                pnts[0].y = (short)position.Y;
                int hr = uiShell.ShowContextMenu(0, ref menuGuid, menuId, pnts, target);
                if (!Microsoft.VisualStudio.ErrorHandler.Succeeded(hr))
                {
                    System.Diagnostics.Debug.Assert(false, "IVsUiShell.ShowContextMenu failed, hr= " + hr);
                }
            }
        }
    }
}
