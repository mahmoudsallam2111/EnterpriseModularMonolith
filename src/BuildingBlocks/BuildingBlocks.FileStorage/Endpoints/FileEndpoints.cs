using BuildingBlocks.FileStorage.Abstractions;
using BuildingBlocks.FileStorage.Hashing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.FileStorage.Endpoints;

/// <summary>
/// HTTP endpoints that back the local-file-store presigned URL flow:
///   GET  /api/v1/files/{container}/{objectKey}        → time-limited download
///   PUT  /api/v1/files/{container}/{objectKey}        → time-limited upload (browser-direct)
/// Both gate on HMAC signature verification — no auth required because the URL is the
/// capability. With S3 / Azure Blob the equivalent endpoints come from the cloud provider.
/// </summary>
public static class FileEndpoints
{
    public static IEndpointRouteBuilder MapFileStorageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<FileStorageOptions>>().Value;
        var prefix = options.Local.RoutePrefix.TrimEnd('/');

        var group = endpoints.MapGroup(prefix).WithTags("Files").AllowAnonymous();

        group.MapGet("/{container}/{**objectKey}", DownloadAsync).WithName("DownloadFile");
        group.MapPut("/{container}/{**objectKey}", UploadAsync).WithName("UploadFile");

        return endpoints;
    }

    private static async Task<IResult> DownloadAsync(
        string container, string objectKey,
        IFileStore store, PresignedUrlSigner signer,
        long expires, int purpose, string sig,
        CancellationToken cancellationToken)
    {
        if (purpose != (int)PresignedUrlPurpose.Read || !signer.Verify(PresignedUrlPurpose.Read, container, objectKey, expires, sig))
            return Results.StatusCode(StatusCodes.Status403Forbidden);

        var info = await store.GetInfoAsync(container, objectKey, cancellationToken);
        if (info is null) return Results.NotFound();

        var stream = await store.DownloadAsync(container, objectKey, cancellationToken);
        if (stream is null) return Results.NotFound();
        return Results.Stream(stream, info.ContentType, info.ObjectKey, info.LastModifiedUtc, EntityTagFromETag(info.ETag));
    }

    private static async Task<IResult> UploadAsync(
        string container, string objectKey,
        HttpRequest request,
        IFileStore store, PresignedUrlSigner signer,
        long expires, int purpose, string sig,
        CancellationToken cancellationToken)
    {
        if (purpose != (int)PresignedUrlPurpose.Write || !signer.Verify(PresignedUrlPurpose.Write, container, objectKey, expires, sig))
            return Results.StatusCode(StatusCodes.Status403Forbidden);

        var contentType = request.ContentType ?? "application/octet-stream";
        var originalFileName = request.Headers.TryGetValue("X-Original-Filename", out var v) ? v.ToString() : null;
        var meta = new FileMetadata(contentType, originalFileName);

        var result = await store.UploadAsync(container, objectKey, request.Body, meta, cancellationToken);
        return Results.Ok(result);
    }

    private static Microsoft.Net.Http.Headers.EntityTagHeaderValue? EntityTagFromETag(string? eTag) =>
        string.IsNullOrEmpty(eTag) ? null : new Microsoft.Net.Http.Headers.EntityTagHeaderValue(eTag);
}
