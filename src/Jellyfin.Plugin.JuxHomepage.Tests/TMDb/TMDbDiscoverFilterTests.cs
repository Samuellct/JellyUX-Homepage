using Jellyfin.Plugin.JuxHomepage.TMDb.Models;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests.TMDb;

public sealed class TMDbDiscoverFilterTests
{
    [Fact]
    public void FromExtraParams_EmptyDictionary_UsesDefaults()
    {
        var filter = TMDbDiscoverFilter.FromExtraParams(new Dictionary<string, string>());

        Assert.Null(filter.GenreIds);
        Assert.Null(filter.PersonIds);
        Assert.Null(filter.KeywordIds);
        Assert.Null(filter.CompanyIds);
        Assert.Equal("popularity.desc", filter.SortBy);
        Assert.Null(filter.PrimaryReleaseYear);
        Assert.Null(filter.VoteAverageGte);
        Assert.Equal(50, filter.VoteCountGte);
        Assert.Equal(1, filter.Pages);
    }

    [Fact]
    public void FromExtraParams_AllKeysPresent_MapsEveryField()
    {
        var extraParams = new Dictionary<string, string>
        {
            ["genreIds"] = "28,12",
            ["personIds"] = "6193",
            ["keywordIds"] = "818,1234",
            ["companyIds"] = "420",
            ["sortBy"] = "vote_average.desc",
            ["year"] = "2020",
            ["minRating"] = "7.5",
            ["minVotes"] = "100",
            ["pages"] = "3"
        };

        var filter = TMDbDiscoverFilter.FromExtraParams(extraParams);

        Assert.Equal([28, 12], filter.GenreIds);
        Assert.Equal([6193], filter.PersonIds);
        Assert.Equal([818, 1234], filter.KeywordIds);
        Assert.Equal([420], filter.CompanyIds);
        Assert.Equal("vote_average.desc", filter.SortBy);
        Assert.Equal(2020, filter.PrimaryReleaseYear);
        Assert.Equal(7.5, filter.VoteAverageGte);
        Assert.Equal(100, filter.VoteCountGte);
        Assert.Equal(3, filter.Pages);
    }

    [Fact]
    public void FromExtraParams_PagesAboveMax_ClampedToFive()
    {
        var filter = TMDbDiscoverFilter.FromExtraParams(new Dictionary<string, string> { ["pages"] = "999" });

        Assert.Equal(5, filter.Pages);
    }

    [Fact]
    public void FromExtraParams_MalformedNumericValue_FallsBackToDefault()
    {
        var filter = TMDbDiscoverFilter.FromExtraParams(new Dictionary<string, string> { ["year"] = "not-a-number" });

        Assert.Null(filter.PrimaryReleaseYear);
    }
}
