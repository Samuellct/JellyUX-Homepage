namespace Jellyfin.Plugin.JuxHomepage.Widgets.Personalized;

/// <summary>
/// Which item types a "Because You Watched" row's reference item may be picked from (see
/// <see cref="ScoringService.GetRecentlyWatched"/>), admin-configurable per row via
/// <c>ExtraParams["scope"]</c> (see <see cref="BecauseYouWatchedWidget"/>). The recommended items
/// themselves always match the reference item's own type (see
/// <see cref="BecauseYouWatchedWidget.ApplyFilter"/>) regardless of this setting -- this only narrows
/// which recently-watched items are eligible to become the reference in the first place.
/// </summary>
public enum BecauseYouWatchedScope
{
    /// <summary>Only movies may be picked as the reference item.</summary>
    Movies,

    /// <summary>Only series may be picked as the reference item.</summary>
    Series,

    /// <summary>Both movies and series are eligible, ranked together by recency.</summary>
    Both
}
