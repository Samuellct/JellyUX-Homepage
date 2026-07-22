'use strict';

// JellyUX Statistics tab rendering.
// TODO_V3.md Phase 6.3. Same click-delegation convention as jux-watchlist.js/jux-progress.js/
// jux-history.js, but this view is not a card grid -- just a handful of number tiles built from
// GET JuxHomepage/Statistics (Watchlist/StatisticsService.cs, purely derived from the already-cached
// Series Progress / Movie History data, no new library scan).
(function () {
    if (typeof window.juxStatistics !== 'undefined') {
        return;
    }

    var _labels = {
        en: {
            moviesWatched: 'Movies watched',
            seriesTracked: 'Series tracked',
            seriesCompleted: 'Series completed',
            episodesWatched: 'Episodes watched'
        },
        fr: {
            moviesWatched: 'Films vus',
            seriesTracked: 'Séries suivies',
            seriesCompleted: 'Séries terminées',
            episodesWatched: 'Épisodes vus'
        }
    };

    window.juxStatistics = {
        init: function () {
            var self = this;
            document.addEventListener('click', function (event) {
                if (event.target.closest('#jux-tabbtn-statistics')) {
                    self.render();
                }
            });
        },

        render: function () {
            var pane = document.getElementById('jux-tab-statistics');
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

            var url = window.ApiClient.getUrl('JuxHomepage/Statistics', { userId: userId });

            window.ApiClient.getJSON(url).then(function (stats) {
                sections.innerHTML = _buildTilesHtml(stats, lang);
            }).catch(function (err) {
                console.error('[JellyUX] Statistics fetch failed:', err);
                sections.innerHTML = _buildTilesHtml(
                    { MoviesWatched: 0, SeriesTracked: 0, SeriesCompleted: 0, EpisodesWatched: 0 },
                    lang);
            });
        }
    };

    function _resolveLang() {
        return (document.documentElement.lang || 'en').toLowerCase().indexOf('fr') === 0 ? 'fr' : 'en';
    }

    function _buildTilesHtml(stats, lang) {
        var t = _labels[lang];
        return '<div class="jux-statistics-tiles" style="display:flex;flex-wrap:wrap;gap:1.5em;padding:0 2em;">' +
            _tile(stats.MoviesWatched, t.moviesWatched) +
            _tile(stats.SeriesTracked, t.seriesTracked) +
            _tile(stats.SeriesCompleted, t.seriesCompleted) +
            _tile(stats.EpisodesWatched, t.episodesWatched) +
            '</div>';
    }

    function _tile(value, label) {
        return '<div class="jux-statistics-tile" style="min-width:10em;">' +
            '<div class="jux-statistics-value" style="font-size:2.2em;font-weight:600;">' + _escHtml(String(value)) + '</div>' +
            '<div class="jux-statistics-label">' + _escHtml(label) + '</div>' +
            '</div>';
    }

    function _escHtml(str) {
        if (!str) { return ''; }
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    window.juxStatistics.init();

    // Guarded UMD-lite export (same convention as jux-watchlist.js), so Vitest can exercise the pure
    // functions directly without a real browser/DOM.
    if (typeof module !== 'undefined' && module.exports) {
        module.exports = {
            _escHtml: _escHtml,
            _tile: _tile,
            _buildTilesHtml: _buildTilesHtml
        };
    }
})();
