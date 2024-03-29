﻿using System;
using System.Threading;
using System.Diagnostics;
using Microsoft.Phone.Info;

namespace Where.Common.Diagnostics
{
	public static class WhereDebug
	{

#if DEBUG
		internal static bool EnableOnlineDebug = true;
#else
		public static bool EnableOnlineDebug = false;
#endif
		private static readonly String UserDevice = string.Format("{0} {1}", DeviceExtendedProperties.GetValue("DeviceManufacturer"), DeviceExtendedProperties.GetValue("DeviceName"));

		public static void InitDebug()
		{
			WhereDebugService.Instance.BeginRemoteDebug();
#if DEBUG
			WriteLine("Debugging thread started");
#else
			WriteLine("Online debugging started via command!");
#endif
		}

		/// <summary>
		/// Call standard debugger but with web debugging
		/// </summary>
		/// <param name="output"></param>
		[Conditional("REMOTE_DEBUG")]
		[Conditional("DEBUG")]
		public static void WriteLine(String output)
		{
			Debug.WriteLine("({0}): {1}", GetThreadInfo(), output);
			if (EnableOnlineDebug)
				WhereDebugService.Instance.SendDebugMessage(UserDevice, output);
		}

		/// <summary>
		/// Calls standard debugger output but if a condition is met (is true)
		/// </summary>
		/// <param name="condition">If condition is "true", will write to Debugger output</param>
		/// <param name="output">Writes the output string with a line terminator if a condition is met</param>
		[Conditional("REMOTE_DEBUG")]
		[Conditional("DEBUG")]
		public static void WriteLineIf(bool condition, String output)
		{
			if (condition)
				WriteLine(output);
		}

		/// <summary>
		/// Gets the calling thread's ID and IsBackground property
		/// </summary>
		/// <returns></returns>
		private static string GetThreadInfo()
		{
			var aquireBuilder = Utils.GetStringBuilderWithHandle;

			var result = aquireBuilder.ExecuteSafeOperationOnObject(builder =>
														{
															builder.Flush();

															if (String.IsNullOrEmpty(Thread.CurrentThread.Name))
															{
																builder.Append("Thread: ").Append(Thread.CurrentThread.ManagedThreadId);
															}
															else
																builder.Append(Thread.CurrentThread.Name);

															builder.Append(Thread.CurrentThread.IsBackground ? "(Background)" : "(Foreground)");


															return builder.ToString();
														});

			return result;
		}

		public static void Assert(bool conditionMustBeTrue, string message = null)
		{

#if DEBUG
			if (!conditionMustBeTrue)
				throw new WhereException("Assertion failed for " + message, message);
#else
			Debug.Assert(conditionMustBeTrue,message);
#endif
		}

		public static void Assert<T>(Predicate<T> predicate, T value, string message = null)
		{
			Assert(predicate(value), message);
		}
	}
}
