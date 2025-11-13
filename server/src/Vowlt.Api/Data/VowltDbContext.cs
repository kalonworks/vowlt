using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Vowlt.Api.Features.Auth.Models;
using Vowlt.Api.Features.Bookmarks.Models;
using Vowlt.Api.Features.OAuth.Models;

namespace Vowlt.Api.Data;

public class VowltDbContext(DbContextOptions<VowltDbContext> options) : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuthorizationCode> AuthorizationCodes => Set<AuthorizationCode>();
    public DbSet<OAuthClient> OAuthClients => Set<OAuthClient>();



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.HasPostgresExtension("pg_search");

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

        // Configure OAuth authorization codes
        modelBuilder.Entity<AuthorizationCode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ExpiresAt);

            entity.Property(e => e.Code).IsRequired().HasMaxLength(64);
            entity.Property(e => e.CodeChallenge).IsRequired().HasMaxLength(128);
            entity.Property(e => e.CodeChallengeMethod).IsRequired().HasMaxLength(10);
            entity.Property(e => e.UserEmail).IsRequired().HasMaxLength(256);
            entity.Property(e => e.ClientId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RedirectUri).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.State).HasMaxLength(500);
        });


        // Configure OAuth clients
        modelBuilder.Entity<OAuthClient>(entity =>
        {
            entity.HasKey(e => e.ClientId);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.AllowedRedirectUris)
                .IsRequired()
                .HasMaxLength(2000);
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

