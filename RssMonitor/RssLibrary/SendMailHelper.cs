using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace RssLibrary
{
    class SendMailHelper
    {

        public static void SendEmail(string smtpHost, int smtpPort, string userName, string password, string fromEmailAddress, string toEmailAddress, string subject, string messageHtml)
        {

            // Command line argument must the the SMTP host.
            SmtpClient client = new SmtpClient(smtpHost, smtpPort);

            if (userName != null)
            {
                client.Credentials = new NetworkCredential(userName, password);
            }

            // Specify the e-mail sender. 
            // Create a mailing address that includes a UTF8 character 
            // in the display name.
            MailAddress from = new MailAddress(fromEmailAddress, fromEmailAddress, System.Text.Encoding.UTF8);

            // Set destinations for the e-mail message.
            MailAddress to = new MailAddress(toEmailAddress);

            // Specify the message content.
            MailMessage message = new MailMessage(from, to);

            message.Body = messageHtml;
            message.BodyEncoding = System.Text.Encoding.UTF8;
            message.IsBodyHtml = true;

            message.Subject = subject;
            message.SubjectEncoding = System.Text.Encoding.UTF8;

            client.Send(message);
        }
    }
}
