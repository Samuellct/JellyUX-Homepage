'use strict';

// JellyUX Watchlist tab rendering.
// TODO_V3.md Phase 5.1. Loaded as a plain <script src> (see TransformationPatches.IndexHtml),
// independent of the loadSections override -- unlike jux-homepage.js, this script does not need
// window.JellyfinAPI to exist (it only needs window.JellyfinAPI.cardBuilder, which is lazily read at
// render time, by which point the user has necessarily visited Home at least once this session,
// since navigating there is how the Watchlist tab itself got created -- see jux-tab-injector.js).
//
// The tab bar (and our button) is recreated on every view mount (see jux-tab-injector.js), so the
// click listener below is delegated on `document`, never attached to the button element directly.
(function () {
    if (typeof window.juxWatchlist !== 'undefined') {
        return;
    }

    var _labels = {
        en: {
            title: 'Watchlist',
            empty: 'Your watchlist is empty.',
            sortName: 'Name',
            sortDateAdded: 'Date Added',
            sortReleaseDate: 'Release Date',
            sortCommunityRating: 'Community Rating',
            typeAll: 'All',
            typeMovie: 'Movies',
            typeSeries: 'Series'
        },
        fr: {
            title: 'Watchlist',
            empty: 'Ta watchlist est vide.',
            sortName: 'Nom',
            sortDateAdded: 'Date d’ajout',
            sortReleaseDate: 'Date de sortie',
            sortCommunityRating: 'Note communauté',
            typeAll: 'Tous',
            typeMovie: 'Films',
            typeSeries: 'Séries'
        }
    };

    window.juxWatchlist = {
        state: { sortBy: 'DateAdded', includeItemTypes: 'All' },

        init: function () {
            var self = this;
            document.addEventListener('click', function (event) {
                if (event.target.closest('#jux-tabbtn-watchlist')) {
                    self.render();
                }
            });
        },

        render: function () {
            var pane = document.getElementById('jux-tab-watchlist');
            if (!pane) {
                return;
            }

            var sections = pane.querySelector('.sections');
            if (!sections) {
                return;
            }

            var lang = _resolveLang();
            var userId = window.ApiClient && window.ApiClient.getCurrentUserId();
            if (!userId) {
                return;
            }

            sections.innerHTML = _buildControlsHtml(lang, this.state);
            _wireControls(sections, this.state, function () {
                window.juxWatchlist.render();
            });

            var url = window.ApiClient.getUrl('JuxHomepage/Watchlist/Items', {
                userId: userId,
                sortBy: this.state.sortBy,
                sortOrder: 'Descending',
                includeItemTypes: this.state.includeItemTypes,
                startIndex: 0,
                limit: 100
            });

            window.ApiClient.getJSON(url).then(function (result) {
                _renderItems(sections, result, lang);
            }).catch(function (err) {
                console.error('[JellyUX] Watchlist fetch failed:', err);
                _renderItems(sections, { Items: [], TotalRecordCount: 0 }, lang);
            });
        }
    };

    function _resolveLang() {
        return (document.documentElement.lang || 'en').toLowerCase().indexOf('fr') === 0 ? 'fr' : 'en';
    }

    function _buildControlsHtml(lang, state) {
        var t = _labels[lang];
        return '<div class="jux-watchlist-controls" style="display:flex;gap:1em;padding:0 2em 1em;">' +
            '<select id="jux-watchlist-sort" is="emby-select" class="emby-select">' +
            _option('Name', t.sortName, state.sortBy) +
            _option('DateAdded', t.sortDateAdded, state.sortBy) +
            _option('ReleaseDate', t.sortReleaseDate, state.sortBy) +
            _option('CommunityRating', t.sortCommunityRating, state.sortBy) +
            '</select>' +
            '<select id="jux-watchlist-type" is="emby-select" class="emby-select">' +
            _option('All', t.typeAll, state.includeItemTypes) +
            _option('Movie', t.typeMovie, state.includeItemTypes) +
            _option('Series', t.typeSeries, state.includeItemTypes) +
            '</select>' +
            '</div>' +
            '<div class="jux-watchlist-items itemsContainer vertical-wrap padded-left padded-right"></div>' +
            '<div class="jux-watchlist-empty" style="display:none;padding:0 2em;">' + _escHtml(t.empty) + '</div>';
    }

    function _option(value, label, current) {
        return '<option value="' + _escHtml(value) + '"' + (value === current ? ' selected' : '') + '>' + _escHtml(label) + '</option>';
    }

    function _wireControls(sections, state, onChange) {
        var sortEl = sections.querySelector('#jux-watchlist-sort');
        var typeEl = sections.querySelector('#jux-watchlist-type');
        if (sortEl) {
            sortEl.addEventListener('change', function () {
                state.sortBy = sortEl.value;
                onChange();
            });
        }
        if (typeEl) {
            typeEl.addEventListener('change', function () {
                state.includeItemTypes = typeEl.value;
                onChange();
            });
        }
    }

    function _renderItems(sections, result, lang) {
        var itemsContainer = sections.querySelector('.jux-watchlist-items');
        var emptyEl = sections.querySelector('.jux-watchlist-empty');
        if (!itemsContainer || !emptyEl) {
            return;
        }

        var items = (result && result.Items) || [];
        if (items.length === 0) {
            itemsContainer.innerHTML = '';
            emptyEl.style.display = '';
            return;
        }

        emptyEl.style.display = 'none';

        var api = window.JellyfinAPI;
        if (!api || !api.cardBuilder) {
            itemsContainer.innerHTML = '';
            return;
        }

        itemsContainer.innerHTML = api.cardBuilder.getCardsHtml({
            items: items,
            shape: api.getPortraitShape(false),
            showTitle: true,
            showParentTitle: true,
            overlayText: false,
            overlayPlayButton: true,
            centerText: true,
            showYear: true,
            lazy: true,
            lines: 2
        });
    }

    function _escHtml(str) {
        if (!str) { return ''; }
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    window.juxWatchlist.init();

    // Guarded UMD-lite export (same convention as jux-homepage.js), so Vitest can exercise the pure
    // functions directly without a real browser/DOM.
    if (typeof module !== 'undefined' && module.exports) {
        module.exports = {
            _escHtml: _escHtml,
            _option: _option,
            _buildControlsHtml: _buildControlsHtml
        };
    }
})();
