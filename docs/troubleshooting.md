# Troubleshooting

## Plugin becomes unreachable (HTTP 500) after an update

A simple Jellyfin restart may not be enough after updating the plugin. Do a full restart of the
Jellyfin process itself (for example `docker restart <container>`) to clear the File Transformation
plugin's static state.

## JellyUX sections don't appear on Jellyfin Media Player (Windows)

Jellyfin Media Player keeps an internal auto-connect cache (`userWebClient` in
`%LOCALAPPDATA%\JellyfinMediaPlayer\jellyfinmediaplayer.conf`, `main` section) that isn't invalidated
when you switch servers in the app. It keeps loading the web client, so every plugin (not just
JellyUX), from the previously connected server, regardless of which server is currently selected.

**Recommended fix**: in the app's settings, use the **Reset Saved Server** button (shown automatically
whenever a server is cached).

**Fallback**: edit `jellyfinmediaplayer.conf` and reset `userWebClient` to an empty string. Don't
confuse this with the `%LOCALAPPDATA%\Jellyfin Media Player` cache folder (with spaces): clearing that
one does not fix this specific issue, since it's an auto-connect cache, not a browser cache.

## "Forgot password?" button missing on the login page with ZestyTheme

This is unrelated to JellyUX. ZestyTheme's `theme.css` intentionally hides `.btnForgotPassword` on the
login page. To use password recovery, temporarily disable the ZestyTheme custom CSS (or remove the
button's `display: none` override locally), recover the password, then re-enable the theme.

## A widget pack fails to load

If an external widget pack (a third-party DLL dropped into the plugin's `widget-packs` folder) fails
to load, a warning banner appears at the top of the admin configuration page listing the failed
file(s) and the reason. The rest of the plugin, including all built-in widgets, keeps working normally.
Check the failure reason in the banner (or in the Jellyfin server logs) to determine whether the pack
targets an incompatible plugin version, or is otherwise malformed.

## TMDb requests stop working after a network outage

Connected widgets rely on TMDb's public API. After 3 consecutive network-level failures (not an
invalid API key, which is reported separately), a circuit breaker opens and TMDb requests are skipped
for 5 minutes rather than retried on every widget load. Look for a log line similar to:

```
TMDb circuit breaker opened after 3 consecutive failures; TMDb requests will be skipped for 5 minute(s).
```

This is expected behavior during a sustained TMDb outage and requires no action: normal requests
resume automatically once the 5-minute window elapses and TMDb is reachable again.

## Home page falls back to native rendering after a Jellyfin update

JellyUX patches a specific Jellyfin Web chunk to render its widgets. If Jellyfin Web's internal
bundle changes shape (a "drift"), the patch may no longer apply, and a warning appears in the server
logs:

```
Chunk <file>.chunk.js contains ',loadSections:' but the minified hook could not be resolved.
```

If you see this line, the home page falls back to Jellyfin's native rendering rather than showing a
broken page. This is a known limitation of patching a third-party bundle that isn't part of JellyUX's
public API. Check the project's GitHub Issues for a compatibility update, or open a new issue if none
exists yet.
