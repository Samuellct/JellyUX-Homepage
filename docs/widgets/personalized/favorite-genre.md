# Favorite Genre

**Category:** Personalized

Displays items from the requesting user's favorite genres, derived from their watch history.

## How ranking works

Each Favorite Genre section you add gets a **rank**, based on the order the sections were added (the
first section is rank 1, the second is rank 2, and so on). At render time, each user's genres are
scored from their own watch history, and each section shows the genre at its rank for that user: rank
1 shows their single most-watched genre, rank 2 shows their second most-watched genre, and so on. If a
user doesn't have enough distinct genres to fill every configured rank, the extra sections are simply
not shown for that user, rather than repeating an already-shown genre.

## Parameters

In addition to the [common parameters](../../configuration.md#common-per-widget-parameters):

| Field | Description |
|---|---|
| Exclude already watched | When enabled, titles the user has already watched are not recommended. |

Note: Personalized widgets don't have a custom name field. Section names are generated per user (for
example "Because you watched ...").

## On a small library

With **Exclude already watched** on, a section only shows if enough *unwatched* titles in that genre
remain. A genre match is broad, so this is rarely an issue -- but on a small library, or for a genre
the user has watched most of already, the section may be hidden rather than shown empty. See
[Favorite Actor](favorite-actor.md#on-a-small-library) for the same behavior in its narrower, more
noticeable form.
