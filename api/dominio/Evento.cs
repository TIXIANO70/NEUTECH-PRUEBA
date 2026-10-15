
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Neutech.Api.Dominio
{
    public class Evento
    {
        [JsonPropertyName("event_code")]
        public string EventCode { get; set; } = "";

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";

        [JsonPropertyName("related_record_ids")]
        public List<string> RelatedRecordIds { get; set; } = new List<string>();
    }
}
