using MediaBrowser.Controller.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace Jellyfin.Plugin.JuxHomepage.Widgets;

/// <summary>
/// Shared DtoOptions builder for widget queries. Requests primary image aspect ratio, creation
/// date, thumbnail, and backdrop images -- previously duplicated identically across all four widget
/// base classes.
/// </summary>
public static class WidgetDtoOptions
{
    /// <summary>Builds the standard <see cref="DtoOptions"/> used by every widget category.</summary>
    /// <returns>A pre-configured <see cref="DtoOptions"/> instance.</returns>
    public static DtoOptions Standard() => new()
    {
        Fields =
        [
            ItemFields.PrimaryImageAspectRatio,
            ItemFields.DateCreated
        ],
        ImageTypeLimit = 1,
        ImageTypes =
        [
            ImageType.Primary,
            ImageType.Thumb,
            ImageType.Backdrop
        ]
    };
}
