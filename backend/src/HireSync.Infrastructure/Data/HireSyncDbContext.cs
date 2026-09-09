using HireSync.Application.Interfaces.Persistence;
using HireSync.Application.Security;
using HireSync.Domain.Entities;
using HireSync.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Infrastructure.Data;

public class HireSyncDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>,
      IHireSyncDbContext
{
    public HireSyncDbContext(DbContextOptions<HireSyncDbContext> options)
        : base(options)
    {
    }

    public DbSet<EmailOtpChallenge> EmailOtpChallenges =>
        Set<EmailOtpChallenge>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(user => user.AccountStatus)
                .HasConversion<byte>();

            entity.Property(user => user.TokenVersion)
                .HasDefaultValue(1);

            entity.Property(user => user.CreatedAtUtc)
                .HasPrecision(3);

            entity.Property(user => user.UpdatedAtUtc)
                .HasPrecision(3);

            entity.Property(user => user.RowVersion)
                .IsRowVersion();

            entity.HasIndex(user => user.NormalizedEmail)
                .IsUnique();
        });

        builder.Entity<EmailOtpChallenge>(entity =>
        {
            entity.ToTable("EmailOtpChallenges");

            entity.HasKey(challenge => challenge.Id);

            entity.Property(challenge => challenge.Email)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(challenge => challenge.Purpose)
                .HasConversion<byte>();

            entity.Property(challenge => challenge.CodeHash)
                .HasMaxLength(44)
                .IsRequired();

            entity.Property(challenge => challenge.CreatedAtUtc)
                .HasPrecision(3);

            entity.Property(challenge => challenge.ExpiresAtUtc)
                .HasPrecision(3);

            entity.Property(challenge => challenge.ConsumedAtUtc)
                .HasPrecision(3);

            entity.Property(challenge => challenge.FailedAttempts)
                .HasDefaultValue(0);

            entity.HasIndex(challenge => new
            {
                challenge.Email,
                challenge.Purpose,
                challenge.CreatedAtUtc
            });
        });

        builder.Entity<IdentityRole<Guid>>().HasData(
            new IdentityRole<Guid>
            {
                Id = IdentitySeedIds.JobSeekerRoleId,
                Name = RoleNames.JobSeeker,
                NormalizedName = "JOBSEEKER",
                ConcurrencyStamp = "20000000-0000-0000-0000-000000000001"
            },
            new IdentityRole<Guid>
            {
                Id = IdentitySeedIds.EmployerRoleId,
                Name = RoleNames.Employer,
                NormalizedName = "EMPLOYER",
                ConcurrencyStamp = "20000000-0000-0000-0000-000000000002"
            },
            new IdentityRole<Guid>
            {
                Id = IdentitySeedIds.AdministratorRoleId,
                Name = RoleNames.Administrator,
                NormalizedName = "ADMINISTRATOR",
                ConcurrencyStamp = "20000000-0000-0000-0000-000000000003"
            });
    }
}
