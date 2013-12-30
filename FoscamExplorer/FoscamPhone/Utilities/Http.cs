using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    class HttpClient
    {
        HttpClientHandler settings;

        public HttpClient(HttpClientHandler settings)
        {
            this.settings = settings;
        }

        public async Task<HttpResponseMessage> GetAsync(Uri url, HttpCompletionOption option)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.CreateHttp(url);

            if (settings.Credentials != null)
            {
                req.Credentials = settings.Credentials;
            }

            HttpResponseMessage msg = null;

            await Task.Run(new Action(() =>
            {
                ManualResetEvent evt = new ManualResetEvent(false);

                try
                {
                    req.BeginGetResponse(new AsyncCallback((ar) =>
                    {
                        // IAsyncResult ar
                        try
                        {
                            HttpWebResponse response = (HttpWebResponse)req.EndGetResponse(ar);
                            msg = new HttpResponseMessage(response);
                        }
                        catch (Exception ex)
                        {
                            msg = new HttpResponseMessage(ex);
                        }
                        evt.Set();
                    }), req);

                }
                catch (Exception ex)
                {
                    msg = new HttpResponseMessage(ex);
                }

                evt.WaitOne();
            }));

            return msg;
        }
    }

    class HttpResponseMessage
    {
        HttpWebResponse response;
        Exception error;

        public HttpResponseMessage(Exception error)
        {
            this.error = error;

            StatusCode = HttpStatusCode.ServiceUnavailable;

            WebException we = error as WebException;
            if (we != null)
            {
                HttpWebResponse wr = we.Response as HttpWebResponse;
                if (wr != null)
                {
                    StatusCode = wr.StatusCode;
                }
            }
        }

        public HttpResponseMessage(HttpWebResponse response)
        {
            this.response = response;
            this.StatusCode = response.StatusCode;
            this.Content = new HttpContent(response);
        }

        public HttpContent Content
        {
            get;
            set;
        }

        public HttpStatusCode StatusCode { get; set; }
    }

    enum HttpCompletionOption
    {
        ResponseHeadersRead
    }

    class HttpContent : IDisposable
    {        
        HttpWebResponse response;

        public HttpContent(HttpWebResponse response)
        {
            this.response = response;
            this.Headers = new HttpHeaderCollection(response.Headers);
        }

        public void Dispose()
        {
            response.Dispose();
        }

        public HttpHeaderCollection Headers { get; set; }


        internal async Task<Stream> ReadAsStreamAsync()
        {
            Stream stream = response.GetResponseStream();

            await Task.Delay(1);

            return stream;

        }

        internal async Task<string> ReadAsStringAsync()
        {
            string encodingName = response.Headers[HttpRequestHeader.ContentEncoding];
            if (encodingName == null) 
            {
                encodingName = "UTF-8";
            }
            Encoding encoding = Encoding.GetEncoding(encodingName);
            string result = null;

            using (Stream stream = response.GetResponseStream())
            {
                using (StreamReader reader = new StreamReader(stream, encoding))
                {
                    result = reader.ReadToEnd();
                }
            }

            await Task.Delay(1);

            return result;
        }
    }

    class HttpHeaderCollection 
    {
        System.Net.WebHeaderCollection col;

        public HttpHeaderCollection(System.Net.WebHeaderCollection col)
        {
            this.col = col;
        }

        public MediaTypeHeaderValue ContentType
        {
            get
            {
                string ct = col[HttpRequestHeader.ContentType];
                MediaTypeHeaderValue value;
                MediaTypeHeaderValue.TryParse(ct, out value);
                return value;
            }
        }
    }

    class HttpClientHandler
    {
        public NetworkCredential Credentials { get; set; }
    }

    class HttpRequestException : Exception
    {
    }
    // Summary:
    //     Represents a media-type as defined in the RFC 2616.
    public class MediaTypeHeaderValue
    {
        List<NameValueHeaderValue> parameters = new List<NameValueHeaderValue>();

        public string CharSet { get; set; }

        public string MediaType { get; set; }

        public ICollection<NameValueHeaderValue> Parameters { get { return parameters; } }

        public static bool TryParse(string input, out MediaTypeHeaderValue parsedValue)
        {
            // e.g.  multipart/x-mixed-replace;boundary=ipcamera
            parsedValue = new MediaTypeHeaderValue();
            
            string[] parts = input.Split(';');
            parsedValue.MediaType = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                NameValueHeaderValue nameValue;
                if (NameValueHeaderValue.TryParse(parts[i], out nameValue))
                {
                    if (nameValue.Name == "charset")
                    {
                        parsedValue.CharSet = nameValue.Value;
                    }
                    parsedValue.parameters.Add(nameValue);
                }
            }
            return true;
        }
    }


    // Summary:
    //     Represents a name/value pair.
    public class NameValueHeaderValue
    {
        public NameValueHeaderValue(string name, string value)
        {
            this.Name = name.Trim();
            this.Value = value.Trim();
        }

        public string Name { get; private set;  }

        public string Value { get; set; }

        public static bool TryParse(string input, out NameValueHeaderValue parsedValue)
        {
            parsedValue = null;
            int i = input.IndexOf('=');
            if (i >= 0)
            {
                parsedValue = new NameValueHeaderValue(input.Substring(0, i), input.Substring(i + 1));
                return true;
            }
            return false;
        }
    }
}
