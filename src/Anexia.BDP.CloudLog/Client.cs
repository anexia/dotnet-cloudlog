using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net.Http;
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
    public class Client : IDisposable
    {
        private const string Api = "https://api0401.bdp.anexia-it.com";

        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly string _index;
        private readonly string _token;

        private string _clientType;

        private HttpClient _httpClient;

        private readonly Func<long> _createTimeStamp =
            () => (long)DateTime.Now.Subtract(DateTime.MinValue.AddYears(1969)).TotalMilliseconds;

        /// <summary>
        ///     Creates a new client instance
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
            _clientType = "dotnet-client-http";
            CheckHttpConfiguration();

            _httpClient = client;
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
        }

        /// <summary>
        ///     Creates a new client instance
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
            _clientType = clientType;
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
            var timestamp = _createTimeStamp();
            var result =
                new ImmutableDictionary<string, JsonNode>[events.Length];
            for (var i = 0; i < events.Length; i++)
            {
                ref var eventObj = ref events[i];
                result[i] = AddMetadata(ref eventObj, ref timestamp);
            }

            return SendOverHttp(result);
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
            var timestamp = _createTimeStamp();
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
            return _httpClient.PostAsync(Api + "/v1/index/" + _index + "/data", content).ContinueWith(task =>
            {
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
                    data = JsonSerializer.Deserialize<JsonObject>(evt, JsonSerializerOptions)!;
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
            data.TryAdd("cloudlog_client_type", _clientType);
            data.TryAdd("cloudlog_source_host", Environment.MachineName);

            return data.ToImmutableDictionary();
        }

        /// <summary>
        ///     Check http configuration
        /// </summary>
        private void CheckHttpConfiguration()
        {
            if (string.IsNullOrEmpty(_index))
            {
                throw new ArgumentException("index is missing");
            }

            if (string.IsNullOrEmpty(_token))
            {
                throw new ArgumentException("token is missing");
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
