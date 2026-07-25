'use strict';

// JellyUX menu tab shortcuts. TODO_V3.md Phase 8.2.
//
// Phase 8 is "Navigation & admin" -- this feature is a navigation shortcut, not a free-form custom
// link system: it injects a direct drawer entry for each JellyUX home tab the admin has enabled
// (Watchlist/Progress/History/Statistics, added in Phase 6), so a user can jump straight to one of
// them without first visiting Home and clicking its tab (jux-tab-injector.js).
//
// Injected into Jellyfin Web's left navigation drawer, in its own labeled section -- rather than
// mixing into a native group ("Media"/"Administration") -- matching how other real plugins already
// running on jellyux-test do this (Jellyfin-Enhanced, and the jellyfin-plugin-pages reference
// implementation, Controller/inject.js), which avoids any risk of native re-renders wiping out
// entries inserted into a group Jellyfin itself owns and may redraw.
//
// Anchor: .mainDrawer-scrollContainer, confirmed by the same jellyfin-plugin-pages reference
// (already proven live on this exact server) -- verified again directly on jellyux-test before this
// was relied on (see TODO_V3.md Phase 8 "Vérifications live").
//
// Unlike the Phase 7 page-scoped hooks, the drawer is a persistent, singleton element that is not
// recreated per page navigation, so this only needs to run once (a MutationObserver waiting for the
// drawer to first appear, then a one-time fetch+render) rather than re-arming on every hashchange.
(function () {
    if (typeof window.juxMenuLinks !== 'undefined') {
        return;
    }

    // Tab metadata, in display order. buttonId/dataIndex mirror jux-tab-injector.js's own `tabs`
    // array exactly (kept as a small, deliberate duplication -- same convention as _loadCardImages
    // being copied per file rather than shared -- since importing that file's internal array would
    // couple two otherwise-independent injectors).
    var _tabOrder = ['watchlist', 'progress', 'history', 'statistics'];
    var _tabMeta = {
        watchlist: { buttonId: 'jux-tabbtn-watchlist', icon: 'bookmark' },
        progress: { buttonId: 'jux-tabbtn-progress', icon: 'trending_up' },
        history: { buttonId: 'jux-tabbtn-history', icon: 'history' },
        statistics: { buttonId: 'jux-tabbtn-statistics', icon: 'bar_chart' }
    };

    // Labels match jux-tab-injector.js's own labelEn/labelFr values exactly, so the drawer shortcut
    // reads the same as the tab it jumps to.
    var _labels = {
        en: { watchlist: 'Watchlist', progress: 'Progress', history: 'History', statistics: 'Statistics' },
        fr: { watchlist: 'Watchlist', progress: 'Progression', history: 'Historique', statistics: 'Statistiques' }
    };

    window.juxMenuLinks = {
        init: function () {
            var observer = new MutationObserver(function () {
                _tryInject();
            });
            observer.observe(document.body, { childList: true, subtree: true });

            _tryInject();
        }
    };

    function _resolveLang() {
        return (document.documentElement.lang || 'en').toLowerCase().indexOf('fr') === 0 ? 'fr' : 'en';
    }

    // Cached (not re-fetched on every mutation), but deliberately NOT a one-shot "already injected"
    // flag -- confirmed live on jellyux-test that Jellyfin Web can rebuild the drawer's own DOM
    // subtree independently of any of our own actions (e.g. once its own async user/plugin data
    // finishes loading), which silently discards a previously-appended section along with it. A
    // one-shot flag would then permanently believe the job was done while the drawer visibly has no
    // JellyUX section. Instead, every mutation re-checks whether the section is present in the
    // *current* live container and re-appends the (already-fetched) shortcuts if it is missing --
    // self-healing against any number of native rebuilds, same lesson as the Collection page re-sort
    // race documented in jux-collections.js.
    var _shortcutsPromise = null;

    function _tryInject() {
        var scrollContainer = document.querySelector('.mainDrawer-scrollContainer');
        if (!scrollContainer) {
            return;
        }

        if (scrollContainer.querySelector('.jux-menu-links')) {
            return;
        }

        if (!window.ApiClient) {
            return;
        }

        if (!_shortcutsPromise) {
            var url = window.ApiClient.getUrl('JuxHomepage/MenuShortcuts');
            _shortcutsPromise = window.ApiClient.getJSON(url).catch(function (err) {
                console.error('[JellyUX] Failed to load menu tab shortcuts:', err);
                _shortcutsPromise = null; // allow a retry on a later mutation
                return null;
            });
        }

        _shortcutsPromise.then(function (enabledIds) {
            if (!enabledIds || enabledIds.length === 0) {
                return;
            }

            // Re-queried fresh: the drawer may have been rebuilt again while this fetch was in
            // flight, or by the time a cached _shortcutsPromise resolves for a later call.
            var liveContainer = document.querySelector('.mainDrawer-scrollContainer');
            if (!liveContainer || liveContainer.querySelector('.jux-menu-links')) {
                return;
            }

            liveContainer.appendChild(_buildSection(enabledIds));
        });
    }

    // Pure-ish: builds the section element from the enabled tab ids. Unknown ids (e.g. a stale
    // client cache after an admin config change) are silently skipped rather than breaking the whole
    // section -- the server-side ValidateConfiguration already filters these, so this is a second,
    // harmless line of defense, not the primary guard.
    function _buildSection(enabledIds) {
        var section = document.createElement('div');
        section.className = 'jux-menu-links';

        var header = document.createElement('h3');
        header.className = 'sidebarHeader';
        header.textContent = 'JellyUX';
        section.appendChild(header);

        var lang = _resolveLang();
        _tabOrder.forEach(function (id) {
            if (enabledIds.indexOf(id) === -1) {
                return;
            }
            var meta = _tabMeta[id];
            if (!meta) {
                return;
            }
            section.appendChild(_buildLink(meta, _labels[lang][id]));
        });

        return section;
    }

    // Markup/classes confirmed by the jellyfin-plugin-pages and KefinTweaks reference
    // implementations, both of which build real, working native-styled drawer entries this same way.
    // href is always the Home route (a plain "#/home.html" link degrades gracefully -- middle-click/
    // open-in-new-tab still lands on Home -- even before the click handler below runs); the actual
    // tab switch is done by the click handler, since JellyUX tabs have no URL/hash state of their own
    // (jux-tab-injector.js toggles panes purely by DOM position, confirmed by reading its source).
    function _buildLink(meta, label) {
        var a = document.createElement('a');
        a.setAttribute('is', 'emby-linkbutton');
        a.className = 'emby-button navMenuOption lnkMediaFolder';
        a.href = '#/home.html';

        a.addEventListener('click', function (event) {
            event.preventDefault();
            _goToTab(meta.buttonId);
        });

        var iconSpan = document.createElement('span');
        iconSpan.className = 'material-icons navMenuOptionIcon ' + meta.icon;
        iconSpan.setAttribute('aria-hidden', 'true');
        a.appendChild(iconSpan);

        var textSpan = document.createElement('span');
        textSpan.className = 'navMenuOptionText';
        textSpan.textContent = label;
        a.appendChild(textSpan);

        return a;
    }

    function _goToTab(buttonId) {
        if (!/^#\/home(\.html)?([/?]|$)/i.test(location.hash)) {
            location.hash = '#/home.html';
        }

        // Confirmed live on jellyux-test: dispatching immediately (even with a full pointerdown/
        // mousedown/pointerup/mouseup/click sequence) is unreliable -- the native tab bar can ignore
        // it entirely, seemingly regardless of how many times it's immediately retried. Waiting ~600ms
        // before the very first attempt (e.g. long enough for the drawer's own close animation and
        // whatever internal state the tab bar settles right after mount/navigation) made the exact
        // same dispatch succeed reliably in live testing, whereas rapid repeated re-dispatching without
        // that initial wait did not recover even after several seconds -- so this waits once up front
        // rather than hammering the button with retries.
        setTimeout(function () {
            _clickTabButtonWhenReady(buttonId, 20);
        }, 1200);
    }

    // Retries for up to ~4 seconds (20 * 200ms) while waiting for the button to exist at all:
    // generous enough for a cold navigation to Home to finish mounting the page and for
    // jux-tab-injector.js to have created its buttons, without depending on a fixed short delay that
    // could fire too early on a slower session (confirmed live in Phase 7 that this project's own
    // tab-mount timing isn't perfectly deterministic).
    function _clickTabButtonWhenReady(buttonId, attemptsLeft) {
        var button = document.getElementById(buttonId);
        if (!button) {
            if (attemptsLeft > 0) {
                setTimeout(function () {
                    _clickTabButtonWhenReady(buttonId, attemptsLeft - 1);
                }, 200);
            }
            return;
        }

        _dispatchRealisticClick(button);

        // A single retry, generously spaced (not a tight loop -- confirmed live that rapid repeated
        // re-dispatching does not help and may even leave the tab bar in a worse state): if the click
        // still didn't register after a full second, try once more.
        setTimeout(function () {
            if (!button.classList.contains('emby-tab-button-active')) {
                _dispatchRealisticClick(button);
            }
        }, 1000);
    }

    // Confirmed live on jellyux-test: a plain button.click() only dispatches a bare "click" event,
    // which reaches jux-tab-injector.js's own listener (toggling the pane's is-active class) but does
    // NOT reach whatever native mechanism actually renders the tab's content and highlights it in the
    // header -- that native tab-switching code only reacts to a full pointerdown/mousedown/pointerup/
    // mouseup/click sequence, matching what a real physical click produces. Dispatching only "click"
    // left the pane marked active internally while the page kept showing Home's own content.
    function _dispatchRealisticClick(element) {
        var rect = element.getBoundingClientRect();
        var x = rect.left + (rect.width / 2);
        var y = rect.top + (rect.height / 2);
        var opts = { bubbles: true, cancelable: true, view: window, clientX: x, clientY: y, button: 0 };

        element.dispatchEvent(new PointerEvent('pointerdown', opts));
        element.dispatchEvent(new MouseEvent('mousedown', opts));
        element.dispatchEvent(new PointerEvent('pointerup', opts));
        element.dispatchEvent(new MouseEvent('mouseup', opts));
        element.dispatchEvent(new MouseEvent('click', opts));
    }

    window.juxMenuLinks.init();

    // Guarded UMD-lite export (same convention as the rest of the project), so Vitest can exercise
    // the DOM-building functions directly against jsdom, the same way jux-series-flatten.test.js
    // exercises _buildFlattenedSectionSkeleton.
    if (typeof module !== 'undefined' && module.exports) {
        module.exports = {
            _buildLink: _buildLink,
            _buildSection: _buildSection
        };
    }
})();
