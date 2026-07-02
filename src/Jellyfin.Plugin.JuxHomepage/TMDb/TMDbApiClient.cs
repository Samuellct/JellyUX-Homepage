using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.JuxHomepage.Configuration;
using Jellyfin.Plugin.JuxHomepage.TMDb.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JuxHomepage.TMDb;

/// <summary>
/// HTTP client for the TMDb (The Movie Database) API v3.
/// <para>
/// Supports both TMDb key formats: a 32-character v3 API key (sent as an <c>api_key</c> query
/// parameter) and a v4 read-access token (a JWT-like string starting with "ey", sent as an
/// <c>Authorization: Bearer</c> header). The same regexes used to validate the key in
/// <c>config.html</c> decide which format is in use.
/// </para>
/// <para>
/// Every public method degrades gracefully: a missing key results in a silent skip (debug log
/// only), an invalid key (HTTP 401) is logged as an explicit error, and a network failure is
/// retried once before being logged as an error. No public method throws.
/// </para>
/// </summary>
public sealed partial class TMDbApiClient : ITMDbApiClient
{
    private const string HttpClientName = "TMDb";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Func<PluginConfiguration?> _getConfiguration;
    private readonly ILogger<TMDbApiClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TMDbApiClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Factory used to create the named "TMDb" HTTP client.</param>
    /// <param name="getConfiguration">
    /// Factory that returns the current plugin configuration, used to read the live API key.
    /// Defaults to <c>Plugin.Instance?.Configuration</c> in production.
    /// </param>
    /// <param name="logger">Logger.</param>
    public TMDbApiClient(
        IHttpClientFactory httpClientFactory,
        Func<PluginConfiguration?> getConfiguration,
        ILogger<TMDbApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _getConfiguration = getConfiguration;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TMDbMovie>> GetTrendingMoviesAsync(CancellationToken cancellationToken)
    {
        var result = await GetAsync<TMDbTrending<TMDbMovie>>("trending/movie/week", cancellationToken)
            .ConfigureAwait(false);
        return result?.Results ?? [];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TMDbShow>> GetTrendingShowsAsync(CancellationToken cancellationToken)
    {
        var result = await GetAsync<TMDbTrending<TMDbShow>>("trending/tv/week", cancellationToken)
            .ConfigureAwait(false);
        return result?.Results ?? [];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TMDbShow>> GetAiringTodayAsync(CancellationToken cancellationToken)
    {
        var result = await GetAsync<TMDbTrending<TMDbShow>>("tv/airing_today", cancellationToken)
            .ConfigureAwait(false);
        return result?.Results ?? [];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TMDbMovie>> GetUpcomingMoviesAsync(CancellationToken cancellationToken)
    {
        var result = await GetAsync<TMDbTrending<TMDbMovie>>("movie/upcoming", cancellationToken)
            .ConfigureAwait(false);
        return result?.Results ?? [];
    }

    /// <inheritdoc/>
    public async Task<string?> GetMovieExternalIdsAsync(int tmdbId, CancellationToken cancellationToken)
    {
        var result = await GetAsync<TMDbExternalIds>($"movie/{tmdbId}/external_ids", cancellationToken)
            .ConfigureAwait(false);
        return result?.ImdbId;
    }

    /// <inheritdoc/>
    public async Task<string?> GetShowExternalIdsAsync(int tmdbId, CancellationToken cancellationToken)
    {
        var result = await GetAsync<TMDbExternalIds>($"tv/{tmdbId}/external_ids", cancellationToken)
            .ConfigureAwait(false);
        return result?.ImdbId;
    }

    private async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken)
        where T : class
    {
        var apiKey = _getConfiguration()?.ApiKeys?.TMDb;
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogDebug("TMDb API key not configured; skipping request to '{Path}'.", path);
            return null;
        }

        var isV4Token = V4TokenPattern().IsMatch(apiKey);
        var client = _httpClientFactory.CreateClient(HttpClientName);

        var requestUri = isV4Token
            ? path
            : path + (path.Contains('?', StringComparison.Ordinal) ? '&' : '?') + "api_key=" + Uri.EscapeDataString(apiKey);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
                if (isV4Token)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }

                using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogError(
                        "TMDb rejected the configured API key (HTTP 401) for request to '{Path}'.",
                        path);
                    return null;
                }

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<T>(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                if (attempt == 0)
                {
                    _logger.LogWarning(ex, "TMDb request to '{Path}' failed; retrying once.", path);
                    continue;
                }

                _logger.LogError(ex, "TMDb request to '{Path}' failed after retry.", path);
                return null;
            }
        }

        return null;
    }

    [GeneratedRegex(@"^ey[\w-]+\.[\w-]+\.[\w-]+$")]
    private static partial Regex V4TokenPattern();
}
