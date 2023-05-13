using System;
using System.IO;
using System.Collections;
using System.Text;
using System.Net;
using System.Xml;
using System.Xml.Schema;

namespace dtd2xsd {
  public enum LiteralType {
    CDATA, SDATA, PI
  };

  public class Entity {
    public const Char EOF = (char)65535;
    public string Proxy;

    public Entity(string name, string pubid, string uri, string ndata, string proxy) {
      Name = name;
      PublicId = pubid;
      Uri = uri;
      Ndata = ndata;
      Proxy = proxy;
    }

    public Entity(string name, string literal) {
      Name = name;
      Literal = literal;
      Internal = true;
    }

    public Entity(string name, Uri baseUri, TextReader stm, string proxy) {
      Name = name;
      Internal = true;
      _stm = stm;
      _resolvedUri = baseUri;
      Proxy = proxy;
    }

    public string Name;
    public bool Internal;
    public string PublicId;
    public string Ndata;
    public string Uri;
    public string Literal;
    public LiteralType LiteralType;
    public Entity Parent;

    public Uri ResolvedUri {
      get {
        if (_resolvedUri != null) return _resolvedUri;
        else if (Parent != null) return Parent.ResolvedUri;
        return null;
      }
    }

    Uri _resolvedUri;
    TextReader _stm;

    public int Line;
    int _LineStart;
    int _absolutePos;
    public char Lastchar;
    public bool IsWhitespace;

    public int LinePosition {
      get { return _absolutePos - _LineStart + 1; }
    }

    public char ReadChar() {
      char ch = (char)_stm.Read();
      _absolutePos++;
      if (ch == 0xa) {
        IsWhitespace = true;
        _LineStart = _absolutePos+1;
        Line++;
      } 
      else if (ch == ' ' || ch == '\t') {
        IsWhitespace = true;
        if (Lastchar == 0xd) {
          _LineStart = _absolutePos;
          Line++;
        }
      }
      else if (ch == 0xd) {
        if (Lastchar == 0xd) {
          _LineStart = _absolutePos;
          Line++;
        }
        IsWhitespace = true;
      }
      else {
        IsWhitespace = false;
        if (Lastchar == 0xd) {
          Line++;
          _LineStart = _absolutePos;
        }
      } 
      Lastchar = ch;
      return ch;
    }

    public void Open(Entity parent, Uri baseUri) {
      Parent = parent;
      this.Line = 1;
      if (Internal) {
        if (this.Literal != null) {
          _stm = new StringReader(this.Literal);
        }
        _resolvedUri = baseUri;
      } 
      else if (this.Uri == null) {
        this.Error("Unresolvable entity '{0}'", this.Name);
      }
      else {
        if (baseUri != null) {
          _resolvedUri = new Uri(baseUri, this.Uri);				
        }
        else {
          _resolvedUri = new Uri(this.Uri);
        }
        Stream rawStream = null;
        switch (_resolvedUri.Scheme) {
          case "file": {
            string path = _resolvedUri.LocalPath;
            rawStream = new FileStream(path, FileMode.Open, FileAccess.Read);
          }
            break;
          case "http":
            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(ResolvedUri);
            wr.Timeout = 5000;
            if (Proxy != null) wr.Proxy = new WebProxy(Proxy);
            WebResponse resp = wr.GetResponse();
            Uri actual = resp.ResponseUri;
            if (actual.AbsoluteUri != _resolvedUri.AbsoluteUri) {
              _resolvedUri = actual;
            }	
					  rawStream = resp.GetResponseStream();
            break;
          default:
            this.Error("Unsupported Uri Scheme '{0}'", _resolvedUri.Scheme);
            break;
        }
        _stm = new XmlStream(rawStream);
      }
    }

    public void Close() {
      _stm.Close();
    }

    public char SkipWhitespace() {
      char ch = Lastchar;
      while (ch == ' ' || ch == '\r' || ch == '\n' || ch == '\t') {
        ch = ReadChar();
      }
      return ch;
    }

    public string ScanToken(StringBuilder sb, string term, bool nmtoken) {
      sb.Length = 0;
      char ch = Lastchar;
      if (nmtoken && !XmlCharType.IsStartNameChar(ch)) {
        throw new Exception(
          String.Format("Invalid name start character '{0}'", ch));
      }
      while (ch != Entity.EOF && term.IndexOf(ch)<0) {
        if (!nmtoken || XmlCharType.IsNameChar(ch)) {
          sb.Append(ch);
        } 
        else {
          throw new Exception(
            String.Format("Invalid name character '{0}'", ch));
        }
        ch = ReadChar();
      }
      return sb.ToString();
    }

    public string ScanToEnd(StringBuilder sb, string type, string end) {
      if (sb != null) sb.Length = 0;
      int start = Line;
      // This method scans over a chunk of text looking for the
      // termination sequence specified by the 'end' parameter.
      char ch = ReadChar();            
      int state = 0;
      char next = end[state];
      while (ch != Entity.EOF) {
        if (Char.ToLower(ch) == Char.ToLower(next)) {
          state++;
          if (state >= end.Length) {
            // found it!
            break;
          }
          next = end[state];
        } 
        else if (state > 0) {
          // char didn't match, so go back and see how much does still match.
          int i = state - 1;
          int newstate = 0;
          while (i>=0 && newstate==0) {
            if (end[i] == ch) {
              // character is part of the end pattern, ok, so see if we can
              // match all the way back to the beginning of the pattern.
              int j = 1;
              while( i-j>=0) {
                if (end[i-j] != end[state-j])
                  break;
                j++;
              }
              if (j>i) {
                newstate = i+1;
              }
            } 
            else {
              i--;
            }
          }
          if (sb != null) {
            i = (i<0) ? 1 : 0;
            for (int k = 0; k <= state-newstate-i; k++) {
              sb.Append(end[k]); 
            }
            if (i>0) // see if we've matched this char or not
              sb.Append(ch); // if not then append it to buffer.
          }
          state = newstate;
          next = end[newstate];
        }
        else {
          if (sb != null) sb.Append(ch);
        }
        ch = ReadChar();
      }
      if (ch == Entity.EOF) Error(type + " starting on line {0} was never closed", start);      
      if (sb != null) return sb.ToString();
      return "";
    }
    public void Error(string msg) {
      throw new Exception(msg);
    }

    public void Error(string msg, char ch) {
      string str = (ch == Entity.EOF) ? "EOF" : Char.ToString(ch);			
      throw new Exception(String.Format(msg, str));
    }

    public void Error(string msg, int x) {
      throw new Exception(String.Format(msg, x));
    }

    public void Error(string msg, string str) {
      throw new Exception(String.Format(msg, str));
    }

    public string Context() {
      Entity p = this;
      StringBuilder sb = new StringBuilder();
      while (p != null) {
        string msg;
        if (p.Internal) {
          msg = String.Format("\nReferenced on line {0}, position {1} of internal entity '{2}'", p.Line, p.LinePosition, p.Name);
        } 
        else if (p.Name == null || p.Name.Length == 0) {
          msg = String.Format("\nReferenced on line {0}, position {1} in [{2}]", p.Line, p.LinePosition, p.ResolvedUri.AbsolutePath);
        } else {
          msg = String.Format("\nReferenced on line {0}, position {1} of '{2}' entity at [{3}]", p.Line, p.LinePosition, p.Name, p.ResolvedUri.AbsolutePath);
        }
        sb.Append(msg);
        p = p.Parent;
      }
      return sb.ToString();
    }

    public static bool IsLiteralType(string tok) {
      return (tok == "CDATA" || tok == "SDATA" || tok == "PI");
    }

