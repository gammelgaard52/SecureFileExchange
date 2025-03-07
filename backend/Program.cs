using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace SecureFileExchange
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("http://*:5000"); // ✅ Force the application to listen on port 5000
                });
    }
}
