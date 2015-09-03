using System;

namespace SteamWebApiTest.v2
{
	public class SteamUser
	{
		public long SteamId { get; set; }
		public string Name { get; set; }
		public TimeSpan TimePlayed { get; set; }
		public BanType BanType { get; set; }

		public SteamUser(long steamId)
		{
			SteamId = steamId;
		}
	}
}