    public void SetLiteralType(string tok) {
      switch (tok) {
        case "CDATA":
          LiteralType = LiteralType.CDATA;
          break;
        case "SDATA":
          LiteralType = LiteralType.SDATA;
          break;
        case "PI":
          LiteralType = LiteralType.PI;
          break;
      }
    }
  }

  public class Notation {
    public Notation(string name, string pubid, string syslit) {
      this.Name = name;
      this.PublicId = pubid;
      this.SysLit = syslit;
    }

    public string Name;
    public string PublicId;
    public string SysLit;
  }

  public class ElementDecl {
    public ElementDecl(string name, bool sto, bool eto, ContentModel cm, string[] inclusions, string[] exclusions) {
      Name = name;
      StartTagOptional = sto;
      EndTagOptional = eto;
      ContentModel = cm;
      Inclusions = inclusions;
      Exclusions = exclusions;
    }
    public string Name;
    public bool StartTagOptional;
    public bool EndTagOptional;
    public ContentModel ContentModel;
    public string[] Inclusions;
    public string[] Exclusions;

    public AttList AttList;

    public void AddAttDefs(AttList list) {
      if (AttList == null) {
        AttList = list;
      } 
      else {				
        foreach (AttDef a in list) {
          if (AttList[a.Name] == null) {
            AttList.Add(a);
          }
        }
      }
    }

    public XmlSchemaElement GetSchemaElement(SchemaBuilder sb) {
      XmlSchemaElement e = new XmlSchemaElement();
      e.Name = this.Name;
      
      XmlSchemaComplexType ct = null;
      XmlSchemaSimpleContentExtension sce = null;
      XmlSchemaObjectCollection attributes = null;

      if (ContentModel != null) {
        if (ContentModel.IsComplex) {
          ct = ContentModel.GetComplexType(sb);
          attributes = ct.Attributes;
          e.SchemaType = ct;    
        } else if (ContentModel.IsTextOnly) {
          if (AttList != null) {
            ct = new XmlSchemaComplexType();
            XmlSchemaSimpleContent sc = new XmlSchemaSimpleContent();
            sce = new XmlSchemaSimpleContentExtension();
            sce.BaseTypeName = new XmlQualifiedName("string", Dtd.XsdNamespace);
            attributes = sce.Attributes;
            sc.Content = sce;
            ct.ContentModel = sc;
            e.SchemaType = ct;
          } else {
            e.SchemaTypeName = new XmlQualifiedName("string", Dtd.XsdNamespace);
          }
        } else if (ContentModel.IsEmpty) {
          // Create empty complex type.
          ct = new XmlSchemaComplexType();
          attributes = ct.Attributes;
          e.SchemaType = ct;
        }
      }
      if (AttList != null) {
        if (attributes == null) {
          ct = new XmlSchemaComplexType();
          attributes = ct.Attributes;
          e.SchemaType = ct;
        }
        foreach (AttDef ad in AttList) {
          XmlSchemaAttribute a = ad.GetSchemaAttribute(sb);
          if (a != null) {
            attributes.Add(a);
          }
        }

        if (AttList.PELists != null) {
          foreach (AttList elist in AttList.PELists) {
            string name = elist.PE.Name;
            AddAttributeGroup(sb, elist);
            XmlSchemaAttributeGroupRef aref = new XmlSchemaAttributeGroupRef();
            aref.RefName = new XmlQualifiedName(name, sb.s.TargetNamespace);
            attributes.Add(aref);  
          }
        }
      }
      return e;
    }

    public void AddAttributeGroup(SchemaBuilder sb, AttList elist) {
      string name = elist.PE.Name;
      if (sb.FindAttributeGroup(name) != null)
        return; // already done

      XmlSchemaAttributeGroup ag = new XmlSchemaAttributeGroup();
      ag.Name = name;

      foreach (AttDef ad in elist) {
        XmlSchemaAttribute a = ad.GetSchemaAttribute(sb);
        if (a != null) {
            ag.Attributes.Add(a);
        }
      }
      if (elist.PELists != null) {
        foreach (AttList child in elist.PELists) {
          name = child.PE.Name;
          AddAttributeGroup(sb, child);
          XmlSchemaAttributeGroupRef aref = new XmlSchemaAttributeGroupRef();
          aref.RefName = new XmlQualifiedName(name, sb.s.TargetNamespace);
          ag.Attributes.Add(aref);
        }
      }
      sb.s.Items.Add(ag);
    }


  }

  public enum DeclaredContent {
    Default, CDATA, RCDATA, EMPTY, ANY
  }

  public class ContentModel {
    public DeclaredContent DeclaredContent;
    public int CurrentDepth;
    public Group Model;

    public ContentModel() {
      Model = new Group(null);
    }

    public void PushParameterEntity(Entity e) {
      PushGroup();
      Model.PE = e;
    }

    public void PopParameterEntity() {
      if (Model.IsTextOnly) {
        // then this entity just contained "#PCDATA" which is not
        // worth preserving in a group, so we just pick up the
        // mixed status and ignore it (we drop the extra Group node).
        if (Model.Parent == null) {
          // must be just "#PCDATA". 
        } else {
          Model = Model.Parent;          
        }
        CurrentDepth--;
      } else {
        PopGroup();
      }
    }

    public void PushGroup() {
      Model = new Group(Model);
      CurrentDepth++;
    }

    public int PopGroup() {
      if (CurrentDepth == 0) return -1;
      CurrentDepth--;
      Model.Parent.AddGroup(Model);
      Model = Model.Parent;
      return CurrentDepth;
    }

    public void AddSymbol(string sym) {
      Model.AddSymbol(sym);
    }

    public void AddConnector(char c) {
      Model.AddConnector(c);
    }

    public void AddOccurrence(char c) {
      Model.AddOccurrence(c);
    }

    public void SetDeclaredContent(string dc, bool sgml) {
      switch (dc) {
        case "EMPTY":
          this.DeclaredContent = DeclaredContent.EMPTY;
          break;
        case "RCDATA": 
          if (!sgml) goto default; // sgml only
          this.DeclaredContent = DeclaredContent.RCDATA;
          break;
        case "CDATA": 
          if (!sgml) goto default; // sgml only
          this.DeclaredContent = DeclaredContent.CDATA;
          break;
        case "ANY": 
          this.DeclaredContent = DeclaredContent.ANY;
          break;
        default:
          throw new Exception(
            String.Format("Declared content type '{0}' is not supported", dc));
      }
    }

    public bool IsComplex {
      get { return this.Model.IsComplex || IsAny; }
    }
    public bool IsAny {
      get { return this.DeclaredContent == DeclaredContent.ANY; }
    }
    public bool IsTextOnly {
      get { return (DeclaredContent == DeclaredContent.CDATA || 
              DeclaredContent == DeclaredContent.RCDATA || this.Model.IsTextOnly); }
    }
    public bool IsMixed {
      get { return this.Model.IsMixed; }
    }
    public bool IsEmpty {
      get { return (DeclaredContent == DeclaredContent.EMPTY || this.Model.IsEmpty); }
    }

    public XmlSchemaComplexType GetComplexType(SchemaBuilder sb) {
      XmlSchemaComplexType ct = new XmlSchemaComplexType();
      if (Model.IsMixed) {
        ct.IsMixed = true;
      }
      if (IsAny) {
        XmlSchemaSequence seq = new XmlSchemaSequence();
        XmlSchemaAny any = new XmlSchemaAny();
        any.MinOccurs = 0;
        any.MaxOccursString = "unbounded";
        seq.Items.Add(any);
        ct.Particle = seq;
        ct.IsMixed = true; // DTD ANY also means mixed!
      } else {
        ct.Particle = Model.GetParticle(sb);
      }
      return ct;
    }

  }

