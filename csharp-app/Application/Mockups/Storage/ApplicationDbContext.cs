using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Mockups.Storage
{
    public class ApplicationDbContext
        : IdentityDbContext<User, Role, Guid, IdentityUserClaim<Guid>, UserRole, IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            //Database.EnsureDeleted();
            Database.EnsureCreated();
        }

        public override DbSet<User> Users { get; set; }
        public override DbSet<Role> Roles { get; set; }
        public override DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderMenuItem> OrderMenuItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Npgsql для PostgreSQL timestamptz (timestamp with time zone) требует UTC DateTime.
            // В проекте DateTime часто появляется как Unspecified/Local (new DateTime(...), model binding, DateTime.Now).
            // Нормализуем ВСЕ DateTime/DateTime? в модели в UTC при записи, и помечаем UTC при чтении.
            var utcDateTimeConverter = new ValueConverter<DateTime, DateTime>(
                v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableUtcDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue
                    ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : v.Value.ToUniversalTime())
                    : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(utcDateTimeConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(nullableUtcDateTimeConverter);
                    }
                }
            }

            builder.Entity<User>(o =>
            {
                o.ToTable("Users");
                o.HasMany(x => x.Addresses).WithOne(x => x.User);
            });
            builder.Entity<Role>(o =>
            {
                o.ToTable("Roles");
            });
            builder.Entity<UserRole>(o =>
            {
                o.ToTable("UserRoles");
                o.HasOne(x => x.Role)
                    .WithMany(x => x.Users)
                    .HasForeignKey(x => x.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);
                o.HasOne(x => x.User)
                    .WithMany(x => x.Roles)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            builder.Entity<Address>(o =>
            {
                o.ToTable("Addresses");
            });
            builder.Entity<MenuItem>(o =>
            {
                o.ToTable("MenuItems");
            });
            builder.Entity<Order>(o =>
            {
                o.ToTable("Orders");
            });
            builder.Entity<OrderMenuItem>(o =>
            {
                o.ToTable("OrderMenuItems");
                o.HasKey(q => new
                {
                    q.OrderId,
                    q.ItemId
                });

            });
        }
    }
}
