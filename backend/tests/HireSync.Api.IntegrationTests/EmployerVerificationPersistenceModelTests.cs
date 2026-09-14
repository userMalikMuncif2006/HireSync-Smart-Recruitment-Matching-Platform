using HireSync.Domain.Enums;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Api.IntegrationTests;

public class EmployerVerificationPersistenceModelTests
{
    [Fact]
    public void ApplicationUser_employer_verification_status_is_nullable_tinyint()
    {
        var options = new DbContextOptionsBuilder<HireSyncDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=HireSyncModelOnly;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        using var context = new HireSyncDbContext(options);

        var entityType = context.Model.FindEntityType(typeof(ApplicationUser));

        Assert.NotNull(entityType);

        var property = entityType!.FindProperty(
            nameof(ApplicationUser.EmployerVerificationStatus));

        Assert.NotNull(property);
        Assert.True(property!.IsNullable);
        Assert.Equal(
            typeof(EmployerVerificationStatus?),
            property.ClrType);
        Assert.Equal("tinyint", property.GetColumnType());
    }
}