  public enum GroupType {
    None, Entity, And, Or, Sequence 
  };

  public enum Occurrence {
    Required, Optional, ZeroOrMore, OneOrMore
  }

  public class Group {
    public Group Parent;
    public ArrayList Members;
    public GroupType GroupType;
    public Occurrence Occurrence;
    bool mixed;
    public Entity PE; // that was expanded while parsing this content model

    public Group(Group parent) {
      Parent = parent;
      Members = new ArrayList();      
      this.GroupType = GroupType.None;
      Occurrence = Occurrence.Required;
    }
    
    public void AddGroup(Group g) {
      Members.Add(g);
    }
    public void AddSymbol(string sym) {
      if (sym == "#PCDATA") {				
        mixed = true;
        Group p = Parent;
        while (p != null) {
          p.mixed = true;
          p = p.Parent;
        }
      } 
      else {
        Members.Add(sym);
      }
    }
    public bool IsMixed {
      get { return (Members.Count > 0 && mixed); } 
      set { mixed = true; }
    }
    public bool IsTextOnly {
      get { return (Members.Count == 0 && mixed); } 
    }
    public bool IsEmpty {
      get { return Members.Count == 0; }
    }
    public bool IsComplex {
      get { return Members.Count > 0; }
    }

    public void AddConnector(char c) {
      if (!mixed && Members.Count == 0) {
        throw new Exception(
          String.Format("Missing token before connector '{0}'.", c)
          );
      }
      GroupType gt = GroupType.None;
      switch (c) {
        case ',': 
          gt = GroupType.Sequence;
          break;
        case '|':
          gt = GroupType.Or;
          break;
        case '&':
          gt = GroupType.And;
          break;
      }
      if (GroupType != GroupType.None && GroupType != gt) {
        throw new Exception(
          String.Format("Connector '{0}' is inconsistent with {1} group.", c, GroupType.ToString())
          );
      }
      GroupType = gt;
    }

    public void AddOccurrence(char c) {
      Occurrence o = Occurrence.Required;
      switch (c) {
        case '?': 
          o = Occurrence.Optional;
          break;
        case '+':
          o = Occurrence.OneOrMore;
          break;
        case '*':
          o = Occurrence.ZeroOrMore;
          break;
      }
      Occurrence = o;
    }

    public XmlSchemaParticle GetParticle(SchemaBuilder sb) {
            
      if (PE != null) {
        XmlQualifiedName gname = new XmlQualifiedName(PE.Name, sb.s.TargetNamespace);
        XmlSchemaGroup g = sb.FindGroup(PE.Name);
        if (g == null) {
          g = new XmlSchemaGroup();
          g.Name = PE.Name;
          g.Particle = GetGroupBase(sb);
          if (g.Particle != null) {
            sb.s.Items.Add(g);
          } else {
            g = null;
          }
        }        
        if (g != null) {
          string t = g.Particle.GetType().Name;
          XmlSchemaGroupRef gref = new XmlSchemaGroupRef();
          gref.RefName = new XmlQualifiedName(PE.Name, sb.s.TargetNamespace);
          SetOccurrence(gref);
          return gref;
        }
        return null;
      } 
      return GetGroupBase(sb);
    }

    public XmlSchemaGroupBase GetGroupBase(SchemaBuilder sb) {
      XmlSchemaGroupBase p = null;
      if (Members.Count>0) {
        switch (GroupType) {
          case GroupType.None: 
            // this Group was created because of (...)* wrapper.
            goto case GroupType.Sequence; 
          case GroupType.Sequence: 
            p = new XmlSchemaSequence();
            SetOccurrence(p);
            break;
          case GroupType.Or:
            p = new XmlSchemaChoice();
            SetOccurrence(p);
            break;
          case GroupType.And:
            p = new XmlSchemaAll();
            SetOccurrence(p);
            break;
        }       
        if (Members != null) {
          foreach (object m in Members) {
            if (m is string) {
              XmlSchemaElement e = new XmlSchemaElement();
              e.RefName = new XmlQualifiedName((string)m, sb.s.TargetNamespace);
              p.Items.Add(e);
            } else {            
              Group child = (Group)m;
              XmlSchemaParticle pc = child.GetParticle(sb);
              if (pc is XmlSchemaSequence) {
                // flatten single element sequences.
                XmlSchemaSequence seq = (XmlSchemaSequence)pc;
                if (seq.Items.Count == 1 && seq.Items[0] is XmlSchemaElement) {
                  XmlSchemaElement se = (XmlSchemaElement)seq.Items[0];
                  child.SetOccurrence(se);
                  p.Items.Add (se);
                } else if (p is XmlSchemaAll) {         
                  ExtractElements(pc, p); // XmlSchemaAll can only contain elements
                } else {
                  p.Items.Add(pc);
                }
              } else if (pc is XmlSchemaGroupBase && 
                pc.GetType() == p.GetType()) {
                // optimize away redundant containers.
                XmlSchemaGroupBase b = (XmlSchemaGroupBase)pc;
                foreach (XmlSchemaParticle pcc in b.Items) {
                  child.SetOccurrence(pcc);
                  p.Items.Add(pcc);
                }
              } else if (pc != null) {
                if (p is XmlSchemaAll) {         
                  ExtractElements(pc, p); // XmlSchemaAll can only contain elements
                } else {
                  p.Items.Add(pc);                  
                }
              }
            }
          }
        }
      }
      return p;
    }

    void ExtractElements(XmlSchemaParticle pc, XmlSchemaGroupBase p) {
      if (pc is XmlSchemaElement) {
        p.Items.Add(pc);
      } else if (pc is XmlSchemaGroupBase) {
        foreach (XmlSchemaParticle p2 in ((XmlSchemaGroupBase)pc).Items) {
          ExtractElements(p2, p);
        }
      } else {
        throw new NotImplementedException();
      }
    }

    void SetOccurrence(XmlSchemaParticle p) {
      switch (Occurrence) {
        case Occurrence.OneOrMore:
          p.MinOccurs = 1;
          p.MaxOccursString = "unbounded";
          break;
        case Occurrence.ZeroOrMore: 
          p.MinOccurs = 0;
          p.MaxOccursString = "unbounded";
          break;
        case Occurrence.Optional:
          p.MinOccurs = 0;
          p.MaxOccurs = 1;
          break;
        case Occurrence.Required:
          break;
      }
    }
  }


  public enum AttributeType {
    DEFAULT, CDATA, ENTITY, ENTITIES, ID, IDREF, IDREFS, NAME, NAMES, NMTOKEN, NMTOKENS, 
    NUMBER, NUMBERS, NUTOKEN, NUTOKENS, NOTATION, ENUMERATION
  }

  public enum AttributePresence {
    DEFAULT, FIXED, REQUIRED, IMPLIED
  }

  public class AttDef {
    public string Name;
    public AttributeType Type;
    public string[] EnumValues;
    public string Default;
    public AttributePresence Presence;

    public AttDef(string name) {
      Name = name;
    }


    public void SetType(string type) {
      switch (type) {
        case "CDATA":
          Type = AttributeType.CDATA;
          break;
        case "ENTITY":
          Type = AttributeType.ENTITY;
          break;
        case "ENTITIES":
          Type = AttributeType.ENTITIES;
          break;
        case "ID":
          Type = AttributeType.ID;
          break;
        case "IDREF":
          Type = AttributeType.IDREF;
          break;
        case "IDREFS":
          Type = AttributeType.IDREFS;
          break;
        case "NAME":
          Type = AttributeType.NAME;
          break;
        case "NAMES":
          Type = AttributeType.NAMES;
          break;
        case "NMTOKEN":
          Type = AttributeType.NMTOKEN;
          break;
        case "NMTOKENS":
          Type = AttributeType.NMTOKENS;
          break;
        case "NUMBER":
          Type = AttributeType.NUMBER;
          break;
        case "NUMBERS":
          Type = AttributeType.NUMBERS;
          break;
        case "NUTOKEN":
          Type = AttributeType.NUTOKEN;
          break;
        case "NUTOKENS":
          Type = AttributeType.NUTOKENS;
          break;
        default:
          throw new Exception("Attribute type '"+type+"' is not supported");
      }
    }

