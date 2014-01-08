using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Deployment.Application;
using System.Reflection;
using System.Threading;

namespace OutlookSync.Utilities
{
    class Updater
    {
        public event EventHandler UpdateAvailable;

        private CancellationTokenSource watchingTokenSource;

        bool waiting;

        public bool IsWaitingForUpdate
        {
            get { return waiting; }
        }

        public void StopWatchingForUpdate()
        {
            if (watchingTokenSource != null)
            {
                watchingTokenSource.Cancel();
            }
        }

        public void BeginWatchForUpdate()
        {
            if (!waiting)
            {
                waiting = true;
                watchingTokenSource = new CancellationTokenSource();
                Task.Run(new Action(WatchForUpdateTask));
            }
        }

        public bool IsFirstLaunch
        {
            get
            {
                try
                {
                    return ApplicationDeployment.CurrentDeployment.IsFirstRun;
                }
                catch (Exception)
                {
                    // must be debugging, or task was cancelled.
                    return false;
                }
            }
        }

        private async void WatchForUpdateTask()
        {
            waiting = true;

            while (!watchingTokenSource.IsCancellationRequested)
            {
                int start = Environment.TickCount;
                try
                {
                    var current = ApplicationDeployment.CurrentDeployment;
                    bool rc = current.CheckForUpdate();
                    if (rc) 
                    {
                        OnUpdateAvailable();
                        break;
                    }
                }
                catch (Exception)
                {
                    // must be debugging, or task was cancelled.
                }

                int end = Environment.TickCount;
                int delay = end - start + 5000;
                await Task.Delay(delay, watchingTokenSource.Token);
            }

            waiting = false;
        }

        private void OnUpdateAvailable()
        {
            if (UpdateAvailable != null)
            {
                UpdateAvailable(this, EventArgs.Empty);
            }
        }
        

        public Version GetCurrentVersion()
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

        public async Task<UpdateCheckInfo> CheckForUpdate()
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

        public async Task<bool> Update()
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
