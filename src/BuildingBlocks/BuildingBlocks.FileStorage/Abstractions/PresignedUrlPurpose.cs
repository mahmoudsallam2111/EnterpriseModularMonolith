namespace BuildingBlocks.FileStorage.Abstractions;

public enum PresignedUrlPurpose
{
    /// <summary>Time-limited GET access to the stored file.</summary>
    Read = 0,
    /// <summary>Time-limited PUT access used by browsers to upload directly to the store.</summary>
    Write = 1
}
