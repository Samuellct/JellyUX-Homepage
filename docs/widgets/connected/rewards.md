# Rewards

**Category:** Connected (Wikidata)

Displays films from your local library that won an administrator-defined award, sourced from
[Wikidata](https://www.wikidata.org/)'s structured data rather than a proprietary API. Like Discover
Movies, each Rewards section has its own filter and its own independent cache: you can add several
sections, each targeting a different ceremony, category, or year.

## Parameters

In addition to the [common parameters](../../configuration.md#common-per-widget-parameters), each
section has its own filter block:

| Field | Description |
|---|---|
| Ceremony | Restrict results to every film-attached award category belonging to this ceremony (e.g. "Academy Awards"). Combine with Year to scope to a single edition. |
| Category | Restrict results to films that won this exact award category (e.g. "Academy Award for Best Picture"), across every edition unless Year is also set. |
| Year | Restrict results to a specific ceremony/award year. |

At least one of Ceremony or Category must be set. Refreshed on the same weekly schedule as the
scheduled task, or immediately via the "Refresh now" button in the TMDb Cache section of the admin
panel (shared with TMDb's own cache, since both are refreshed together).

## Data coverage

Wikidata's award data is community-maintained and not guaranteed to be complete for every ceremony and
year. Well-established, high-profile ceremonies (e.g. the Academy Awards) tend to have excellent,
continuous coverage; smaller or more regional ceremonies may have gaps for some years. If a
configured combination returns no results, check the ceremony/category directly on Wikidata before
assuming a plugin issue.
