using Microsoft.VisualStudio.GraphModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsBuildImportGraph
{
    /// <summary>
    /// This is a DGML schema which defines GraphCategory and GraphProperty objects that are 
    /// used by the DgmlTestModel to find various important things in a DGML Test Document.
    /// </summary>
    class MsBuildGraphSchema
    {
        /// <summary>
        /// Instance of the GraphSchema
        /// </summary>
        public static GraphSchema Schema;

        static MsBuildGraphSchema()
        {
            Schema = new GraphSchema("MsBuildGraphSchema");
            FileCategory = Schema.Categories.AddNewCategory("File");

            ErrorProperty = Schema.Properties.AddNewProperty("Error", typeof(string));
        }

        /// <summary>
        /// Nodes with this category are used on msbuild files.
        /// </summary>
        public static GraphCategory FileCategory;

        /// <summary>
        /// This property is used to store error information about a given node or link.
        /// </summary>
        public static GraphProperty ErrorProperty;


    }
}
