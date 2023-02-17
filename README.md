# codenow-net-tracing
* NET 5.0, NetCoreApp 3.1
* C# 9.0

# build package

`dotnet pack -c Release -o Packages`


# changelog
**2.1.0**
- Use OpenTelemetry Http instrumentation to pass activity context into Http Client calls, instead of using header propagation 

**2.0.0**
- Add reference to [OpenTelemetry](https://opentelemetry.io/) (pre-release 1.0.0-rc9)
- Mark `AddCodeNowHeaderPropagation` as obsolete and replace with `AddCodeNowTracing`
- `AddCodeNowTracing` now adds OpenTelemetry's filling of `System.Diagnostics.Activity` from [W3C Trace Context](https://www.w3.org/TR/trace-context/) and [Zipkin's B3 Propagation](https://github.com/openzipkin/b3-propagation). 
 
**1.0.1**
- fix typo in methods name

**1.0.0**
- add tracing middleware to propagate request tracing headers into response
- add extension to register the middleware
- add estension to register header propagation to the http client