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
                .IsRequired(false); // EF Core всё равно генерирует foreign key constraint, его нужно удалять вручную!!!

            modelBuilder.Entity<Student>()
                .HasKey(c => c.Name);
        }

        /*protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Server=dpg-cmnu2imd3nmc739h84h0-a.frankfurt-postgres.render.com;Port=5432;Database=summer_project_bd_byde;User Id=user;Password=xynG5n9OygPsORJds4LTAidWVMObA8nv");
                optionsBuilder.EnableSensitiveDataLogging();
            }
        }*/

        public virtual DbSet<FormRecord> Forms { get; set; } = default!;

        public virtual DbSet<FormFieldsOptionalDbEntity> FormFields { get; set; } = default!;

        public virtual DbSet<Student> Students { get; set; } = default!;
    }
}
