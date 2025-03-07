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

            // ✅ Enable CORS for API requests
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader());
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // ✅ Serve frontend (static files from wwwroot)
            app.UseDefaultFiles(); // Ensures index.html is served automatically
            app.UseStaticFiles();  // Serves JS, CSS, etc.

            app.UseRouting();
            app.UseCors("AllowAll");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers(); // API routes
            });

            // ✅ Redirect unknown routes (except API) to index.html (for SPA support)
            app.Use(async (context, next) =>
            {
                if (!Path.HasExtension(context.Request.Path.Value) && 
                    !context.Request.Path.Value.StartsWith("/api"))
                {
                    context.Request.Path = "/index.html";
                    await next();
                }
                else
                {
                    await next();
                }
            });

            // ✅ Serve index.html as fallback
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(env.WebRootPath)),
                RequestPath = ""
            });
        }
    }
}
