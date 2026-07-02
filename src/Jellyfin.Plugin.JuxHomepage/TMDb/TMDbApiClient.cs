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

    /// <summary>Hard ceiling on pages fetched per call, regardless of the requested count.</summary>
    private const int MaxPagesPerFetch = 5;

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
    public Task<IReadOnlyList<TMDbMovie>> GetTrendingMoviesAsync(int pages, CancellationToken cancellationToken) =>
        GetPagedAsync<TMDbMovie>("trending/movie/week", pages, cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<TMDbShow>> GetTrendingShowsAsync(int pages, CancellationToken cancellationToken) =>
        GetPagedAsync<TMDbShow>("trending/tv/week", pages, cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<TMDbShow>> GetAiringTodayAsync(int pages, CancellationToken cancellationToken) =>
        GetPagedAsync<TMDbShow>("tv/airing_today", pages, cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<TMDbMovie>> GetUpcomingMoviesAsync(int pages, CancellationToken cancellationToken) =>
        GetPagedAsync<TMDbMovie>("movie/upcoming", pages, cancellationToken);

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

    /// <summary>
    /// Fetches and concatenates up to <paramref name="pages"/> pages (clamped to
    /// <see cref="MaxPagesPerFetch"/>) of a standard TMDb paged list response. Stops early if a page
    /// returns no results, which covers both "TMDb ran out of pages" and "the request failed"
    /// (<see cref="GetAsync{T}"/> already logs the failure in the latter case).
    /// </summary>
    private async Task<IReadOnlyList<T>> GetPagedAsync<T>(string basePath, int pages, CancellationToken cancellationToken)
    {
        var results = new List<T>();
        var clampedPages = Math.Clamp(pages, 1, MaxPagesPerFetch);

        for (var page = 1; page <= clampedPages; page++)
        {
            var separator = basePath.Contains('?', StringComparison.Ordinal) ? '&' : '?';
            var pagedPath = $"{basePath}{separator}page={page}";

            var pageResult = await GetAsync<TMDbTrending<T>>(pagedPath, cancellationToken).ConfigureAwait(false);
            if (pageResult is null || pageResult.Results.Count == 0)
            {
                break;
            }

            results.AddRange(pageResult.Results);
        }

        return results;
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
