using System.Collections.Immutable;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Anexia.BDP.CloudLog.Models
{
    public sealed record PushEventPayload
    {
        [JsonConstructor]
        public PushEventPayload(ImmutableDictionary<string, JsonNode>[] records)
        {
            Records = records;
        }

        [JsonPropertyName("records")] public ImmutableDictionary<string, JsonNode>[] Records { get; }

        public static implicit operator PushEventPayload(ImmutableDictionary<string, JsonNode>[] value) => new(value);
    }
}
