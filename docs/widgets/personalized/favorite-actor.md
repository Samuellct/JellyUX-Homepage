# Favorite Actor

**Category:** Personalized

Displays movies featuring the requesting user's favorite actors, derived from their watch history.

## How ranking works

Each Favorite Actor section you add gets a **rank**, based on the order the sections were added. At
render time, each user's actors are scored from their own watch history, and each section shows the
actor at its rank for that user: rank 1 shows their single most-watched actor, rank 2 shows their
second most-watched actor, and so on. If a user doesn't have enough distinct actors to fill every
configured rank, the extra sections are simply not shown for that user.

## Parameters

In addition to the [common parameters](../../configuration.md#common-per-widget-parameters):

| Field | Description |
|---|---|
| Exclude already watched | When enabled, titles the user has already watched are not recommended. |

Note: Personalized widgets don't have a custom name field. Section names are generated per user.

## On a small library

An actor match is narrow -- usually just a handful of titles per person, unlike a genre match. With
**Exclude already watched** on, a section only shows if enough *unwatched* titles from that actor
remain; if a user has already watched most or all of what the library owns from their top-scored
actor, there may be nothing left to recommend, and the section is hidden rather than shown empty.
This is expected behavior, not a bug -- it becomes less noticeable as the library grows. If it's too
aggressive on a smaller library, either disable **Exclude already watched** for this widget or lower
**Min items**.
