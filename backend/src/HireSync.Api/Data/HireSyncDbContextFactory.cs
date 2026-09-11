using HireSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace HireSync.Api.Data;

public sealed class HireSyncDbContextFactory
    : IDesignTimeDbContextFactory<HireSyncDbContext>
{
    public HireSyncDbContext CreateDbContext(string[] args)
    {
        var projectDirectory = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                ".."));

        var environment =
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(projectDirectory)
            .AddJsonFile(
                "appsettings.json",
                optional: true)
            .AddJsonFile(
                $"appsettings.{environment}.json",
                optional: true)
            .AddUserSecrets<HireSyncDbContextFactory>(
                optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("HireSyncDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'HireSyncDatabase' was not found.");

        var optionsBuilder =
            new DbContextOptionsBuilder<HireSyncDbContext>();

        optionsBuilder.UseSqlServer(connectionString);

        return new HireSyncDbContext(
            optionsBuilder.Options);
    }
}