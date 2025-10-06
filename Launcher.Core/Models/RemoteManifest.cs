using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Launcher.Core.Models;

public class RemoteManifest
{
    // Version of the remote release, e.g., "1.2.3"
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    // Optional timestamp when the manifest was generated
    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }

    // Release notes or description
    [JsonPropertyName("releaseNotes")]
    public string? ReleaseNotes { get; set; }

    // Files included in the release
    [JsonPropertyName("files")]
    public List<ManifestFile>? Files { get; set; }
}