    public bool SetPresence (string token) {
      bool hasDefault = true;
      if (token == "FIXED") {
        Presence = AttributePresence.FIXED;				
      } 
      else if (token == "REQUIRED") {
        Presence = AttributePresence.REQUIRED;
        hasDefault = false;
      }
      else if (token == "IMPLIED") {
        Presence = AttributePresence.IMPLIED;
        hasDefault = false;
      }
      else {
        throw new Exception(String.Format("Attribute value '{0}' not supported", token));
      }
      return hasDefault;
    }

    public XmlSchemaAttribute GetSchemaAttribute(SchemaBuilder sb) {
      XmlSchemaAttribute a = new XmlSchemaAttribute();
      if (this.Name.IndexOf(':')>0) {
        string[] sa = this.Name.Split(':');
        if (sa.Length>2) {
          // todo: perhaps we could switch to attributeForm="unqualified" at this point.
          throw new Exception("Cannot handle multiple colons in an attribute name '" + this.Name + "'");
        }
        string ns = sb.AddImport(sa[0]);
        if (ns != null) {
          a.RefName = new XmlQualifiedName(sa[1], ns);
        } else {
          a = null; // unmappable...
        }
        // ref attributes cannot specify all the stuff below,
        // so we're done at this point.
        return a;
      }
      if (this.Name == "xmlns") {
        // ignore these attributes.
        return null;
      }
      a.Name = this.Name;
      switch (this.Presence) {
        case AttributePresence.REQUIRED:
          a.Use = XmlSchemaUse.Required;
          break;
        case AttributePresence.FIXED:
          a.FixedValue = this.Default;
          break;
        case AttributePresence.DEFAULT:
          a.DefaultValue = this.Default;
          break;
      }
      // BUGBUG: need to special case some things
      switch (this.Type) {
        case AttributeType.CDATA: goto case AttributeType.DEFAULT;
        case AttributeType.DEFAULT:
          a.SchemaTypeName = new XmlQualifiedName("string",Dtd.XsdNamespace);
          break;
        case AttributeType.ENTITIES: goto case AttributeType.NMTOKENS;
        case AttributeType.ENTITY:goto case AttributeType.NMTOKENS;
        case AttributeType.ID:goto case AttributeType.NMTOKENS;
        case AttributeType.IDREF:goto case AttributeType.NMTOKENS;
        case AttributeType.IDREFS:goto case AttributeType.NMTOKENS;
        case AttributeType.NMTOKEN:goto case AttributeType.NMTOKENS;
        case AttributeType.NMTOKENS:
          a.SchemaTypeName = new XmlQualifiedName(this.Type.ToString(),Dtd.XsdNamespace);
          break;
        case AttributeType.NOTATION: goto case AttributeType.ENUMERATION;
        case AttributeType.ENUMERATION:
          XmlSchemaSimpleType st = new XmlSchemaSimpleType();
          a.SchemaType = st;
          XmlSchemaSimpleTypeRestriction restriction = new XmlSchemaSimpleTypeRestriction();            
          st.Content = restriction;
          if (this.Type ==AttributeType.NOTATION) {
            restriction.BaseTypeName = new XmlQualifiedName("NOTATION",Dtd.XsdNamespace);
          } else {
            restriction.BaseTypeName = new XmlQualifiedName("NMTOKEN",Dtd.XsdNamespace);
          }
          foreach( string s in this.EnumValues) {
            XmlSchemaEnumerationFacet e = new XmlSchemaEnumerationFacet();
            e.Value = s;
            restriction.Facets.Add(e);
          }
          break;
          // SGML types have no XSD equivalent.
          // we could potentially use "list" of "number"...
        case AttributeType.NAME:goto case AttributeType.DEFAULT;
        case AttributeType.NAMES:goto case AttributeType.DEFAULT;
        case AttributeType.NUMBER:goto case AttributeType.DEFAULT;
        case AttributeType.NUMBERS:goto case AttributeType.DEFAULT;
        case AttributeType.NUTOKEN:goto case AttributeType.DEFAULT;
        case AttributeType.NUTOKENS:goto case AttributeType.DEFAULT;            
      }        
      return a;
    }
  }

  // AttList is a list of AttDef's and/or AttLists.  Child AttLists
  // are created when parameter entities are expanded which contain 
  // the AttDefs.  By remembering where the parameter entities are
  // we are able to generate the right XSD AttributeGroups.
  public class AttList : IEnumerable {
    public Entity PE; // that was expanded while parsing this content model
    Hashtable AttDefs;
    public ArrayList PELists;
		
    public AttList() {
      AttDefs = new Hashtable();
    }

    public void Add(AttDef a) {
      if (!AttDefs.ContainsKey(a.Name)) {// let first declaration win
        AttDefs.Add(a.Name, a);
      }
    }

    public int Count {
      get { return AttDefs.Count; }
    }

    public void Add(AttList a) {
      if (PELists == null) PELists = new ArrayList();
      PELists.Add(a);
    }

    public AttDef this[string name] {
      get {
        return (AttDef)AttDefs[name];
      }
    }

    public IEnumerator GetEnumerator() {
      return AttDefs.Values.GetEnumerator();
    }

  }

  public class SchemaBuilder {
    public XmlSchema s;
    public TextWriter errorlog;

    public SchemaBuilder(XmlSchema s, TextWriter errorlog) {
      this.s = s;
      this.errorlog = errorlog;
    }


    public XmlSchemaGroup FindGroup(string n) {
      foreach (XmlSchemaObject o in s.Items) {
        if (o is XmlSchemaGroup) {
          XmlSchemaGroup g = o as XmlSchemaGroup;
          if (g.Name == n) return g;
        }
      }
      return null;
    }

    public XmlSchemaAttributeGroup FindAttributeGroup(string n) {
      foreach (XmlSchemaObject o in s.Items) {
        if (o is XmlSchemaAttributeGroup) {
          XmlSchemaAttributeGroup g = o as XmlSchemaAttributeGroup;
          if (g.Name == n) return g;
        }
      }
      return null;
    }

    public XmlSchemaImport FindImport(string ns) {
      foreach (XmlSchemaObject o in s.Includes) {
        if (o is XmlSchemaImport) {
          XmlSchemaImport i = o as XmlSchemaImport;
          if (i.Namespace == ns) return i;
        }
      }
      return null;
    }

    public string AddImport(string prefix) {
      if (prefix == "xmlns") {
        return null; // don't have to declare these.
      }
      foreach (XmlQualifiedName n in s.Namespaces.ToArray()) {
        if (n.Name == prefix)
          return null; // already defined
      }
      if (prefix == "xml") {
        // This one is sometimes built into the XSD processor, so it doesn't
        // need a real schema location.
        string ns = "http://www.w3.org/XML/1998/namespace";
        AddImport(prefix, ns, prefix + ".xsd");
        return ns;
      }

      errorlog.WriteLine("Unresolved namespace prefix '"+prefix+"'");
      errorlog.WriteLine("Use '-s' argument to import schema for this namespace.");
      return null;
    }

