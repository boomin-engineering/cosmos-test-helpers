﻿using System.Diagnostics.CodeAnalysis;
using System.Text;
using CosmosTestHelpers.IntegrationTests.TestModels;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Xunit;

namespace CosmosTestHelpers.IntegrationTests
{
    [Collection("Integration Tests")]
    public sealed class CosmosCreateStreamTests : IAsyncLifetime, IDisposable
    {
        private TestCosmos _testCosmos;

        [Fact]
        [SuppressMessage("", "CA2000", Justification = "AsyncDispose....")]
        public async Task CreateStreamNonExistingIsEquivalent()
        {
            var bytes = GetItemBytes(
                new TestModel
                {
                    Id = "RECORD2",
                    Name = "Bob Bobertson",
                    EnumValue = TestEnum.Value1,
                    PartitionKey = "test-cst1"
                });

            await using var ms = new MemoryStream(bytes);
            var (real, test) = await _testCosmos.WhenCreatingStream(ms, new PartitionKey("test-cst1"));

            real.StatusCode.Should().Be(test.StatusCode);
        }

        [Fact]
        [SuppressMessage("", "CA2000", Justification = "AsyncDispose....")]
        public async Task CreateStreamExistingIsEquivalent()
        {
            await _testCosmos.GivenAnExistingItem(new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value2,
                PartitionKey = "test-cst2"
            });

            var bytes = GetItemBytes(
                new TestModel
                {
                    Id = "RECORD1",
                    Name = "Bob Bobertson",
                    EnumValue = TestEnum.Value1,
                    PartitionKey = "test-cst2"
                });

            await using var ms = new MemoryStream(bytes);
            var (real, test) = await _testCosmos.WhenCreatingStream(ms, new PartitionKey("test-cst2"));

            real.StatusCode.Should().Be(test.StatusCode);
        }

        [Fact]
        [SuppressMessage("", "CA2000", Justification = "AsyncDispose....")]
        public async Task CreateStreamWithEmptyIdIsEquivalent()
        {
            var bytes = GetItemBytes(
                new TestModel
                {
                    Id = string.Empty,
                    Name = "Bob Bobertson",
                    EnumValue = TestEnum.Value1,
                    PartitionKey = "test-cst2"
                });

            await using var ms = new MemoryStream(bytes);
            var (real, test) = await _testCosmos.WhenCreatingStream(ms, new PartitionKey("test-cst2"));

            real.StatusCode.Should().Be(test.StatusCode);
        }
        
        [Fact]
        [SuppressMessage("", "CA2000", Justification = "AsyncDispose....")]
        public async Task CreateStreamUniqueKeyViolationIsEquivalent()
        {
            await _testCosmos.GivenAnExistingItem(new TestModel
            {
                Id = "RECORD1",
                Name = "Bob Bobertson",
                EnumValue = TestEnum.Value2,
                PartitionKey = "test-cst3"
            });

            var bytes = GetItemBytes(
                new TestModel
                {
                    Id = "RECORD2",
                    Name = "Bob Bobertson",
                    EnumValue = TestEnum.Value1,
                    PartitionKey = "test-cst3"
                });

            await using var ms = new MemoryStream(bytes);
            var (real, test) = await _testCosmos.WhenCreatingStream(ms, new PartitionKey("test-cst3"));
            
            real.StatusCode.Should().Be(test.StatusCode);
        }
        
        public Task InitializeAsync()
        {
            _testCosmos = new TestCosmos();
            return _testCosmos.SetupAsync("/partitionKey", new UniqueKeyPolicy { UniqueKeys = { new UniqueKey { Paths = {"/Name"}}}});
        }

        public async Task DisposeAsync()
        {
            await _testCosmos.CleanupAsync();
        }

        public void Dispose()
        {
            _testCosmos?.Dispose();
        }

        private static byte[] GetItemBytes(TestModel model)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model));
        }
    }
}