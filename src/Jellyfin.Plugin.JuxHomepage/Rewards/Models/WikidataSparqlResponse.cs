using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.JuxHomepage.Rewards.Models;

/// <summary>
/// Root envelope of the standard SPARQL 1.1 JSON results format returned by
/// <c>https://query.wikidata.org/sparql</c> (with <c>Accept: application/sparql-results+json</c>).
/// </summary>
public sealed class WikidataSparqlResponse
{
    /// <summary>Gets or sets the query results.</summary>
    [JsonPropertyName("results")]
    public WikidataSparqlResults Results { get; set; } = new();
}

/// <summary>The "results" section of a SPARQL JSON response.</summary>
public sealed class WikidataSparqlResults
{
    /// <summary>Gets or sets each result row, keyed by the SPARQL variable name (e.g. "film", "imdbId").</summary>
    [JsonPropertyName("bindings")]
    public IReadOnlyList<Dictionary<string, WikidataSparqlBinding>> Bindings { get; set; } = [];
}

/// <summary>A single bound value for one SPARQL variable in one result row.</summary>
public sealed class WikidataSparqlBinding
{
    /// <summary>Gets or sets the binding type ("uri", "literal", etc.). Not used directly; <see cref="Value"/> is read as-is.</summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the bound value (an entity URI for "uri" bindings, plain text for "literal" bindings).</summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}
