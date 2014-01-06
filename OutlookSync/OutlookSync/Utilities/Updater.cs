using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Deployment.Application;
using System.Reflection;

namespace OutlookSync.Utilities
{
    class Updater
    {
        public static Version GetCurrentVersion()
        {
            try
            {
                return ApplicationDeployment.CurrentDeployment.CurrentVersion;
            }
            catch
            {
                // must be debugging...
                var nameHelper = new AssemblyName(Assembly.GetExecutingAssembly().FullName);
                return nameHelper.Version;
            }
        }

        public static async Task<UpdateCheckInfo> CheckForUpdate()
        {
            UpdateCheckInfo info = null;
            await Task.Run(new Action(() =>
            {
                try
                {
                    var current = ApplicationDeployment.CurrentDeployment;
                    info = current.CheckForDetailedUpdate();
                }
                catch (InvalidDeploymentException)
                {
                    // must be debugging.
                }
            }));
            return info;
        }

        public static async Task<bool> Update()
        {
            bool rc = false;
            await Task.Run(new Action(() =>
            {
                var current = ApplicationDeployment.CurrentDeployment;
                rc = current.Update();
            }));
            return rc;
        }

    }
}
