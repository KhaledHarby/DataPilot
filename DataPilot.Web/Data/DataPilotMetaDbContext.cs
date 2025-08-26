using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DataPilot.Web.Data;

public class DataPilotMetaDbContext : IdentityDbContext<ApplicationUser>
{
    public DataPilotMetaDbContext(DbContextOptions<DataPilotMetaDbContext> options) : base(options) { }

    public DbSet<DbConnectionInfo> Connections => Set<DbConnectionInfo>();
    public DbSet<SchemaTable> SchemaTables => Set<SchemaTable>();
    public DbSet<SchemaColumn> SchemaColumns => Set<SchemaColumn>();
    public DbSet<QueryHistory> QueryHistories => Set<QueryHistory>();
    public DbSet<LlmProfile> LlmProfiles => Set<LlmProfile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<DbConnectionInfo>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.ConnectionStringEncrypted).IsRequired();
        });

        builder.Entity<SchemaTable>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Connection)
                .WithMany(c => c.Tables)
                .HasForeignKey(x => x.ConnectionId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
        });

        builder.Entity<SchemaColumn>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Table)
                .WithMany(t => t.Columns)
                .HasForeignKey(x => x.TableId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Name).HasMaxLength(256).IsRequired();
        });

        builder.Entity<QueryHistory>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Connection)
                .WithMany()
                .HasForeignKey(x => x.ConnectionId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Prompt).HasMaxLength(4000);
        });

        builder.Entity<LlmProfile>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Provider).HasMaxLength(100);
            e.Property(x => x.Model).HasMaxLength(200);
        });
    }
}


