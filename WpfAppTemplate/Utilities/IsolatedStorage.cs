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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LovettSoftware.Utilities
{

    /// <summary>
    /// Isolated storage file helper class
    /// </summary>
    /// <typeparam name="T">Data type to serialize/deserialize</typeparam>
    public class IsolatedStorage<T>
    {
        static ConcurrentDictionary<string, object> locks = new ConcurrentDictionary<string, object>();


        public IsolatedStorage()
        {
        }

        class FileLock : IDisposable
        {
            object lockObject;
            string key;

            public FileLock(object lockObject, string path)
            {
                this.lockObject = lockObject;
                this.key = path;
            }

            public void Dispose()
            {
                Monitor.Exit(lockObject);
            }
        }

        private IDisposable EnterLock(string path)
        {
            while (true)
            {
                object lockObject = null;
                lock (locks)
                {
                    if (!locks.TryGetValue(path, out lockObject))
                    {
                        lockObject = new object();
                        locks[path] = lockObject;
                    }
                }

                if (lockObject != null)
                {
                    Monitor.Enter(lockObject);
                    return new FileLock(lockObject, path);
                }
            }
        }

        /// <summary>
        /// Loads data from a file asynchronously.
        /// </summary>
        /// <param name="folder">The folder to get the file from</param>
        /// <param name="fileName">Name of the file to read.</param>
        /// <returns>Deserialized data object</returns>
        public async Task<T> LoadFromFileAsync(string folder, string fileName)
        {
            return await LoadFromFileAsync(Path.Combine(folder, fileName));
        }

        /// <summary>
        /// Loads data from a file asynchronously.
        /// </summary>
        /// <param name="fullPath">The full path to the file to read.</param>
        /// <returns>Deserialized data object</returns>
        public async Task<T> LoadFromFileAsync(string fullPath)
        {
            T loadedFile = default(T);

            using (EnterLock(fullPath))
            {
                if (fullPath != null)
                {
                    Debug.WriteLine(string.Format("Loading file: {0}", fullPath));
                    await Task.Run(() =>
                    {
                        try
                        {
                            using (Stream myFileStream = File.OpenRead(fullPath))
                            {
                                // Call the Deserialize method and cast to the object type.
                                loadedFile = LoadFromStream(myFileStream);
                            }
                        }
                        catch (Exception)
                        {
                            // silently rebuild data file if it got corrupted.
                        }
                    });
                }
            }
            return loadedFile;
        }

        /// <summary>
        /// Loads data from a file synchronously.
        /// </summary>
        /// <param name="folder">The folder to get the file from</param>
        /// <param name="fileName">Name of the file to read.</param>
        /// <returns>Deserialized data object</returns>
        public T LoadFromFile(string folder, string fileName)
        {
            return LoadFromFile(Path.Combine(folder, fileName));
        }

        /// <summary>
        /// Loads data from a file synchronously.
        /// </summary>
        /// <param name="fullPath">The full path to the file</param>
        /// <returns>Deserialized data object</returns>
        public T LoadFromFile(string fullPath)
        {
            T loadedFile = default(T);

            using (EnterLock(fullPath))
            {
                if (fullPath != null)
                {
                    Debug.WriteLine(string.Format("Loading file: {0}", fullPath));
                    try
                    {
                        using (Stream myFileStream = File.OpenRead(fullPath))
                        {
                            // Call the Deserialize method and cast to the object type.
                            loadedFile = LoadFromStream(myFileStream);
                        }
                    }
                    catch (Exception)
                    {
                        // silently rebuild data file if it got corrupted.
                    }
                }
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
        public async Task SaveToFileAsync(string folder, string fileName, T data)
        {
            await SaveToFileAsync(System.IO.Path.Combine(folder, fileName), data);
        }

        /// <summary>
        /// Saves data to a file.
        /// </summary>
        /// <param name="path">Full path to the file to write to</param>
        public async Task SaveToFileAsync(string path, T data)
        {
            using (EnterLock(path))
            {
                try
                {
                    await Task.Run(() =>
                    {
                        using (var stream = File.Create(path))
                        {
                            XmlSerializer mySerializer = new XmlSerializer(typeof(T));
                            mySerializer.Serialize(stream, data);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("### SaveToFileAsync failed: {0}", ex.Message);
                }
            }
        }

    }
}
