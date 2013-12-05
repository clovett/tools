<%@ Page LANGUAGE="C#" SRC="XmlCsvReader.cs" validateRequest=false %>
<%@Import Namespace="System"%>
<%@Import Namespace="System.Xml"%>
<%@Import Namespace="System.IO"%>
<%@Import Namespace="Microsoft.Xml"%>
<script runat="server">
void SubmitBtn_Click(Object Src, EventArgs E) 
{
  Uri uri = new Uri("file://" + Server.MapPath("."));
  XmlCsvReader reader = new XmlCsvReader(new StringReader(DATA.InnerText), uri, new NameTable());
  reader.RootName = ROOTNAME.Value;
  reader.RowName = ROWNAME.Value;
  reader.ColumnsAsAttributes = ASATTR.Checked;
  reader.FirstRowHasColumnNames = HASCOLNAMES.Checked;
  StringWriter output = new StringWriter();
  XmlTextWriter w = new XmlTextWriter(output);
  w.Formatting = Formatting.Indented;
  while (reader.Read()) 
  {
    w.WriteNode(reader, false);
  }
  w.Close();
  XML.InnerText =  output.ToString();
}
</script>
<form runat="server" method="POST" action="demo.aspx">
  <h4 style="margin:0;background-color:navy;color:white">Data</h4>
  <textarea runat="server" rows="10" cols="70" id="DATA">
"id","lname","fname"
1,"Nowmer","Sheri"
2,"Whelply","Derrick"
3,"Derry","Jeanne"
4,"Spence","Michael"
5,"Gutierrez","Maya"
6,"Damstra","Robert"
7,"Kanagaki","Rebecca"
8,"Brunner","Kim"
9,"Blumberg","Brenda"
10,"Stanz","Darren"
  </textarea>
  <h4 style="margin:0;background-color:navy;color:white">Xml</h4>
  <textarea runat="server" rows="10" cols="70" id="XML"></textarea>
  <br />
  Root element name: <input type="text" value="root" runat="server" id="ROOTNAME" size="15" /><br />
  Row element name: <input type="text" value="item" runat="server" id="ROWNAME" size="15" /><br />
  <asp:checkbox text="As Attributes" checked="true" runat="server" id="ASATTR" /><br />
  <asp:checkbox text="First row is column names" checked="true" runat="server" id="HASCOLNAMES" /><br />
  <asp:button text="SUBMIT" Onclick="SubmitBtn_Click" runat="server" />
  <%
// Dynamically figure out where we are on the server so we don't hard code this fact in the href.
Uri one = new Uri("file://"+Server.MapPath("/"));
Uri two = new Uri("file://"+Server.MapPath("."));
string path = one.MakeRelative(two).Substring(2);
%>
  <br />
  <br />
  See <a href="/srcview/srcview.aspx?path=<%=path%>/XmlCsvReader.src&file=Demo.aspx">
    Source Code</a> for this page.
</form>
