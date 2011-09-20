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
				return DeviceStatus.DeviceManufacturer;
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
				return DeviceStatus.DeviceName;
			}
		}

		public static string DeviceHardwareVersion
		{
			get
			{
				return DeviceStatus.DeviceHardwareVersion;
			}
		}

		public static string DeviceFirmwareVersion
		{
			get
			{
				return DeviceStatus.DeviceFirmwareVersion;
			}
		}

		public static string AppVersion
		{
			get
			{
				var asmbly = Assembly.GetCallingAssembly().FullName;
				var sFullName = new AssemblyName(asmbly);
				var version = sFullName.Version;
				return version.ToString();
			}
		}

		public static double DeviceTotalMemory
		{
			get { return DeviceStatus.DeviceTotalMemory; }

		}
	}
}
