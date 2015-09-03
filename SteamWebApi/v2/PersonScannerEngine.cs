using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PortableSteam;
using PortableSteam.Fluent.General.IPlayerService;
using PortableSteam.Fluent.General.ISteamUser;
using PortableSteam.Interfaces.General.IPlayerService;
using PortableSteam.Interfaces.General.ISteamUser;

namespace SteamWebApiTest.v2
{
	public class PlayerScannerEngine
	{
		private readonly SteamIdQueue _queue;
		private readonly CSGOPlayerDatabase _database;
		private static readonly int QUEUE_SIZE_BEFORE_LOOKUPS = 1000;
		private static readonly int BATCH_SIZE = 750;

		public PlayerScannerEngine(SteamIdQueue queue, CSGOPlayerDatabase database)
		{
			_queue = queue;
			_database = database;
		}

		public void BatchLookup()
		{
			Console.BackgroundColor = ConsoleColor.DarkGreen;
			Console.WriteLine("Performing batch lookup of players");
			Console.BackgroundColor = ConsoleColor.Black;

			List<long> ids = _queue.BatchPop(BATCH_SIZE);

			Parallel.ForEach(ids, (steamId) =>
			{
				Console.WriteLine(" - Looking up {0}", steamId);
				SteamUser user = GetUser(steamId);
				if (user != null)
				{
					_database.Push(user);
					Console.WriteLine(" [] Adding to database: {0} {1} - {2}", user.BanType, user.Name, user.TimePlayed.TotalHours);
				}
			});
		}

		public void FindAndLookupPlayers()
		{
			if (_queue.Count > QUEUE_SIZE_BEFORE_LOOKUPS)
			{
				BatchLookup();
			}

			long steamId = _queue.Pop();

			if (steamId > -1)
			{
				FindFriends(steamId);
			}
		}

		public void FindFriends(long steamId)
		{
			SteamIdentity identity = SteamIdentity.FromSteamID(steamId);
			GetFriendListBuilder friendBuilder = SteamWebAPI.General().ISteamUser().GetFriendList(identity, RelationshipType.Friend);

			try
			{
				var response = friendBuilder.GetResponse();
				if (response != null && response.Data != null)
				{
					List<Friend> friends = response.Data.Friends.ToList();

					foreach (Friend friend in friends)
					{
						_queue.Push(friend.Identity.SteamID);
					}

					Console.WriteLine("Found friends for {0} (found {1}), queue: {2}", steamId, friends.Count, _queue.Count);
				}
			}
			catch (Exception)
			{
				
			}
		}

		public TimeSpan IsCsgoInstalled(long steamId)
		{
			SteamIdentity identity = SteamIdentity.FromSteamID(steamId);
			GetOwnedGamesBuilder gamesBuilder = SteamWebAPI.General().IPlayerService().GetOwnedGames(identity);

			try
			{
				GetOwnedGamesResponse response = gamesBuilder.GetResponse();
				if (response != null && response.Data != null)
				{
					if (response.Data.Games != null)
					{
						var csgo = response.Data.Games.FirstOrDefault(x => x.AppID == 730);
						if (csgo != null)
						{
							return csgo.PlayTimeTotal;
						}
					}
				}
			}
			catch (Exception)
			{
				
			}

			return TimeSpan.Zero;
		}

		public string GetName(long steamId)
		{
			SteamIdentity identity = SteamIdentity.FromSteamID(steamId);
			GetPlayerSummariesBuilder summaryBuilder = SteamWebAPI.General().ISteamUser().GetPlayerSummaries(identity);

			try
			{
				GetPlayerSummariesResponse response = summaryBuilder.GetResponse();
				if (response != null && response.Data != null)
				{
					GetPlayerSummariesResponsePlayer player = response.Data.Players.FirstOrDefault();
					if (player != null)
					{
						return player.PersonaName;
					}
				}
			}
			catch (Exception)
			{

			}

			return "";
		}

		public BanType GetBanType(long steamId)
		{
			SteamIdentity identity = SteamIdentity.FromSteamID(steamId);
			GetPlayerBansBuilder summaryBuilder = SteamWebAPI.General().ISteamUser().GetPlayerBans(identity);

			try
			{
				GetPlayerBansResponse response = summaryBuilder.GetResponse();
				if (response != null && response.Players != null)
				{
					GetPlayerBansResponsePlayer player = response.Players.FirstOrDefault();
					if (player != null)
					{
						if (player.VACBanned)
						{
							// Ignore MW2/3 vac bans
							bool hasMw2OrMw3 = HasMw2OrMw3(steamId);
							return hasMw2OrMw3 ? BanType.None : BanType.VAC;
						}
						else if (player.CommunityBanned)
						{
							return BanType.Community;
						}
					}
				}
			}
			catch (Exception)
			{

			}

			return BanType.None;
		}

		public bool HasMw2OrMw3(long steamId)
		{
			SteamIdentity identity = SteamIdentity.FromSteamID(steamId);
			GetOwnedGamesBuilder gamesBuilder = SteamWebAPI.General().IPlayerService().GetOwnedGames(identity);

			try
			{
				GetOwnedGamesResponse response = gamesBuilder.GetResponse();
				if (response != null && response.Data != null)
				{
					if (response.Data.Games != null)
					{
						var mw2AndMw3 = response.Data.Games.FirstOrDefault(x => x.AppID == 10180 || x.AppID == 42690);
						if (mw2AndMw3 != null)
						{
							return true;
						}
					}
				}
			}
			catch (Exception)
			{

			}

			return false;
		}

		public SteamUser GetUser(long steamId)
		{
			TimeSpan timePlayed = IsCsgoInstalled(steamId);
			if (timePlayed > TimeSpan.Zero)
			{
				string name = GetName(steamId);
				BanType banType = GetBanType(steamId);

				var steamUser2 = new SteamUser(steamId)
				{
					TimePlayed = timePlayed,
					BanType = banType,
					Name = name
				};

				return steamUser2;
			}

			return null;
		}
	}
}
