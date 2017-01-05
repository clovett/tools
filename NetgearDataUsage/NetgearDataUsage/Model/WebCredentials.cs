using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetgearDataUsage.Model
{
    public class WebCredential
    {
        public string Uri { get; set; }

        public string UserName { get; set; }
        public string Password { get; set; }

        public bool RememberCredentials { get; set; }

        public string Realm { get; set; }

        public static WebCredential LoadCredential(string uri)
        {
            try
            {
                // load password from the Windows PasswordVault.
                // https://blogs.msdn.microsoft.com/windowsappdev/2013/05/30/credential-locker-your-solution-for-handling-usernames-and-passwords-in-your-windows-store-app/
                var vault = new Windows.Security.Credentials.PasswordVault();
                var credential = (from c in vault.FindAllByResource(uri)
                                  select c).FirstOrDefault();
                if (credential != null)
                {
                    credential.RetrievePassword();
                    return new WebCredential()
                    {
                        Uri = uri,
                        UserName = credential.UserName,
                        Password = credential.Password,
                        RememberCredentials = true
                    };
                }
            }
            catch
            {
                // ignore ElementNotFound exception
            }
            return null;
        }


        public static void SavePassword(WebCredential credential)
        {
            var vault = new Windows.Security.Credentials.PasswordVault();
            if (!credential.RememberCredentials)
            {
                try
                {
                    var pc = new Windows.Security.Credentials.PasswordCredential(
                        credential.Uri, credential.UserName, credential.Password);
                    vault.Remove(pc);
                }
                catch
                {
                    // ignore ElementNotFound exception
                }
            }
            else
            { 
                var pc = new Windows.Security.Credentials.PasswordCredential(
                        credential.Uri, credential.UserName, credential.Password);
                vault.Add(pc);
            }
        }
    }
}
