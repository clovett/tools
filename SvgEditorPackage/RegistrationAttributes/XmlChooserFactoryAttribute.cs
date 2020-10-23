/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Globalization;

namespace Microsoft.VisualStudio.Shell
{
    /// <summary>
    /// This attribute adds support for registering an XML file extension with the XmlChooser editor factory.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    internal sealed class ProvideXmlChooserAttribute : RegistrationAttribute
    {
        private Guid _factory;
        private Guid _defaultLogicalView;
        private Guid _editor;
        private string _extension;
        private string _namespace;
        private bool _matchboth;

        /// <summary>
        /// Creates a new ProvideXmlChooserAttribute attribute to register an XML file extension with the XmlChooser editor factory.
        /// </summary>
        /// <param name="factoryType">The type of factory; can be a Type, a GUID or a string representation of a GUID</param>
        /// <param name="logicalViewGuid">The guid of the logical view to register.</param>
        /// <param name="extension">The file extension your custom XML document uses</param>
        public ProvideXmlChooserAttribute(object factoryType, string logicalViewGuid, string extension)
        {
            this._defaultLogicalView = new Guid(logicalViewGuid);

            // figure out what type of object they passed in and get the GUID from it
            if (factoryType is string)
                this._factory = new Guid((string)factoryType);
            else if (factoryType is Type)
            {
                this._factory = ((Type)factoryType).GUID;
            }
            else if (factoryType is Guid)
                this._factory = (Guid)factoryType;
            else
                throw new ArgumentException("factoryType");

            _extension = extension;
            if (string.IsNullOrEmpty(extension))
            {
                throw new ArgumentNullException("extension");
            }

            this._editor = this._factory;
        }

        /// <summary>
        /// Get the Guid representing the type of the editor factory
        /// </summary>
        public Guid FactoryType
        {
            get { return _factory; }
        }

        /// <summary>
        /// Get/Set the Guid representing the type of the editor 
        /// </summary>
        public string Editor
        {
            get { return _editor.ToString(); }
            set { _editor = new Guid(value); }
        }

        /// <summary>
        /// Get the Guid representing the logical view
        /// </summary>
        public Guid DefaultLogicalView
        {
            get { return _defaultLogicalView; }
            set { _defaultLogicalView = value; }
        }

        // Optional additional view mappings to editor Guid for that view.
        public string DesignerView { get; set; }
        public string CodeView { get; set; }
        public string TextView { get; set; }
        public string DebuggingView { get; set; }

        /// <summary>
        /// Get or Set the namespace of the XML files that support this view
        /// </summary>
        public string Namespace
        {
            get { return _namespace; }
            set { _namespace = value; }
        }

        /// <summary>
        /// Get or Set the extension of the XML files that support this view
        /// </summary>
        public string Extension
        {
            get { return _extension; }
            set { _extension = value; }
        }

        /// <summary>
        /// Get or Set the whether the XML files that support this view must
        /// match both the file extension and the namespace (Match = "both")
        /// or it is OK to merely match the file extension (Match not specified)
        /// </summary>
        public bool MatchBoth
        {
            get { return _matchboth; }
            set { _matchboth = value; }
        }

        private string XmlChooserPath
        {
            get { return string.Format(CultureInfo.InvariantCulture, "XmlChooserFactory\\{0}", _extension); }
        }


        /// <summary>
        ///     Called to register this attribute with the given context.  The context
        ///     contains the location where the registration inforomation should be placed.
        ///     It also contains other information such as the type being registered and path information.
        /// </summary>
        public override void Register(RegistrationContext context)
        {
            context.Log.WriteLine(String.Format(CultureInfo.CurrentCulture, "XmlChooserFactory:  {0}\n", _extension));

            using (Key childKey = context.CreateKey(XmlChooserPath))
            {
                childKey.SetValue("Extension", _extension);
                childKey.SetValue("DefaultLogicalView", _defaultLogicalView.ToString("D"));

                if (DebuggingView != null)
                {
                    childKey.SetValue(Microsoft.VisualStudio.Shell.Interop.LogicalViewID.Debugging, DebuggingView);
                }
                if (CodeView != null)
                {
                    childKey.SetValue(Microsoft.VisualStudio.Shell.Interop.LogicalViewID.Code, CodeView);
                }
                if (DesignerView != null)
                {
                    childKey.SetValue(Microsoft.VisualStudio.Shell.Interop.LogicalViewID.Designer, DesignerView);
                }
                if (TextView != null)
                {
                    childKey.SetValue(Microsoft.VisualStudio.Shell.Interop.LogicalViewID.TextView, TextView);
                }

                if (_namespace != null)
                    childKey.SetValue("Namespace", _namespace);

                if (_matchboth)
                    childKey.SetValue("Match", "both");
            }
        }

        /// <summary>
        /// Unregister this logical view.
        /// </summary>
        /// <param name="context"></param>
        public override void Unregister(RegistrationContext context)
        {
            context.RemoveKey(XmlChooserPath);
        }
    }
}
