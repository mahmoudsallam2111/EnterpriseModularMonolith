FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything
COPY . .

# Restore and build the API
RUN dotnet restore "EnterpriseModularMonolith.sln"
RUN dotnet build "src/Bootstrapper/EnterpriseModularMonolith.Api/EnterpriseModularMonolith.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/Bootstrapper/EnterpriseModularMonolith.Api/EnterpriseModularMonolith.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EnterpriseModularMonolith.Api.dll"]