    public string AddImport(string prefix, string ns, string uri) {
      XmlSchemaImport si = FindImport(ns);
      if (si == null) {
        si = new XmlSchemaImport();
        si.Namespace = ns;
        si.SchemaLocation = uri;
        s.Includes.Add(si);
        if (prefix != "xml") {
          s.Namespaces.Add(prefix, ns);
        }
      }
      return ns;
    }

  }

  public class SchemaInclude {
    public string Prefix;
    public string NamespaceURI;
    public string URI;

    public SchemaInclude(string p, string ns, string uri) {
      this.Prefix = p;
      this.NamespaceURI = ns;
      this.URI = uri;
    }
  }

  public class Dtd {
    public string Name;

    Hashtable _elements;
    Hashtable _pentities;
    Hashtable _entities;
    Hashtable _notations;
    StringBuilder _sb;		
    Entity _current;
    XmlNameTable _nt;
    TextWriter _errorLog;
    bool _sgml;
    int _ConditionalSections;
    bool _preserveGroups;

    public Dtd(string name, XmlNameTable nt) {
      _nt = nt;
      if (name == null) name = String.Empty;
      if (_sgml) name = name.ToLower();
      Name = _nt.Add(name);
      _elements = new Hashtable();
      _pentities = new Hashtable();
      _entities = new Hashtable();
      _notations = new Hashtable();
      _sb = new StringBuilder();
      _sgml= false;
      _preserveGroups = true;
    }

    public XmlNameTable NameTable { get { return _nt; } }

    public static Dtd Parse(Uri baseUri, TextWriter errorLog, string name, string pubid, string url, string subset, string proxy, XmlNameTable nt, bool sgml, bool preserveGroups) {
      Dtd dtd = new Dtd(name, nt);
      dtd._errorLog = errorLog;
      dtd._sgml = sgml;
      dtd._preserveGroups = preserveGroups;
      if (url != null && url != "") {
        dtd.PushEntity(baseUri, new Entity(dtd.Name, pubid, url, null, proxy));
      }
      if (subset != null && subset != "") {
        dtd.PushEntity(baseUri, new Entity(name, subset));
      }
      try {
        dtd.Parse();
      } 
      catch (Exception e) {
        throw new Exception(e.Message + dtd._current.Context());
      }			
      return dtd;
    }

    public Entity FindEntity(string name) {
      return (Entity)_entities[name];
    }

    public ElementDecl FindElement(string name) {
      return (ElementDecl)_elements[name];
    }    

    public static string XsdNamespace = "http://www.w3.org/2001/XMLSchema";

    public XmlSchema GetSchema(string targetNamespace, SchemaInclude[] includes) {
      XmlSchema s = new XmlSchema();
      SchemaBuilder sb =new SchemaBuilder(s, this._errorLog);
      if (targetNamespace != null) {
        s.Namespaces.Add("", targetNamespace);        
        s.TargetNamespace = targetNamespace;
      } 
      if (includes != null) {
        foreach (SchemaInclude si in includes) {
          sb.AddImport(si.Prefix, si.NamespaceURI, si.URI);
        }
      }
      s.Namespaces.Add("xs", "http://www.w3.org/2001/XMLSchema");
      s.ElementFormDefault = XmlSchemaForm.Qualified;
      foreach (string name in _notations.Keys) {
        Notation n = (Notation)_notations[name];
        XmlSchemaNotation notation = new XmlSchemaNotation();
        notation.Name = n.Name;
        if (n.PublicId == null) {
          notation.Public = "";
        } else {
          notation.Public = n.PublicId;
        }
        notation.System = n.SysLit;
        s.Items.Add(notation);
      }
      foreach (string key in _elements.Keys) {
        ElementDecl ed = (ElementDecl)_elements[key];
        s.Items.Add(ed.GetSchemaElement(sb));
      }
      return s;
    }

    //-------------------------------- Parser -------------------------
    void PushEntity(Uri baseUri, Entity e) {
      try {
        e.Open(_current, baseUri);
      } catch (Exception ex) {
        if (e.Name != null && e.Name.Length != 0) {
          _errorLog.WriteLine("Error opening entity '"+e.Name+"'");
        }
        if (!e.Internal) {
          _errorLog.WriteLine("Error loading: '"+e.Uri+"'");
        }
        _errorLog.WriteLine(ex.Message);
        throw ex;
      }
      _current = e;
      _current.ReadChar();
    }

    void PopEntity() {
      if (_current != null) _current.Close();
      if (_current.Parent != null) {
        _current = _current.Parent;
      } 
      else {
        _current = null;
      }
    }

    void Parse() {
      char ch = _current.Lastchar;
      while (true) {
        switch (ch) {
          case Entity.EOF:
            PopEntity();
            if (_current == null)
              return;
            ch = _current.Lastchar;
            break;
          case ' ':
          case '\n':
          case '\r':
          case '\t':
            ch = _current.ReadChar();
            break;
          case '<':
            ParseMarkup();
            ch = _current.ReadChar();
            break;
          case '%':
            Entity e = ParseParameterEntity(_ws);
            try {
              PushEntity(_current.ResolvedUri, e);
            } 
            catch (Exception ex) {
              this._errorLog.WriteLine(ex.Message + _current.Context());
            }
            ch = _current.Lastchar;
            break;
          default:
            if (ch == ']' && _ConditionalSections>0) {
              return;
            }
            _current.Error("Unexpected character '{0}'", ch);
            break;
        }				
      }
    }

    void ParseMarkup() {
      char ch = _current.ReadChar();
      if (ch == '?') {
        ParseProcessingInstruction();
        return;
      }
      else if (ch != '!') {
        _current.Error("Found '"+ch+"', but expecing declaration starting with '<!'");
        return;
      }
      ch = _current.ReadChar();
      
      if (ch == '-') {
        ch = _current.ReadChar();
        if (ch != '-') _current.Error("Expecting comment '<!--' but found {0}", ch);
        _current.ScanToEnd(_sb, "Comment", "-->");                
        return;
      } 
      if (ch == '[') {
        ParseMarkedSection();
      }
      else {
        string token = _current.ScanToken(_sb, _ws, true);
        switch (token) {
          case "ENTITY":
            ParseEntity();
            break;
          case "ELEMENT":
            ParseElementDecl();
            break;
          case "ATTLIST":
            ParseAttList();
            break;
          case "NOTATION":
            ParseNotation();
            break;
          default:
            _current.Error("Invalid declaration '<!{0}'.  Expecting 'ENTITY', 'ELEMENT' or 'ATTLIST'.", token);
            break;
        }
      }
    }

    char ParseDeclComments() {
      char ch = _current.Lastchar;
      while (ch == '-') {
        ch = ParseDeclComment(true);
      }
      return ch;
    }

    char ParseDeclComment(bool full) {
      int start = _current.Line;
      // -^-...--
      // This method scans over a comment inside a markup declaration.
      char ch = _current.ReadChar();
      if (full && ch != '-') _current.Error("Expecting comment delimiter '--' but found {0}", ch);
      _current.ScanToEnd(_sb, "Markup Comment", "--");
      _current.ReadChar(); // consume next char
      return _current.SkipWhitespace();
    }

    void ParseMarkedSection() {
      // <![^ S? name S? [ ... ]]>
      _current.ReadChar(); // move to next char.
      char ch = _current.SkipWhitespace();
      Entity start = _current;

      if (ch == '%') {
        Entity e = ParseParameterEntity("[");
        PushEntity(_current.ResolvedUri, e);
      }
      string name = ScanName("[ \r\n");

      ch = _current.SkipWhitespace();
      if (ch == Entity.EOF && start != _current) {
        PopEntity();
        ch =  _current.SkipWhitespace();
      }
      if (ch != '[') _current.Error("Expecting '[' but found {0}", ch);
      ch = _current.ReadChar();
      if (ch == Entity.EOF && start != _current) {
        PopEntity();
      }

      if (name == "INCLUDE") {
        ParseIncludeSection();
      } 
      else if (name == "IGNORE") {
        ParseIgnoreSection();
      }
      else {
        _current.Error("Unsupported marked section type '{0}'", name);
      }
    }

