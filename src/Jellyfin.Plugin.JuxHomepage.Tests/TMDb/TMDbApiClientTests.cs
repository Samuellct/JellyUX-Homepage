using System.Net;
using System.Net.Http.Json;
using Jellyfin.Plugin.JuxHomepage.Configuration;
using Jellyfin.Plugin.JuxHomepage.TMDb;
using Jellyfin.Plugin.JuxHomepage.TMDb.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests.TMDb;

public sealed class TMDbApiClientTests
{
    private const string V3Key = "abcdef0123456789abcdef0123456789";

    // -------------------------------------------------------------------------
    // Successful call / deserialization
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetTrendingMoviesAsync_SuccessfulResponse_DeserializesResults()
    {
        var handler = new StubHttpMessageHandler(() => JsonResponse(new TMDbTrending<TMDbMovie>
        {
            Page = 1,
            Results = [new TMDbMovie { Id = 27205, Title = "Inception" }],
            TotalPages = 1,
            TotalResults = 1
        }));

        var client = BuildClient(handler, V3Key);

        var result = await client.GetTrendingMoviesAsync(CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(27205, result[0].Id);
        Assert.Equal("Inception", result[0].Title);
        Assert.Equal(1, handler.CallCount);
    }

    // -------------------------------------------------------------------------
    // Missing key
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetTrendingMoviesAsync_NoApiKeyConfigured_ReturnsEmptyWithoutHttpCall()
    {
        var handler = new StubHttpMessageHandler(() => JsonResponse(new TMDbTrending<TMDbMovie>()));
        var client = BuildClient(handler, apiKey: null);

        var result = await client.GetTrendingMoviesAsync(CancellationToken.None);

        Assert.Empty(result);
        Assert.Equal(0, handler.CallCount);
    }

    // -------------------------------------------------------------------------
    // Invalid key (HTTP 401)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetTrendingMoviesAsync_Unauthorized_ReturnsEmptyWithoutRetry()
    {
        var handler = new StubHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = BuildClient(handler, V3Key);

        var result = await client.GetTrendingMoviesAsync(CancellationToken.None);

        Assert.Empty(result);
        Assert.Equal(1, handler.CallCount);
    }

    // -------------------------------------------------------------------------
    // Network failure retry
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetTrendingMoviesAsync_TransientNetworkFailureThenSuccess_RetriesOnceAndSucceeds()
    {
        var handler = new StubHttpMessageHandler(
            () => throw new HttpRequestException("network down"),
            () => JsonResponse(new TMDbTrending<TMDbMovie>
            {
                Results = [new TMDbMovie { Id = 1, Title = "Recovered" }]
            }));

        var client = BuildClient(handler, V3Key);

        var result = await client.GetTrendingMoviesAsync(CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Recovered", result[0].Title);
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task GetTrendingMoviesAsync_TwoConsecutiveNetworkFailures_ReturnsEmptyWithoutThrowing()
    {
        var handler = new StubHttpMessageHandler(
            () => throw new HttpRequestException("network down"),
            () => throw new HttpRequestException("still down"));

        var client = BuildClient(handler, V3Key);

        var result = await client.GetTrendingMoviesAsync(CancellationToken.None);

        Assert.Empty(result);
        Assert.Equal(2, handler.CallCount);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static TMDbApiClient BuildClient(StubHttpMessageHandler handler, string? apiKey)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.themoviedb.org/3/") };

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient("TMDb")).Returns(httpClient);

        return new TMDbApiClient(
            factoryMock.Object,
            () => new PluginConfiguration { ApiKeys = new ApiKeysConfig { TMDb = apiKey } },
            NullLogger<TMDbApiClient>.Instance);
    }

    private static HttpResponseMessage JsonResponse<T>(T payload) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(payload) };

    // Hand-written HttpMessageHandler stub (no Moq.Protected dependency needed): each call to
    // SendAsync dequeues and invokes the next responder, allowing a responder to either return a
    // response or throw (e.g. HttpRequestException) to simulate a network failure.
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpResponseMessage>> _responses;

        public StubHttpMessageHandler(params Func<HttpResponseMessage>[] responses)
        {
            _responses = new Queue<Func<HttpResponseMessage>>(responses);
        }

        public int CallCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            await Task.Yield();
            var responder = _responses.Dequeue();
            return responder();
        }
    }
}
