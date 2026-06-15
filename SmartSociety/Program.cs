using Microsoft.Data.SqlClient;
using System.Data;

namespace SmartSociety
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            
            // Add HttpContextAccessor to use HttpContext in views
            builder.Services.AddHttpContextAccessor();

            // Register IDbConnection for Dapper/Stored Procedures
            builder.Services.AddScoped<IDbConnection>(sp => 
                new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Register Repositories
            builder.Services.AddScoped<SmartSociety.Repositories.IUserRepository, SmartSociety.Repositories.UserRepository>();
            builder.Services.AddScoped<SmartSociety.Repositories.IBlockRepository, SmartSociety.Repositories.BlockRepository>();
            builder.Services.AddScoped<SmartSociety.Repositories.IFlatRepository, SmartSociety.Repositories.FlatRepository>();
            builder.Services.AddScoped<SmartSociety.Repositories.IParkingRepository, SmartSociety.Repositories.ParkingRepository>();
            builder.Services.AddScoped<SmartSociety.Repositories.IVisitorRepository, SmartSociety.Repositories.VisitorRepository>();
            builder.Services.AddScoped<SmartSociety.Repositories.IMaintenanceRepository, SmartSociety.Repositories.MaintenanceRepository>();
            builder.Services.AddScoped<SmartSociety.Repositories.IUtilityRepository, SmartSociety.Repositories.UtilityRepository>();
            builder.Services.AddScoped<SmartSociety.Repositories.IComplaintRepository, SmartSociety.Repositories.ComplaintRepository>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
