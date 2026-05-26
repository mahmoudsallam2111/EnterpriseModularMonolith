using BuildingBlocks.FileStorage.Abstractions;
using BuildingBlocks.FileStorage.Hashing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
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

        endpoints.MapGroup(prefix)
            .WithTags("Files")
            .RequireAuthorization()
            .MapPost("/{container}/upload", UploadFormAsync)
            .WithName("UploadFileFromSwagger")
            .Produces<FileStorageResult>()
            .DisableAntiforgery();

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

    private static async Task<IResult> UploadFormAsync(
        string container,
        HttpRequest request,
        IFileStore store,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
            return Results.BadRequest(new { error = "Expected multipart/form-data." });

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file") ?? (form.Files.Count > 0 ? form.Files[0] : null);
        if (file is null)
            return Results.BadRequest(new { error = "File is required." });

        if (file.Length == 0)
            return Results.BadRequest(new { error = "File is empty." });

        var objectKey = form.TryGetValue("objectKey", out var values)
            ? values.FirstOrDefault()
            : null;

        var originalFileName = Path.GetFileName(file.FileName);
        var targetKey = ResolveTargetKey(objectKey, originalFileName);

        if (string.IsNullOrWhiteSpace(targetKey))
            return Results.BadRequest(new { error = "Object key is required when the uploaded file has no filename." });

        try
        {
            await using var stream = file.OpenReadStream();
            var metadata = new FileMetadata(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                originalFileName);

            var result = await store.UploadAsync(container, targetKey, stream, metadata, cancellationToken);
            return Results.Ok(result);
        }
        catch (FileStorageException ex)
        {
            return Results.BadRequest(new { error = ex.Code, message = ex.Message });
        }
        catch (IOException ex)
        {
            return Results.Problem(
                title: "File upload failed.",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Problem(
                title: "File upload path is not writable.",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static bool ShouldUseOriginalFileName(string? objectKey) =>
        string.IsNullOrWhiteSpace(objectKey) ||
        string.Equals(objectKey, "string", StringComparison.OrdinalIgnoreCase);

    private static string ResolveTargetKey(string? objectKey, string originalFileName)
    {
        if (ShouldUseOriginalFileName(objectKey))
            return originalFileName;

        var targetKey = objectKey!.Replace('\\', '/');
        if (targetKey.EndsWith('/'))
            return targetKey + originalFileName;

        var originalExtension = Path.GetExtension(originalFileName);
        return string.IsNullOrWhiteSpace(originalExtension) || !string.IsNullOrWhiteSpace(Path.GetExtension(targetKey))
            ? targetKey
            : targetKey + originalExtension;
    }

    private static Microsoft.Net.Http.Headers.EntityTagHeaderValue? EntityTagFromETag(string? eTag) =>
        string.IsNullOrEmpty(eTag) ? null : new Microsoft.Net.Http.Headers.EntityTagHeaderValue(eTag);
}
