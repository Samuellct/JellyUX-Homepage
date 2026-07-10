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
