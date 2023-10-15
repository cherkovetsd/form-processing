using Data.Models;
using Microsoft.EntityFrameworkCore;

namespace DatabaseInfo
{
    public class Context : DbContext
    {
        public Context(DbContextOptions<Context> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FormFieldsOptionalDbEntity>(
                builder =>
                {
                    builder.ToTable("Forms");
                    builder.Property(e => e.Name).HasColumnName("Name");
                });
            modelBuilder.Entity<FormRecord>(
                builder =>
                {
                    builder.ToTable("Forms");
                    builder.Property(e => e.Name).HasColumnName("Name");
                    builder.HasOne(e => e.Fields).WithOne(e => e.Record).HasForeignKey<FormFieldsOptionalDbEntity>(e => e.Id);
                    builder.Navigation(e => e.Fields).IsRequired();
                });

            modelBuilder.Entity<FormRecord>()
                .HasOne(e => e.Student)
                .WithMany(e => e.Forms)
                .HasForeignKey(e => e.Name)
                .HasPrincipalKey(e => e.Name)
                .IsRequired(true);

            modelBuilder.Entity<Student>()
                .HasKey(c => c.Name);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Server=dpg-ck5d1nmg2bec738cqgf0-a.oregon-postgres.render.com;Port=5432;Database=db_ix7c;User Id=user;Password=FWvALG9UcftQkqwsLZdzZggNll32W9JX;: null,: null,: null,: null,");
                optionsBuilder.EnableSensitiveDataLogging();
            }
        }

        public DbSet<FormRecord> Forms { get; set; } = default!;

        public DbSet<FormFieldsOptionalDbEntity> FormFields { get; set; } = default!;

        public DbSet<Student> Students { get; set; } = default!;
    }
}
