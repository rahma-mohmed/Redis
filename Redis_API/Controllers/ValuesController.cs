using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Redis_API.Controllers
{
	[ApiController]
	[Route("api/values")]
	public class ValuesController : ControllerBase
	{
		public IMemoryCache _memoryCache { get; }

		public ValuesController(IMemoryCache memoryCache)
		{
			_memoryCache = memoryCache;
		}

		[HttpGet("set-values-list", Name = "GetSetValuesList")]
		public IEnumerable<int> GetSetValuesList()
		{
			string cacheEntry = "Values";
			if (!_memoryCache.TryGetValue(cacheEntry, out List<int> values))
			{
				values = Enumerable.Range(1, 10).Select(index => index).ToList();

				//setSlidingExpiration: The time at which the cache entry expires if it is not accessed.
				//setAbsoluteExpiration: The time at which the cache entry expires regardless of how often it is accessed.

				var cacheEntryOptions = new MemoryCacheEntryOptions()
					.SetSlidingExpiration(TimeSpan.FromSeconds(10))
					.SetAbsoluteExpiration(TimeSpan.FromSeconds(30));

				_memoryCache.Set(cacheEntry, values, cacheEntryOptions);
			}

			return values;
		}

		[HttpGet("get-or-create-async", Name = "GetValuesList_GetOrCreateAsync")]
		public async Task<IEnumerable<int>> GetValuesList_GetOrCreateAsync()
		{
			string cacheEntry = "Values";
			var cacheValue = await _memoryCache.GetOrCreateAsync(cacheEntry, entry =>
			{
				entry.SlidingExpiration = TimeSpan.FromSeconds(10);
				entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
				return Task.FromResult(Enumerable.Range(1, 10).Select(index => index).ToList());
			});

			return cacheValue;
		}

		[HttpGet("post-eviction-callback", Name = "GetValuesList_PostEvictionCallback")]
		public IEnumerable<int> GetValuesList_PostEvictionCallback()
		{
			string cacheEntry = "Values";
			if (!_memoryCache.TryGetValue(cacheEntry, out List<int> values))
			{
				values = Enumerable.Range(1, 10).Select(index => index).ToList();

				var cacheEntryOptions = new MemoryCacheEntryOptions()
					.RegisterPostEvictionCallback((key, value, reason, state) =>
					{
						Console.WriteLine($"Key: {key}, Value: {value}, Reason: {reason}, State: {state}");
					});

				_memoryCache.Set(cacheEntry, values, cacheEntryOptions);
			}
			return values;
		}

		[HttpGet("post-eviction-callback-method", Name = "GetValuesList_PostEvictionCallbackMethod")]
		public IEnumerable<int> GetValuesList_PostEvictionCallbackMethod()
		{
			string cacheEntry = "Values";
			if (!_memoryCache.TryGetValue(cacheEntry, out List<int> values))
			{
				values = Enumerable.Range(1, 10).Select(index => index).ToList();

				var cacheEntryOptions = new MemoryCacheEntryOptions()
					.SetAbsoluteExpiration(TimeSpan.FromSeconds(30))

					// in memory cache : memory of web server
					// CacheItemPriority.Low: The cache entry should be evicted after other cache entries.
					// CacheItemPriority.High: The cache entry should be evicted before other cache entries.
					// CacheItemPriority.NeverRemove: The cache entry should never be removed from the cache.
					// CacheItemPriority.Normal: The cache entry should be evicted as a typical cache entry.

					// first time the cache expires, it will be removed from the cache
					.SetPriority(CacheItemPriority.Low)
					// Register a callback that will run after the cache expires
					.RegisterPostEvictionCallback(PostEvictionCallBack, _memoryCache);

				_memoryCache.Set(cacheEntry, values, cacheEntryOptions);
			}
			return values;
		}

		private static void PostEvictionCallBack(object key, object value, EvictionReason reason, object state)
		{
			var memorycache = (IMemoryCache)state;

			memorycache.Set(
				"removeMessage",
				$"Key: {key}, Value: {value}, Reason: {reason}"
				);
		}
	}
}
