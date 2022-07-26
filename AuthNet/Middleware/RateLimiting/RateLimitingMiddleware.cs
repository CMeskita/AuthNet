using AspNetCoreRateLimit;

namespace AuthNet.Middleware.RateLimiting
{
    internal static class RateLimitingMiddleware
    {
        internal static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {  // Usado para armazenar contadores de limite de taxa e regras de ip
            services.AddMemoryCache();

            // Carregar na configuração geral do appsettings.json
            services.Configure<IpRateLimitOptions>(options => configuration.GetSection("IpRateLimitingSettings").Bind(options));

            services.AddSingleton<IRateLimitCounterStore, DistributedCacheRateLimitCounterStore>();
            services.AddDistributedRateLimiting();
            //Injetar Contador e Regras 
            services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
            services.AddInMemoryRateLimiting();

            // Return the services
            return services;
        }
        internal static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
        {
            app.UseIpRateLimiting();
            return app;
        }
    }
}
