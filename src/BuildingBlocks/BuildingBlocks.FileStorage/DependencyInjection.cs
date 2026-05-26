using BuildingBlocks.FileStorage.Abstractions;
using BuildingBlocks.FileStorage.AzureBlob;
using BuildingBlocks.FileStorage.Hashing;
using BuildingBlocks.FileStorage.Local;
using BuildingBlocks.FileStorage.Pipeline;
using BuildingBlocks.FileStorage.S3;
using BuildingBlocks.FileStorage.Scanning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.FileStorage;

public static class DependencyInjection
{
    /// <summary>
    /// Wires the file storage subsystem. Provider is chosen by the
    /// <c>FileStorage:Provider</c> config value (Local | S3 | AzureBlob).
    /// Validators are registered as IEnumerable&lt;IFileValidator&gt; so callers can
    /// add their own with TryAddEnumerable.
    /// </summary>
    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FileStorageOptions>()
            .Bind(configuration.GetSection(FileStorageOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<PresignedUrlSigner>(sp =>
        {
            var opt = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<FileStorageOptions>>().Value;
            return new PresignedUrlSigner(string.IsNullOrWhiteSpace(opt.SigningKey)
                ? "dev-only-signing-key-please-replace-in-production-32+"
                : opt.SigningKey);
        });

        // Validators run in registration order.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IFileValidator, MaxSizeValidator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IFileValidator, ContentTypeAllowListValidator>());

        // Scanner: NoOp default; replaceable.
        services.TryAddSingleton<IFileScanner, NoOpFileScanner>();

        var provider = configuration.GetSection(FileStorageOptions.SectionName)["Provider"] ?? "Local";
        switch (provider)
        {
            case "S3":
                services.AddSingleton<S3FileStore>();
                break;
            case "AzureBlob":
                services.AddSingleton<AzureBlobFileStore>();
                break;
            default:
                services.AddSingleton<LocalFileStore>();
                break;
        }

        services.AddSingleton<IFileStore>(sp =>
        {
            IFileStore inner = provider switch
            {
                "S3" => sp.GetRequiredService<S3FileStore>(),
                "AzureBlob" => sp.GetRequiredService<AzureBlobFileStore>(),
                _ => sp.GetRequiredService<LocalFileStore>()
            };

            return new ValidatedFileStore(
                inner,
                sp.GetServices<IFileValidator>(),
                sp.GetRequiredService<IFileScanner>(),
                sp.GetRequiredService<IOptions<FileStorageOptions>>(),
                sp.GetRequiredService<ILogger<ValidatedFileStore>>());
        });

        return services;
    }

    public static IServiceCollection AddClamAvFileScanner(this IServiceCollection services)
    {
        services.Replace(ServiceDescriptor.Singleton<IFileScanner, ClamAvFileScanner>());
        return services;
    }
}