    void ParseIncludeSection() {
      _ConditionalSections++;
      Parse();
      _ConditionalSections--;
      char ch = _current.SkipWhitespace(); 
      if (ch == ']') {
        ch = _current.ReadChar();
        if (ch == ']') {
          ch = _current.ReadChar();
          if (ch == '>') {
            return;
          }
        }
      }
      if (ch != ']') {
        _current.Error("Missing or bad end of conditional section: expecting ']]>'");
      }     
    }

    void ParseIgnoreSection() {
      int start = _current.Line;
      // <![IGNORE^[...]]>   -- can also be nested.
      int count = 1;
      char ch = _current.Lastchar;
      while (true) {
        switch (ch) {
          case Entity.EOF:
            PopEntity();
            if (_current == null)
              return;
            ch = _current.Lastchar;
            break;
          case '<': // see if it's the start of another conditional section.
            ch = _current.ReadChar();
            if (ch == '!') {
              ch = _current.ReadChar();
              if (ch == '[') {
                count++; // start of another conditional section.
              }
            }
            break;
          case ']': // see if it is the end of a conditional section.
            ch = _current.ReadChar();
            if (ch == ']') {
              ch = _current.ReadChar();
              if (ch == '>') {
                count--; 
                if (count == 0)
                  return; // we're done!
              }
            }
            break;
          default:
            ch = _current.ReadChar();
            break;
        }
      }     
    }

    void ParseProcessingInstruction() {
      string picontent = _current.ScanToEnd(_sb, "Processing instruction", "?>");      
    }

    string ScanName(string term) {
      // skip whitespace, scan name
      char ch = _current.SkipWhitespace();
      return _current.ScanToken(_sb, term, true);
    }

    Entity ParseParameterEntity(string term) {
      // almost the same as _current.ScanToken, except we also terminate on ';'
      char ch = _current.ReadChar();
      string name =  _current.ScanToken(_sb, ";"+term, false);
      if (_current.Lastchar == Entity.EOF && ! this._sgml) { 
          // in Sgml the closing ';' is optional.
        _current.Error("Unexpected end of file while parsing parameter entity");
      }
      name = _nt.Add(name);
      if (_current.Lastchar == ';') 
        _current.ReadChar();
      Entity e = GetParameterEntity(name);
      return e;
    }

    Entity GetParameterEntity(string name) {
      Entity e = (Entity)_pentities[name];
      if (e == null) _current.Error("Reference to undefined parameter entity '{0}'", name);
      return e;
    }
		
    static string _ws = " \r\n\t%"; // allow parameter entities also

    void ParseEntity() {
      char ch = _current.SkipWhitespace();
      bool pe = (ch == '%');
      if (pe) {
        // parameter entity.
        _current.ReadChar(); // move to next char
        ch = _current.SkipWhitespace();
      }
      string name = _current.ScanToken(_sb, _ws, true);
      name = _nt.Add(name);
      ch = _current.SkipWhitespace();
      Entity e = null;
      if (ch == '"' || ch == '\'') {
        string literal = ScanLiteral(_sb, ch, true, false); // expand numeric entities only
        e = new Entity(name, literal);
      } 
      else {
        string pubid = null;
        string extid = null;
        string tok = _current.ScanToken(_sb, _ws, true);
        if (Entity.IsLiteralType(tok) ) {
          ch = _current.SkipWhitespace();
          string literal = ScanLiteral(_sb, ch, false, false);
          e = new Entity(name, literal);
          e.SetLiteralType(tok);
        }
        else {
          extid = tok;
          if (extid == "PUBLIC") {
            ch = _current.SkipWhitespace();
            if (ch == '"' || ch == '\'') {
              pubid = ScanLiteral(_sb, ch, false, false);
            } 
            else {
              _current.Error("Expecting public identifier literal but found '{0}'",ch);
            }
          } 
          else if (extid != "SYSTEM") {
            _current.Error("Invalid external identifier '{0}'.  Expecing 'PUBLIC' or 'SYSTEM'.", extid);
          }
          string uri = null;
          ch = _current.SkipWhitespace();
          if (ch == '"' || ch == '\'') {
            uri = ScanLiteral(_sb, ch, false, false);
          } 
          else if (ch != '>') {
            _current.Error("Expecting system identifier literal but found '{0}'",ch);
          }
          ch = _current.SkipWhitespace();
          string ndata = null;
          if (!pe && ch == 'N') { // perhaps we have NDATA
            tok = _current.ScanToken(_sb, _ws, true);
            if (tok == "NDATA") {
              ch = _current.SkipWhitespace();
              ndata = _current.ScanToken(_sb, _ws+">", true);
            } else {
              _current.Error("Expecting NDATA, but found '{0}'",ch);
            }
          }
          e = new Entity(name, pubid, uri, ndata, _current.Proxy);
        }
      }
      ch = _current.SkipWhitespace();

      if (_sgml && ch == '-') ch = ParseDeclComments();  
      if (ch != '>') {
        _current.Error("Expecting end of entity declaration '>' but found '{0}'", ch);	
      }			
      if (pe) { 
        if (!_pentities.ContainsKey(e.Name)) // let first declaration win
          _pentities.Add(e.Name, e);
      }
      else {
        if (!_entities.ContainsKey(e.Name)) // let first declaration win
          _entities.Add(e.Name, e);
      }
    }

    Entity GetGeneralEntity(string name) {
      Entity e = (Entity)_entities[name];
      if (e == null) _current.Error("Reference to undefined general entity '{0}'", name);
      return e;
    }

    void ParseNotation() {
      char ch = _current.SkipWhitespace();
      string name = ScanName(_ws);
      // 'SYSTEM' S SystemLiteral
      // 'PUBLIC' S PubidLiteral S SystemLiteral
      // 'PUBLIC' S PubidLiteral 
      ch = _current.SkipWhitespace();
      
      string extid = ScanName(_ws);
      string pubid = null;
      string uri = null;

      if (extid == "PUBLIC") {
        ch = _current.SkipWhitespace();
        if (ch == '"' || ch == '\'') {
          pubid = ScanLiteral(_sb, ch, false, false);
        } 
        else {
          _current.Error("Expecting public identifier literal but found '{0}'",ch);
        }
      } 
      else if (extid != "SYSTEM") {
        _current.Error("Invalid external identifier '{0}'.  Expecing 'PUBLIC' or 'SYSTEM'.", extid);
      }
      ch = _current.SkipWhitespace();
      if (ch == '"' || ch == '\'') {
        uri = ScanLiteral(_sb, ch, false, false);
      } 
      else if (ch != '>') {
        _current.Error("Expecting system identifier literal but found '{0}'",ch);
      }
      ch = _current.SkipWhitespace();
      if (ch != '>') {
        _current.Error("Expecting end of notation declaration, but found '{0}'",ch);
      }
      _notations.Add(name, new Notation(name, pubid, uri));
      
      
    }

