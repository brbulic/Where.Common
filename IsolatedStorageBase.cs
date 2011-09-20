using System;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading;
using System.Windows;
using Where.Common.Services;

namespace Where.Common
{

    /// <summary>
    /// IsolatedStorage base that contains static methods to check if some files exist. Inherit to use specific Read and Write Functionality.
    /// </summary>
    public abstract class IsolatedStorageBase
    {

        public static bool WriteSessionRunning { get; protected set; }

        protected static IsolatedStorageFile GetIsolatedStorageFileSystem()
        {
            return IsolatedStorageFile.GetUserStoreForApplication();
        }

        private static readonly object InternalLock = new object();

        protected T ReadStreamFromFile<T>(String file) where T : class
        {
            using (var isoStream = GetIsolatedStorageFileSystem())
            {

                if (!isoStream.FileExists(file))
                    return null;

                using (var stream = isoStream.OpenFile(file, FileMode.Open, FileAccess.Read))
                {
                    var result = StreamOperationOnFile(stream, file) as T;
                    return result;
                }

            }
        }

        protected abstract object StreamOperationOnFile(Stream isoStream, string file = null);


        #region Binary writes

        /// <summary>
        /// Write a stream to a file. Very generic :)
        /// </summary>
        /// <param name="writableStream">Stream that needs to be written</param>
        /// <param name="file">Filename</param>
        protected void WriteStreamBinary(Stream writableStream, String file)
        {
            var dataplan = new IsoStorageDataContainer(file, writableStream);
            InternalWriteStreamBinary(dataplan);
        }

        /// <summary>
        /// Do an asynchronous write to the Stream
        /// </summary>
        /// <param name="writableStream">Stream to write</param>
        /// <param name="file">Filename</param>
        /// <param name="errorDelegate">Calls on operation completed</param>
        protected void WriteStreamBinaryAsync(Stream writableStream, String file, Action<string> errorDelegate)
        {
            var dataplan = new IsoStorageDataContainer(file, writableStream);
            Action modifiedActionDelegate = () => errorDelegate(file);
            BackgroundDispatcher.Instance.QueueSimple(InternalWriteStreamBinary, dataplan, modifiedActionDelegate);
        }

        private static void InternalWriteStreamBinary(object state)
        {
            var data = (IsoStorageDataContainer)state;
            var writableStream = (Stream)data.WritableData;
            var file = data.Filename;

            lock (InternalLock)
            {
                if (WriteSessionRunning)
                    throw new NotSupportedException("Another session is still active");

                using (var storage = GetIsolatedStorageFileSystem())
                {
                    WriteSessionRunning = true;
                    writableStream.Flush();

                    var array = new byte[writableStream.Length];

                    using (var fileStream = storage.OpenFile(file, FileMode.Create, FileAccess.Write))
                    {
                        fileStream.Write(array, 0, array.Length);
                        fileStream.Flush();
                    }
                }

                Debug.WriteLine("------------> STATUS: Filename {0} writen to isolated storage!", file);
                WriteSessionRunning = false;
            }
        }

        #endregion

        #region String writes

        /// <summary>
        /// Write a string
        /// </summary>
        /// <param name="writableString"></param>
        /// <param name="file"></param>
        protected void WriteStringUnicode(string writableString, String file)
        {
            if (string.IsNullOrEmpty(writableString))
                return;

            var dataplan = new IsoStorageDataContainer(file, writableString);
            InternalWriteStringUnicode(dataplan);
        }

        protected void WriteStringUnicodeAsync(string writableString, String file, Action<string> callback)
        {
            if (string.IsNullOrEmpty(writableString))
            {
                callback(String.Empty);
                return;
            }

            var dataplan = new IsoStorageDataContainer(file, writableString);
            Action convertedFunction = () => callback(file);

            // Do it in another thread
            BackgroundDispatcher.Instance.QueueSimple(InternalWriteStringUnicode, dataplan, convertedFunction);

        }

        private static void InternalWriteStringUnicode(object state)
        {
            var data = (IsoStorageDataContainer)state;

            var writableString = data.WritableData;
            var file = data.Filename;

            lock (InternalLock)
            {
                if (WriteSessionRunning)
                    throw new NotSupportedException("Another session is still active");

                using (var storage = GetIsolatedStorageFileSystem())
                {
                    WriteSessionRunning = true;

                    using (var fileStream = storage.OpenFile(file, FileMode.Create, FileAccess.Write))
                    using (var streamWriter = new StreamWriter(fileStream))
                    {
                        streamWriter.Write(writableString as String);
                        fileStream.Flush();
                    }

                    Debug.WriteLine("------------> STATUS: Filename {0} writen to isolated storage!", file);
                    WriteSessionRunning = false;
                }
            }
        }

        #endregion

        private class IsoStorageDataContainer
        {
            private readonly string _filename;
            private readonly object _writableData;

            public IsoStorageDataContainer(string filename, object writableData)
            {
                _filename = filename;
                _writableData = writableData;
            }

            public object WritableData
            {
                get { return _writableData; }
            }

            public string Filename
            {
                get { return _filename; }
            }
        }

        #region Statics

        public static bool DeleteFile(string name)
        {
            using (var storage = GetIsolatedStorageFileSystem())
            {
                if (storage.FileExists(name))
                {
                    storage.DeleteFile(name);
                    return true;
                }
            }

            return false;
        }

        public static bool DeleteDirectory(string name)
        {
            using (var storage = GetIsolatedStorageFileSystem())
            {
                if (storage.FileExists(name))
                {
                    storage.DeleteDirectory(name);
                    return true;
                }
            }

            return false;
        }

        public static bool CreateDirectory(string name)
        {
            using (var storage = GetIsolatedStorageFileSystem())
            {
                if (storage.DirectoryExists(name))
                    return false;

                storage.CreateDirectory(name);
                return true;
            }
        }

        public static bool FileExists(string name)
        {
            using (var storage = GetIsolatedStorageFileSystem())
            {
                return storage.FileExists(name);
            }
        }

        #endregion
    }

}
