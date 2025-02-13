using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Redis_API.Helper;

namespace Redis_API.Controllers
{
	[Route("api/values")]
	[ApiController]
	public class DistributedCacheController : ControllerBase
	{
		public IDistributedCache _distributedCache { get; }

		public DistributedCacheController(IDistributedCache distributedCache)
		{
			_distributedCache = distributedCache;
		}

		[HttpPost("set-distributed-cache-value", Name = "SetDistributedCacheValue")]
		public async Task<IActionResult> SetDistributedCacheValue()
		{
			await _distributedCache.setEntryAsync("DistributedCacheKey", "DistributedCacheValue");
			//HttpContext.Session.SetString("SessionKey", "SessionValue"); // store in redis
			return Ok("Distributed Cache Value Set");
		}

		[HttpGet("get-distributed-cache-value", Name = "GetDistributedCacheValue")]
		public async Task<IActionResult> GetDistributedCacheValue()
		{
			var cacheValue = await _distributedCache.GetStringAsync("DistributedCacheKey");
			return Ok(cacheValue);
		}

		// Eviction polisy used when cache is full to remove the least used cache and make space for new cache
		// least recently used (LRU), least frequently used (LFU), windows Tiny Lfu which is a combination of LRU and LFU, Time to live (TTL)

		// Approximation algorithm used to remove the cache when the cache is full in LRU and LFU work in sample of cases.
	}
}
