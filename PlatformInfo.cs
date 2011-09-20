using System;
using System.Globalization;
using System.Reflection;
using Microsoft.Phone.Info;

namespace Where.Framework.Support
{
	public static class PlatformInfo
	{

		public static string DeviceManufacturer
		{
			get
			{
				object obj;
				if (DeviceExtendedProperties.TryGetValue("DeviceManufacturer", out obj))
					return (string)obj;
				return "Unknown";
			}
		}

		public static string CurrentTimeZone
		{
			get
			{
				var tzi = TimeZoneInfo.Local;
				var hours = tzi.BaseUtcOffset.TotalHours;
				return hours.ToString(CultureInfo.InvariantCulture);
			}
		}

		public static string DeviceName
		{
			get
			{
				object obj;
				if (DeviceExtendedProperties.TryGetValue("DeviceName", out obj))
					return (string)obj;
				return "Unknown";
			}
		}

		public static string DeviceHardwareVersion
		{
			get
			{
				object obj;
				if (DeviceExtendedProperties.TryGetValue("DeviceHardwareVersion", out obj))
					return (string)obj;
				return "Unknown";
			}
		}

		public static string DeviceFirmwareVersion
		{
			get
			{
				object obj;
				if (DeviceExtendedProperties.TryGetValue("DeviceFirmwareVersion", out obj))
					return (string)obj;
				return "Unknown";
			}
		}

		public static string AppVersion
		{
			get
			{
				var asmbly = Assembly.GetCallingAssembly();
				var sFullName = asmbly.FullName;
				return sFullName.Substring(0, sFullName.IndexOf(", Culture"));

			}
		}

		public static long DeviceTotalMemory {

			get
			{
				var totalMemory = GC.GetTotalMemory(true);
				return totalMemory;
			}
		}
	}
}
