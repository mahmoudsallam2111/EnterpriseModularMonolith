namespace BuildingBlocks.FileStorage.Abstractions;

/// <summary>
/// Wire-format ticket that a client uses to upload directly to the store without
/// streaming the body through the API. The local provider serves a signed PUT
/// against /api/v1/files; cloud providers return their native presigned URLs.
/// </summary>
public sealed record PresignedUploadTicket(
    Uri Url,
    string Method,
    IReadOnlyDictionary<string, string> Headers,
    DateTimeOffset ExpiresAtUtc);
