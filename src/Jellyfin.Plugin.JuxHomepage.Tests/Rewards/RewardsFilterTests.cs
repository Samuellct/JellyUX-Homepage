using Jellyfin.Plugin.JuxHomepage.Rewards.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests.Rewards;

public sealed class RewardsFilterTests
{
    // -------------------------------------------------------------------------
    // Well-formed Q-ids -- accepted unchanged
    // -------------------------------------------------------------------------

    [Fact]
    public void FromExtraParams_ValidCategoryQid_IsAccepted()
    {
        var filter = RewardsFilter.FromExtraParams(new Dictionary<string, string>
        {
            ["categoryQid"] = "Q102427"
        });

        Assert.Equal("Q102427", filter.CategoryQid);
    }

    [Fact]
    public void FromExtraParams_ValidCeremonyQid_IsAccepted()
    {
        var filter = RewardsFilter.FromExtraParams(new Dictionary<string, string>
        {
            ["ceremonyQid"] = "Q19020"
        });

        Assert.Equal("Q19020", filter.CeremonyQid);
    }

    // -------------------------------------------------------------------------
    // Malformed Q-ids -- ignored, never reach WikidataApiClient.BuildAwardWinnersQuery,
    // and a warning is logged so an admin-supplied injection attempt is traceable.
    // -------------------------------------------------------------------------

    [Fact]
    public void FromExtraParams_MalformedCategoryQid_IsIgnoredAndLogged()
    {
        var loggerMock = new Mock<ILogger>();

        var filter = RewardsFilter.FromExtraParams(
            new Dictionary<string, string>
            {
                ["categoryQid"] = "Q102427 . } SERVICE <http://127.0.0.1:9999/> { ?s ?p ?o } #"
            },
            loggerMock.Object,
            instanceId: "test-instance");

        Assert.Null(filter.CategoryQid);
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void FromExtraParams_MalformedCeremonyQid_IsIgnoredAndLogged()
    {
        var loggerMock = new Mock<ILogger>();

        var filter = RewardsFilter.FromExtraParams(
            new Dictionary<string, string>
            {
                ["ceremonyQid"] = "Q19020 . } SERVICE <http://127.0.0.1:9999/> { ?s ?p ?o } #"
            },
            loggerMock.Object,
            instanceId: "test-instance");

        Assert.Null(filter.CeremonyQid);
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void FromExtraParams_QidWithoutLeadingQ_IsIgnored()
    {
        // Confirms the pattern is anchored (^Q[1-9]\d*$), not just "contains digits".
        var filter = RewardsFilter.FromExtraParams(new Dictionary<string, string>
        {
            ["categoryQid"] = "102427"
        });

        Assert.Null(filter.CategoryQid);
    }

    // -------------------------------------------------------------------------
    // Year parsing is independent of Q-id validation
    // -------------------------------------------------------------------------

    [Fact]
    public void FromExtraParams_ValidYearAlongsideRejectedQid_YearStillParsed()
    {
        var filter = RewardsFilter.FromExtraParams(new Dictionary<string, string>
        {
            ["categoryQid"] = "not-a-qid",
            ["year"] = "2024"
        });

        Assert.Null(filter.CategoryQid);
        Assert.Equal(2024, filter.Year);
    }
}
