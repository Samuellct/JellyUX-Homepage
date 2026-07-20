using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Jellyfin.Plugin.JuxHomepage.Rewards;
using Jellyfin.Plugin.JuxHomepage.Rewards.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests.Rewards;

// TODO_V3.md Phase 3.7 (mirrors TMDbContractTests.cs, TODO_V2.md Phase 15.2): guards against an
// undetected Wikidata SPARQL response schema drift by replaying a realistic, complete
// query.wikidata.org response through the real WikidataApiClient (literal JSON text via
// StringContent, not a hand-built C# object round-tripped through JsonContent.Create as
// WikidataApiClientTests.cs's other tests do).
public sealed class WikidataContractTests
{
    // Fixture based on the standard SPARQL 1.1 Query Results JSON Format
    // (https://www.w3.org/TR/sparql11-results-json/), matching the shape query.wikidata.org actually
    // returns for the SELECT built by WikidataApiClient.BuildAwardWinnersQuery -- not a live-captured
    // response (see TODO_V2.md Phase 15.2 precedent for the same limitation on the TMDb contract
    // test). Includes, deliberately, the "type" field on every binding and a "head"/"vars" envelope
    // that WikidataSparqlResponse does not map at all -- the point of this test is to confirm
    // System.Text.Json quietly ignores everything this plugin doesn't read, rather than throwing.
    private const string SparqlResponseFixture = """
        {
          "head": {
            "vars": ["film", "filmLabel", "awardLabel", "pointInTime", "imdbId"]
          },
          "results": {
            "bindings": [
              {
                "film": { "type": "uri", "value": "http://www.wikidata.org/entity/Q1041981" },
                "filmLabel": { "xml:lang": "en", "type": "literal", "value": "Oppenheimer" },
                "awardLabel": { "xml:lang": "en", "type": "literal", "value": "Academy Award for Best Picture" },
                "pointInTime": { "datatype": "http://www.w3.org/2001/XMLSchema#dateTime", "type": "literal", "value": "2024-03-10T00:00:00Z" },
                "imdbId": { "type": "literal", "value": "tt15398776" }
              }
            ]
          }
        }
        """;

    [Fact]
    public async Task GetAwardWinnersAsync_RealSparqlResponseShape_DeserializesKnownFieldsAndIgnoresExtras()
    {
        var handler = new StubHttpMessageHandler(SparqlResponseFixture);
        var client = BuildClient(handler);

        var result = await client.GetAwardWinnersAsync(
            new RewardsFilter { CategoryQid = "Q102427" },
            CancellationToken.None);

        var winner = Assert.Single(result);
        Assert.Equal(1041981, winner.Id);
        Assert.Equal("Q1041981", winner.FilmQid);
        Assert.Equal("Oppenheimer", winner.FilmLabel);
        Assert.Equal("Academy Award for Best Picture", winner.AwardLabel);
        Assert.Equal("tt15398776", winner.ImdbId);
        Assert.Equal(2024, winner.PointInTimeYear);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static WikidataApiClient BuildClient(StubHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient("Wikidata")).Returns(httpClient);

        return new WikidataApiClient(factoryMock.Object, NullLogger<WikidataApiClient>.Instance);
    }

    // Hand-written HttpMessageHandler stub, mirroring TMDbContractTests.StubHttpMessageHandler, but
    // returning literal JSON text (via StringContent) rather than a re-serialized C# object -- this
    // test is specifically about replaying real response *text*, not a round-tripped fixture.
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responseJson;

        public StubHttpMessageHandler(string responseJson)
        {
            _responseJson = responseJson;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Yield();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson, Encoding.UTF8)
                {
                    Headers = { ContentType = new MediaTypeHeaderValue("application/sparql-results+json") }
                }
            };
        }
    }
}
