using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace SteamWebApiTest.v2
{
	public class DatabaseRepository
	{
		private CSGOPlayerDatabase _database;
		private ConnectionMultiplexer _redis;
		private IDatabase _db;

		public DatabaseRepository(CSGOPlayerDatabase database)
		{
			_database = database;
			_redis = ConnectionMultiplexer.Connect("localhost");
			_db = _redis.GetDatabase();
		}

		public List<SteamUser> Load()
		{
			var server = _redis.GetServer("localhost:6379");
			List<SteamUser> list = new List<SteamUser>();
			foreach (RedisKey redisKey in server.Keys())
			{
				SteamUser user = JsonConvert.DeserializeObject<SteamUser>(_db.StringGet(redisKey));
				list.Add(user);
			}

			return list;
		}

		public void Store()
		{
			if (_database.Count > 0)
			{
				SteamUser user = _database.Pop();
				if (user != null)
				{
					string json = JsonConvert.SerializeObject(user);
					_db.StringSet(user.SteamId.ToString(), json);
				}
			}
		}
	}
}
