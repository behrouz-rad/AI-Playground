using System.Text.Json.Serialization;

namespace AiPlayground.Test.Helpers;

internal sealed record AiResponse
{
    [JsonPropertyName("response")]
    public string Response { get; set; } = "";
}
