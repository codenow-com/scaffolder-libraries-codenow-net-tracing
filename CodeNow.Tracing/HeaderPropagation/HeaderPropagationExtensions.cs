using System;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HeaderPropagation;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;

namespace CodeNow.Tracing.HeaderPropagation
{
    /// <summary>
    /// Methods for configuring CodeNow tracing.
    /// </summary>
    public static class HeaderPropagationExtensions
    {
        /// <summary>
        /// Adds services required for propagating headers to a <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        [Obsolete("Use AddCodeNowTracing instead.")]
        public static IServiceCollection AddCodeNowHeaderPropagation(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddHeaderPropagation(o =>
            {
                o.Headers.Add("x-request-id");
                o.Headers.Add("x-b3-traceid");
                o.Headers.Add("x-b3-spanid");
                o.Headers.Add("x-b3-parentspanid");
                o.Headers.Add("x-b3-sampled");
                o.Headers.Add("x-b3-flags");
                o.Headers.Add("x-ot-span-context");
            });

            return services;
        }

        /// <summary>
        /// Adds services required for activity tracing within CodeNow infrastructure.
        /// </summary>
        /// <remarks>
        /// Sets up header propagation to a <see cref="HttpClient"/>.
        /// Uses <a href="https://opentelemetry.io/">OpenTelemetry</a> library to fill <see cref="System.Diagnostics.Activity"/> from tracing headers. Supports <a href="https://www.w3.org/TR/trace-context/">W3C Trace Context</a> and <a href="https://github.com/openzipkin/b3-propagation">Zipkin's B3 Propagation</a>.
        /// </remarks>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="traceConfiguration">Method for configuring OpenTelemetry's trace provider.</param>
        public static IServiceCollection AddCodeNowTracing(
            this IServiceCollection services,
            Action<TracerProviderBuilder>? traceConfiguration = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // keep "x-request-id" header, as it is not part of any tracing scheme
            services.AddHeaderPropagation(o =>
            {
                o.Headers.Add("x-request-id");
            });
            AddOpenTelemetryTracing(services, traceConfiguration ?? (_ => {}) );

            return services;
        }

        private static void AddOpenTelemetryTracing(IServiceCollection services, Action<TracerProviderBuilder> traceConfiguration)
        {
            OpenTelemetry.Sdk.SetDefaultTextMapPropagator(
                new CompositeTextMapPropagator(
                    new TextMapPropagator[]
                    {
                        new TraceContextPropagator(),
                        new B3Propagator(),
                        new BaggagePropagator()
                    }));
            services.AddOpenTelemetryTracing(builder =>
            {
                builder.AddAspNetCoreInstrumentation();
                builder.AddHttpClientInstrumentation();
                traceConfiguration(builder);
            });
        }

        /// <summary>
        /// Add middleware that collect headers to be propagated to a <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static IApplicationBuilder UseCodeNowHeaderPropagation(this IApplicationBuilder app)
        {
            app.UseHeaderPropagation();
            return app;
        }
        
         /// <summary>
        /// Adds a message handler for propagating headers collected by the <see cref="HeaderPropagationMiddleware"/> to a outgoing request.
        /// </summary>
        /// <remarks>
        /// When using this method, all the configured headers will be applied to the outgoing HTTP requests.
        /// </remarks>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/> to add the message handler to.</param>
        /// <returns>The <see cref="IHttpClientBuilder"/> so that additional calls can be chained.</returns>
        public static IHttpClientBuilder AddCodeNowHeaderPropagation(this IHttpClientBuilder builder) 
        {
            builder.AddHeaderPropagation();
            return builder;
        }

        /// <summary>
        /// Adds a message handler for propagating headers collected by the <see cref="HeaderPropagationMiddleware"/> to a outgoing request,
        /// explicitly specifying which headers to propagate.
        /// </summary>
        /// <remarks>This also allows to redefine the name to use for a header in the outgoing request.</remarks>
        /// <param name="builder">The <see cref="IHttpClientBuilder"/> to add the message handler to.</param>
        /// <param name="configure">A delegate used to configure the <see cref="HeaderPropagationMessageHandlerOptions"/>.</param>
        /// <returns>The <see cref="IHttpClientBuilder"/> so that additional calls can be chained.</returns>
        public static IHttpClientBuilder AddCodeNowHeaderPropagation(this IHttpClientBuilder builder, Action<HeaderPropagationMessageHandlerOptions> configure)
        {
            builder.AddHeaderPropagation(configure);
            return builder;
        }
    }
}