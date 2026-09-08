using HireSync.Domain.Entities;
using HireSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Api.IntegrationTests;

public class EmailOtpPersistenceModelTests
{
    [Fact]
    public void Email_otp_challenge_is_registered_in_shared_context()
    {
        var options =
            new DbContextOptionsBuilder<HireSyncDbContext>()
                .UseSqlServer(
                    "Server=(local);Database=HireSyncModelTest;Trusted_Connection=True;TrustServerCertificate=True")
                .Options;

        using var context =
            new HireSyncDbContext(options);

        var entityType =
            context.Model.FindEntityType(
                typeof(EmailOtpChallenge));

        Assert.NotNull(entityType);
        Assert.Equal(
            "EmailOtpChallenges",
            entityType!.GetTableName());
    }

    [Fact]
    public void Email_otp_hash_storage_is_bounded()
    {
        var options =
            new DbContextOptionsBuilder<HireSyncDbContext>()
                .UseSqlServer(
                    "Server=(local);Database=HireSyncModelTest;Trusted_Connection=True;TrustServerCertificate=True")
                .Options;

        using var context =
            new HireSyncDbContext(options);

        var property =
            context.Model
                .FindEntityType(typeof(EmailOtpChallenge))!
                .FindProperty(nameof(EmailOtpChallenge.CodeHash));

        Assert.NotNull(property);
        Assert.Equal(44, property!.GetMaxLength());
        Assert.False(property.IsNullable);
    }
}
