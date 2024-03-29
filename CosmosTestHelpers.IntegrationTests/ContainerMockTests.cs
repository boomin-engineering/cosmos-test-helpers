﻿using System.Diagnostics.CodeAnalysis;
using System.Text;
using CosmosTestHelpers.IntegrationTests.TestModels;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Xunit;

namespace CosmosTestHelpers.IntegrationTests
{
    public class ContainerMockTests
    {
        [Theory]
        [InlineData("/")]
        [InlineData(@"\\")]
        [InlineData("?")]
        [InlineData("#")]
        public async Task When_inserting_an_item_with_a_forward_slash_in_the_id__Then_an_error_occurs(string invalidChars)
        {
            var containerMock = new ContainerMock();

            var model = new TestModel { Id = "url" + invalidChars + "WillBreak", PartitionKey = "pk" };
            Func<Task> act = () => containerMock.CreateItemAsync(model, new PartitionKey(model.PartitionKey));
            await act.Should().ThrowAsync<InvalidOperationException>();
        }        
        
        [Theory]
        [InlineData("/")]
        [InlineData(@"\")]
        [InlineData("?")]
        [InlineData("#")]
        [SuppressMessage("Reliability", "CA2000", Justification = "Calling AsyncDispose")]
        public async Task When_inserting_an_item_by_stream_with_a_forward_slash_in_the_id__Then_an_error_occurs(string invalidChars)
        {
            var containerMock = new ContainerMock();

            var model = new TestModel { Id = "url" + invalidChars + "WillBreak", PartitionKey = "pk" };
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model));
            await using var ms = new MemoryStream(bytes);
            Func<Task> act = () => containerMock.CreateItemStreamAsync(ms, new PartitionKey(model.PartitionKey));
            await act.Should().ThrowAsync<InvalidOperationException>();
        }
        
        [Theory]
        [InlineData("/")]
        [InlineData(@"\")]
        [InlineData("?")]
        [InlineData("#")]
        public async Task When_upserting_an_item__with_a_forward_slash_in_the_id__Then_an_error_occurs(string invalidChars)
        {
            var containerMock = new ContainerMock();

            var model = new TestModel { Id = "url" + invalidChars + "WillBreak", PartitionKey = "pk" };
            Func<Task> act = () => containerMock.UpsertItemAsync(model, new PartitionKey(model.PartitionKey));
            await act.Should().ThrowAsync<InvalidOperationException>();
        }
        
        [Theory]
        [InlineData("/")]
        [InlineData(@"\\")]
        [InlineData("?")]
        [InlineData("#")]
        [SuppressMessage("Reliability", "CA2000", Justification = "Calling AsyncDispose")]
        public async Task When_upserting_an_item_by_stream__with_a_forward_slash_in_the_id__Then_an_error_occurs(string invalidChars)
        {
            var containerMock = new ContainerMock();

            var model = new TestModel { Id = "url" + invalidChars + "WillBreak", PartitionKey = "pk"};
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model));
            await using var ms = new MemoryStream(bytes);
            Func<Task> act = () => containerMock.UpsertItemStreamAsync(ms, new PartitionKey(model.PartitionKey));
            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}