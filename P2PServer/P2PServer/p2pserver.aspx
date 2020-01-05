<%@ Page Language="C#"  %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Data.SqlClient" %>
<%@ Import Namespace="Newtonsoft.Json" %>
<%@ Import Namespace="P2PLibrary" %>
<%
    string error = null;
    Response.ContentType = "text/json";
    try
    {
        string constr = System.Configuration.ConfigurationManager.ConnectionStrings["P2PDBConnectionString"].ConnectionString;
        using (SqlConnection connection = new SqlConnection(constr))
        {
            connection.Open();

            Model model = new Model(connection);

            model.EnsureDatabase();
            model.RemoveOldEntries();

            if (Request.HttpMethod == "POST")
            {
                byte[] buffer = new byte[5000];
                int len = Request.InputStream.Read(buffer, 0, 5000);
                if (len == 5000)
                {
                    throw new Exception("Input stream is too long");
                }
                string json = System.Text.Encoding.UTF8.GetString(buffer, 0, len);

                Client c = (Client)JsonConvert.DeserializeObject(json, typeof(Client));
                c.RemoteAddress = Request.ServerVariables["REMOTE_ADDR"];
                c.RemotePort = Request.ServerVariables["REMOTE_PORT"];

                string type = Request.QueryString["type"];
                if (type == "add")
                {
                    // then this is a new record we are adding...
                    Client f = model.FindClientByName(c.Name);
                    if (f != null)
                    {
                        // then this is an update!
                        f.LocalAddress = c.LocalAddress;
                        f.LocalPort = c.LocalPort;
                        f.RemoteAddress = c.RemoteAddress;
                        f.RemotePort = c.RemotePort;
                        model.SaveChanges();
                    }
                    else
                    {
                        model.AddClient(c);
                    }
                    c.Message = "added";
                    Response.Write(JsonConvert.SerializeObject(c));
                }
                else if (type == "find")
                {
                    // then we are looking for matching rendezvous by name
                    Client f = model.FindClientByName(c.Name);
                    if (f != null)
                    {
                        Response.Write(JsonConvert.SerializeObject(f));
                    }
                    else
                    {
                        c = null;
                        error = "not found";
                    }
                }
                else if (type == "delete")
                {
                    // then we are looking for matching rendezvous by name
                    Client f = model.FindClientByName(c.Name);
                    if (f != null)
                    {
                        model.RemoveClient(f);

                        f.Message = "deleted";
                        Response.Write(JsonConvert.SerializeObject(f));
                    }
                    else
                    {
                        c = null;
                        error = "not found";
                    }
                }
                else
                {
                    error = "Unknown query string type: " + type;
                }
            }
            else
            {
                error = "expecting HTTP POST method";
            }
        }
    }
    catch (Exception ex)
    {
        error = ex.ToString();
    }

    if (error != null)
    {
        StringWriter sw = new StringWriter();
        JsonTextWriter writer = new JsonTextWriter(sw);
        writer.WriteStartObject();
        writer.WritePropertyName("error");
        writer.WriteValue(error);
        writer.WriteEndObject();
        Response.Write(sw.ToString());
    }
%>