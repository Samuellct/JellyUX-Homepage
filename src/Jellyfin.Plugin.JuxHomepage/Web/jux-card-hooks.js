'use strict';

// JellyUX Watchlist toggle -- everywhere an item can be acted on.
// TODO_V3.md Phase 5.2, extended after the Phase 5 manual test round to also cover the item
// detail page's action row (next to the native "Add to favorites" heart) and the "More" (...)
// action-sheet menu, both on cards and on the detail page -- confirmed live on jellyux-test that
// both menu triggers (card `button[data-action="menu"]` and the detail page's `.btnMoreCommands`)
// open the exact same native `.actionSheetScroller` component, so a single injector covers both.
//
// A single MutationObserver on <body> catches every surface (card overlays, the detail page's
// button row, and action sheets) without needing window.JellyfinAPI (only populated after a Home
// visit) or any chunk-level patch of cardBuilder itself.
//
// TODO_V3.md Phase 7.3 (Infinite Scroll) is expected to extend this same file with its own card-level
// hook, rather than introducing a second, competing MutationObserver -- keep that in mind before
// adding a second observer here.
(function () {
    if (typeof window.juxCardHooks !== 'undefined') {
        return;
    }

    var _labels = {
        en: { add: 'Add to watchlist', remove: 'Remove from watchlist' },
        fr: { add: 'Ajouter à la watchlist', remove: 'Retirer de la watchlist' }
    };

    // How long a captured "More" menu context stays valid -- the action-sheet observer fires
    // within a frame or two of the menu opening; this just bounds how stale a context can be.
    var MENU_CONTEXT_TTL_MS = 5000;
    var _menuContext = null;

    window.juxCardHooks = {
        likedIds: null,

        init: function () {
            var self = this;
            self.likedIds = new Set();

            _hookExistingCards(self.likedIds);
            _hookDetailButton(self.likedIds);
            _observe(self.likedIds);
            _watchMenuTriggers();
            _watchNavigation(self);

            // Preloads the full liked-id set in one call (TODO_V3.md Phase 5.2's confirmed design:
            // pre-fetch once at load rather than a per-card request). Confirmed live on
            // jellyux-test that on a fresh page load this can resolve to an empty set if
            // ApiClient's user session isn't hydrated yet at the moment this script runs -- _waitForUserId
            // retries instead of giving up immediately, and _resyncAllButtons below repairs any
            // button that was already rendered (with the wrong icon) before the real data arrived.
            _loadLikedIds().then(function (ids) {
                ids.forEach(function (id) { self.likedIds.add(id); });
                _resyncAllButtons(self.likedIds);
            }).catch(function (err) {
                console.error('[JellyUX] Failed to preload watchlist ids:', err);
            });
        }
    };

    function _resolveLang() {
        return (document.documentElement.lang || 'en').toLowerCase().indexOf('fr') === 0 ? 'fr' : 'en';
    }

    function _waitForUserId(attemptsLeft) {
        attemptsLeft = attemptsLeft === undefined ? 25 : attemptsLeft;
        var userId = window.ApiClient && window.ApiClient.getCurrentUserId();
        if (userId) {
            return Promise.resolve(userId);
        }
        if (attemptsLeft <= 0) {
            return Promise.resolve(null);
        }
        return new Promise(function (resolve) {
            setTimeout(function () { resolve(_waitForUserId(attemptsLeft - 1)); }, 300);
        });
    }

    function _loadLikedIds() {
        return _waitForUserId().then(function (userId) {
            if (!userId || !window.ApiClient) {
                return new Set();
            }
            var url = window.ApiClient.getUrl('JuxHomepage/Watchlist/Ids', { userId: userId });
            return window.ApiClient.getJSON(url).then(function (ids) { return new Set(ids || []); });
        });
    }

    // -------------------------------------------------------------------------
    // DOM observation
    // -------------------------------------------------------------------------

    function _observe(likedIds) {
        var observer = new MutationObserver(function (mutations) {
            var mightHaveNewActionSheet = false;
            for (var i = 0; i < mutations.length; i++) {
                var added = mutations[i].addedNodes;
                for (var j = 0; j < added.length; j++) {
                    var node = added[j];
                    if (node.nodeType !== 1) { continue; }
                    _hookNode(node, likedIds);
                    mightHaveNewActionSheet = true;
                }
            }
            if (mightHaveNewActionSheet) {
                _addWatchlistMenuItem(likedIds);
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

        var hasDetailButton = (node.classList && node.classList.contains('btnUserRating')) ||
            (typeof node.querySelector === 'function' && node.querySelector('.btnUserRating'));
        if (hasDetailButton) {
            _hookDetailButton(likedIds);
        }
    }

    // -------------------------------------------------------------------------
    // Card overlay toggle button
    // -------------------------------------------------------------------------

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
        button.setAttribute('data-jux-item-id', itemInfo.id);

        var icon = document.createElement('span');
        icon.className = 'material-icons';
        icon.setAttribute('aria-hidden', 'true');
        button.appendChild(icon);

        _applyIconState(button, likedIds.has(itemInfo.id), _resolveLang());

        button.addEventListener('click', function (event) {
            event.preventDefault();
            event.stopPropagation();
            _toggle(itemInfo.id, likedIds);
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

    // -------------------------------------------------------------------------
    // Item detail page button, next to the native "Add to favorites" heart
    // -------------------------------------------------------------------------

    function _currentDetailItemId() {
        var match = /[#&?]id=([a-f0-9-]+)/i.exec(location.hash);
        return match ? match[1] : null;
    }

    function _hookDetailButton(likedIds) {
        var heart = document.querySelector('.btnUserRating.detailButton');
        if (!heart) {
            return;
        }

        var itemId = _currentDetailItemId();
        if (!itemId) {
            return;
        }

        var existing = heart.parentElement.querySelector('.jux-watchlist-detail-toggle');
        if (existing) {
            if (existing.getAttribute('data-jux-item-id') === itemId) {
                return;
            }
            // A different item's detail page reused the same button row (SPA navigation) --
            // the stale button belongs to the previous item and must not linger.
            existing.remove();
        }

        var button = document.createElement('button');
        button.type = 'button';
        button.setAttribute('is', 'emby-button');
        button.className = heart.className.replace(/\bbtnUserRating\b/, '').replace(/\s+/g, ' ').trim() + ' jux-watchlist-detail-toggle';
        button.setAttribute('data-jux-item-id', itemId);

        var icon = document.createElement('span');
        icon.className = 'material-icons';
        icon.setAttribute('aria-hidden', 'true');
        button.appendChild(icon);

        _applyIconState(button, likedIds.has(itemId), _resolveLang());

        button.addEventListener('click', function (event) {
            event.preventDefault();
            event.stopPropagation();
            _toggle(itemId, likedIds);
        });

        heart.insertAdjacentElement('afterend', button);
    }

    // -------------------------------------------------------------------------
    // "More" (...) action-sheet menu item -- shared by card menus and the detail page menu
    // -------------------------------------------------------------------------

    function _watchMenuTriggers() {
        document.body.addEventListener('mousedown', function (event) {
            var trigger = event.target.closest('button[data-action="menu"], .btnMoreCommands');
            if (!trigger) {
                return;
            }

            var card = trigger.closest('.card[data-id]');
            var itemId = card ? card.getAttribute('data-id') : _currentDetailItemId();
            _menuContext = itemId ? { itemId: itemId, ts: Date.now() } : null;
        }, true);
    }

    function _getActiveActionSheetScroller() {
        var scrollers = document.querySelectorAll('.actionSheetScroller');
        for (var i = scrollers.length - 1; i >= 0; i--) {
            if (scrollers[i].offsetParent !== null) {
                return scrollers[i];
            }
        }
        return null;
    }

    function _addWatchlistMenuItem(likedIds) {
        var scroller = _getActiveActionSheetScroller();
        if (!scroller) {
            return;
        }

        var existing = scroller.querySelector('.jux-watchlist-menuitem');
        var contextIsFresh = _menuContext && (Date.now() - _menuContext.ts) <= MENU_CONTEXT_TTL_MS;

        if (!contextIsFresh) {
            if (existing) { existing.remove(); }
            return;
        }

        if (existing) {
            if (existing.getAttribute('data-jux-item-id') === _menuContext.itemId) {
                return;
            }
            existing.remove();
        }

        var ref = scroller.querySelector('.actionSheetMenuItem');
        if (!ref) {
            return;
        }

        var itemId = _menuContext.itemId;
        var itemClass = ref.getAttribute('class').replace(/\bselected\b/g, '').replace(/\s+/g, ' ').trim();

        var wrapper = document.createElement('div');
        wrapper.innerHTML =
            '<button is="emby-button" type="button" class="' + _escHtml(itemClass) + ' jux-watchlist-menuitem" data-jux-item-id="' + _escHtml(itemId) + '">' +
            '<span class="actionsheetMenuItemIcon listItemIcon listItemIcon-transparent material-icons" aria-hidden="true"></span>' +
            '<div class="listItemBody actionsheetListItemBody"><div class="listItemBodyText actionSheetItemText"></div></div>' +
            '</button>';
        var button = wrapper.firstElementChild;
        _applyIconState(button, likedIds.has(itemId), _resolveLang());

        button.addEventListener('click', function (event) {
            event.preventDefault();
            event.stopPropagation();
            _toggle(itemId, likedIds);
        });

        var insertionPoint = scroller.querySelector('[data-id="playallfromhere"]') ||
            scroller.querySelector('[data-id="resume"]') ||
            scroller.querySelector('[data-id="play"]');

        if (insertionPoint) {
            insertionPoint.insertAdjacentElement('afterend', button);
        } else {
            ref.parentElement.insertBefore(button, ref);
        }
    }

    // -------------------------------------------------------------------------
    // Navigation re-arming (SPA route changes don't reload the page)
    // -------------------------------------------------------------------------

    function _watchNavigation(state) {
        window.addEventListener('hashchange', function () {
            setTimeout(function () { _hookDetailButton(state.likedIds); }, 400);
        });
    }

    // -------------------------------------------------------------------------
    // Shared icon/label state + toggle
    // -------------------------------------------------------------------------

    function _iconName(isLiked) {
        return isLiked ? 'bookmark' : 'bookmark_border';
    }

    // Applies the liked/unliked visual state to any of the three button kinds this file creates
    // (card overlay button, detail-page button, action-sheet menu item) -- each carries a
    // ".material-icons" child, and the menu item additionally carries a text label to keep in sync.
    function _applyIconState(el, isLiked, lang) {
        var icon = el.querySelector('.material-icons');
        if (icon) {
            icon.className = 'material-icons ' + _iconName(isLiked);
        }

        var label = isLiked ? _labels[lang].remove : _labels[lang].add;
        el.title = label;

        var textEl = el.querySelector('.actionSheetItemText');
        if (textEl) {
            textEl.textContent = label;
        }
    }

    function _toggle(itemId, likedIds) {
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

            _syncButtonsForItem(itemId, nowLiked);
        }).catch(function (err) {
            console.error('[JellyUX] Failed to toggle watchlist state:', err);
        });
    }

    function _syncButtonsForItem(itemId, isLiked) {
        var lang = _resolveLang();
        var matches = document.querySelectorAll('[data-jux-item-id="' + CSS.escape(itemId) + '"]');
        Array.prototype.forEach.call(matches, function (el) { _applyIconState(el, isLiked, lang); });
    }

    // Repairs any button that rendered before the preloaded liked-id set finished loading (see
    // the race documented on window.juxCardHooks.init above).
    function _resyncAllButtons(likedIds) {
        var lang = _resolveLang();
        var all = document.querySelectorAll('[data-jux-item-id]');
        Array.prototype.forEach.call(all, function (el) {
            _applyIconState(el, likedIds.has(el.getAttribute('data-jux-item-id')), lang);
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

    window.juxCardHooks.init();

    // Guarded UMD-lite export (same convention as jux-homepage.js), so Vitest can exercise the pure
    // functions directly without a real browser/DOM.
    if (typeof module !== 'undefined' && module.exports) {
        module.exports = {
            _iconName: _iconName,
            _readCardItemInfo: _readCardItemInfo,
            _currentDetailItemId: _currentDetailItemId,
            _escHtml: _escHtml
        };
    }
})();
