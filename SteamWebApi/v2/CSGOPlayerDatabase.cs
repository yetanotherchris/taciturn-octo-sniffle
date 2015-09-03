using System;
using System.Collections.Concurrent;

namespace SteamWebApiTest.v2
{
	public class CSGOPlayerDatabase
	{
		private ConcurrentQueue<SteamUser> _queue;

		public int Count
		{
			get
			{
				return _queue.Count;
			}
		}

		public CSGOPlayerDatabase()
		{
			_queue = new ConcurrentQueue<SteamUser>();
		}

		public void Push(SteamUser user)
		{
			_queue.Enqueue(user);
		}

		public SteamUser Pop()
		{
			SteamUser result;
			_queue.TryDequeue(out result);

			return result;
		}
	}
}
