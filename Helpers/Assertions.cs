using System;
#if DEBUG
using System.Diagnostics;
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

		public static void NotNullArgument(object argument, string argumentName)
		{

#if DEBUG
			Debug.Assert(argument != null, ArgumentNullMessage);
#else

			if (argument == null)
				throw new ArgumentNullException(argumentName, ArgumentNullMessage);
#endif
		}

		public static void NotNullValue(object argument, string reason = null)
		{
#if DEBUG
			Debug.Assert(argument != null, ArgumentNullMessage);
#else
			if (argument == null)
				throw new NullReferenceException(ArgumentNullMessage, new WhereException(ArgumentNullMessage, reason));
#endif
		}


		public static void IsValueValid<T>(Predicate<T> validationDelegate, T value, string reason = null)
		{
			IsValueValid(validationDelegate(value));
		}

		public static void IsValueValid(bool condition, string reason = null)
		{
#if DEBUG
			Debug.Assert(condition, ArgumentOutOfRangeMessage);
#else
			if (!condition)
				throw new InvalidOperationException(ArgumentOutOfRangeMessage, new WhereException(ArgumentOutOfRangeMessage, reason));
#endif
		}

		public static void StringNotNullOrEmpty(string argumentValue, string reason = null)
		{
#if DEBUG
			Debug.Assert(!string.IsNullOrEmpty(argumentValue), StringNullOrEmptyMessage);
#else
			if (string.IsNullOrEmpty(argumentValue))
				throw new ArgumentNullException("argumentValue", new WhereException(StringNullOrEmptyMessage, reason));
#endif
		}



	}
}
