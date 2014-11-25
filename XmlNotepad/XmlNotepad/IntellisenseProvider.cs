using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Schema;

namespace XmlNotepad {
    public interface IIntellisenseProvider {
        Uri BaseUri { get; }
        TreeNode ContextNode {get;set;}
        void SetContextNode(TreeNode node);
        bool IsNameEditable { get; }
        bool IsValueEditable { get; }
        XmlSchemaType GetSchemaType();
        string GetDefaultValue();
        string[] GetExpectedNames();
        string[] GetExpectedValues();
        IXmlBuilder Builder { get; }
        IXmlEditor Editor { get; }
    }
}
