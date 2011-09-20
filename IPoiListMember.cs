using Newtonsoft.Json;

namespace Where.Common
{
	public interface IPoiListMember
	{
		[JsonIgnore]
		string ImageUrl { get; }

		[JsonIgnore]
		string Header { get; }

		[JsonIgnore]
		string Subtitle { get; }

		[JsonIgnore]
		string Description { get; }

		[JsonIgnore]
		string Footer { get; }
	}
}
