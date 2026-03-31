using Microsoft.EntityFrameworkCore;
using TheFallenWastes_Infrastructure;
using TheFallenWastes_Application;

namespace TheFallenWastes_WebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers()
                .AddJsonOptions(opts =>
                {
                    opts.JsonSerializerOptions.Converters.Add(
                        new System.Text.Json.Serialization.JsonStringEnumConverter());
                    opts.JsonSerializerOptions.ReferenceHandler =
                        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                });

            builder.Services.AddScoped<PlayerDataMigrationService>();

            builder.Services.AddDbContext<GameDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception("DefaultConnection is null or empty.");
            }

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddCors(options =>
                options.AddDefaultPolicy(b =>
                    b.AllowAnyOrigin()
                     .AllowAnyMethod()
                     .AllowAnyHeader()));

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Ensure database migrations are applied at startup so schema changes (like BuildingUpgradeQueueItems)
            // exist when the app runs under the debugger or locally.
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
                    // db.Database.Migrate();
                }
                catch (Exception ex)
                {
                    // If migration fails, rethrow so developer sees the error during startup.
                    Console.WriteLine($"Database migration failed: {ex}");
                    throw;
                }
            }

            app.UseHttpsRedirection();

            app.UseCors();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}