    void ParseElementDecl() {
      char ch = _current.SkipWhitespace();
      string[] names = ParseNameGroup(ch, true, true, false);
      bool sto = false;
      bool eto = false; 

      if (_sgml) {
        sto = (_current.SkipWhitespace() == 'O'); // start tag optional?	
        _current.ReadChar();
        eto = (_current.SkipWhitespace() == 'O'); // end tag optional?	
        _current.ReadChar();
        ch = _current.SkipWhitespace();
      } else {
        ch = _current.SkipWhitespace();
      }
      ContentModel cm = ParseContentModel(ch);
      ch = _current.SkipWhitespace();

      string [] exclusions = null;
      string [] inclusions = null;
      if (_sgml) {
        if ( ch == '-') {
          ch = _current.ReadChar();
          if (ch == '(') {
            exclusions = ParseNameGroup(ch, true, false, false);
            ch = _current.SkipWhitespace();
          }
          else if (ch == '-') {
            ch = ParseDeclComment(false);
          } 
          else {
            _current.Error("Invalid syntax at '{0}'", ch);	
          }
        }

        if (ch == '-') ch = ParseDeclComments();

        if (ch == '+') {
          ch = _current.ReadChar();
          if (ch != '(') {
            _current.Error("Expecting inclusions name group", ch);	
          }
          inclusions = ParseNameGroup(ch, true, false, false);
          ch = _current.SkipWhitespace();
        }
        if (ch == '-') ch = ParseDeclComments();
      }

      if (ch != '>') {
        _current.Error("Expecting end of ELEMENT declaration '>' but found '{0}'", ch);	
      }

      foreach (string name in names) {
        string n = name;
        if (_sgml) n = n.ToLower();
        if (this.Name == "") Name = n; // default
        string atom = _nt.Add(n); 
        ElementDecl ed = (ElementDecl)_elements[atom];
        if (ed != null) {
          if (ed.ContentModel == null) {
            // it was a shell ElementDecl created by an AttList forward declaration.
            // so now we fill in the rest
            ed.ContentModel = cm;
            ed.EndTagOptional = eto;
            ed.StartTagOptional = sto;
            ed.Inclusions = inclusions;
            ed.Exclusions = exclusions;
          } else {
            _current.Error("Duplicate ELEMENT declaration '{0}'", name);	
          }
        } else {
          ed = new ElementDecl(atom, sto, eto, cm, inclusions, exclusions);
          _elements.Add(atom, ed);
        }
      }
    }

    static string _ngterm = " \r\n\t,|)";
    string[] ParseNameGroup(char ch, bool nmtokens, bool decl, bool leaveEntitiesOpen) {
      ArrayList names = new ArrayList();
      
      if (ch == '%') {
        Entity e = ParseParameterEntity(_ngterm);
        PushEntity(_current.ResolvedUri, e);
        ch = _current.SkipWhitespace();
        string[] result = ParseNameGroup(ch, nmtokens, decl, leaveEntitiesOpen);
        if (!leaveEntitiesOpen) PopEntity();
        return result;
      }

      if ((!decl || _sgml) && ch == '(') {
        ch = _current.ReadChar();
        ch = _current.SkipWhitespace();
        while (ch != Entity.EOF && ch != ')') {
          // skip whitespace, scan name (which may be parameter entity reference
          // which is then expanded to a name)					
          ch = _current.SkipWhitespace();
          if (ch == '%') {
            Entity e = ParseParameterEntity(_ngterm);
            PushEntity(_current.ResolvedUri, e);
            ParseNameList(names, nmtokens, decl);
            PopEntity();
            ch = _current.Lastchar;
          }
          else {
            string token = _current.ScanToken(_sb, _ngterm, nmtokens);
            string atom = _nt.Add(token);
            if (_sgml && decl) atom = atom.ToLower();
            names.Add(atom);
          }
          ch = _current.SkipWhitespace();
          if (ch == '|' || ch == ',') ch = _current.ReadChar();
        }
        _current.ReadChar(); // consume ')'
      } 
      else {
        string name = _current.ScanToken(_sb, _ws+">", nmtokens);
        if (_sgml && decl) name = name.ToLower();
        name = _nt.Add(name);
        names.Add(name);
      }
      return (string[])names.ToArray(typeof(String));
    }

    void ParseNameList(ArrayList names, bool nmtokens, bool decl) {
      char ch = _current.Lastchar;
      ch = _current.SkipWhitespace();
      while (ch != Entity.EOF) {
        string name;
        if (ch == '%') {
          Entity e = ParseParameterEntity(_ngterm);
          PushEntity(_current.ResolvedUri, e);
          ParseNameList(names, nmtokens, decl);
          PopEntity();
          ch = _current.Lastchar;
        } 
        else if (ch != '|') {
          name = _current.ScanToken(_sb, _ngterm, true);
          if (_sgml && decl) name = name.ToLower();
          name = _nt.Add(name);
          names.Add(name);
        }
        ch = _current.SkipWhitespace();
        if (ch == '|') {
          ch = _current.ReadChar();
          ch = _current.SkipWhitespace();
        }
      }
    }

    static string _dcterm = " \r\n\t>";
    ContentModel ParseContentModel(char ch) {
      ContentModel cm = new ContentModel();
      if (ch == '(') {
        _current.ReadChar();
        ParseModel(')', cm);
        ch = _current.ReadChar();
        if (ch == '?' || ch == '+' || ch == '*') {
          cm.AddOccurrence(ch);
          _current.ReadChar();
        }
      } 
      else if (ch == '%') {
        Entity e = ParseParameterEntity(_dcterm);
        PushEntity(_current.ResolvedUri, e);
        if (_preserveGroups) cm.PushParameterEntity(e);
        cm = ParseContentModel(_current.Lastchar);
        if (_preserveGroups) cm.PopParameterEntity();
        PopEntity(); // bugbug should be at EOF.
      }
      else {
        string dc = ScanName(_dcterm);
        cm.SetDeclaredContent(dc, this._sgml);
      }
      return cm;
    }

    static string _cmterm = " \r\n\t,&|()?+*";
    void ParseModel(char cmt, ContentModel cm) {
      // Called when part of the model is made up of the contents of a parameter entity
      int depth = cm.CurrentDepth;
      char ch = _current.Lastchar;
      ch = _current.SkipWhitespace();
      while (ch != cmt || cm.CurrentDepth > depth) { // the entity must terminate while inside the content model.
        if (ch == Entity.EOF) {
          _current.Error("End of file while parsing content model");
        }
        if (ch == '%') {
          Entity e = ParseParameterEntity(_cmterm);
          PushEntity(_current.ResolvedUri, e);
          if (_preserveGroups) cm.PushParameterEntity(e);
          ParseModel(Entity.EOF, cm);
          if (_preserveGroups) cm.PopParameterEntity();
          PopEntity();					
          ch = _current.SkipWhitespace();
        } 
        else if (ch == '(') {
          cm.PushGroup();
          _current.ReadChar();// consume '('
          ch = _current.SkipWhitespace();
        }
        else if (ch == ')') {
          ch = _current.ReadChar();// consume ')'
          if (ch == '*' || ch == '+' || ch == '?') {
            cm.AddOccurrence(ch);
            ch = _current.ReadChar();
          }
          if (cm.PopGroup() < depth) {
            _current.Error("Parameter entity cannot close a paren outside it's own scope");
          }
          ch = _current.SkipWhitespace();
        }
        else if (ch == ',' || ch == '|' || ch == '&') {
          cm.AddConnector(ch);
          _current.ReadChar(); // skip connector
          ch = _current.SkipWhitespace();
        }
        else {
          string token;
          if (ch == '#') {
            ch = _current.ReadChar();
            token = "#" + _current.ScanToken(_sb, _cmterm, true); // since '#' is not a valid name character.
          } 
          else {
            token = _current.ScanToken(_sb, _cmterm, true);
            if (_sgml) token = token.ToLower();
          }          
          token = _nt.Add(token);// atomize it.
          ch = _current.Lastchar;
          if (ch == '?' || ch == '+' || ch == '*') {
            cm.PushGroup();
            cm.AddSymbol(token);
            cm.AddOccurrence(ch);
            cm.PopGroup();
            _current.ReadChar(); // skip connector
            ch = _current.SkipWhitespace();
          } 
          else {
            cm.AddSymbol(token);
            ch = _current.SkipWhitespace();
          } 					
        }
      }
    }

