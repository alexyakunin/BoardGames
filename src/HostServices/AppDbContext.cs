using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ActualLab.Fusion.Authentication.Services;
using ActualLab.Fusion.EntityFramework;
using ActualLab.Fusion.EntityFramework.Operations;

namespace BoardGames.HostServices;

public class AppDbContext(DbContextOptions options) : DbContextBase(options), IDataProtectionKeyContext
{
    // App's own tables
    public DbSet<DbGame> Games { get; protected set; } = null!;
    public DbSet<DbGamePlayer> GamePlayers { get; protected set; } = null!;
    public DbSet<DbChatMessage> ChatMessages { get; protected set; } = null!;

    // ActualLab.Fusion.EntityFramework tables
    public DbSet<DbUser<long>> Users { get; protected set; } = null!;
    public DbSet<DbUserIdentity<long>> UserIdentities { get; protected set; } = null!;
    public DbSet<DbSessionInfo<long>> Sessions { get; protected set; } = null!;

    // ActualLab.Fusion.EntityFramework.Operations tables
    public DbSet<DbOperation> Operations { get; protected set; } = null!;
    public DbSet<DbEvent> Events { get; protected set; } = null!;

    // Data protection key storage
    public DbSet<DataProtectionKey> DataProtectionKeys { get; protected set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var dbGamePlayer = modelBuilder.Entity<DbGamePlayer>();
        dbGamePlayer.HasKey(e => new { e.DbGameId, UserId = e.DbUserId });
    }
}
