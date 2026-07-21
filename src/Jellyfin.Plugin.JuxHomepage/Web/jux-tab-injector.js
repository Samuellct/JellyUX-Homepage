// Injects the 4 JellyUX tab buttons (Watchlist/Progress/History/Statistics) into the home page's
// native tab bar (.emby-tabs-slider), so they sit alongside the built-in "Home"/"Favorites" tabs.
//
// TODO_V3.md Phase 4.1. Modeled directly on the jellyfin-plugin-custom-tabs approach (verified
// against its real source, not just its README): a plain <button data-index="N"> appended into
// .emby-tabs-slider is enough -- Jellyfin's native "emby-tabs" custom element handles the click and
// visibility toggling itself, keyed by DOM position among all .tabContent panes (see
// Inject/TransformationPatches.cs for the matching content-pane splice at the same data-index
// values). No custom click handling is needed or attempted here.
//
// The tab bar is rebuilt from scratch by React on every view mount (home.js calls setTabs(), which
// replaces .headerTabs innerHTML entirely), so this script must re-run on every navigation, not just
// once at initial page load -- mirrors the re-arming strategy already used by jux-homepage.js.
if (typeof window.juxTabInjector === 'undefined') {
    window.juxTabInjector = {
        // buttonId is deliberately distinct from the content pane's id (jux-tab-* -- see
        // Inject/TransformationPatches.cs's HomeTabIds, spliced server-side into the home-html
        // chunk): reusing the same id for both caused document.getElementById(paneId) to always
        // find the (always-present) pane and wrongly conclude the button already existed, so no
        // button was ever created. Bug found by live DOM inspection on jellyux-test.
        tabs: [
            { buttonId: 'jux-tabbtn-watchlist', dataIndex: 2, labelEn: 'Watchlist', labelFr: 'Watchlist' },
            { buttonId: 'jux-tabbtn-progress', dataIndex: 3, labelEn: 'Progress', labelFr: 'Progression' },
            { buttonId: 'jux-tabbtn-history', dataIndex: 4, labelEn: 'History', labelFr: 'Historique' },
            { buttonId: 'jux-tabbtn-statistics', dataIndex: 5, labelEn: 'Statistics', labelFr: 'Statistiques' }
        ],

        init: function () {
            this.waitForUI();
        },

        waitForUI: function () {
            var hash = window.location.hash;
            if (hash !== '' && hash !== '#/home' && hash !== '#/home.html' && hash.indexOf('#/home?') !== 0 && hash.indexOf('#/home.html?') !== 0) {
                return;
            }

            if (document.querySelector('.emby-tabs-slider')) {
                this.createTabs();
            } else {
                setTimeout(function () { window.juxTabInjector.waitForUI(); }, 200);
            }
        },

        createTabs: function () {
            var tabsSlider = document.querySelector('.emby-tabs-slider');
            if (!tabsSlider) {
                return;
            }

            if (tabsSlider.querySelector('#' + this.tabs[0].buttonId)) {
                // Already created for this view mount.
                return;
            }

            var lang = (document.documentElement.lang || 'en').toLowerCase().indexOf('fr') === 0 ? 'fr' : 'en';

            this.tabs.forEach(function (tab) {
                if (document.getElementById(tab.buttonId)) {
                    return;
                }

                var title = document.createElement('div');
                title.className = 'emby-button-foreground';
                title.textContent = lang === 'fr' ? tab.labelFr : tab.labelEn;

                // NOT document.createElement('button', { is: 'emby-button' }): confirmed live on
                // jellyux-test that this second-argument form throws ("t.toLowerCase is not a
                // function") on this Jellyfin Web build, aborting the whole forEach before any
                // button is appended -- the exact cause of a real regression (v2.3.0.0) where the
                // entire custom tab bar silently disappeared. setAttribute after creation is inert
                // for real customized-built-in upgrade, but harmless here since the click routing
                // is delegation-based on the slider/tabs controller, not on this button being a
                // real emby-button instance (confirmed working before and after this revert).
                var button = document.createElement('button');
                button.type = 'button';
                button.setAttribute('is', 'emby-button');
                button.className = 'emby-tab-button emby-button';
                button.setAttribute('data-index', tab.dataIndex);
                button.id = tab.buttonId;
                button.appendChild(title);

                // Belt-and-suspenders pane switch: confirmed live on jellyux-test that the very
                // first click on a freshly-injected tab (right after a page load/reload) can leave
                // the tab bar's own selection state unresolved for a beat -- the MutationObserver
                // below reacts as soon as Jellyfin's controller does settle it, but a fixed short
                // delay after our own button's click guarantees the pane switches immediately
                // regardless of that timing, without needing to fully reverse-engineer the
                // minified controller's internal warm-up behavior.
                button.addEventListener('click', function () {
                    setTimeout(function () { _activatePane(tab.dataIndex); }, 50);
                });

                tabsSlider.appendChild(button);
            });

            _watchActiveTabButton(tabsSlider);
        }
    };

    // Manual pane activation for our own tabs. Confirmed by live DOM inspection on jellyux-test:
    // clicking a freshly-injected tab can leave the corresponding ".tabContent.is-active" pane
    // unswitched even though the tab bar's own visual selection looks right (TODO_V3.md Phase 5
    // manual test report -- "obligé de rafraîchir plusieurs fois avant de voir les médias"). Every
    // click afterwards works fine, so this isn't a permanent break, just an unreliable first switch.
    // Rather than depend on fully reverse-engineering Jellyfin's minified tab controller to find
    // the exact cause, two independent, idempotent mechanisms guarantee the pane switches anyway:
    // a MutationObserver reacting to the button's own "emby-tab-button-active" class (below), and a
    // fixed-delay fallback on each button's own click handler (see createTabs above). Whichever
    // fires first wins; the other is a harmless no-op repeat.
    var _tabBarObserver = null;
    var _observedSlider = null;

    function _activatePane(index) {
        var panes = document.querySelectorAll('.tabContent');
        Array.prototype.forEach.call(panes, function (pane, paneIndex) {
            pane.classList.toggle('is-active', paneIndex === index);
        });
    }

    function _watchActiveTabButton(tabsSlider) {
        if (_observedSlider === tabsSlider) {
            return;
        }

        if (_tabBarObserver) {
            _tabBarObserver.disconnect();
        }

        _observedSlider = tabsSlider;
        _tabBarObserver = new MutationObserver(function (mutations) {
            for (var i = 0; i < mutations.length; i++) {
                var button = mutations[i].target;
                if (button.id && button.id.indexOf('jux-tabbtn-') === 0 && button.classList.contains('emby-tab-button-active')) {
                    _activatePane(parseInt(button.getAttribute('data-index'), 10));
                }
            }
        });

        _tabBarObserver.observe(tabsSlider, { attributes: true, attributeFilter: ['class'], subtree: true });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () { window.juxTabInjector.init(); });
    } else {
        window.juxTabInjector.init();
    }

    var handleNavigation = function () {
        setTimeout(function () { window.juxTabInjector.init(); }, 800);
    };

    window.addEventListener('popstate', handleNavigation);
    window.addEventListener('pageshow', handleNavigation);
    window.addEventListener('focus', handleNavigation);

    var originalPushState = history.pushState;
    history.pushState = function () {
        originalPushState.apply(history, arguments);
        handleNavigation();
    };

    var originalReplaceState = history.replaceState;
    history.replaceState = function () {
        originalReplaceState.apply(history, arguments);
        handleNavigation();
    };

    document.addEventListener('visibilitychange', function () {
        if (!document.hidden) {
            setTimeout(function () { window.juxTabInjector.init(); }, 300);
        }
    });
}
