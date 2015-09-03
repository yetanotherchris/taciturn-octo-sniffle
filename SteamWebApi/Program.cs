using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PortableSteam;
using PortableSteam.Interfaces.General.ISteamUser;
using SteamWebApiTest.v2;

namespace SteamWebApiTest
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			SteamWebAPI.SetGlobalKey("put your key here");

			var steamIdQueue = new SteamIdQueue();
			var database = new CSGOPlayerDatabase();
			var repository = new DatabaseRepository(database);
			var scanner = new PlayerScannerEngine(steamIdQueue, database);

			//ShowStats(repository);return;
			steamIdQueue.Push(76561198029142573);
			scanner.FindFriends(76561198029142573);
			var thread = new Thread(() =>
			{
				while (true)
				{
					repository.Store();
				}
			});

			thread.Start();

			var thread2 = new Thread(() =>
			{
				while (true)
				{
					scanner.FindAndLookupPlayers();
				}
			});

			thread2.Start();

			while (true)
			{
				
			}
		}

		private static void ShowStats(DatabaseRepository repository)
		{
			List<SteamUser> list = repository.Load();

			int playerCount = list.Count;
			List<SteamUser> vacs = list.Where(x => x.BanType == BanType.VAC).ToList();
			List<SteamUser> community = list.Where(x => x.BanType == BanType.Community).ToList();
			List<SteamUser> noBans = list.Where(x => x.BanType == BanType.None).ToList();

			int noBansCount = noBans.Count;
			var newPlayers = list.Count(x => x.TimePlayed.TotalHours < 15);

			var below30 = list.Count(x => x.TimePlayed.TotalHours < 30);

			Console.WriteLine("Total: {0}", playerCount);
			Console.WriteLine("Average hours: {0}", list.Median(x => x.TimePlayed.TotalHours));
			Percentage.ConsoleWrite("New players", newPlayers, playerCount);
			Percentage.ConsoleWrite("Below 30 hours", below30, playerCount);
			Percentage.ConsoleWrite("No bans", noBansCount, playerCount);

			//
			// VAC bans
			//
			int vacCount = vacs.Count;
			var vacsBelow30 = vacs.Count(x => x.TimePlayed.TotalHours < 30);

			Percentage.ConsoleWrite("VACS", vacCount, playerCount);
			Console.WriteLine("VAC average hours: {0}", vacs.Median(x => x.TimePlayed.TotalHours));
			Percentage.ConsoleWrite("VAC below 30 hours", vacsBelow30, vacCount);

			//
			// Community
			//
			int communityCount = community.Count;
			var communityBelow30 = vacs.Count(x => x.TimePlayed.TotalHours < 30);

			Percentage.ConsoleWrite("Community", communityCount, playerCount);
			Console.WriteLine("Community average hours: {0}", community.Median(x => x.TimePlayed.TotalHours));
			Percentage.ConsoleWrite("Community below 30 hours", communityBelow30, communityCount);

			Console.WriteLine("OK?");
			Console.Read();

			return;
		}

		public class Percentage
		{
			public static void ConsoleWrite(string message, int amount, int total)
			{
				Console.WriteLine("{0}: {1} {2:P}", message, amount, ((decimal)amount / total));
			}
		}
	}

	public static class LinqExtensions
	{
		// Stolen from SO: http://stackoverflow.com/questions/4134366/math-stats-with-linq

		public static double? Median<TColl, TValue>(this IEnumerable<TColl> source,Func<TColl, TValue> selector)
		{
			return source.Select<TColl, TValue>(selector).Median();
		}

		public static double? Median<T>(this IEnumerable<T> source)
		{
			if (Nullable.GetUnderlyingType(typeof(T)) != null)
				source = source.Where(x => x != null);

			int count = source.Count();
			if (count == 0)
				return null;

			source = source.OrderBy(n => n);

			int midpoint = count / 2;
			if (count % 2 == 0)
				return (Convert.ToDouble(source.ElementAt(midpoint - 1)) + Convert.ToDouble(source.ElementAt(midpoint))) / 2.0;
			else
				return Convert.ToDouble(source.ElementAt(midpoint));
		}
	}
}
