using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using WebSocketServer.ConnectionManager;

namespace WebSocketServer.Middleware{
    public static class WebSocketServerMiddlwareExtensions{
        public static IApplicationBuilder UseWebSocketServer(this IApplicationBuilder builder){
            return builder.UseMiddleware<WebSocketServerMiddleware>();
        }

        public static IServiceCollection AddWebSocketServerConnection(this IServiceCollection services){
            services.AddSingleton<WebSocketServerConnectionManager>();
            return services;
        }
    }
}