using System.Text.Json.Serialization;

namespace Launcher.Core.Models;

public class ManifestFile
{
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("sha256")]
    public string? Sha256 { get; set; }

    [JsonPropertyName("size")]
    public long? Size { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
