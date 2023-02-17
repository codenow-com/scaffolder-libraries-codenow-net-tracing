using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.IO;

namespace CodeNow.Tracing
{
    public class TracingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public TracingMiddleware(RequestDelegate next)
        {
            _next = next;
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        }

        public async Task Invoke(HttpContext context)
        {
            await ForwardTracingHeadersFromRequestToResponse(context);
        }

        private async Task ForwardTracingHeadersFromRequestToResponse(HttpContext context)
        {
            var originalBodyStream = context.Response.Body;
          
            await using var responseBody = _recyclableMemoryStreamManager.GetStream();
            context.Response.Body = responseBody;

            await _next(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            var traceId = "";
            var spanId = "";
            var parentSpanId = "";
            
            try
            {
                traceId = context.Request.Headers.FirstOrDefault(x =>
                        string.Equals(x.Key, "x-b3-traceid", StringComparison.InvariantCultureIgnoreCase))
                    .Value
                    .ToString().ToLowerInvariant();
                
                spanId = context.Request.Headers.FirstOrDefault(x =>
                        string.Equals(x.Key, "x-b3-spanid", StringComparison.InvariantCultureIgnoreCase))
                    .Value
                    .ToString().ToLowerInvariant();
                
                parentSpanId = context.Request.Headers.FirstOrDefault(x =>
                        string.Equals(x.Key, "x-b3-parentspanid", StringComparison.InvariantCultureIgnoreCase))
                    .Value
                    .ToString().ToLowerInvariant();
            }
            catch (Exception)
            {
                //omit
            }

            if (spanId != "")
            {
                if (!context.Response.Headers.ContainsKey("x-b3-spanid"))
                {
                    context.Response.Headers.Add("x-b3-spanid",spanId);
                }
            }

            if (traceId != "")
            {

                if (!context.Response.Headers.ContainsKey("x-b3-traceid"))
                {
                    context.Response.Headers.Add("x-b3-traceid",traceId);
                }
            }

            if (parentSpanId != "")
            {
                if (!context.Response.Headers.ContainsKey("x-b3-parentspanid"))
                {
                    context.Response.Headers.Add("x-b3-parentspanid",parentSpanId);
                }
            }

            await responseBody.CopyToAsync(originalBodyStream);
        }
    }
}