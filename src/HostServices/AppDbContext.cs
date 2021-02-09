using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework.Authentication;
using Stl.Fusion.EntityFramework.Operations;

namespace BoardGames.HostServices
{
    public class AppDbContext : DbContext, IDataProtectionKeyContext
    {
        public DbSet<DbGame> Games { get; protected set; } = null!;
        public DbSet<DbGamePlayer> GamePlayers { get; protected set; } = null!;

        // Stl.Fusion.EntityFramework tables
        public DbSet<DbOperation> Operations { get; protected set; } = null!;
        public DbSet<DbSessionInfo> Sessions { get; protected set; } = null!;
        public DbSet<DbUser> Users { get; protected set; } = null!;
        public DbSet<DbUserIdentity> UserIdentities { get; protected set; } = null!;

        // Data protection key storage
        public DbSet<DataProtectionKey> DataProtectionKeys { get; protected set; } = null!;

        public AppDbContext(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var dbGamePlayer = modelBuilder.Entity<DbGamePlayer>();
            dbGamePlayer.HasKey(p => new { p.GameId, p.UserId });
        }
    }
}
