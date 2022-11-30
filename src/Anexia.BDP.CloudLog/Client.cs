using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net.Http;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Anexia.BDP.CloudLog.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Anexia.BDP.CloudLog
{
    /// <summary>
    ///     Implements a CloudLog client
    /// </summary>
    public class Client
    {
        private const string api = "https://api0401.bdp.anexia-it.com";

        private static JsonSerializerOptions _jsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly string _index;
        private readonly string _token;
        private readonly bool _isHttp;
        private long queue = 0;

        private string ClientType;

        private HttpClient httpClient;

        private readonly Func<long> createTimeStamp =
            () => (long)DateTime.Now.Subtract(DateTime.MinValue.AddYears(1969)).TotalMilliseconds;

        /// <summary>
        ///     Creates a new client instance (http)
        /// </summary>
        /// <param name="index">
        ///     Index name
        /// </param>
        /// <param name="token">
        ///     Authentication token
        /// </param>
        /// <param name="client">
        ///     Client for .net managed client capabilities
        /// </param>
        public Client(string index, string token, HttpClient client)
        {
            _index = index;
            _token = token;
            _isHttp = true;
            ClientType = "dotnet-client-http";
            CheckHttpConfiguration();

            httpClient = client;
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
        }

        /// <summary>
        ///     Creates a new client instance (http)
        /// </summary>
        /// <param name="index">
        ///     Index name
        /// </param>
        /// <param name="token">
        ///     Authentication token
        /// </param>
        public Client(string index, string token)
            : this(index, token, new HttpClient())
        {
        }

        /// <summary>
        ///     Set custom client type name
        /// </summary>
        /// <param name="clientType">
        ///     Client type
        /// </param>
        public void SetClientType(string clientType)
        {
            ClientType = clientType;
        }

        /// <summary>
        ///     Push events to CloudLog
        /// </summary>
        /// <param name="events">
        ///     Events
        /// </param>
        /// <returns>Task of post async function</returns>
        public Task PushEvents(string[] events)
        {
            long timestamp = createTimeStamp();
            ImmutableDictionary<string, JsonNode>[] result =
                new ImmutableDictionary<string, JsonNode>[events.Length];
            for (var i = 0; i < events.Length; i++)
            {
                ref var eventObj = ref events[i];
                result[i] = AddMetadata(ref eventObj, ref timestamp);
            }

            return SendOverHttp(result);
        }

        /// <summary>
        ///     Flush events
        /// </summary>
        public void Flush()
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (queue > 0)
            {
                if (sw.ElapsedMilliseconds > 5000)
                {
                    queue = 0;
                }
            }

            sw.Stop();
        }

        /// <summary>
        ///     Sends an event to CloudLog
        /// </summary>
        /// <param name="evt">
        ///     Event
        /// </param>
        /// <returns>Task of post async function</returns>
        public Task PushEvent(string evt)
        {
            long timestamp = createTimeStamp();
            return SendOverHttp(new[] { AddMetadata(ref evt, ref timestamp) });
        }

        /// <summary>
        ///     Send events to http api
        /// </summary>
        /// <param name="events">
        ///     Events
        /// </param>
        /// <returns>Task of post async function</returns>
        private Task SendOverHttp(PushEventPayload events)
        {
            var content = new StringContent(JsonSerializer.Serialize(events), Encoding.UTF8, "application/json");
            queue++;
            return httpClient.PostAsync(api + "/v1/index/" + _index + "/data", content).ContinueWith(task =>
            {
                queue--;
                if (task.Result.StatusCode != System.Net.HttpStatusCode.Created &&
                    task.Result.StatusCode != System.Net.HttpStatusCode.Accepted)
                {
                    Console.Error.WriteLine("Failed send message with status code: " + task.Result.StatusCode);
                }
            });
        }

        /// <summary>
        ///     Parse and add meta data to event
        /// </summary>
        /// <param name="evt">
        ///     Event
        /// </param>
        /// <param name="timeStamp">
        ///     Timestamp
        /// </param>
        private ImmutableDictionary<string, JsonNode> AddMetadata(ref string evt, ref long timeStamp)
        {
            JsonObject data = null;
            if (!string.IsNullOrEmpty(evt))
            {
                try
                {
                    data = JsonSerializer.Deserialize<JsonObject>(evt, _jsonSerializerOptions)!;
                    data.Remove("cloudlog_client_type");
                    data.Remove("cloudlog_source_host");
                }
                catch
                {
                    // ignored
                }
            }

            if (data == null)
            {
                data = new JsonObject();
                data.TryAdd("message", evt);
            }

            data.TryAdd("timestamp", timeStamp);
            data.TryAdd("cloudlog_client_type", ClientType);
            data.TryAdd("cloudlog_source_host", Environment.MachineName);

            return data.ToImmutableDictionary();
        }

        /// <summary>
        ///     Check http configuration
        /// </summary>
        private void CheckHttpConfiguration()
        {
            if (String.IsNullOrEmpty(_index))
            {
                throw new ArgumentException("mssing index");
            }

            if (String.IsNullOrEmpty(_token))
            {
                throw new ArgumentException("mssing token");
            }
        }
    }
}
