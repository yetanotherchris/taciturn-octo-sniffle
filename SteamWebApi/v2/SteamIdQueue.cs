using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamWebApiTest.v2
{
	public class SteamIdQueue
	{
		private readonly ConcurrentQueue<long> _queue;

		public int Count
		{
			get
			{
				return _queue.Count;
			}
		}

		public SteamIdQueue()
		{
			_queue = new ConcurrentQueue<long>();
		}

		public void Push(long steamdId)
		{
			_queue.Enqueue(steamdId);
		}

		public long Pop()
		{
			long result;
			if (!_queue.TryDequeue(out result))
				result = -1;

			return result;
		}

		public List<long> BatchPop(int batchSize)
		{
			var list = _queue.ToList().Take(750).ToList();
            Parallel.ForEach(list, (id) =>
			{
				long result;
				if (!_queue.TryDequeue(out result))
					list.Remove(result);
			});

			return list;
		}
	}
}
