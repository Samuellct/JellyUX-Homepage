# Favorite Director

**Category:** Personalized

Displays movies directed by the requesting user's favorite directors, derived from their watch
history.

## How ranking works

Each Favorite Director section you add gets a **rank**, based on the order the sections were added.
At render time, each user's directors are scored from their own watch history, and each section shows
the director at its rank for that user: rank 1 shows their single most-watched director, rank 2 shows
their second most-watched director, and so on. If a user doesn't have enough distinct directors to
fill every configured rank, the extra sections are simply not shown for that user.

## Parameters

In addition to the [common parameters](../../configuration.md#common-per-widget-parameters):

| Field | Description |
|---|---|
| Exclude already watched | When enabled, titles the user has already watched are not recommended. |

Note: Personalized widgets don't have a custom name field. Section names are generated per user.

## On a small library

A director match is narrow -- usually just a handful of titles per person, unlike a genre match. With
**Exclude already watched** on, a section only shows if enough *unwatched* titles from that director
remain; if a user has already watched most or all of what the library owns from their top-scored
director, there may be nothing left to recommend, and the section is hidden rather than shown empty.
This is expected behavior, not a bug -- it becomes less noticeable as the library grows. If it's too
aggressive on a smaller library, either disable **Exclude already watched** for this widget or lower
**Min items**.
