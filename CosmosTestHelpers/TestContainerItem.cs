namespace CosmosTestHelpers
{
    public class TestContainerItem<T>
    {
        public string PartitionKey { get; }
        public string Id { get; }
        public T Document { get; }

        public TestContainerItem(string partitionKey, string id, T document)
        {
            PartitionKey = partitionKey;
            Id = id;
            Document = document;
        }
    }
}