# Discover Movies

**Category:** Connected (TMDb)

Displays movies matching an administrator-defined TMDb Discover filter, that are present in your local
library. Unlike the other Connected widgets, each Discover Movies section has its own filter and its
own independent cache: you can add several sections, each targeting a different combination of
filters.

## Parameters

In addition to the [common parameters](../../configuration.md#common-per-widget-parameters), each
section has its own filter block:

| Field | Description |
|---|---|
| Genre | Restrict results to a specific TMDb genre. |
| Person | Restrict results to movies featuring a specific person (actor or crew). |
| Keyword | Restrict results to movies matching a specific TMDb keyword. |
| Company | Restrict results to movies from a specific production company. |
| Sort by | TMDb sort order for the discover query. Defaults to popularity, descending. |
| Year | Restrict results to a specific primary release year. |
| Min rating | Minimum TMDb average vote (0-10) a movie must have. |
| Min votes | Minimum number of TMDb votes a movie must have, so a handful of extreme ratings don't skew the results. Defaults to 50. |
| Pages | How many pages of TMDb results to fetch for this section (1-5, roughly 20 movies per page). Defaults to 1. This is independent of the global TMDb Lists page counts used by the other Connected widgets. |

All fields are optional except Pages; leave a field empty to not filter on it.
