using HireSync.Application.Interfaces.Persistence;
using HireSync.Infrastructure.Data;
using HireSync.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Shared persistence
var connectionString = builder.Configuration.GetConnectionString("HireSyncDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'HireSyncDatabase' was not found.");

builder.Services.AddDbContext<HireSyncDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IHireSyncDbContext>(serviceProvider =>
    serviceProvider.GetRequiredService<HireSyncDbContext>());

// ASP.NET Core Identity
builder.Services
    .AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<HireSyncDbContext>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
