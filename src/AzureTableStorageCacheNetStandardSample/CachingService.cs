using System;
using Microsoft.Extensions.Caching.Distributed;
using static System.Text.Encoding;

namespace AzureTableStorageCacheNetStandardSample
{
    class CachingService
    {
        private readonly IDistributedCache cacheHandler;

        public CachingService(IDistributedCache cacheHandler)
        {
            this.cacheHandler = cacheHandler;
        }

        public void SetCache(string key, string msg)
        {
            cacheHandler.Set(key,
                             UTF32.GetBytes(msg),
                             new DistributedCacheEntryOptions
                             {
                                 AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(120)
                             });
        }

        public string GetCache(string key)
        {
            var bytes = cacheHandler.Get(key);
            if (bytes == null)
                return "NO CACHE ITEM!";

            var result = UTF32.GetString(bytes);
            return result;
        }
    }
}