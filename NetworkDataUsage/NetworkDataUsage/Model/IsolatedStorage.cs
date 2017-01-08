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

namespace Microsoft.Storage
{

    /// <summary>
    /// Isolated storage file helper class
    /// </summary>
    /// <typeparam name="T">Data type to serialize/deserialize</typeparam>
    public class IsolatedStorage<T>
    {
        public IsolatedStorage()
        {
        }

        public async Task<T> LoadFromFileAsync(string fullPath)
        {
            T loadedFile = default(T);

            await Task.Run(() =>
            {
                try
                {
                    lock (storageLock)
                    {
                        if (File.Exists(fullPath))
                        {
                            using (Stream myFileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
                            {
                                // Call the Deserialize method and cast to the object type.
                                loadedFile = LoadFromStream(myFileStream);
                            }
                        }
                    }
                }
                catch
                {
                    // silently rebuild data file if it got corrupted.
                }
            });
            return loadedFile;
        }

        public T LoadFromStream(Stream s)
        {
            // Call the Deserialize method and cast to the object type.
            XmlSerializer mySerializer = new XmlSerializer(typeof(T));
            return (T)mySerializer.Deserialize(s);
        }

        internal async Task SaveToFileAsync(string fullPath, T data)
        {
            await Task.Run(() =>
            {
                try
                {
                    lock (storageLock)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                        using (var stream = File.Create(fullPath))
                        {
                            XmlSerializer mySerializer = new XmlSerializer(typeof(T));
                            mySerializer.Serialize(stream, data);
                            stream.SetLength(stream.Position);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(string.Format("Exception saving file '{0}':{1}", fullPath, e.ToString()));
                }
            });
        }

        static object storageLock = new object();
    }
}
