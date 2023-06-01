using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Identity
{
    public class MyUserDbContext : IdentityDbContext<MyUser> //Extensão para novas possibilidades de tabelas
    {
        public MyUserDbContext(DbContextOptions<MyUserDbContext> options) 
            : base(options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Organization>(org => 
            {
                org.ToTable("Organizations");
                org.HasKey(x => x.Id);

                org.HasMany<MyUser>()
                    .WithOne()
                    .HasForeignKey(x => x.OrgId)
                    .IsRequired(false);
            }
            );
        }
    }
}
