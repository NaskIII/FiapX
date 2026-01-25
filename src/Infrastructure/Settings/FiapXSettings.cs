namespace FiapX.Infrastructure.Settings
{
    public class FiapXSettings
    {
        public ServiceBusSettings ServiceBus { get; set; } = new();
        public CosmosSettings Cosmos { get; set; } = new();
        public StorageSettings Storage { get; set; } = new();
    }

    public class ServiceBusSettings
    {
        public string Connection { get; set; } = string.Empty;
        public string QueueName { get; set; } = string.Empty;
    }

    public class CosmosSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;
    }

    public class StorageSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string ContainerRaw { get; set; } = string.Empty;
        public string ContainerZip { get; set; } = string.Empty;
    }
}
