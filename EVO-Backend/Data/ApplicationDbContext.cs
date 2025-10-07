using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EVO_Backend.Models;

namespace EVO_Backend.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<EmailVerification> EmailVerifications => Set<EmailVerification>();

        public DbSet<Course> Courses => Set<Course>();

        public DbSet<TypingScore> TypingScores => Set<TypingScore>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // كل مستخدم له سجل واحد
            builder.Entity<TypingScore>()
                .HasIndex(t => t.UserId)
                .IsUnique();

            builder.Entity<TypingScore>()
                .HasOne(t => t.User)
                .WithMany() // ما بدنا مجموعة
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<EmailVerification>(b =>
            {
                b.ToTable("EmailVerifications");
                b.HasKey(x => x.Id);
                b.Property(x => x.Code).HasMaxLength(6).IsRequired();
                b.HasIndex(x => x.UserId).IsUnique(); // كود واحد فعال لكل مستخدم
            });

            // نخزّن enum كنص عشان يظل ثابت لو تغير ترتيب القيم
            builder.Entity<ApplicationUser>()
                   .Property(u => u.Specialization)
                   .HasConversion<string>();

            // (اختياري) إعادة تسمية جداول الهوية
            builder.Entity<ApplicationUser>().ToTable("Users");
            builder.Entity<IdentityRole<Guid>>().ToTable("Roles");
            builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
            builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

            builder.Entity<ApplicationUser>()
                   .HasIndex(u => u.Email)
                   .IsUnique();
        }
    }
}
