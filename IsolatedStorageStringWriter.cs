using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;

namespace Where.Common
{
	public class IsolatedStorageStringWriter : IsolatedStorageBase
	{
		private static IsolatedStorageStringWriter _writer;

		public static IsolatedStorageStringWriter GetWriter
		{
			get { return _writer ?? (_writer = new IsolatedStorageStringWriter()); }
		}

		private IsolatedStorageStringWriter()
		{
		}


		public void BeginWriteStringToFile(String contents, String file)
		{
			Action createDerivation = () =>
			{
				var dumb1 = contents;
				var dumb2 = file;
				WriteStringToFileReal(dumb1, dumb2);
			};


			Debug.WriteLine("Enqueuing write operation for file \"{0}\"...", file);
			Utils.BackgroundWorkerDefault.QueueSimple(createDerivation);

		}

		public void WriteStringToFileSync(String contents, String file)
		{
			Debug.WriteLine("Writing file \"{0}\" to isolated storage...", file);
			WriteStringToFileReal(contents, file);
		}

		private static void WriteStringToFileReal(String contents, String file)
		{
			using (var fsystem = GetIsolatedStorageFileSystem())
			using (var fileStream = fsystem.OpenFile(file, FileMode.Create, FileAccess.Write))
			using (var streamWriter = new StreamWriter(fileStream))
			{
				streamWriter.Write(contents);
				streamWriter.Flush();
			}

		}

		public String ReadStringFromIsolatedStorage(String filename)
		{
			var result = ReadStreamFromFile<string>(filename);
			return result;
		}

		#region Overrides of IsolatedStorageBase

		protected override object StreamOperationOnFile(Stream isoStream, string file)
		{
			if (isoStream != null)
				using (var stringReader = new StreamReader(isoStream))
				{
					var fileContents = stringReader.ReadToEnd();
					return fileContents;
				}

			return String.Empty;
		}

		#endregion
	}
}