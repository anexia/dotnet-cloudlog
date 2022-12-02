dotnet-cloudlog
===============
[![Nuget](https://img.shields.io/nuget/v/Anexia.BDP.CloudLog)](https://www.nuget.org/packages/Anexia.BDP.CloudLog)
[![Test status](https://github.com/anexia/dotnet-cloudlog/actions/workflows/test.yml/badge.svg?branch=main)](https://github.com/anexia/dotnet-cloudlog/actions/workflows/test.yml)

`dotnet-cloudlog` is a client library for Anexia CloudLog. It provides a simple API for sending events to a CloudLog
index directly from your .NET application.

**Note:** Usually it is considered best-practice to write rotating log-files to the filesystem, and send those logs to
CloudLog via `Filebeat`.

# Install

With a correctly set up .NET SDK, run in `PowerShell`:

```powershell
Install-Package Anexia.BDP.CloudLog
```

# Getting started

## Example 1

To send unstructured messages to the CloudLog index, use the code as follows:

```cs
using System.Net.Http;
using Anexia.BDP.CloudLog;

…
var client = new Client("SomeIndex", "SomeToken");
var message = "Some message that you want to send to the CloudLog index";

client.PushEvent(message);
…
```

## Example 2

To send structured messages to the CloudLog index, use the code as follows:

```cs
using System.Net.Http;
using Anexia.BDP.CloudLog;

…
var client = new Client("SomeIndex", "SomeToken");
var message = "{\"message\":\"Something\",\"timestamp\":\"1669816693\"}"; // `timestamp` is a UNIX timestamp

client.PushEvent(message);
…
```

## Example 3

`HttpClient` may be passed to the constructor to allow for modification of the HTTP requests, as shown in the code
as follows:

```cs
using System.Net.Http;
using Anexia.BDP.CloudLog;

…
var client1 = new Client("SomeIndex", "SomeToken", new HttpClient());
var client2 = new Client("SomeIndex", "SomeToken", HttpFactory.Create());
…
```

## Example 4

The methods `PushEvent` and `PushEvents` return an awaitable `PostAsync` task, as shown in the code
as follows:

```cs
using System;
using System.Net.Http;
using Anexia.BDP.CloudLog;

…
var client = new Client("SomeIndex", "SomeToken");
var message = "Some message that you want to send to the CloudLog index";
var response = await client.PushEvent(message);

Console.WriteLine(response.StatusCode); // should print `201`
…
```

# Supported versions

|          | Supported |
|----------|-----------|
| .Net 5.0 | ✓         |
| .Net 6.0 | ✓         |
| .Net 7.0 | ✓         |
