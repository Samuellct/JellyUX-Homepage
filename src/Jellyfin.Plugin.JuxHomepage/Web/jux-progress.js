'use strict';

// JellyUX Series Progress tab rendering.
// TODO_V3.md Phase 6.1. Same conventions as jux-watchlist.js: loaded as a plain <script src>, click
// listener delegated on `document` (the tab bar/button is recreated on every view mount), rendered via
// window.JellyfinAPI.cardBuilder.getCardsHtml + the same manual _loadCardImages workaround (a plain
// <div> pane never gets Jellyfin's native lazy-image loading).
//
// Each row is a series -- not just a BaseItemDto, but a { Item, WatchedEpisodes, TotalEpisodes,
// LastEpisodeName, ... } object (see Watchlist/SeriesProgressViewService.cs), so after cardBuilder
// renders the poster/name, this file decorates each card with a small progress line and a "mark as
// watched" button.
(function () {
    if (typeof window.juxProgress !== 'undefined') {
        return;
    }

    var _labels = {
        en: {
            empty: 'No series in progress yet.',
            sortLastPlayed: 'Last Watched',
            sortName: 'Name',
            episodesWatched: '{watched}/{total} episodes',
            lastEpisode: 'Last: {code} - {name}',
            markWatched: 'Mark as watched'
        },
        fr: {
            empty: 'Aucune série en cours.',
            sortLastPlayed: 'Dernier vu',
            sortName: 'Nom',
            episodesWatched: '{watched}/{total} épisodes',
            lastEpisode: 'Dernier : {code} - {name}',
            markWatched: 'Marquer comme vu'
        }
    };

    window.juxProgress = {
        state: { sortBy: 'LastPlayed' },

        init: function () {
            var self = this;
            document.addEventListener('click', function (event) {
                if (event.target.closest('#jux-tabbtn-progress')) {
                    self.render();
                }
            });
        },

        render: function () {
            var self = this;
            var pane = document.getElementById('jux-tab-progress');
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

            sections.innerHTML = _buildControlsHtml(lang, self.state);
            _wireControls(sections, self.state, function () {
                self.render();
            });

            var url = window.ApiClient.getUrl('JuxHomepage/SeriesProgress/Items', {
                userId: userId,
                sortBy: self.state.sortBy,
                sortOrder: 'Descending',
                startIndex: 0,
                limit: 100
            });

            window.ApiClient.getJSON(url).then(function (result) {
                _renderItems(sections, result, lang);
            }).catch(function (err) {
                console.error('[JellyUX] Series Progress fetch failed:', err);
                _renderItems(sections, { Items: [], TotalRecordCount: 0 }, lang);
            });
        }
    };

    function _resolveLang() {
        return (document.documentElement.lang || 'en').toLowerCase().indexOf('fr') === 0 ? 'fr' : 'en';
    }

    function _buildControlsHtml(lang, state) {
        var t = _labels[lang];
        return '<div class="jux-progress-controls" style="display:flex;gap:1em;padding:0 2em 1em;">' +
            '<select id="jux-progress-sort" is="emby-select" class="emby-select">' +
            _option('LastPlayed', t.sortLastPlayed, state.sortBy) +
            _option('Name', t.sortName, state.sortBy) +
            '</select>' +
            '</div>' +
            '<div class="jux-progress-items itemsContainer vertical-wrap padded-left padded-right"></div>' +
            '<div class="jux-progress-empty" style="display:none;padding:0 2em;">' + _escHtml(t.empty) + '</div>';
    }

    function _option(value, label, current) {
        return '<option value="' + _escHtml(value) + '"' + (value === current ? ' selected' : '') + '>' + _escHtml(label) + '</option>';
    }

    function _wireControls(sections, state, onChange) {
        var sortEl = sections.querySelector('#jux-progress-sort');
        if (sortEl) {
            sortEl.addEventListener('change', function () {
                state.sortBy = sortEl.value;
                onChange();
            });
        }
    }

    function _renderItems(sections, result, lang) {
        var itemsContainer = sections.querySelector('.jux-progress-items');
        var emptyEl = sections.querySelector('.jux-progress-empty');
        if (!itemsContainer || !emptyEl) {
            return;
        }

        var rows = (result && result.Items) || [];
        if (rows.length === 0) {
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
            items: rows.map(function (r) { return r.Item; }),
            shape: api.getPortraitShape(false),
            showTitle: true,
            overlayText: false,
            overlayPlayButton: true,
            centerText: true,
            showYear: true,
            lazy: true,
            lines: 2
        });

        _loadCardImages(itemsContainer);
        _decorateCards(itemsContainer, rows, lang);
    }

    // See jux-watchlist.js -- a plain <div> container never gets Jellyfin's native lazy-image
    // loading, so the background image has to be applied manually.
    function _loadCardImages(container) {
        var lazyImages = container.querySelectorAll('.cardImageContainer.lazy[data-src]');
        Array.prototype.forEach.call(lazyImages, function (el) {
            var src = el.getAttribute('data-src');
            if (!src) { return; }
            el.style.backgroundImage = 'url(\'' + src + '\')';
            el.classList.remove('lazy');
            el.classList.add('lazy-image-fadein-fast');
        });
    }

    // Adds a small progress line ("3/10 episodes", "Last: S01E04 - Name") and a "mark as watched"
    // button under each card. cardBuilder only knows about the BaseItemDto -- it has no notion of our
    // extra progress fields -- so this walks the freshly-rendered cards and matches each back to its
    // row by item id.
    function _decorateCards(container, rows, lang) {
        var t = _labels[lang];
        var byId = {};
        for (var i = 0; i < rows.length; i++) {
            byId[rows[i].Item.Id] = rows[i];
        }

        var cards = container.querySelectorAll('.card[data-id]');
        Array.prototype.forEach.call(cards, function (card) {
            var row = byId[card.getAttribute('data-id')];
            if (!row) { return; }

            var info = document.createElement('div');
            info.className = 'jux-progress-info';
            info.style.padding = '0.2em 0.5em';
            info.style.fontSize = '0.85em';

            var countsLine = document.createElement('div');
            countsLine.textContent = _formatTemplate(t.episodesWatched, { watched: row.WatchedEpisodes, total: row.TotalEpisodes });
            info.appendChild(countsLine);

            if (row.LastEpisodeName) {
                var lastLine = document.createElement('div');
                lastLine.textContent = _formatTemplate(t.lastEpisode, {
                    code: _formatEpisodeCode(row.LastEpisodeSeasonNumber, row.LastEpisodeIndexNumber),
                    name: row.LastEpisodeName
                });
                info.appendChild(lastLine);
            }

            var button = document.createElement('button');
            button.type = 'button';
            button.className = 'button-flat jux-progress-mark-watched';
            button.textContent = t.markWatched;
            button.addEventListener('click', function (event) {
                event.preventDefault();
                event.stopPropagation();
                _markSeriesWatched(row.Item.Id, card, button, t);
            });
            info.appendChild(button);

            card.appendChild(info);
        });
    }

    // Marks the whole series as watched via Jellyfin's own native endpoint (same approach as the
    // favorite toggle in jux-history.js) -- POST /Users/{userId}/PlayedItems/{itemId} cascades to
    // every episode server-side, no JellyUX endpoint needed. The Series Progress list itself is a
    // once-a-day server cache (WatchlistDailyRefreshTask), so rather than re-fetching (which would
    // still show the old counts until the next refresh), this optimistically removes the card from
    // view -- it reappears correctly counted after the next daily cache refresh.
    function _markSeriesWatched(itemId, card, button, t) {
        if (!window.ApiClient) { return; }
        var userId = window.ApiClient.getCurrentUserId();

        button.disabled = true;
        window.ApiClient.markPlayed(userId, itemId, new Date()).then(function () {
            card.style.display = 'none';
        }).catch(function (err) {
            console.error('[JellyUX] Failed to mark series as watched:', err);
            button.disabled = false;
        });
    }

    function _formatEpisodeCode(seasonNumber, episodeNumber) {
        if (seasonNumber == null || episodeNumber == null) {
            return '';
        }
        return 'S' + _padTwoDigits(seasonNumber) + 'E' + _padTwoDigits(episodeNumber);
    }

    function _padTwoDigits(value) {
        var str = String(value);
        return str.length >= 2 ? str : '0' + str;
    }

    function _formatTemplate(template, values) {
        return template.replace(/\{(\w+)\}/g, function (match, key) {
            return Object.prototype.hasOwnProperty.call(values, key) ? String(values[key]) : match;
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

    window.juxProgress.init();

    // Guarded UMD-lite export (same convention as jux-watchlist.js), so Vitest can exercise the pure
    // functions directly without a real browser/DOM.
    if (typeof module !== 'undefined' && module.exports) {
        module.exports = {
            _escHtml: _escHtml,
            _option: _option,
            _buildControlsHtml: _buildControlsHtml,
            _formatEpisodeCode: _formatEpisodeCode,
            _formatTemplate: _formatTemplate
        };
    }
})();
