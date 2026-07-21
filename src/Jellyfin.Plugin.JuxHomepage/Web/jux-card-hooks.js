'use strict';

// JellyUX card-overlay Watchlist toggle button.
// TODO_V3.md Phase 5.2. Adds a bookmark-style toggle button to every card's overlay, app-wide (not
// just JellyUX's own home-page sections) -- confirmed by direct DOM inspection that every card
// (library pages, search, home, detail pages) shares the same ".card[data-id][data-type]" >
// ".cardOverlayContainer" structure, so a single MutationObserver on <body> catches all of them
// without needing window.JellyfinAPI (which is only populated after a Home-page visit) or any
// chunk-level patch of cardBuilder itself.
//
// TODO_V3.md Phase 7.3 (Infinite Scroll) is expected to extend this same file with its own card-level
// hook, rather than introducing a second, competing MutationObserver -- keep that in mind before
// adding a second observer here.
(function () {
    if (typeof window.juxCardHooks !== 'undefined') {
        return;
    }

    window.juxCardHooks = {
        likedIds: null,

        init: function () {
            var self = this;
            _loadLikedIds().then(function (ids) {
                self.likedIds = ids;
                _hookExistingCards(ids);
                _observe(ids);
            }).catch(function (err) {
                console.error('[JellyUX] Failed to preload watchlist ids:', err);
                self.likedIds = new Set();
                _observe(self.likedIds);
            });
        }
    };

    function _loadLikedIds() {
        if (!window.ApiClient) {
            return Promise.resolve(new Set());
        }

        var userId = window.ApiClient.getCurrentUserId();
        if (!userId) {
            return Promise.resolve(new Set());
        }

        var url = window.ApiClient.getUrl('JuxHomepage/Watchlist/Ids', { userId: userId });
        return window.ApiClient.getJSON(url).then(function (ids) {
            return new Set(ids || []);
        });
    }

    function _observe(likedIds) {
        var observer = new MutationObserver(function (mutations) {
            for (var i = 0; i < mutations.length; i++) {
                var added = mutations[i].addedNodes;
                for (var j = 0; j < added.length; j++) {
                    var node = added[j];
                    if (node.nodeType !== 1) { continue; }
                    _hookNode(node, likedIds);
                }
            }
        });

        observer.observe(document.body, { childList: true, subtree: true });
    }

    function _hookExistingCards(likedIds) {
        var containers = document.querySelectorAll('.cardOverlayContainer');
        Array.prototype.forEach.call(containers, function (c) { _hookOverlay(c, likedIds); });
    }

    function _hookNode(node, likedIds) {
        if (node.classList && node.classList.contains('cardOverlayContainer')) {
            _hookOverlay(node, likedIds);
        }

        var descendants = typeof node.querySelectorAll === 'function'
            ? node.querySelectorAll('.cardOverlayContainer')
            : [];
        Array.prototype.forEach.call(descendants, function (c) { _hookOverlay(c, likedIds); });
    }

    function _hookOverlay(overlayEl, likedIds) {
        if (overlayEl.getAttribute('data-jux-hooked')) {
            return;
        }

        var card = overlayEl.closest('.card');
        var itemInfo = card ? _readCardItemInfo(card) : null;
        if (!itemInfo) {
            return;
        }

        overlayEl.setAttribute('data-jux-hooked', '1');

        var button = document.createElement('button');
        button.type = 'button';
        button.setAttribute('is', 'paper-icon-button-light');
        button.className = 'cardOverlayButton cardOverlayButton-hover itemAction paper-icon-button-light jux-watchlist-toggle';
        button.title = 'Watchlist';

        var icon = document.createElement('span');
        icon.className = 'material-icons ' + _iconName(likedIds.has(itemInfo.id));
        icon.setAttribute('aria-hidden', 'true');
        button.appendChild(icon);

        button.addEventListener('click', function (event) {
            event.preventDefault();
            event.stopPropagation();
            _toggle(itemInfo.id, likedIds, icon);
        });

        overlayEl.appendChild(button);
    }

    function _readCardItemInfo(card) {
        var id = card.getAttribute('data-id');
        if (!id) {
            return null;
        }

        return { id: id, type: card.getAttribute('data-type') };
    }

    function _iconName(isLiked) {
        return isLiked ? 'bookmark' : 'bookmark_border';
    }

    function _toggle(itemId, likedIds, icon) {
        if (!window.ApiClient) {
            return;
        }

        var userId = window.ApiClient.getCurrentUserId();
        var nowLiked = !likedIds.has(itemId);

        window.ApiClient.updateUserItemRating(userId, itemId, nowLiked).then(function () {
            if (nowLiked) {
                likedIds.add(itemId);
            } else {
                likedIds.delete(itemId);
            }

            icon.className = 'material-icons ' + _iconName(nowLiked);
        }).catch(function (err) {
            console.error('[JellyUX] Failed to toggle watchlist state:', err);
        });
    }

    window.juxCardHooks.init();

    // Guarded UMD-lite export (same convention as jux-homepage.js), so Vitest can exercise the pure
    // functions directly without a real browser/DOM.
    if (typeof module !== 'undefined' && module.exports) {
        module.exports = {
            _iconName: _iconName,
            _readCardItemInfo: _readCardItemInfo
        };
    }
})();
