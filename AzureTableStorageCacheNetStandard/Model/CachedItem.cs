using System;
using Microsoft.Azure.Cosmos.Table;

namespace AzureTableStorageCacheNetStandard.Model
{
    public class CachedItem : TableEntity
    {
        //public CachedItem() { }

        public CachedItem(string partitionKey, string rowKey, byte[] data = null) : base(partitionKey, rowKey)
        {
            Data = data;
        }

        public byte[] Data { get; set; }
        public TimeSpan? SlidingExperiation { get; set; }
        public DateTimeOffset? AbsolutExperiation { get; set; }
        public DateTimeOffset? LastAccessTime { get; set; }
    }
}