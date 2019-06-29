using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeetingBuzzer
{
    class AccessManager
    {
        string _accessToken;

        public async Task<string> GetTokenAsync(string clientId)
        {
            if (string.IsNullOrEmpty(_accessToken))
            {
                var clientApp = PublicClientApplicationBuilder.Create(clientId);
                var publicApp = clientApp.Build();

                var accounts = await publicApp.GetAccountsAsync();
                var firstAccount = accounts.FirstOrDefault();
                var scopes = new string[] { "User.Read" };
                try
                {
                    var parameterBuidler = publicApp.AcquireTokenSilent(scopes, firstAccount);
                    var result = await parameterBuidler.ExecuteAsync();
                    _accessToken = result.AccessToken;
                }
                catch (MsalUiRequiredException ex)
                {
                    var parameterBuidler = publicApp.AcquireTokenInteractive(scopes).WithAccount(firstAccount).WithPrompt(Prompt.SelectAccount);
                    var result = await parameterBuidler.ExecuteAsync();
                    _accessToken = result.AccessToken;
                }
            }
            return _accessToken;
        }

    }
}
