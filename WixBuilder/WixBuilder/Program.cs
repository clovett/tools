using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WixBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            string dir = args[0];

            Console.WriteLine("<DirectoryRef Id=\"{0}\" FileSource=\"???\">", Path.GetFileName(dir));

            foreach (var file in Directory.GetFiles(args[0], "*"))
            {
                //<Component Id="FontBuilder.dll" Guid="0F3EDCEC-529C-489F-9717-A0D3A06A33A9">
                // <File Id="FontBuilder.dll" KeyPath="yes" Checksum="yes"/>
                // </Component>      
                string fname = Path.GetFileName(file);
                Console.WriteLine("  <Component Id=\"{0}\" Guid=\"{1}\">", fname, Guid.NewGuid());
                Console.WriteLine("    <File Id=\"{0}\" KeyPath=\"yes\"/>", fname);
                Console.WriteLine("  </Component>");
            }
            Console.WriteLine("</DirectoryRef>");


            Console.WriteLine("<Feature Id=\"{0}\" Title=\"{0}\">", Path.GetFileName(dir));

            foreach (var file in Directory.GetFiles(args[0], "*"))
            {
                //<Component Id="FontBuilder.dll" Guid="0F3EDCEC-529C-489F-9717-A0D3A06A33A9">
                // <File Id="FontBuilder.dll" KeyPath="yes" Checksum="yes"/>
                // </Component>      
                string fname = Path.GetFileName(file);
                Console.WriteLine("  <ComponentRef Id=\"{0}\" />", fname);
            }

            Console.WriteLine("</Feature>");

        }
    }
}
