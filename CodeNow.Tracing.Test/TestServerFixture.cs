using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace CodeNow.Tracing
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class TestServerFixture : IAsyncLifetime
    {
        private readonly IMessageSink _messageSink;
        private TestServer? _testServer;
        private readonly string _serverTag;
        private HttpClient? _client;

        public TestServer TestServer => _testServer ?? throw new Exception("Fixture wasn't initialized");

        public HttpClient HttpClient => _client ?? throw new Exception("Fixture wasn't initialized");

        public TestServerFixture(IMessageSink messageSink)
        {
            _messageSink = messageSink;
            _serverTag = $"[TestServerFixture-{Guid.NewGuid()}]";
        }

        public static void ConfigureServices(IServiceCollection services)
        {
        }

        private async Task<TestServer> CreateTestServer(CancellationToken cancellationToken)
        {
            _messageSink.OnMessage(new DiagnosticMessage($"{_serverTag} Initializing test server."));

            var builder = Host.CreateDefaultBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseEnvironment("Testing")
                        .UseStartup<TestStartup>()
                        .ConfigureTestServices(ConfigureServices)
                        .UseTestServer();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddXUnit(_messageSink,
                        cfg => { cfg.MessageSinkMessageFactory = DiagnosticMessageFactory; });
                });

            IMessageSinkMessage DiagnosticMessageFactory(string msg) => new DiagnosticMessage($"{_serverTag} {msg}");

            var host = await builder.StartAsync(cancellationToken);
            var testServer = host.GetTestServer();

            testServer.BaseAddress = new Uri("https://localhost/"); // use HTTPS for all requests

            _messageSink.OnMessage(new DiagnosticMessage($"{_serverTag} Initialized test server."));

            return testServer;
        }

        public async Task InitializeAsync()
        {
            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
            _testServer = await CreateTestServer(cts.Token);
            _client = _testServer.CreateClient();
        }

        public Task DisposeAsync()
        {
            _messageSink.OnMessage(new DiagnosticMessage($"{_serverTag} Disposing test server."));

            HttpClient.Dispose();
            TestServer.Dispose();

            _messageSink.OnMessage(new DiagnosticMessage($"{_serverTag} Disposed test server."));

            return Task.CompletedTask;
        }
    }
}