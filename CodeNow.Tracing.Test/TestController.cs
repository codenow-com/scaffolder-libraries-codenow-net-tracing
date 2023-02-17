using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace CodeNow.Tracing
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("/activity")]
        public Task<ActivityInfo> ActivityAction()
        {
            var current = Activity.Current!;
            var activityInfo = new ActivityInfo
            {
                TraceId = current.TraceId.ToString(),
                ParentId = current.ParentSpanId.ToString()
            };
            return Task.FromResult(activityInfo);
        }

        private class ActivityStartData
        {
            // OpenTelemetry will use reflection to access this property
            public HttpRequestMessage Request { get; }

            public ActivityStartData(HttpRequestMessage Request)
            {
                this.Request = Request;
            }
        }
        
        [HttpGet("/http-request")]
        public Task<RequestHeaderInjection> HttpRequest()
        {
            var activitySource = new ActivitySource("Test.HttpRequest.Source");
            using var activity = activitySource.StartActivity("Test.HttpRequest", ActivityKind.Client);
            if (activity is null)
            {
                throw new Exception("Activity listener should be registered");
            }
            var request = new HttpRequestMessage(HttpMethod.Get, "http://request.test");
            // Emulates what System.Net.Http.DiagnosticsHandler.SendAsyncCore does when running real HTTP request
            // because DiagnosticsHandler is internal, it is not possible to use it directly
            // I haven't found more white-box test for this
            DiagnosticListener diagnosticListener = new("HttpHandlerDiagnosticListener");
            if (!diagnosticListener.IsEnabled())
            {
                throw new Exception("Diagnostics should be enabled");
            }
            
            diagnosticListener.StartActivity(activity, new ActivityStartData(request));

            var activityInfo = new ActivityInfo
            {
                TraceId = activity.TraceId.ToString(),
                SpanId = activity.SpanId.ToString(),
                ParentId = activity.ParentSpanId.ToString()
            };

            var requestHeaders = request.Headers.ToDictionary(x => x.Key, x => x.Value.Single());
            return Task.FromResult(new RequestHeaderInjection {RequestHeaders = requestHeaders, ActivityInfo = activityInfo});
        }
    }
}