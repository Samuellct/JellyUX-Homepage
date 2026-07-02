using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JuxHomepage.TMDb.Models;

/// <summary>
/// A country as returned by TMDb's <c>/configuration/countries</c> endpoint. Used to populate the
/// region dropdown for the "Now Playing" widget.
/// </summary>
public sealed class TMDbCountry
{
    /// <summary>Gets or sets the ISO 3166-1 country code (e.g. "US", "FR").</summary>
    [JsonPropertyName("iso_3166_1")]
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the English name of the country.</summary>
    [JsonPropertyName("english_name")]
    public string Name { get; set; } = string.Empty;
}
