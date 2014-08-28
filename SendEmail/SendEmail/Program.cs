using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace SendEmail
{
    class Program
    {
        string host;
        int port;
        string userName;
        string password;
        string from;
        string fromDisplay;
        string to;
        string subject;
        string message;
        string messageFile;
        List<string> attachments = new List<string>();

        static void PrintUsage()
        {
            Console.WriteLine("Usage: SendMail [options]\n");
            Console.WriteLine("options:");
            Console.WriteLine(" -host SMTP host computer name");
            Console.WriteLine(" -port host port number");
            Console.WriteLine(" -user SMTP user name");
            Console.WriteLine(" -password SMTP password");
            Console.WriteLine(" -from emailaddress");
            Console.WriteLine(" -display from display name");
            Console.WriteLine(" -to email address");
            Console.WriteLine(" -subject subject text   // subject on the command line");
            Console.WriteLine(" -message message text   // message body on the command line");
            Console.WriteLine(" -messagefile file name  // message content in the given ");
            Console.WriteLine(" -attach file            // zero or more file attachments" );                
        }

        private bool ParseCommandLine(string[] args)
        {
            for (int i = 0, n = args.Length; i < n; i++)
            {
                string arg = args[i];
                if (arg[0] == '-' || arg[0] == '/')
                {
                    string next = (i + 1 < n) ? args[i + 1] : null;
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "?":
                        case "h":
                        case "help":
                            return false;
                        case "host":
                            host = next;
                            i++;
                            break;
                        case "port":
                            if (!int.TryParse(next, out port))
                            {
                                Console.WriteLine("Invalid port number '" + next  + "'");
                                return false;
                            }
                            i++;
                            break;
                        case "user":
                            userName = next;
                            i++;
                            break;
                        case "password":
                            password = next;
                            i++;
                            break;
                        case "from":
                            from = next;
                            i++;
                            break;
                        case "display":
                            fromDisplay = next;
                            i++;
                            break;
                        case "to":
                            to = next;
                            i++;
                            break;
                        case "subject":
                            subject = next;
                            i++;
                            break;
                        case "message":
                            message = next;
                            i++;
                            break;
                        case "messagefile":
                            messageFile = next;
                            i++;
                            break;
                        case "attach":
                            if (next != null)
                            {
                                attachments.Add(next);
                            }
                            i++;
                            break;
                        default:
                            Console.WriteLine("### Error: unexpected command line argument: " + arg);
                            return false;
                    }
                }
                else
                {
                    Console.WriteLine("### Error: too many arguments");
                    return false;
                }
            }
            if (host == null)
            {
                Console.WriteLine("### Error: missing 'host' argument");
                return false;
            }
            if (from == null)
            {
                Console.WriteLine("### Error: missing 'from' argument");
                return false;
            }
            if (to == null)
            {
                Console.WriteLine("### Error: missing 'to' argument");
                return false;
            }
            if (subject == null)
            {
                Console.WriteLine("### Error: missing 'subject' argument");
                return false;
            }
            if (message == null && messageFile == null)
            {
                Console.WriteLine("### Error: missing 'message' argument");
                return false;
            }
            return true;
        }


        static void Main(string[] args)
        {
            Program p = new Program();
            if (!p.ParseCommandLine(args))
            {
                PrintUsage();
            }
            else
            {
                p.SendEmail();
            }
        }

        private void SendEmail()
        {

            // Command line argument must the the SMTP host.
            SmtpClient client = new SmtpClient(this.host, this.port);

            if (userName != null)
            {
                client.Credentials = new NetworkCredential(this.userName, this.password);
            }

            // Specify the e-mail sender. 
            // Create a mailing address that includes a UTF8 character 
            // in the display name.
            MailAddress from = new MailAddress(this.from, this.fromDisplay, System.Text.Encoding.UTF8);

            // Set destinations for the e-mail message.
            MailAddress to = new MailAddress(this.to);

            // Specify the message content.
            MailMessage message = new MailMessage(from, to);
            if (this.messageFile != null)
            {
                using (StreamReader reader = new StreamReader(this.messageFile, true))
                {
                    this.message = reader.ReadToEnd();
                }
            }
            message.Body = this.message;
            message.BodyEncoding = System.Text.Encoding.UTF8;

            message.Subject = this.subject;
            message.SubjectEncoding = System.Text.Encoding.UTF8;

            foreach (string fileName in this.attachments)
            {
                message.Attachments.Add(new Attachment(fileName));
            }

            Console.Write("Sending message...");
            try
            {
                client.Send(message);
                Console.WriteLine("ok");
            } 
            catch (Exception ex)
            {
                Console.WriteLine("#error: " + ex.Message);
            }

        }
        
    }
}
