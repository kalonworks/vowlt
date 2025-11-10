using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vowlt.Api.Features.Auth.Models;
using Vowlt.Api.Features.Bookmarks.Models;

namespace Vowlt.Api.Data;

public class VowltDbContext(DbContextOptions<VowltDbContext> options) : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("vector");

        // Configure Bookmark
        modelBuilder.Entity<Bookmark>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Multi-tenant index (critical for performance)
            entity.HasIndex(e => e.UserId);

            // Unique constraint: one URL per user
            entity.HasIndex(e => new { e.UserId, e.Url }).IsUnique();

            // Domain index (for filtering by site)
            entity.HasIndex(e => e.Domain);

            // Created date index (for sorting)
            entity.HasIndex(e => e.CreatedAt);

            // Configure vector column with pgvector
            entity.Property(e => e.Embedding)
                .HasColumnType("vector(384)");  // 384 dimensions

            // HNSW index for vector similarity search
            entity.HasIndex(e => e.Embedding)
                .HasMethod("hnsw")
                .HasOperators("vector_cosine_ops");

            // Required fields
            entity.Property(e => e.Url).IsRequired().HasMaxLength(2048);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Domain).HasMaxLength(255);

            // Configure user-defined tags
            entity.Property(e => e.Tags)
                .HasColumnType("text[]")
                .HasDefaultValueSql("'{}'");

            entity.HasIndex(e => e.Tags)
                .HasMethod("gin");

            // Configure AI-generated tags
            entity.Property(e => e.GeneratedTags)
                .HasColumnType("text[]")
                .HasDefaultValueSql("'{}'");

            entity.HasIndex(e => e.GeneratedTags)
                .HasMethod("gin");
        });

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");

            entity.HasMany(u => u.RefreshTokens)
                .WithOne(rt => rt.User)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");

            entity.HasIndex(rt => rt.Token).IsUnique();
            entity.HasIndex(rt => rt.UserId);
            entity.HasIndex(rt => rt.ExpiresAt);
        });

        //friendly names for user tables.
        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("Roles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
    }
}

