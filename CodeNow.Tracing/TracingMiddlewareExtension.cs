using Microsoft.AspNetCore.Builder;


namespace CodeNow.Tracing
{
        public static  class TracingMiddlewareExtension
        {
            public static IApplicationBuilder UseCodeNowTracing(this IApplicationBuilder builder)
            {
                return builder.UseMiddleware<TracingMiddleware>();
            }
        }
}