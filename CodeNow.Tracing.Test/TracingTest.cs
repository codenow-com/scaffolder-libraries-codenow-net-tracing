using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace CodeNow.Tracing
{
    public class TracingTest : IClassFixture<TestServerFixture>
    {
        private TestServerFixture Fixture { get; }

        public TracingTest(TestServerFixture fixture)
        {
            Fixture = fixture;
        }

        [Fact]
        public async Task Is_TestServer_healthy()
        {
            var health = await Fixture.HttpClient.GetStringAsync("/health");
            Assert.Equal("Healthy", health);
        }

        public async Task<ActivityInfo> GetActivity(Action<HttpRequestHeaders> setHeaders)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/activity");

            setHeaders(request.Headers);
            var response = await Fixture.HttpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var activityJson = await response.Content.ReadAsStringAsync();
            var activityInfo = JsonSerializer.Deserialize<ActivityInfo>(activityJson,
                new JsonSerializerOptions {PropertyNameCaseInsensitive = true});
            
            return activityInfo!;
        }

        [Fact]
        public async Task Retrieves_activity_from_w3c_trace_context()
        {
            string traceId = "0af7651916cd43dd8448eb211c80319c";
            string parentId = "b7ad6b7169203331";
            var activityInfo = await GetActivity(headers =>
            {
                headers.Add("traceparent", $"00-{traceId}-{parentId}-01");
            });
            Assert.Equal(traceId, activityInfo.TraceId);
            Assert.Equal(parentId, activityInfo.ParentId);
        }

        [Fact]
        public async Task Retrieves_activity_from_xb3_headers()
        {
            string traceId = "0af7651916cd43dd8448eb211c80319c";
            string parentId = "b7ad6b7169203331";
            var activityInfo = await GetActivity(headers =>
            {
                headers.Add("x-b3-traceid", traceId);
                headers.Add("x-b3-spanid", parentId);
            });
            Assert.Equal(traceId, activityInfo.TraceId);
            Assert.Equal(parentId, activityInfo.ParentId);
        }
    }
}