    static string _peterm = " \t\r\n>";
    void ParseAttList() {
      Entity start = _current; // make sure parameter entities are not left open.
      char ch = _current.SkipWhitespace();
      string[] names = ParseNameGroup(ch, true, true, true);
      AttList list = new AttList();

      ParseAttList(list, start, null);

      // Now add these attdefs to the list element names parsed in the beginning
      // of the attlist.
      foreach (string name in names) {
        string n = name;
        if (_sgml) n = n.ToLower();
        ElementDecl e = (ElementDecl)_elements[n];
        if (e == null) {
          // forward declaration
          string atom = _nt.Add(n); 
          e = new ElementDecl(atom, false, false, null, null, null);
          _elements.Add(atom, e);
        }
        e.AddAttDefs(list);
      }

    }

    void ParseAttList(AttList list, Entity start, Entity current) {

      char ch = _current.SkipWhitespace();
      int state = 0; 
      AttDef attdef = null;

      while ( ch != '>') {
        if (ch == Entity.EOF) {
          if (_current == start) // we better be done!
            break;
          if (state == 0 && _current == current) // we can safely pop out of this level
            break;
          PopEntity();
          if (_current == null) {
            _current.Error("Unexpected end of file parsing ATTLIST");
          }
        }
        else if (ch == '%') {
          Entity e = ParseParameterEntity(_peterm);
          PushEntity(_current.ResolvedUri, e);
          if (state == 0 && _preserveGroups) {
            // only create attribute group if it is an entire attdef.
            AttList child = new AttList();
            child.PE = e;
            ParseAttList(child, start, _current);
            if (child.Count>0) {
              list.Add(child);
            }
          }
          ch = _current.SkipWhitespace();
        } 
        else if (_sgml && ch == '-') {
          ch = ParseDeclComments();
        }
        else {
          switch (state) {
            case 0: // name
              string name = ScanName(_ws);
              if (_sgml) name = name.ToLower();
              name = _nt.Add(name);
              attdef = new AttDef(name);
              list.Add(attdef);
              state = 1;
              break;
            case 1: // type
              ParseAttType(ch, attdef);
              state = 2;
              break;
            case 2: // presence
              if (ParseAttPresence(ch, attdef)) 
                state = 3;
              else 
                state = 0;
              break;
            case 3: // default value
              ParseAttDefault(ch, attdef);
              state = 0;
              break;
          }
        }
        ch = _current.SkipWhitespace();
      }
      if (state != 0) {
        if (state == 1) _current.Error("Expecting Attribute Type");
        else if (state == 2) _current.Error("Expecting Attribute Presence");
      }
      if (_current != start && _current != current) {
        _current.Error("Unexpected end of ATTLIST inside parameter entity '" + _current.Name + "'");
      }
    }

    void ParseAttType(char ch, AttDef attdef) {

      if (ch == '(') {
        attdef.EnumValues = ParseNameGroup(ch, false, false, true);	
        attdef.Type = AttributeType.ENUMERATION;
      } 
      else {
        string token = ScanName(_ws);
        if (token == "NOTATION") {
          ch = _current.SkipWhitespace();
          if (ch != '(') {
            _current.Error("Expecting name group '(', but found '{0}'", ch);
          }
          attdef.Type = AttributeType.NOTATION;
          attdef.EnumValues = ParseNameGroup(ch, true, false, true);
        } 
        else {
          attdef.SetType(token);
        }
      }
    }

    static string _attterm = " \t\r\n>";
    bool ParseAttPresence(char ch, AttDef attdef) {
      bool hasdef = true;
      if (ch == '#') {
        _current.ReadChar();
        string token = _current.ScanToken(_sb, _attterm, true);
        hasdef = attdef.SetPresence(token);
        ch = _current.SkipWhitespace();
      } 
      return hasdef;
    }

    void ParseAttDefault(char ch, AttDef attdef) {
      if (ch == '\'' || ch == '"') {
        string lit = ScanLiteral(_sb, ch, true, true);
        attdef.Default = lit;
        ch = _current.SkipWhitespace();
      }
      else if (ch != '>') {
        string name = _current.ScanToken(_sb, _attterm, false);          
        name = _nt.Add(name);
        attdef.Default = name; 
        ch = _current.SkipWhitespace();
      }
    }

    public string ScanLiteral(StringBuilder sb, char quote, bool expandCharEntities, bool expandGeneralEntities) {
      sb.Length = 0;
      char ch = _current.ReadChar();
      int level = 0;
      while (level > 0 || ch != quote ) {
        if (ch == Entity.EOF) {
          if (level>0) {
            PopEntity();
            level--;
          } else {
            break;
          }
        }
        else if (ch == '&' && expandCharEntities) {
          ch = _current.ReadChar();
            if (ch == '#') {
                string charent = ExpandCharEntity();
                sb.Append(charent);
            } else if (ch == ' ' && this._sgml) {
                // then it is probably a content model '&' operator.
                sb.Append(ch);
            } else {
                StringBuilder sb2 = new StringBuilder();
                string name = _current.ScanToken(sb2, ";"+quote, true);
                switch (name) {
                    case "lt":
                        sb.Append('<');
                        break;
                    case "gt":
                        sb.Append('>');
                        break;
                    case "quot":
                        sb.Append('"');
                        break;
                    case "apos":
                        sb.Append('\'');
                        break;
                    case "amp":
                        sb.Append('&');
                        break;
                    default:
                        if (expandGeneralEntities) {
                            // lookup entity and expand it
                            Entity e = GetGeneralEntity(name);
                            PushEntity(_current.ResolvedUri, e);
                            level++;
                            ch = _current.Lastchar;
                            if (ch != Entity.EOF) sb.Append(ch);
                        } else {
                            sb.Append('&');
                            sb.Append(name);
                            sb.Append(';');                                    
                        }
                        break;
                }            
            }
        } 				
        else {
          sb.Append(ch);
        }
        ch = _current.ReadChar();
      }
      if (ch == Entity.EOF) {
        _current.Error("Unexpected end of file while scanning literal");
      }
      _current.ReadChar(); // consume end quote.			
      return sb.ToString();
    }

    public string ExpandCharEntity() {
      char ch = _current.ReadChar();
      int v = 0;
      if (ch == 'x') {
        for (; ch != Entity.EOF && ch != ';'; ch = _current.ReadChar()) {
          int p = 0;
          if (ch >= '0' && ch <= '9') {
            p = (int)(ch-'0');
          } 
          else if (ch >= 'a' && ch <= 'f') {
            p = (int)(ch-'a')+10;
          } 
          else if (ch >= 'A' && ch <= 'F') {
            p = (int)(ch-'A')+10;
          }
          else {
            break;//we must be done!
            //Error("Hex digit out of range '{0}'", (int)ch);
          }
          v = (v*16)+p;
        }
      } 
      else {					
        for (; ch != Entity.EOF && ch != ';'; ch = _current.ReadChar()) {
          if (ch >= '0' && ch <= '9') {
            v = (v*10)+(int)(ch-'0');
          } 
          else {
            break; // we must be done!
            //Error("Decimal digit out of range '{0}'", (int)ch);
          }
        }
      }
      if (ch == Entity.EOF) {
        _current.Error("Premature {0} parsing entity reference", ch); 
      }
      // ReadChar(); // don't consume ';' terminator!
      return Convert.ToChar(v).ToString();
    }
  }	
}
