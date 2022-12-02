using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Anexia.BDP.CloudLog.Models;
using Moq;
using Moq.Protected;
using Xunit;

namespace Anexia.BDP.CloudLog.Test
{
    [Trait("CloudLog", "Client")]
    public class ClientTest
    {
        private const string TestContent = "test content";

        private Client CreateClient(Action<HttpRequestMessage, CancellationToken> callbackFunc)
        {
            var moqHttpMessageHandler = new Mock<HttpMessageHandler>();
            moqHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback(callbackFunc)
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(TestContent)
                });
            return new Client("SomeIndex", "SomeToken", new HttpClient(moqHttpMessageHandler.Object));
        }

        [Theory]
        [InlineData("Something", 4)]
        [InlineData(
            "{\"message\":\"Something\",\"timestamp\":\"1669816693\", \"cloudlog_client_type\":\"SomeType\", \"cloudlog_source_host\": \"SomeHost\"}",
            4)]
        [InlineData(
            "{\"message\":\"Something\", \"cloudlog_client_type\":\"SomeType\", \"cloudlog_source_host\": \"SomeHost\"}",
            4)]
        [InlineData(
            "{\"message\":\"Something\", \"cloudlog_client_type\":\"SomeType\", \"cloudlog_source_host\": \"SomeHost\", \"some_other_data\": \"SomeExtensiveData\", \"some_more_numbers\": 1541864}",
            6)]
        public async Task PushEvent(string eventString, int expectedKeyCount)
        {
            var result = default(PushEventPayload);
            var clientMoq = CreateClient((msg, cancel) =>
            {
                result = msg.Content.ReadFromJsonAsync<PushEventPayload>().Result;
            });
            await clientMoq.PushEvent(eventString);
            Assert.Contains("message", result.Records[0].Keys);
            Assert.Contains("timestamp", result.Records[0].Keys);
            Assert.Contains("cloudlog_client_type", result.Records[0].Keys);
            Assert.Contains("cloudlog_source_host", result.Records[0].Keys);
            Assert.Equal(expectedKeyCount, result.Records[0].Keys.Count());
        }

        [Theory]
        [InlineData("Something", 4)]
        [InlineData(
            "{\"message\":\"Something\",\"timestamp\":\"1669816693\", \"cloudlog_client_type\":\"SomeType\", \"cloudlog_source_host\": \"SomeHost\"}",
            4)]
        [InlineData(
            "{\"message\":\"Something\", \"cloudlog_client_type\":\"SomeType\", \"cloudlog_source_host\": \"SomeHost\"}",
            4)]
        [InlineData(
            "{\"message\":\"Something\", \"cloudlog_client_type\":\"SomeType\", \"cloudlog_source_host\": \"SomeHost\", \"some_other_data\": \"SomeExtensiveData\", \"some_more_numbers\": 1541864}",
            6)]
        public async Task PushEvents(string eventString, int expectedKeyCount)
        {
            var result = default(PushEventPayload);
            var clientMoq = CreateClient((msg, cancel) =>
            {
                result = msg.Content.ReadFromJsonAsync<PushEventPayload>().Result;
            });
            await clientMoq.PushEvents(new[] { eventString });
            Assert.Contains("message", result.Records[0].Keys);
            Assert.Contains("timestamp", result.Records[0].Keys);
            Assert.Contains("cloudlog_client_type", result.Records[0].Keys);
            Assert.Contains("cloudlog_source_host", result.Records[0].Keys);
            Assert.Equal(expectedKeyCount, result.Records[0].Keys.Count());
        }
    }
}
