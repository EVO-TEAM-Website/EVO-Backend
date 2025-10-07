using EVO_Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EVO_Backend.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
        {
            using var scope = services.CreateScope();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            foreach (var role in new[] { "Admin", "Student" })
                if (!await roleMgr.RoleExistsAsync(role))
                    await roleMgr.CreateAsync(new IdentityRole<Guid>(role));

            var email = config["AdminSeed:Email"];
            var pass = config["AdminSeed:Password"];
            var name = config["AdminSeed:FullName"] ?? "Super Admin";

            if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(pass))
            {
                var existing = await userMgr.FindByEmailAsync(email);
                if (existing == null)
                {
                    var admin = new ApplicationUser
                    {
                        Id = Guid.NewGuid(),
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                        FullName = name,
                        Specialization = Specialization.CS
                    };

                    var result = await userMgr.CreateAsync(admin, pass);
                    if (!result.Succeeded)
                        throw new Exception("Admin seeding failed: " + string.Join("; ", result.Errors.Select(e => e.Description)));

                    await userMgr.AddToRoleAsync(admin, "Admin");
                }
            }
        }
    }
}
