using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Redis_API.Helper
{
	public static class RedisCacheHelper
	{
		// extension method to set cache value	
		public static async Task setEntryAsync<T>(this IDistributedCache distributedCache, string key, T value, TimeSpan? absoluteExpireTime = null, TimeSpan? slidingExpireTime = null)
		{
			var options = new DistributedCacheEntryOptions();
			options.AbsoluteExpirationRelativeToNow = absoluteExpireTime ?? TimeSpan.FromSeconds(30);
			options.SlidingExpiration = slidingExpireTime ?? TimeSpan.FromSeconds(10);

			var json = JsonSerializer.Serialize(value);
			await distributedCache.SetStringAsync(key, json, options);
		}

		// extension method to get cache value
		public static async Task<T?> getEntryAsync<T>(this IDistributedCache distributedCache, string key)
		{
			var json = await distributedCache.GetStringAsync(key);
			if (json == null)
			{
				return default(T);
			}
			return JsonSerializer.Deserialize<T>(json);
		}
	}
}
