using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EnterpriseModularMonolith.Api.Composition;

internal sealed class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var isUploadEndpoint =
            string.Equals(operation.OperationId, "UploadFileFromSwagger", StringComparison.Ordinal) ||
            (string.Equals(context.ApiDescription.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase) &&
             string.Equals(context.ApiDescription.RelativePath, "api/v1/files/{container}/upload", StringComparison.OrdinalIgnoreCase));

        if (!isUploadEndpoint)
            return;

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content =
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Required = new HashSet<string> { "file" },
                        Properties =
                        {
                            ["file"] = new OpenApiSchema
                            {
                                Type = "string",
                                Format = "binary",
                                Description = "The file to upload."
                            },
                            ["objectKey"] = new OpenApiSchema
                            {
                                Type = "string",
                                Description = "Optional. Leave empty to keep the original file name, or use a path like images/avatar.png."
                            }
                        }
                    }
                }
            }
        };
    }
}
