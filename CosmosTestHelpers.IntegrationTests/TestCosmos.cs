﻿using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using CosmosTestHelpers.IntegrationTests.TestModels;
using Microsoft.Azure.Cosmos;

namespace CosmosTestHelpers.IntegrationTests
{
    public sealed class TestCosmos : IDisposable
    {
        private readonly CosmosClient _client;

        private Container _testContainer;

        private Container _container;

        public TestCosmos()
        {
            var testConnectionString = Environment.GetEnvironmentVariable("TEST_COSMOS_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(testConnectionString))
            {
                throw new Exception("TEST_COSMOS_CONNECTION_STRING environment variable must be set");
            }
            _client = new CosmosClient(testConnectionString);
        }

        public async Task SetupAsync(string partitionKeyPath, UniqueKeyPolicy uniqueKeyPolicy = null)
        {
            var response = await CreateCosmosContainer(partitionKeyPath, uniqueKeyPolicy);
            _container = response.Container;
            _testContainer = new ContainerMock(partitionKeyPath, uniqueKeyPolicy);
        }

        public async Task<(CosmosException realException, CosmosException testException)> SetupAsyncProducesExceptions(string partitionKeyPath, UniqueKeyPolicy uniqueKeyPolicy = null)
        {
            CosmosException realException = null, testException = null;

            try
            {
                var response = await CreateCosmosContainer(partitionKeyPath, uniqueKeyPolicy);
                _container = response.Container;
            }
            catch (CosmosException ex)
            {
                realException = ex;
            }

            try
            {
                _testContainer = new ContainerMock(partitionKeyPath, uniqueKeyPolicy);
            }
            catch (CosmosException ex)
            {
                testException = ex;
            }

            return (realException, testException);
        }

        public async Task<(string testETag, string realETag)> GivenAnExistingItem<T>(T item)
        {
            var realItem = await _container.UpsertItemAsync(item);
            var testItem = await _testContainer.UpsertItemAsync(item);

            return (testItem.ETag, realItem.ETag);
        }

        [SuppressMessage("", "CA1031", Justification = "Need to catch exceptions")]
        public async Task<(IList<T> realResults, IList<T> testResults)> WhenExecutingAQuery<T>(string partitionKey, Func<IQueryable<T>, IQueryable<T>> query = null)
        {
            Exception realException = null, testException = null;

            IList<T> realQuery = null, inMemoryQuery = null;

            try
            {
                if (query != null)
                {
                    realQuery = await _container.QueryAsync<T>(partitionKey, query).ToListAsync();
                }
                else
                {
                    realQuery = await _container.QueryAsync<T>(partitionKey).ToListAsync();
                }
            }
            catch (Exception ex)
            {
                realException = ex;
            }

            try
            {
                if (query != null)
                {
                    inMemoryQuery = await _testContainer.QueryAsync<T>(partitionKey, query).ToListAsync();
                }
                else
                {
                    inMemoryQuery = await _testContainer.QueryAsync<T>(partitionKey).ToListAsync();
                }
            }
            catch (Exception ex)
            {
                testException = ex;
            }

            if (realException != null || testException != null)
            {
                throw new CosmosEquivalencyException(realException, testException, realQuery, inMemoryQuery);
            }

            return (realQuery, inMemoryQuery);
        }

        [SuppressMessage("", "CA1031", Justification = "Need to catch exceptions")]
        public async Task<(int? realResults, int? testResults)> WhenCountingAQuery<T>(string partitionKey, Func<IQueryable<T>, IQueryable<T>> query = null)
        {
            Exception realException = null, testException = null;

            int? realQuery = null, inMemoryQuery = null;

            try
            {
                if (query != null)
                {
                    realQuery = await _container.CountAsync(partitionKey, query);
                }
                else
                {
                    realQuery = await _container.CountAsync(partitionKey);
                }
            }
            catch (Exception ex)
            {
                realException = ex;
            }

            try
            {
                if (query != null)
                {
                    inMemoryQuery = await _testContainer.CountAsync(partitionKey, query);
                }
                else
                {
                    inMemoryQuery = await _testContainer.CountAsync(partitionKey);
                }
            }
            catch (Exception ex)
            {
                testException = ex;
            }

            if (realException != null || testException != null)
            {
                throw new CosmosEquivalencyException(realException, testException, realQuery, inMemoryQuery);
            }

            return (realQuery, inMemoryQuery);
        }

        [Obsolete("Testing old query method, until the CreateItemLinqQueryable method is removed")]
        public (IQueryable<T> realResults, IQueryable<T> testResults) WhenExecutingAQuery<T>(Func<IQueryable<T>, IQueryable<T>> query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var realQueryable = _container.GetItemLinqQueryable<T>(true);
            var realQuery = query(realQueryable);

            var inMemoryQueryable = _testContainer.GetItemLinqQueryable<T>();
            var inMemoryQuery = query(inMemoryQueryable);

            return (realQuery, inMemoryQuery);
        }

        [Obsolete("Testing old query method, until the CreateItemLinqQueryable method is removed")]
        [SuppressMessage(null, "CA1031", Justification = "Need to know if any exception occurs")]
        public (TResult realResults, Exception realException, TResult testResults, Exception testException) WhenExecutingAQuery<TIn, TResult>(Func<IQueryable<TIn>, TResult> query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            Exception realException = null, testException = null;
            TResult realQuery = default, inMemoryQuery = default;

            try
            {
                var realQueryable = _container.GetItemLinqQueryable<TIn>(true);
                realQuery = query(realQueryable);
            }
            catch (Exception ex)
            {
                realException = ex;
            }

            try
            {
                var inMemoryQueryable = _testContainer.GetItemLinqQueryable<TIn>();
                inMemoryQuery = query(inMemoryQueryable);
            }
            catch (Exception ex)
            {
                testException = ex;
            }

            return (realQuery, realException, inMemoryQuery, testException);
        }

        public async Task<(ItemResponse<TestModel> realResult, ItemResponse<TestModel> testResult)> WhenCreating(
            TestModel testModel,
            PartitionKey partitionKey,
            ItemRequestOptions testRequestOptions = null,
            ItemRequestOptions realRequestOptions = null)
        {
            var realResult = await _container.CreateItemAsync(testModel, partitionKey, realRequestOptions);
            var testResult = await _testContainer.CreateItemAsync(testModel, partitionKey, testRequestOptions);
            return (realResult, testResult);
        }

        public async Task<(ItemResponse<TripleUniqueKeyModel> realResult, ItemResponse<TripleUniqueKeyModel> testResult)> WhenCreatingTripleKeyModel(
            TripleUniqueKeyModel testModel,
            ItemRequestOptions testRequestOptions = null,
            ItemRequestOptions realRequestOptions = null)
        {
            var realResult = await _container.CreateItemAsync(testModel, requestOptions: realRequestOptions);
            var testResult = await _testContainer.CreateItemAsync(testModel, requestOptions: testRequestOptions);
            return (realResult, testResult);
        }

        public async Task<(CosmosException realException, CosmosException testException)> WhenCreatingProducesException(
            TestModel testModel,
            PartitionKey partitionKey,
            ItemRequestOptions testRequestOptions = null,
            ItemRequestOptions realRequestOptions = null)
        {
            CosmosException real = null;
            CosmosException test = null;

            try
            {
                await _container.CreateItemAsync(testModel, partitionKey, realRequestOptions);
            }
            catch (CosmosException exc)
            {
                real = exc;
            }

            try
            {
                await _testContainer.CreateItemAsync(testModel, partitionKey, testRequestOptions);
            }
            catch (CosmosException exc)
            {
                test = exc;
            }

            return (real, test);
        }

        public async Task<(CosmosException realException, CosmosException testException)> WhenCreatingTripleKeyModelProducesException(
            TripleUniqueKeyModel testModel,
            ItemRequestOptions testRequestOptions = null,
            ItemRequestOptions realRequestOptions = null)
        {
            CosmosException real = null;
            CosmosException test = null;

            try
            {
                await _container.CreateItemAsync(testModel, requestOptions: realRequestOptions);
            }
            catch (CosmosException exc)
            {
                real = exc;
            }

            try
            {
                await _testContainer.CreateItemAsync(testModel, requestOptions: testRequestOptions);
            }
            catch (CosmosException exc)
            {
                test = exc;
            }

            return (real, test);
        }

        public async Task<(ItemResponse<TestModel> realResult, ItemResponse<TestModel> testResult)> WhenUpserting(
            TestModel testModel,
            ItemRequestOptions testRequestOptions = null,
            ItemRequestOptions realRequestOptions = null)
        {
            var realResult = await _container.UpsertItemAsync(testModel, requestOptions: realRequestOptions);
            var testResult = await _testContainer.UpsertItemAsync(testModel, requestOptions: testRequestOptions);
            return (realResult, testResult);
        }

        public async Task<(ItemResponse<TripleUniqueKeyModel> realResult, ItemResponse<TripleUniqueKeyModel> testResult)> WhenUpsertingTripleKeyModel(
            TripleUniqueKeyModel testModel,
            ItemRequestOptions testRequestOptions = null,
            ItemRequestOptions realRequestOptions = null)
        {
            var realResult = await _container.UpsertItemAsync(testModel, requestOptions: realRequestOptions);
            var testResult = await _testContainer.UpsertItemAsync(testModel, requestOptions: testRequestOptions);
            return (realResult, testResult);
        }

        public async Task<(ItemResponse<TestModel> realResult, ItemResponse<TestModel> testResult)> WhenReplacing(
            TestModel testModel,
            string id,
            ItemRequestOptions testRequestOptions = null,
            ItemRequestOptions realRequestOptions = null)
        {
            var realResult = await _container.ReplaceItemAsync(testModel, id, requestOptions: realRequestOptions);
            var testResult = await _testContainer.ReplaceItemAsync(testModel, id, requestOptions: testRequestOptions);
            return (realResult, testResult);
        }

        public async Task<(ItemResponse<TestModel> realResult, ItemResponse<TestModel> testResult)> WhenDeleting(
            string id,
            PartitionKey partitionKey,
            ItemRequestOptions testRequestOptions = null,
            ItemRequestOptions realRequestOptions = null)
        {
            var realResult = await _container.DeleteItemAsync<TestModel>(id, partitionKey, realRequestOptions);
            var testResult = await _testContainer.DeleteItemAsync<TestModel>(id, partitionKey, testRequestOptions);
            return (realResult, testResult);
        }

        public async Task<(ResponseMessage realResult, ResponseMessage testResult)> WhenUpsertingStream(
            Stream stream,
            PartitionKey partitionKey,
            ItemRequestOptions testRequestOptions = null,
            ItemRequestOptions realRequestOptions = null)
        {
            var realResult = await _container.UpsertItemStreamAsync(stream, partitionKey, realRequestOptions);
            var testResult = await _testContainer.UpsertItemStreamAsync(stream, partitionKey, testRequestOptions);
            return (realResult, testResult);
        }

        public async Task<(ResponseMessage realResult, ResponseMessage testResult)> WhenCreatingStream(MemoryStream stream, PartitionKey partitionKey)
        {
            var realResult = await _container.CreateItemStreamAsync(stream, partitionKey);
            var testResult = await _testContainer.CreateItemStreamAsync(stream, partitionKey);
            return (realResult, testResult);
        }

        public async Task<(CosmosException realException, CosmosException testException)> WhenUpsertingProducesException(
            TestModel testModel,
            ItemRequestOptions testRequestOptions = null,
            ItemRequestOptions realRequestOptions = null)
        {
            CosmosException real = null;
            CosmosException test = null;

            try
            {
                await _container.UpsertItemAsync(testModel, requestOptions: realRequestOptions);
            }
            catch (CosmosException exc)
            {
                real = exc;
            }

            try
            {
                await _testContainer.UpsertItemAsync(testModel, requestOptions: testRequestOptions);
            }
            catch (CosmosException exc)
            {
                test = exc;
            }

            return (real, test);
        }

        public async Task<(CosmosException realException, CosmosException testException)> WhenUpsertingTripleKeyModelProducesException(
            TripleUniqueKeyModel testModel,
            ItemRequestOptions testRequestOptions = null,
            ItemRequestOptions realRequestOptions = null)
        {
            CosmosException real = null;
            CosmosException test = null;

            try
            {
                await _container.UpsertItemAsync(testModel, requestOptions: realRequestOptions);
            }
            catch (CosmosException exc)
            {
                real = exc;
            }

            try
            {
                await _testContainer.UpsertItemAsync(testModel, requestOptions: testRequestOptions);
            }
            catch (CosmosException exc)
            {
                test = exc;
            }

            return (real, test);
        }

        public async Task<(CosmosException realException, CosmosException testException)> WhenReplacingProducesException(
            TestModel testModel,
            string id,
            ItemRequestOptions testRequestOptions = null,
            ItemRequestOptions realRequestOptions = null)
        {
            CosmosException real = null;
            CosmosException test = null;

            try
            {
                await _container.ReplaceItemAsync(testModel, id, requestOptions: realRequestOptions);
            }
            catch (CosmosException exc)
            {
                real = exc;
            }

            try
            {
                await _testContainer.ReplaceItemAsync(testModel, id, requestOptions: testRequestOptions);
            }
            catch (CosmosException exc)
            {
                test = exc;
            }

            return (real, test);
        }

        public async Task<(CosmosException realException, CosmosException testException)> WhenDeletingProducesException(
            string id,
            PartitionKey partitionKey,
            ItemRequestOptions testRequestOptions = null,
            ItemRequestOptions realRequestOptions = null)
        {
            CosmosException real = null;
            CosmosException test = null;

            try
            {
                await _container.DeleteItemAsync<TestModel>(id, partitionKey, realRequestOptions);
            }
            catch (CosmosException exc)
            {
                real = exc;
            }

            try
            {
                await _testContainer.DeleteItemAsync<TestModel>(id, partitionKey, testRequestOptions);
            }
            catch (CosmosException exc)
            {
                test = exc;
            }

            return (real, test);
        }

        [SuppressMessage("", "CA1031", Justification = "I want to catch all exceptions.")]
        public async Task<(Exception realException, Exception testException)> WhenReadItemProducesException(string id)
        {
            Exception real = null;
            Exception test = null;

            try
            {
                await _container.ReadItemAsync<TestModel>(id, PartitionKey.None);
            }
            catch (Exception exc)
            {
                real = exc;
            }

            try
            {
                await _testContainer.ReadItemAsync<TestModel>(id, PartitionKey.None);
            }
            catch (Exception exc)
            {
                test = exc;
            }

            return (real, test);
        }
        
        [SuppressMessage("", "CA1031", Justification = "I want to catch all exceptions.")]
        public async Task<(Exception realException, Exception testException)> WhenReadItemStreamProducesException(string id)
        {
            Exception real = null;
            Exception test = null;

            try
            {
                await _container.ReadItemStreamAsync(id, PartitionKey.None);
            }
            catch (Exception exc)
            {
                real = exc;
            }

            try
            {
                await _testContainer.ReadItemStreamAsync(id, PartitionKey.None);
            }
            catch (Exception exc)
            {
                test = exc;
            }

            return (real, test);
        }
        
        public async Task<(ResponseMessage realException, ResponseMessage testException)> WhenReadItemStream(string id)
        {
            var real = await _container.ReadItemStreamAsync(id, PartitionKey.None);
            var test = await _testContainer.ReadItemStreamAsync(id, PartitionKey.None);

            return (real, test);
        }
        
        public async Task CleanupAsync()
        {
            if (_container == null)
            {
                return;
            }
            
            await _container.DeleteContainerAsync();
        }

        public void Dispose()
        {
            _client?.Dispose();
        }

        private async Task<ContainerResponse> CreateCosmosContainer(string partitionKeyPath, UniqueKeyPolicy uniqueKeyPolicy)
        {
            var dbName = typeof(TestCosmos).Assembly.GetName().Name;
            var database = (await _client.CreateDatabaseIfNotExistsAsync(dbName, throughput: null)).Database;

            var iterator = database.GetContainerQueryIterator<ContainerProperties>();
            do
            {
                foreach (var container in await iterator.ReadNextAsync())
                {
                    if (container.LastModified < DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(30)))
                    {
                        try
                        {
                            await _client.GetContainer(dbName, container.Id).DeleteContainerAsync();
                        }
                        catch (CosmosException cex) when (cex.StatusCode == HttpStatusCode.NotFound)
                        {
                            // Another test setup already did the delete in the time it took us to get it, so we don't need to do anything more
                        }
                    }
                }
            }
            while (iterator.HasMoreResults);

            var containerProperties = new ContainerProperties
            {
                Id = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                PartitionKeyPath = partitionKeyPath,
            };

            if (uniqueKeyPolicy != null)
            {
                containerProperties.UniqueKeyPolicy = uniqueKeyPolicy;
            }

            var response = await database.CreateContainerAsync(containerProperties);
            return response;
        }
    }
}