using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using System.IO;

namespace SecureFileExchange
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // ✅ Serve frontend (static files)
            app.UseDefaultFiles(); // Ensures index.html is served automatically
            app.UseStaticFiles();  // Serves JS, CSS, etc.

            // ✅ If accessing an unknown route, return `index.html` (for SPA support)
            app.UseRouting();
            app.UseCors("AllowAll");
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // ✅ Fallback to index.html for unknown routes
            app.Use(async (context, next) =>
            {
                if (!Path.HasExtension(context.Request.Path.Value) && !context.Request.Path.Value.StartsWith("/api"))
                {
                    context.Request.Path = "/index.html";
                    await next();
                }
            });
        }
    }
}
