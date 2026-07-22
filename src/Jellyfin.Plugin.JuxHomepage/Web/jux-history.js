'use strict';

// JellyUX Movie History tab rendering.
// TODO_V3.md Phase 6.2. Same conventions as jux-watchlist.js/jux-progress.js: click listener
// delegated on `document`, rendered via window.JellyfinAPI.cardBuilder.getCardsHtml + the same manual
// _loadCardImages workaround.
//
// Unlike Series Progress, each row here is a plain BaseItemDto (see
// Watchlist/MovieHistoryViewService.cs -- history rows need no extra per-item fields, the last-played
// date is already on BaseItemDto.UserData), so this file only needs to add one thing per card: a
// favorite-toggle button, via Jellyfin's own native endpoint (the same one the classic favorite heart
// uses elsewhere in the app) -- scoped to this view only, not a global card hook.
(function () {
    if (typeof window.juxHistory !== 'undefined') {
        return;
    }

    var _labels = {
        en: {
            empty: 'No watched movies yet.',
            sortLastPlayed: 'Last Watched',
            sortName: 'Name',
            addFavorite: 'Add to favorites',
            removeFavorite: 'Remove from favorites'
        },
        fr: {
            empty: 'Aucun film vu pour le moment.',
            sortLastPlayed: 'Dernier vu',
            sortName: 'Nom',
            addFavorite: 'Ajouter aux favoris',
            removeFavorite: 'Retirer des favoris'
        }
    };

    window.juxHistory = {
        state: { sortBy: 'LastPlayed' },

        init: function () {
            var self = this;
            document.addEventListener('click', function (event) {
                if (event.target.closest('#jux-tabbtn-history')) {
                    self.render();
                }
            });
        },

        render: function () {
            var self = this;
            var pane = document.getElementById('jux-tab-history');
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

            var url = window.ApiClient.getUrl('JuxHomepage/MovieHistory/Items', {
                userId: userId,
                sortBy: self.state.sortBy,
                sortOrder: 'Descending',
                startIndex: 0,
                limit: 100
            });

            window.ApiClient.getJSON(url).then(function (result) {
                _renderItems(sections, result, lang);
            }).catch(function (err) {
                console.error('[JellyUX] Movie History fetch failed:', err);
                _renderItems(sections, { Items: [], TotalRecordCount: 0 }, lang);
            });
        }
    };

    function _resolveLang() {
        return (document.documentElement.lang || 'en').toLowerCase().indexOf('fr') === 0 ? 'fr' : 'en';
    }

    function _buildControlsHtml(lang, state) {
        var t = _labels[lang];
        return '<div class="jux-history-controls" style="display:flex;gap:1em;padding:0 2em 1em;">' +
            '<select id="jux-history-sort" is="emby-select" class="emby-select">' +
            _option('LastPlayed', t.sortLastPlayed, state.sortBy) +
            _option('Name', t.sortName, state.sortBy) +
            '</select>' +
            '</div>' +
            '<div class="jux-history-items itemsContainer vertical-wrap padded-left padded-right"></div>' +
            '<div class="jux-history-empty" style="display:none;padding:0 2em;">' + _escHtml(t.empty) + '</div>';
    }

    function _option(value, label, current) {
        return '<option value="' + _escHtml(value) + '"' + (value === current ? ' selected' : '') + '>' + _escHtml(label) + '</option>';
    }

    function _wireControls(sections, state, onChange) {
        var sortEl = sections.querySelector('#jux-history-sort');
        if (sortEl) {
            sortEl.addEventListener('change', function () {
                state.sortBy = sortEl.value;
                onChange();
            });
        }
    }

    function _renderItems(sections, result, lang) {
        var itemsContainer = sections.querySelector('.jux-history-items');
        var emptyEl = sections.querySelector('.jux-history-empty');
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
            overlayText: false,
            overlayPlayButton: true,
            centerText: true,
            showYear: true,
            lazy: true,
            lines: 2
        });

        _loadCardImages(itemsContainer);
        _decorateCards(itemsContainer, items, lang);
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

    function _decorateCards(container, items, lang) {
        var t = _labels[lang];
        var byId = {};
        for (var i = 0; i < items.length; i++) {
            byId[items[i].Id] = items[i];
        }

        var overlays = container.querySelectorAll('.cardOverlayContainer');
        Array.prototype.forEach.call(overlays, function (overlay) {
            var card = overlay.closest('.card[data-id]');
            var item = card && byId[card.getAttribute('data-id')];
            if (!item) { return; }

            var isFavorite = !!(item.UserData && item.UserData.IsFavorite);

            var button = document.createElement('button');
            button.type = 'button';
            button.setAttribute('is', 'paper-icon-button-light');
            button.className = 'cardOverlayButton cardOverlayButton-hover itemAction paper-icon-button-light jux-history-favorite-toggle';
            button.title = isFavorite ? t.removeFavorite : t.addFavorite;

            var icon = document.createElement('span');
            icon.className = 'material-icons ' + (isFavorite ? 'favorite' : 'favorite_border');
            icon.setAttribute('aria-hidden', 'true');
            button.appendChild(icon);

            button.addEventListener('click', function (event) {
                event.preventDefault();
                event.stopPropagation();
                _toggleFavorite(item.Id, !isFavorite, button, icon, t);
            });

            overlay.appendChild(button);
        });
    }

    // Favorite toggle via Jellyfin's own native endpoint (POST/DELETE
    // /Users/{userId}/FavoriteItems/{itemId}) -- no JellyUX endpoint needed, same convention already
    // used for the Watchlist toggle's underlying Likes field.
    function _toggleFavorite(itemId, nowFavorite, button, icon, t) {
        if (!window.ApiClient) { return; }
        var userId = window.ApiClient.getCurrentUserId();

        window.ApiClient.updateFavoriteStatus(userId, itemId, nowFavorite).then(function () {
            icon.className = 'material-icons ' + (nowFavorite ? 'favorite' : 'favorite_border');
            button.title = nowFavorite ? t.removeFavorite : t.addFavorite;
        }).catch(function (err) {
            console.error('[JellyUX] Failed to toggle favorite state:', err);
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

    window.juxHistory.init();

    // Guarded UMD-lite export (same convention as jux-watchlist.js), so Vitest can exercise the pure
    // functions directly without a real browser/DOM.
    if (typeof module !== 'undefined' && module.exports) {
        module.exports = {
            _escHtml: _escHtml,
            _option: _option,
            _buildControlsHtml: _buildControlsHtml
        };
    }
})();
