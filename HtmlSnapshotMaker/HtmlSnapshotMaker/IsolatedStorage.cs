// Copyright (c) 2010 Microsoft Corporation.  All rights reserved.
//
//
// Use of this source code is subject to the terms of the Microsoft
// license agreement under which you licensed this source code.
// If you did not accept the terms of the license agreement,
// you are not authorized to use this source code.
// For the terms of the license, please see the license agreement
// signed by you and Microsoft.
// THE SOURCE CODE IS PROVIDED "AS IS", WITH NO WARRANTIES OR INDEMNITIES.
//
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace HtmlSnapshotMaker
{

    /// <summary>
    /// Isolated storage file helper class
    /// </summary>
    /// <typeparam name="T">Data type to serialize/deserialize</typeparam>
    public class IsolatedStorage<T>
    {
        Uri baseUri;

        public IsolatedStorage()
        {
            // string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // string directory = Path.Combine(appData, "HtmlSnapshotMaker");
            string directory = Directory.GetCurrentDirectory();
            directory += Path.DirectorySeparatorChar;
            baseUri = new Uri(directory);
        }

        /// <summary>
        /// Loads data from a file asynchronously.
        /// </summary>
        /// <param name="fileName">Name of the file to read.</param>
        /// <returns>Deserialized data object</returns>
        public T LoadFromFile(string fileName)
        {
            T loadedFile = default(T);

            try
            {
                Uri resolved = new Uri(baseUri, fileName);
                string fullPath = resolved.LocalPath;
                if (File.Exists(fullPath))
                {
                    using (Stream myFileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        // Call the Deserialize method and cast to the object type.
                        return LoadFromStream(myFileStream);
                    }
                }
            }
            catch
            {
                // silently rebuild data file if it got corrupted.
            }
            return loadedFile;
        }

        public T LoadFromStream(Stream s)
        {
            // Call the Deserialize method and cast to the object type.
            XmlSerializer mySerializer = new XmlSerializer(typeof(T));
            return (T)mySerializer.Deserialize(s);
        }

        /// <summary>
        /// Saves data to a file.
        /// </summary>
        /// <param name="fileName">Name of the file to write to</param>
        /// <param name="data">The data to save</param>
        public void SaveToFile(string fileName, T data)
        {
            Uri resolved = new Uri(baseUri, fileName);
            string fullPath = resolved.LocalPath;
            using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                XmlSerializer mySerializer = new XmlSerializer(typeof(T));
                mySerializer.Serialize(stream, data);
            }
        }

    }
}
