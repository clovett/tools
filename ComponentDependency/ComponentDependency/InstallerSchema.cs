using Microsoft.VisualStudio.GraphModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComponentDependency
{
    class InstallerSchema
    {
        static GraphSchema schema;

        public static GraphSchema Schema { get { return schema; } }


        public static GraphCategory ProductCategory { get; private set; }
        public static GraphCategory FeatureCategory { get; private set; }
        public static GraphCategory ComponentCategory { get; private set; }
        public static GraphCategory ItemCategory { get; private set; }



        public static GraphProperty IsReferenceProperty { get; private set; }
        public static GraphProperty FilePathProperty { get; private set; }
        public static GraphProperty DisplayNameProperty { get; private set; }
        public static GraphProperty LocalPackageProperty { get; private set; }
        public static GraphProperty PublisherProperty { get; private set; }

        static InstallerSchema()
        {
            // Set default actions.
            schema = new GraphSchema("InstallerSchema");
            
            IsReferenceProperty = schema.Properties.AddNewProperty("IsReference", typeof(bool));
            FilePathProperty = schema.Properties.AddNewProperty("FilePath", typeof(string), () =>
            {
                var m = new GraphMetadata("FilePath", "Location of file", null, GraphMetadataOptions.Default);
                m.SetValue<bool>(IsReferenceProperty, true);
                return m;
            });
            DisplayNameProperty = schema.Properties.AddNewProperty("DisplayName", typeof(string));
            LocalPackageProperty = schema.Properties.AddNewProperty("LocalPackage", typeof(string));
            PublisherProperty = schema.Properties.AddNewProperty("Publisher", typeof(string));

            ProductCategory = schema.Categories.AddNewCategory("Product");
            FeatureCategory = schema.Categories.AddNewCategory("Feature");
            ComponentCategory = schema.Categories.AddNewCategory("Component");
            ItemCategory = schema.Categories.AddNewCategory("Item");
        }
    }
}
