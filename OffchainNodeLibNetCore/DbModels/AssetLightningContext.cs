using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace OffchainNodeLibNetCore.DbModels
{
    public partial class AssetLightningContext : DbContext
    {
        private static DbContextOptions ConvertConnectionStringToDbContextOptions(string connectionString)
        {
            DbContextOptionsBuilder builder = new DbContextOptionsBuilder();
            builder.UseSqlServer(connectionString);
            return builder.Options;
        }

        public AssetLightningContext(string connectionString)
            : base(ConvertConnectionStringToDbContextOptions(connectionString))
        {

        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            #warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
            optionsBuilder.UseSqlServer(@"Server=.\SQLExpress;Database=AssetLightning;Trusted_Connection=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Channel>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedNever();

                entity.Property(e => e.Asset).HasColumnType("varchar(50)");

                entity.Property(e => e.Destination)
                    .IsRequired()
                    .HasColumnType("varchar(50)");

                entity.HasOne(d => d.StateNavigation)
                    .WithMany(p => p.Channel)
                    .HasForeignKey(d => d.State)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_Channel_ChannelState");
            });

            modelBuilder.Entity<ChannelState>(entity =>
            {
                entity.Property(e => e.StateName)
                    .IsRequired()
                    .HasMaxLength(50);
            });

            modelBuilder.Entity<PreGeneratedOutput>(entity =>
            {
                entity.HasKey(e => new { e.TransactionId, e.OutputNumber })
                    .HasName("PK__PreGener__E16A8B3A14A3E150");

                entity.Property(e => e.TransactionId).HasColumnType("varchar(100)");

                entity.Property(e => e.Address).HasColumnType("varchar(100)");

                entity.Property(e => e.AssetId).HasColumnType("varchar(100)");

                entity.Property(e => e.Network).HasColumnType("varchar(10)");

                entity.Property(e => e.PrivateKey)
                    .IsRequired()
                    .HasColumnType("varchar(100)");

                entity.Property(e => e.Script)
                    .IsRequired()
                    .HasColumnType("varchar(1000)");

                entity.Property(e => e.Version)
                    .IsRequired()
                    .HasColumnType("timestamp")
                    .ValueGeneratedOnAddOrUpdate();
            });

            modelBuilder.Entity<Session>(entity =>
            {
                entity.Property(e => e.SessionId).HasColumnType("varchar(50)");

                entity.Property(e => e.Asset).HasColumnType("varchar(50)");

                entity.Property(e => e.CreationDatetime).HasColumnType("datetime");

                entity.Property(e => e.Network)
                    .IsRequired()
                    .HasColumnType("varchar(10)");

                entity.Property(e => e.PubKey)
                    .IsRequired()
                    .HasColumnType("varchar(100)");
            });
        }

        public virtual DbSet<Channel> Channel { get; set; }
        public virtual DbSet<ChannelState> ChannelState { get; set; }
        public virtual DbSet<PreGeneratedOutput> PreGeneratedOutput { get; set; }
        public virtual DbSet<Session> Session { get; set; }
    }
}