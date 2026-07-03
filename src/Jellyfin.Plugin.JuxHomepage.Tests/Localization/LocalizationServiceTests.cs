using Jellyfin.Plugin.JuxHomepage.Localization;
using Xunit;

namespace Jellyfin.Plugin.JuxHomepage.Tests.Localization;

public sealed class LocalizationServiceTests
{
    private static LocalizationService BuildService() => new(
        new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["fr"] = new Dictionary<string, string>
            {
                ["jux.native.my-media"] = "Mes médias",
                ["jux.personalized.favorite-genre.format"] = "Encore du {value}"
            },
            ["en"] = new Dictionary<string, string>
            {
                ["jux.native.my-media"] = "My Media",
                ["jux.native.next-up"] = "Next Up",
                ["jux.personalized.favorite-genre.format"] = "More {value}"
            }
        });

    [Fact]
    public void Translate_KeyPresentInRequestedLanguage_ReturnsTranslatedValue()
    {
        var service = BuildService();

        Assert.Equal("Mes médias", service.Translate("jux.native.my-media", "fr"));
    }

    [Fact]
    public void Translate_KeyMissingInRequestedLanguage_FallsBackToEnglish()
    {
        var service = BuildService();

        Assert.Equal("Next Up", service.Translate("jux.native.next-up", "fr"));
    }

    [Fact]
    public void Translate_KeyMissingEverywhere_FallsBackToRawKey()
    {
        var service = BuildService();

        Assert.Equal("jux.unknown.widget", service.Translate("jux.unknown.widget", "fr"));
    }

    [Fact]
    public void Translate_ValueProvided_SubstitutesPlaceholder()
    {
        var service = BuildService();

        Assert.Equal("Encore du Action", service.Translate("jux.personalized.favorite-genre.format", "fr", "Action"));
        Assert.Equal("More Drama", service.Translate("jux.personalized.favorite-genre.format", "en", "Drama"));
    }

    [Theory]
    [InlineData("fr-FR")]
    [InlineData("FR")]
    [InlineData("fr")]
    public void Translate_LanguageTagVariants_NormalizeToSameResult(string lang)
    {
        var service = BuildService();

        Assert.Equal("Mes médias", service.Translate("jux.native.my-media", lang));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("de")]
    [InlineData("es-ES")]
    public void Translate_NullOrUnrecognizedLanguage_FallsBackToEnglish(string? lang)
    {
        var service = BuildService();

        Assert.Equal("My Media", service.Translate("jux.native.my-media", lang));
    }

    [Fact]
    public void GetDictionary_RequestedLanguage_MergesOverEnglishBase()
    {
        var service = BuildService();

        var dict = service.GetDictionary("fr");

        Assert.Equal("Mes médias", dict["jux.native.my-media"]);
        Assert.Equal("Next Up", dict["jux.native.next-up"]);
    }

    [Fact]
    public void GetDictionary_EnglishOrUnrecognized_ReturnsEnglishBase()
    {
        var service = BuildService();

        var dict = service.GetDictionary("en");

        Assert.Equal("My Media", dict["jux.native.my-media"]);
    }
}
