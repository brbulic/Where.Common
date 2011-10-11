using System;
#if DEBUG
using System.Diagnostics;
using Where.Common.Diagnostics;

#else
using Where.Common;
#endif

namespace Where
{
	public static partial class Utils
	{
		public const string ArgumentNullMessage = @"Argument value should not be NULL.";
		public const string StringNullOrEmptyMessage = @"String should not be null or empty.";
		public const string ArgumentOutOfRangeMessage = @"Argument value should not be out of range.";
		public const string InvalidStateMessage = @"The state you're in should not happen.";
		public const string ListMustBeEmpty = @"List should be empty before requesting values.";
		public const string ListMustNotBeEmpty = @"List should not be empty before requesting values.";

		/// <summary>
		/// Test an argument if it's null. Asserts in DEBUG mode, throws an exception if in RELEASE
		/// </summary>
		/// <param name="argument">Argument to test</param>
		/// <param name="argumentName">Argument name</param>
		/// <param name="reason">Add a reason (optional)</param>
		public static void NotNullArgument(object argument, string argumentName, string reason = null)
		{

#if DEBUG
			WhereDebug.Assert(argument != null, string.Format("{0} {1}", ArgumentNullMessage, reason));
#else

			if (argument == null)
				throw new ArgumentNullException(argumentName, reason ?? ArgumentNullMessage);
#endif
		}

		/// <summary>
		/// Test for a value being null. Asserts in DEBUG, throws in RELEASE.
		/// </summary>
		/// <param name="argument">Reference to test</param>
		/// <param name="reason">Why shouldn't it be null?</param>
		public static void NotNullValue(object argument, string reason = null)
		{
#if DEBUG
			WhereDebug.Assert(argument != null, reason ?? ArgumentNullMessage);
#else
			if (argument == null)
				throw new NullReferenceException(ArgumentNullMessage, new WhereException(ArgumentNullMessage, reason));
#endif
		}

		/// <summary>
		/// Check if a reference has an expected value in the calling Application state. The predicate version. Asserts in DEBUG, throws in RELEASE.
		/// </summary>
		/// <typeparam name="T">Checkable type</typeparam>
		/// <param name="validationDelegate">Predicate to test the value</param>
		/// <param name="value">The tested value's reference</param>
		/// <param name="reason">Write what should be expected and why.</param>
		public static void IsValueValid<T>(Predicate<T> validationDelegate, T value, string reason = null)
		{
			IsValueValid(validationDelegate(value));
		}


		/// <summary>
		/// Check if a state of a value is valid when called. Asserts in DEBUG, throws in RELEASE.
		/// </summary>
		/// <param name="condition">True if value valid. Test by yourself!</param>
		/// <param name="reason">Write what should be expected and why.</param>
		public static void IsValueValid(bool condition, string reason = null)
		{
#if DEBUG
			WhereDebug.Assert(condition, ArgumentOutOfRangeMessage);
#else
			if (!condition)
				throw new InvalidOperationException(ArgumentOutOfRangeMessage, new WhereException(ArgumentOutOfRangeMessage, reason));
#endif
		}

		/// <summary>
		/// Check if a string isn't null or empty. Asserts in DEBUG, throws in RELEASE.
		/// </summary>
		/// <param name="argumentValue">String to test</param>
		/// <param name="reason">Why should it not be null?</param>
		public static void StringNotNullOrEmpty(string argumentValue, string reason = null)
		{
#if DEBUG
			WhereDebug.Assert(!string.IsNullOrEmpty(argumentValue), reason);
#else
			if (string.IsNullOrEmpty(argumentValue))
				throw new ArgumentNullException("argumentValue", new WhereException(StringNullOrEmptyMessage, reason));
#endif
		}

		[Conditional("DEBUG")]
		public static void ThrowBreakpointForPredicate(bool predicate)
		{
			Debug.Assert(!predicate);
		}
	}
}
