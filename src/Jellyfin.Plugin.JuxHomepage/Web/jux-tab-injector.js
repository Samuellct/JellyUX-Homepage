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
        tabs: [
            { id: 'jux-tab-watchlist', dataIndex: 2, labelEn: 'Watchlist', labelFr: 'Watchlist' },
            { id: 'jux-tab-progress', dataIndex: 3, labelEn: 'Progress', labelFr: 'Progression' },
            { id: 'jux-tab-history', dataIndex: 4, labelEn: 'History', labelFr: 'Historique' },
            { id: 'jux-tab-statistics', dataIndex: 5, labelEn: 'Statistics', labelFr: 'Statistiques' }
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

            if (tabsSlider.querySelector('#' + this.tabs[0].id)) {
                // Already created for this view mount.
                return;
            }

            var lang = (document.documentElement.lang || 'en').toLowerCase().indexOf('fr') === 0 ? 'fr' : 'en';

            this.tabs.forEach(function (tab) {
                if (document.getElementById(tab.id)) {
                    return;
                }

                var title = document.createElement('div');
                title.className = 'emby-button-foreground';
                title.textContent = lang === 'fr' ? tab.labelFr : tab.labelEn;

                var button = document.createElement('button');
                button.type = 'button';
                button.setAttribute('is', 'emby-button');
                button.className = 'emby-tab-button emby-button';
                button.setAttribute('data-index', tab.dataIndex);
                button.id = tab.id;
                button.appendChild(title);

                tabsSlider.appendChild(button);
            });
        }
    };

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
