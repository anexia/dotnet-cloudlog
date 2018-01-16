# dotnet-cloudlog
dontnet-cloudlog is a client library for Anexia CloudLog.

Currently it only provides to push events to CloudLog. Querying is possible in a future release.

There are two connection types:

directly connected for high throughput
http client

### Getting started

To use the console sink, first install the [NuGet package](https://nuget.org/packages/Serilog.Sinks.CloudLog):

```powershell
Install-Package Serilog.Sinks.CloudLog
```

Then enable the sink using `WriteTo.CloudLog()`:



```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.CloudLog(index: "index-name", caFile: "C:/private/cloudlog-ca.pem", caFile: "C:/private/cloudlog-client.pem", caFile: "C:/private/cloudlog-client.key")
    .CreateLogger();
    
Log.Information("Hello CloudLog!");
```

Alternative CloudLog HTTP Client

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.CloudLog(index: "index-name", token: "token")
    .CreateLogger();
    
Log.Information("Hello CloudLog!");
```
