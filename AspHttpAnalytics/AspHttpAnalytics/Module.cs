using System;
using System.IO;
using System.Web;
using Google.Apis.Analytics.v3;
using Google.Apis.Services;

namespace AspHttpAnalytics
{
    public class HttpAnalyticsModule : IHttpModule
    {
        static string GoogleApiKey = "AIzaSyDBEi2yagVKmiXNmgWinONOsnUMR2F3x2E";

        public void Dispose()
        {
        }

        public void Init(HttpApplication context)
        {
            context.EndRequest += OnEndRequest;
        }

        private void OnEndRequest(object sender, EventArgs e)
        {
            if (sender is HttpApplication app)
            {
                var context = app.Context;
                var logFile = app.Server.MapPath("/log.txt");
                context.Response.Headers["MyAnalytics"] = $"Request status {context.Response.StatusCode} for resource {context.Request.Url} from {context.Request.UserHostAddress}";

                var service = new AnalyticsService(new BaseClientService.Initializer
                {
                    ApplicationName = "LovettSoftware.com",
                    ApiKey = GoogleApiKey,
                });

                
            }
        }
    }
}
