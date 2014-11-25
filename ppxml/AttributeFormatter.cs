using System;
using System.Xml;
using System.IO;
using System.Text;

//===========================================================================
/// <summary>
/// This custom XmlWriter puts attributes on a new line which makes the 
/// XML files with lots of attributes easier to read.
/// </summary>
class AttributeFormattingXmlWriter : XmlTextWriter 
{
    int depth;
    TextWriter tw;
    string    indent;

    public AttributeFormattingXmlWriter(TextWriter tw) : base(tw) 
    {        
      this.tw = tw;
      this.Formatting = Formatting.Indented;
    }

    string IndentString 
    {
        get 
        {
            if (this.indent == null) 
            {
                StringBuilder sb = new StringBuilder();
                for (int j = 0; j < this.Indentation; j++) 
                {
                    sb.Append(this.IndentChar);
                }
                this.indent = sb.ToString();
            }
            return this.indent;
        }
    }

    public override void WriteEndAttribute() 
    {
        base.WriteEndAttribute();
        this.tw.WriteLine();
        for (int i = 0; i < depth; i++) 
        {
            this.tw.Write( this.IndentString );
        }      
    }

    public override void WriteStartElement(string prefix, string localName, string ns) 
    {
        base.WriteStartElement(prefix, localName, ns);
        this.depth++;
    }

    public override void WriteEndElement() 
    {
        base.WriteEndElement();
        this.depth--;
    }
}