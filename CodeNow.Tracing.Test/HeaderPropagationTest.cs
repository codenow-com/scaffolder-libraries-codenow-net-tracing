using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace CodeNow.Tracing
{
    public class HeaderPropagationTest: IClassFixture<TestServerFixture>
    {
        private TestServerFixture Fixture { get; }

        public HeaderPropagationTest(TestServerFixture fixture)
        {
            Fixture = fixture;
        }

        [Fact]
        public async Task Is_TestServer_healthy()
        {
            var health = await Fixture.HttpClient.GetStringAsync("/health");
            Assert.Equal("Healthy", health);
        }

        private async Task<RequestHeaderInjection> TestHttpRequest(Action<HttpRequestHeaders> setHeaders)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/http-request");

            setHeaders(request.Headers);
            var response = await Fixture.HttpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RequestHeaderInjection>(responseJson,
                new JsonSerializerOptions {PropertyNameCaseInsensitive = true});
        }
        
        [Fact]
        public async Task Passes_activity_into_http_client_as_w3c_tracecontext_headers()
        {
            var requestInformation = await TestHttpRequest(headers =>
            {
                headers.Add("traceparent", "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
            });
            
            var requestHeaders = new Dictionary<string, string>(requestInformation.RequestHeaders, StringComparer.InvariantCultureIgnoreCase);
            var expectedTraceparent = $"00-{requestInformation.ActivityInfo.TraceId}-{requestInformation.ActivityInfo.SpanId}-01";
            Assert.Equal(expectedTraceparent,  requestHeaders["traceparent"]);
        }

        [Fact]
        public async Task Passes_activity_into_http_client_as_zipkin_b3_headers()
        {
            var requestInformation = await TestHttpRequest(headers =>
            {
                headers.Add("x-b3-traceid", "0af7651916cd43dd8448eb211c80319c");
                headers.Add("x-b3-spanid", "b7ad6b7169203331");
                headers.Add("x-b3-sampled", "1");
            });
            
            var requestHeaders = new Dictionary<string, string>(requestInformation.RequestHeaders, StringComparer.InvariantCultureIgnoreCase);
            Assert.Equal(requestInformation.ActivityInfo.TraceId,  requestHeaders["x-b3-traceid"]);
            Assert.Equal(requestInformation.ActivityInfo.SpanId,  requestHeaders["x-b3-spanid"]);
            Assert.Equal("1",  requestHeaders["x-b3-sampled"]);
        }
    }
}