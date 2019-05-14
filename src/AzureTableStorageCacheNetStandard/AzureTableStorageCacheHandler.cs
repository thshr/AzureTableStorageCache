using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AzureTableStorageCacheNetStandard.Model;
using Microsoft.Azure.Cosmos.Table;
//using Microsoft.Azure.Storage;
//using Microsoft.Azure.Storage.Auth;
using Microsoft.Extensions.Caching.Distributed;
using static System.Console;

namespace AzureTableStorageCacheNetStandard
{
    public class AzureTableStorageCacheHandler : IDistributedCache
    {
        private readonly string tableName;
        private readonly string partitionKey;
        private readonly string accountKey;
        private readonly string accountName;
        private readonly string connectionString;
        private CloudTableClient client;
        private CloudTable azuretable;

        private AzureTableStorageCacheHandler(string tableName, string partitionKey)
        {
            this.tableName = tableName ?? throw new ArgumentNullException($"{nameof(tableName)} cannot be null or empty");
            this.partitionKey = partitionKey ?? throw new ArgumentNullException($"{nameof(partitionKey)} cannot be null or empty");
        }

        public AzureTableStorageCacheHandler(string accountName, string accountKey, string tableName, string partitionKey) : this(tableName, partitionKey)
        {
            this.accountName = accountName ?? throw new ArgumentNullException($"{nameof(accountName)} cannot be null or empty");
            this.accountKey = accountKey ?? throw new ArgumentNullException($"{nameof(accountKey)} cannot be null or empty");
            Connect();
        }


        public AzureTableStorageCacheHandler(string connectionString, string tableName, string partitionKey) : this(tableName, partitionKey)
        {
            this.connectionString = connectionString ?? throw new ArgumentNullException($"{nameof(connectionString)} cannot be null or empty");;
            Connect();
        }

        public void Connect() => ConnectAsync().Wait();

        public async Task ConnectAsync()
        {
            if (client == null)
            {
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    var creds = new StorageCredentials(accountName, accountKey);
                    client = new CloudStorageAccount(creds, true).CreateCloudTableClient();
                }
                else
                    client = CloudStorageAccount.Parse(connectionString).CreateCloudTableClient();
            }
            if (azuretable == null)
            {
                azuretable = client.GetTableReference(tableName);
                await azuretable.CreateIfNotExistsAsync();
            }
        }

        public byte[] Get(string key) => GetAsync(key).Result;

        public async Task<byte[]> GetAsync(string key, CancellationToken token = new CancellationToken())
        {
            var cachedItem = await RetrieveAsync(key, token);
            if (cachedItem?.Data == null)
                return null;

            if (ShouldDelete(cachedItem)) {
                await RemoveAsync(cachedItem, token);
                return null;
            }
            return cachedItem.Data;
        }

        public void Refresh(string key) => RefreshAsync(key).Wait();

        public async Task RefreshAsync(string key, CancellationToken token = new CancellationToken())
        {
            var data = await RetrieveAsync(key, token);
            if (data != null && ShouldDelete(data))
                await RemoveAsync(data, token);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => SetAsync(key, value, options).Wait();

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = new CancellationToken())
        {
            DateTimeOffset? absoluteExpiration = null;
            var currentTime = DateTimeOffset.UtcNow;

            if (options.AbsoluteExpirationRelativeToNow.HasValue)
                absoluteExpiration = currentTime.Add(options.AbsoluteExpirationRelativeToNow.Value);
            else if (options.AbsoluteExpiration.HasValue)
            {
                if (options.AbsoluteExpiration.Value <= currentTime)
                    throw new ArgumentOutOfRangeException(nameof(options.AbsoluteExpiration), options.AbsoluteExpiration.Value, "The absolute expiration value must be in the future.");
                
                absoluteExpiration = options.AbsoluteExpiration;
            }

            var item = new CachedItem(partitionKey, key, value)
                       {
                           LastAccessTime = currentTime,
                           AbsoluteExpiration = absoluteExpiration,
                           SlidingExpiration = options.SlidingExpiration
                       };

            var op = TableOperation.InsertOrReplace(item);
            return DoTableOperation(op, token);
        }

        public void Remove(string key) => RemoveAsync(key).Wait();

        public async Task RemoveAsync(string key, CancellationToken token = new CancellationToken())
        {
            var cachedItem = await RetrieveAsync(key, token);
            var op = TableOperation.Delete(cachedItem);
            await DoTableOperation(op, token);
        }
        
        public async Task RemoveAsync(CachedItem delCachedItem, CancellationToken token = new CancellationToken())
        {
            var op = TableOperation.Delete(delCachedItem);
            await DoTableOperation(op, token);
        }

        private async Task<CachedItem> RetrieveAsync(string key, CancellationToken token)
        {
            var op = TableOperation.Retrieve<CachedItem>(partitionKey, key);
            var result = await DoTableOperation(op, token);
            var data = result?.Result as CachedItem;

            if (data == null)
                return null;

            data.LastAccessTime = DateTime.UtcNow;
            op = TableOperation.Merge(data);
            await DoTableOperation(op, token);
            return data;
        }

        private bool ShouldDelete(CachedItem data)
        {
            var currentTime = DateTimeOffset.UtcNow;
            if (data.AbsoluteExpiration != null && data.AbsoluteExpiration.Value <= currentTime)
                return true;
            if (data.SlidingExpiration.HasValue && data.LastAccessTime.HasValue && data.LastAccessTime.Value.Add(data.SlidingExpiration.Value) < currentTime)
                return true;

            return false;
        }

        private async Task<TableResult> DoTableOperation(TableOperation operation, CancellationToken token)
        {
            try
            {
                var result = await azuretable.ExecuteAsync(operation, token);
                WriteLine($"Operation {operation.OperationType}: {(HttpStatusCode)result.HttpStatusCode}\tReq Charge: {result.RequestCharge}");
                return result;
            }
            catch (StorageException ex)
            {
                WriteLine($"Storage exception: {ex.Message}");
                throw;
            }
        }
    }
}