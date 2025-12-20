using ClubActivitiesSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClubActivitiesSystem.Db
{
    public class DBContext : DbContext
    {
        public DBContext(DbContextOptions<DBContext> options) : base(options) { }
        public DbSet<User> Users => Set<User>();
        public DbSet<Session> Sessions => Set<Session>();

        public DbSet<Club> Clubs => Set<Club>();
        public DbSet<ClubMember> ClubMembers => Set<ClubMember>();
        public DbSet<ApplicationForm> ApplicationForms => Set<ApplicationForm>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<EventSession> EventSessions => Set<EventSession>();
        public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<Feedback> Feedbacks => Set<Feedback>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ===== Users =====
            modelBuilder.Entity<User>()
                .HasIndex(x => x.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(x => x.Username)
                .IsUnique();

            // ===== Sessions =====
            modelBuilder.Entity<Session>()
                .HasIndex(x => x.UserId);

            modelBuilder.Entity<Session>()
                .HasOne(s => s.User)
                .WithMany(u => u.Sessions)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Session>()
                .HasIndex(x => x.Token)
                .IsUnique();

            // ===== Club =====
            modelBuilder.Entity<Club>()
                .HasOne(c => c.CreatedByUser)
                .WithMany()
                .HasForeignKey(c => c.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== ClubMember =====
            modelBuilder.Entity<ClubMember>()
                .HasOne(cm => cm.User)
                .WithMany()
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClubMember>()
                .HasOne(cm => cm.Club)
                .WithMany(c => c.Members)
                .HasForeignKey(cm => cm.ClubId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClubMember>()
                .HasIndex(cm => new { cm.ClubId, cm.UserId })
                .IsUnique(); // 同一社團不可重複加入

            // ===== ApplicationForm =====
            modelBuilder.Entity<ApplicationForm>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApplicationForm>()
                .HasOne<Club>()
                .WithMany()
                .HasForeignKey(a => a.ClubId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApplicationForm>()
                .HasIndex(a => new { a.UserId, a.ClubId })
                .IsUnique(); // 防止重複申請

            // ===== Event =====
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Club)
                .WithMany(c => c.Events)
                .HasForeignKey(e => e.ClubId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Event>()
                .HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== EventSession =====
            modelBuilder.Entity<EventSession>()
                .HasOne(es => es.Event)
                .WithMany()
                .HasForeignKey(es => es.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== EventRegistration =====
            modelBuilder.Entity<EventRegistration>()
                .HasOne(er => er.Event)
                .WithMany()
                .HasForeignKey(er => er.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventRegistration>()
                .HasOne(er => er.User)
                .WithMany()
                .HasForeignKey(er => er.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EventRegistration>()
                .HasIndex(er => new { er.EventId, er.UserId })
                .IsUnique(); // 防止重複報名

            // ===== Comment =====
            modelBuilder.Entity<Comment>()
                .HasOne<Event>()
                .WithMany()
                .HasForeignKey(c => c.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== Feedback =====
            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.Event)
                .WithMany()
                .HasForeignKey(f => f.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Feedback>()
                .HasIndex(f => new { f.EventId, f.UserId })
                .IsUnique(); // 一人一份回饋

            // ===== Timestamps =====
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (entityType.FindProperty("CreatedAt") != null)
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property<DateTime>("CreatedAt")
                        .HasDefaultValueSql("GETDATE()");
                }

                if (entityType.FindProperty("UpdatedAt") != null)
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property<DateTime?>("UpdatedAt")
                        .HasDefaultValueSql("GETDATE()");
                }
            }
        }


        // 自動更新 UpdatedAt
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                if (prop != null)
                {
                    prop.CurrentValue = DateTime.UtcNow;
                }
            }
        }
    }
}
