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
using System.IO.IsolatedStorage;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FoscamExplorer
{

    /// <summary>
    /// Isolated storage file helper class
    /// </summary>
    /// <typeparam name="T">Data type to serialize/deserialize</typeparam>
    public class IsolatedStorage<T>
    {
        CacheFolder folder;

        public IsolatedStorage(CacheFolder cache) 
        {
            this.folder = cache;
        }

        /// <summary>
        /// Loads data from a file
        /// </summary>
        /// <param name="fileName">Name of the file to read.</param>
        /// <returns>Data object</returns>
        public async Task<T> LoadFromFileAsync(string fileName)
        {
            T loadedFile = default(T);

            try
            {
                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (store.FileExists(fileName))
                    {
                        using (FileStream myFileStream = store.OpenFile(fileName, FileMode.Open))
                        {
                            // Call the Deserialize method and cast to the object type.
                            XmlSerializer mySerializer = new XmlSerializer(typeof(T));
                            loadedFile = (T)mySerializer.Deserialize(myFileStream);
                        }
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
        public async Task SaveToFileAsync(string fileName, T data)
        {
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                XmlSerializer mySerializer = new XmlSerializer(typeof(T));
                if (store.FileExists(fileName))
                {
                    store.DeleteFile(fileName);
                }

                using (StreamWriter myWriter =
                    new StreamWriter(store.OpenFile(fileName, FileMode.CreateNew)))
                {
                    mySerializer.Serialize(myWriter, data);
                }
            }
        }

    }
}
