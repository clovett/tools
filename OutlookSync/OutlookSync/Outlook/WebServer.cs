using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace OutlookSync.Model
{
    class WebServer
    {
        TcpListener listener;
        bool terminated;
        SslStream _sslStream;

        public void Start()
        {
            Task.Run(new Action(() =>
            {
                Run();
            }));
        }

        public void Stop()
        {
            terminated = true;
        }

        private void Run()
        {
            listener = new TcpListener(IPAddress.Any, 443);
            listener.Start();

            while (!terminated)
            {
                try 
                {
                    TcpClient client = listener.AcceptTcpClient();
                    HandleRequest(client);
                } 
                catch (Exception ex) 
                {
                    Debug.WriteLine("HttpListener caught exception: " + ex.Message);
                }
            }
        }

        private void HandleRequest(TcpClient client)
        {
            try
            {

                bool leaveInnerStreamOpen = true;

                RemoteCertificateValidationCallback validationCallback =
                  new RemoteCertificateValidationCallback(ClientValidationCallback);

                LocalCertificateSelectionCallback selectionCallback =
                  new LocalCertificateSelectionCallback(ServerCertificateSelectionCallback);

                EncryptionPolicy encryptionPolicy = EncryptionPolicy.AllowNoEncryption;

                //create the SSL stream starting from the NetworkStream associated 
                //with the TcpClient instance 
                _sslStream = new SslStream(client.GetStream(),
                  leaveInnerStreamOpen, validationCallback, selectionCallback, encryptionPolicy);

                //1. when the client requests it, the handshake begins 
                ServerSideHandshake();

                //2. read client's data using the encrypted stream 
                ReadClientData();
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nError detected: " + ex.Message);
            }
            finally
            {
                if (_sslStream != null) _sslStream.Close();
                client.Close();
            } 

        }

        private X509Certificate ServerCertificateSelectionCallback(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            //perform some checks on the certificate... 
            // ...
            // ...                                       
            //return the selected certificate.  If null is returned a NotSupported exception is thrown. 
            return localCertificates[0];
        }

        private bool ClientValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            switch (sslPolicyErrors)
            {
                case SslPolicyErrors.RemoteCertificateNameMismatch:
                    Console.WriteLine("Client's name mismatch. End communication ...\n");
                    return false;

                case SslPolicyErrors.RemoteCertificateNotAvailable:
                    Console.WriteLine("Client's certificate not available. End communication ...\n");
                    return false;

                case SslPolicyErrors.RemoteCertificateChainErrors:
                    Console.WriteLine("Client's certificate validation failed. End communication ...\n");
                    return false;
            }

            //Perform others checks using the "certificate" and "chain" objects ... 
            // ... 
            // ... 

            Console.WriteLine("Client's authentication succeeded ...\n");
            return true; 
        }

        private void ReadClientData()
        {
            byte[] buffer = new byte[1024];

            int n = _sslStream.Read(buffer, 0, 1024);

            Array.Resize<byte>(ref  buffer, n);

            string _message = Encoding.UTF8.GetString(buffer);

            Console.WriteLine("Client said: " + _message); 
        }

        private void ServerSideHandshake()
        {
            X509Certificate2 certificate = new X509Certificate2(@"d:\temp\LocalServer.cer", "yeshua");

            bool requireClientCertificate = false;
            SslProtocols enabledSslProtocols = SslProtocols.Ssl3 | SslProtocols.Tls;

            bool checkCertificateRevocation = true;

            _sslStream.AuthenticateAsServer(certificate, requireClientCertificate, enabledSslProtocols, checkCertificateRevocation); 

        }

    }
}
