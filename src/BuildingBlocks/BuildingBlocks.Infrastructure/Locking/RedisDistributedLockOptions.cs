namespace BuildingBlocks.Infrastructure.Locking;

public sealed class RedisDistributedLockOptions
{
    public string KeyPrefix { get; set; } = "emm:locks:"; // change it to something more specific to your application to avoid key collisions if you share Redis with other applications
}
