using System;
using AzureTableStorageCacheNetStandard;
using Microsoft.Extensions.Caching.Distributed;
using static System.Console;
using static System.Text.Encoding;

namespace AzureTableStorageCacheNetStandardSample
{
    class Program
    {
        private static AzureTableStorageCacheHandler cacheHandler;
        private const string KEY = "test_key";

        static void Main(string[] args)
        {
            WriteLine("Hello World!");

            var connString = "CONN STR";
                
            
            cacheHandler = new AzureTableStorageCacheHandler(connString, "test_table", "test_pk");

            //SetCache();

            var result = GetCache();

            WriteLine($"Cache: {result}");

            ReadKey();
        }
        private static void SetCache()
        {
            cacheHandler.Set(KEY,
                             UTF32.GetBytes("something beautiful!"),
                             new DistributedCacheEntryOptions
                             {
                                 AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(120)
                             });
        }

        private static string GetCache()
        {
            var bytes = cacheHandler.Get(KEY);
            if (bytes != null) {
                var result = UTF32.GetString(bytes);

                return result;
            }

            return "NO CACHE ITEM!";
        }
    }
}
