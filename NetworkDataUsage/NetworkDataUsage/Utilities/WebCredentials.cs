using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Walkabout.Utilities
{
    public class WebCredential
    {
        public string Uri { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public bool RememberCredentials { get; set; }

        public string Realm { get; set; }

        public static WebCredential LoadCredential(string uri, string userName)
        {
            try
            {
                // load password from the Windows PasswordVault.
                // https://blogs.msdn.microsoft.com/windowsappdev/2013/05/30/credential-locker-your-solution-for-handling-usernames-and-passwords-in-your-windows-store-app/
                using (var credential = new Credential(uri, CredentialType.Generic))
                {
                    credential.UserName = userName;
                    credential.Persistence = CredentialPersistence.LocalComputer;
                    credential.Load();
                    return new WebCredential()
                    {
                        Uri = uri,
                        UserName = credential.UserName,
                        Password = Credential.SecureStringToString(credential.Password),
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


        public static void SavePassword(WebCredential cred)
        {
            if (!cred.RememberCredentials)
            {
                Credential.Delete(cred.Uri, CredentialType.Generic);
            }
            else
            {
                try
                {
                    using (var credential = new Credential(cred.Uri, CredentialType.Generic))
                    {
                        credential.UserName = cred.UserName;
                        credential.Password = Credential.ToSecureString(cred.Password);
                        credential.Persistence = CredentialPersistence.LocalComputer;
                        credential.Save();
                    }
                }
                catch (Exception ex)
                {
                    // ignore ElementNotFound exception
                    Debug.WriteLine(ex.Message);
                }
            }
        }
    }
}
