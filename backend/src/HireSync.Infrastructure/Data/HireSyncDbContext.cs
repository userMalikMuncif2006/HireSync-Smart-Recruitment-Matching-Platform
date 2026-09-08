using HireSync.Application.Interfaces.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HireSync.Infrastructure.Data;

public class HireSyncDbContext : DbContext, IHireSyncDbContext
{
    public HireSyncDbContext(DbContextOptions<HireSyncDbContext> options)
        : base(options)
    {
    }
}
