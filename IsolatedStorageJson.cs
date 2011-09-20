using System;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;
using System.Threading;
using System.Windows;
using Newtonsoft.Json;
using Where.Common.Logger;

namespace Where.Common
{

    /// <summary>
    /// Isolated storage file helper class
    /// </summary>
    /// <typeparam name="T">Data type to serialize/deserialize</typeparam>
    public class IsolatedStorageJson<T> : IsolatedStorageBase where T : class
    {
        /// <summary>
        /// Lock object for thread pool save operations
        /// </summary>
        private readonly object _saveLock = new object();

        /// <summary>
        /// Saves the data as a JSON string on the caller thread.
        /// </summary>
        /// <param name="fileName">Name of the file to write to.</param>
        /// <param name="data">The data to store.</param>
        public void SaveDataToJson(string fileName, T data)
        {
            try
            {
                SerializeAndSaveToFile(fileName, data);
            }
            catch (Exception e)
            {
                ErrorLogCollection.Instance.AddError("SaveDataToJson", e.Message);
                throw new NotSupportedException(e.Message, e);
            }
        }

        public void SyncSaveDataToJson(string fileName, T data)
        {
            try
            {
                SerializeAndSaveToFile(fileName, data, true);
            }
            catch (Exception e)
            {
                ErrorLogCollection.Instance.AddError("SaveDataToJson", e.Message);
                throw new NotSupportedException(e.Message, e);
            }
        }


        /// <summary>
        /// Save data to JSON asynchronously
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="data"></param>
        /// <param name="onCompleted"></param>
        /// <param name="onFail"></param>
        public void BeginSaveDataToJson(string filename, T data, Action onCompleted = null, Action<String, Exception> onFail = null)
        {
            try
            {
                ThreadPool.QueueUserWorkItem(state =>
                                                 {
                                                     Thread.CurrentThread.Name = "IsolatedStorageJson-BeginSaveDataToJson";

                                                     lock (_saveLock)
                                                     {
                                                         SerializeAndSaveToFile(filename, data);
                                                         if (onCompleted != null)
                                                             Deployment.Current.Dispatcher.BeginInvoke(onCompleted);

                                                     }
                                                 });
            }
            catch (Exception e)
            {
                ErrorLogCollection.Instance.AddError("BeginSaveDataToJson", e.Message);
                if (onFail != null)
                    Deployment.Current.Dispatcher.BeginInvoke(() => onFail("AsyncSaveOperationFailed", e));
            }
        }

        /// <summary>
        /// Loads data from a file
        /// </summary>
        /// <param name="fileName">Name of the file to read.</param>
        /// <returns>Data object</returns>
        public T LoadFromFile(string fileName)
        {
            var result = ReadStreamFromFile<T>(fileName);

            if (result == default(T))
                DeleteFile(fileName);

            return result;
        }

        /// <summary>
        /// Saves data to a file.
        /// </summary>
        /// <param name="fileName">Name of the file to write to</param>
        /// <param name="data">The data to save</param>
        /// <param name="sync"></param>
        private void SerializeAndSaveToFile(string fileName, T data, bool sync = false)
        {
            try
            {
                if (sync)
                {
                    var serializedString = JsonConvert.SerializeObject(data);
                    WriteStringUnicode(serializedString, fileName);
                }
                else
                {
                    Utils.BackgroundWorkerDefault.QueueSimple(
                        () =>
                        {
                            var serializedString = JsonConvert.SerializeObject(data);
                            WriteStringUnicode(serializedString, fileName);
                        },
                        () => Debug.WriteLine("JSON serialization and write completed!"));
                }
            }
            catch (Exception e)
            {
                ErrorLogCollection.Instance.AddError("SaveFileToIsoStorage", e.Message);
                Debug.WriteLine("Json exception! Message:{0}", e.Message);
            }
        }

        protected override object StreamOperationOnFile(Stream isoStream, string file = null)
        {
            T loadedFile;

            if (isoStream == null)
                loadedFile = default(T);
            else
                using (var streamReader = new StreamReader(isoStream))
                {
                    var readerData = streamReader.ReadToEnd();
                    try
                    {
                        loadedFile = JsonConvert.DeserializeObject<T>(readerData);
                    }
                    catch (Exception jsonEx)
                    {
                        ErrorLogCollection.Instance.AddError("LoadFileFromIsoStorage", string.Format("{0} for filename: {1}", jsonEx.Message, file ?? String.Empty));
                        loadedFile = default(T);
                    }
                }


            return loadedFile;
        }
    }
}
