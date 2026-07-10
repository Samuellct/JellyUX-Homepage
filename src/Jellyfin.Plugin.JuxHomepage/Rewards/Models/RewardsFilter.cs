using System.Globalization;

namespace Jellyfin.Plugin.JuxHomepage.Rewards.Models;

/// <summary>
/// Filter parameters for a Rewards widget instance, as configured by the administrator. Stored
/// round-trip via a widget config row's <c>ExtraParams</c> (see <see cref="FromExtraParams"/>), the
/// same mechanism the Discover Movies widget uses (see
/// <see cref="TMDb.Models.TMDbDiscoverFilter"/>).
/// <para>
/// At least one of <see cref="CeremonyQid"/> or <see cref="CategoryQid"/> must be set (enforced by
/// <see cref="Plugin.ValidateConfiguration"/>) -- a bare <see cref="Year"/> alone is not a meaningful
/// Wikidata query (see TODO_V2.md Phase 14 research: award statements are always attached to an
/// award-category item, never to a year in isolation).
/// </para>
/// </summary>
public sealed class RewardsFilter
{
    /// <summary>
    /// Gets or sets the Wikidata Q-id of the ceremony (e.g. "Q19020" for the Academy Awards), used to
    /// select every film-attached award category that is an "instance of" this ceremony. Combine with
    /// <see cref="Year"/> to scope to a single edition.
    /// </summary>
    public string? CeremonyQid { get; set; }

    /// <summary>
    /// Gets or sets the Wikidata Q-id of a specific award category (e.g. "Q102427" for the Academy
    /// Award for Best Picture), used to select every film that won this exact category, across all
    /// editions unless <see cref="Year"/> is also set.
    /// </summary>
    public string? CategoryQid { get; set; }

    /// <summary>
    /// Gets or sets the ceremony/award year to filter by, if any. Combined with
    /// <see cref="CeremonyQid"/> and/or <see cref="CategoryQid"/> (at least one of which is required).
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Reconstructs a <see cref="RewardsFilter"/> from a Rewards widget instance's configured extra
    /// parameters (as forwarded by <see cref="Widgets.WidgetService"/>).
    /// </summary>
    /// <param name="extraParams">The instance's configured extra parameters.</param>
    /// <returns>The reconstructed filter.</returns>
    public static RewardsFilter FromExtraParams(IReadOnlyDictionary<string, string> extraParams)
    {
        var filter = new RewardsFilter();

        if (extraParams.TryGetValue("ceremonyQid", out var ceremonyQid) && !string.IsNullOrWhiteSpace(ceremonyQid))
        {
            filter.CeremonyQid = ceremonyQid;
        }

        if (extraParams.TryGetValue("categoryQid", out var categoryQid) && !string.IsNullOrWhiteSpace(categoryQid))
        {
            filter.CategoryQid = categoryQid;
        }

        if (extraParams.TryGetValue("year", out var yearText)
            && int.TryParse(yearText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var year))
        {
            filter.Year = year;
        }

        return filter;
    }
}
