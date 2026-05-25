namespace BuildingBlocks.Infrastructure.Caching;

public sealed class RedisCacheOptions
{
    public string KeyPrefix { get; set; } = "emm:cache:";  // change it to something more specific to your application to avoid key collisions if you share Redis with other applications
}
