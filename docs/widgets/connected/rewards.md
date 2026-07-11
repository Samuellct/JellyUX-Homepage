# Rewards

**Category:** Connected (Wikidata)

Displays films from your local library that won an administrator-defined award. Unlike every other
Connected widget, Rewards is **not** powered by TMDb: it queries
[Wikidata](https://www.wikidata.org/), a free, structured, community-maintained knowledge base, so
**no API key is required** for this widget (contrast with the [TMDb API
Key](../../configuration.md#api-keys) needed by every other Connected widget).

Like [Discover Movies](discover-movies.md), each Rewards section has its own filter and its own
independent cache: add as many sections as you like, each targeting a different ceremony, category,
or year.

## How targeting works

Every Rewards section is defined by up to three fields, combined as follows:

| Field | What it does |
|---|---|
| **Ceremony** | Every award category that belongs to this ceremony (e.g. every Academy Awards category: Best Picture, Best Director, Best Actor, ...). |
| **Category** | One specific award category, across every year it has been given out (e.g. only Best Picture, every year). |
| **Year** | Restricts to one specific edition/year. Only meaningful combined with Ceremony and/or Category -- a bare Year with nothing else is not a valid Wikidata query and returns nothing. |

**At least one of Ceremony or Category is required.** The three fields combine into three practical
configurations:

1. **Ceremony + Year -- "everything that won at one ceremony's edition"**
   Ceremony: `Academy Awards`, Year: `2024` -> every film in your library that won *any* Academy Award
   category at the 2024 ceremony (Best Picture, Best Director, Best Actor, etc., whichever apply).
2. **Category only -- "every winner of one category, all years"**
   Category: `Academy Award for Best Picture`, Year: *(empty)* -> every Best Picture winner in your
   library across every year Wikidata has data for.
3. **Category + Year -- "the winner of one category in one specific year"**
   Category: `Academy Award for Best Picture`, Year: `2019` -> just that year's Best Picture winner,
   if it's in your library.

Ceremony and Category can also be combined together, but this is rarely useful since Category alone
already pins down the ceremony (a category like "Academy Award for Best Picture" only ever belongs to
one ceremony).

## Searching for a Ceremony or Category

Both fields autocomplete against Wikidata as you type, proxied through the Jellyfin server (a
compliant identification header is required by Wikidata and cannot be set from a browser, so the
search cannot run client-side).

Wikidata's search matches broadly across its entire database, not just awards -- a short, generic
query like "Academy" will surface unrelated entities too (a school, a video game, an unrelated film
literally titled "Academy"). **Type the full, specific name for clean results**, e.g.:

- `Academy Awards` (not `Academy`) for the Ceremony field
- `Academy Award for Best Picture` (not `Best Picture`) for the Category field
- `César Award for Best Film` for a French example

Pick the entry whose description confirms what you meant (e.g. "annual awards for cinematic
achievements" for the Academy Awards ceremony, "annual award from the Academy of Motion Picture Arts
and Sciences" for the Best Picture category).

## Worked examples

**"Films from your library that won an Oscar in 2024"**
- Ceremony: `Academy Awards`
- Category: *(empty)*
- Year: `2024`

**"Every Best Picture winner you own, any year"**
- Ceremony: *(empty)*
- Category: `Academy Award for Best Picture`
- Year: *(empty)*

**"This specific year's César for Best Film, if you own it"**
- Ceremony: *(empty)*
- Category: `César Award for Best Film`
- Year: `2016`

## Data coverage

Wikidata's award data is community-maintained and not guaranteed to be complete for every ceremony and
year. Well-established, high-profile ceremonies (e.g. the Academy Awards) tend to have excellent,
continuous coverage; smaller or more regional ceremonies may have real, confirmed gaps for some years
(the César Award for Best Film, for example, is missing a small number of years). If a configured
combination shows no section on the home screen, it can mean one of three things, roughly in order of
likelihood:

1. **You don't own any of the matching films** -- the query found winners, none are in your library.
   This is normal and expected, not an error.
2. **Wikidata genuinely has no data** for that exact combination (a real gap in its records).
3. A transient failure reached out to Wikidata and got no answer that time. This widget deliberately
   does not retry aggressively (see below) -- click **Refresh now** again, or wait for the next
   scheduled refresh.

To tell these apart, check **Dashboard > Logs** right after a refresh for a line from
`Jellyfin.Plugin.JuxHomepage.Rewards.RewardsCacheService`: it reports how many items were fetched from
Wikidata and how many matched your local library. `6 item(s), 1 matched` means Wikidata found 6
winners and only 1 is in your library (case 1, expected); `0 items` with no library match at all means
either case 2 or 3 -- a preceding warning from `WikidataApiClient` in the same log, if present,
confirms case 3 (a network failure or rate limit).

## Refreshing

Rewards data refreshes automatically once a week (rather than daily like TMDb data -- award results
for a given ceremony change at most once a year, so a more frequent refresh would only add
unnecessary load on Wikidata's public service). It can also be refreshed immediately: Rewards shares
the **Refresh now** button in the [TMDb Cache](../../configuration.md#tmdb-cache) section of the admin
panel with every TMDb-backed widget -- there is no separate Rewards-only refresh button.
