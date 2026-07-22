'use strict';

// JellyUX collections: "Included In" section on the item detail page + sort controls on the
// Collection (BoxSet) page. TODO_V3.md Phase 7.2.
//
// Page-hook idiom: same as jux-series-flatten.js -- item id read from location.hash, a dedicated
// MutationObserver on document.body, a hashchange re-arm. Confirmed with the user to keep this over
// KefinTweaks' window.Emby.Page.onViewShow monkey-patch.
//
// Backend: the reverse index (item -> collections it belongs to) already existed since TODO_V3.md
// Phase 4.3 (Library/CollectionsIndexCacheService.cs, refreshed daily) -- this file only consumes the
// new GET JuxHomepage/Collections/IncludedIn/{itemId} endpoint added in Phase 7.2.
(function () {
    if (typeof window.juxCollections !== 'undefined') {
        return;
    }

    var _labels = {
        en: {
            includedIn: 'Included In',
            sortTitle: 'Sort Title',
            releaseDate: 'Release Date',
            dateAdded: 'Date Added',
            communityRating: 'Community Rating',
            criticRating: 'Critic Rating',
            sortDialogTitle: 'Sort Collection'
        },
        fr: {
            includedIn: 'Fait partie de',
            sortTitle: 'Titre',
            releaseDate: 'Date de sortie',
            dateAdded: 'Date d’ajout',
            communityRating: 'Note communauté',
            criticRating: 'Note critique',
            sortDialogTitle: 'Trier la collection'
        }
    };

    window.juxCollections = {
        init: function () {
            var observer = new MutationObserver(function () {
                _tryIncludedIn();
                _tryCollectionSort();
            });
            observer.observe(document.body, { childList: true, subtree: true });

            window.addEventListener('hashchange', function () {
                setTimeout(function () {
                    _tryIncludedIn();
                    _tryCollectionSort();
                }, 400);
            });

            _tryIncludedIn();
            _tryCollectionSort();
        }
    };

    function _resolveLang() {
        return (document.documentElement.lang || 'en').toLowerCase().indexOf('fr') === 0 ? 'fr' : 'en';
    }

    function _currentDetailItemId() {
        var match = /[#&?]id=([a-f0-9-]+)/i.exec(location.hash);
        return match ? match[1] : null;
    }

    function _isSupportedForCollections(item) {
        return !!item && (item.Type === 'Movie' || item.Type === 'Series');
    }

    function _isBoxSet(item) {
        return !!item && item.Type === 'BoxSet';
    }

    function _sortFields() {
        var t = _labels[_resolveLang()];
        return [
            { value: 'SortName', label: t.sortTitle },
            { value: 'PremiereDate', label: t.releaseDate },
            { value: 'DateCreated', label: t.dateAdded },
            { value: 'CommunityRating', label: t.communityRating },
            { value: 'CriticRating', label: t.criticRating }
        ];
    }

    function _labelFor(options, value) {
        for (var i = 0; i < options.length; i++) {
            if (options[i].value === value) { return options[i].label; }
        }
        return options[0] && options[0].label;
    }

    function _storageKeys(collectionId) {
        return {
            sort: 'jux-collection-sort-' + collectionId,
            order: 'jux-collection-order-' + collectionId
        };
    }

    // -------------------------------------------------------------------------
    // "Included In" -- item detail page (Movie/Series only)
    // -------------------------------------------------------------------------

    function _tryIncludedIn() {
        var itemId = _currentDetailItemId();
        if (!itemId) {
            return;
        }

        var activePage = document.querySelector('.libraryPage:not(.hide)');
        if (!activePage) {
            return;
        }

        if (activePage.dataset.juxCollectionsChecked === itemId || activePage.querySelector('.jux-collections-section')) {
            return;
        }

        if (!window.ApiClient) {
            return;
        }

        var userId = window.ApiClient.getCurrentUserId();
        if (!userId) {
            return;
        }

        window.ApiClient.getItem(userId, itemId).then(function (item) {
            if (!_isSupportedForCollections(item)) {
                return;
            }

            var url = window.ApiClient.getUrl('JuxHomepage/Collections/IncludedIn/' + itemId);
            return window.ApiClient.getJSON(url).then(function (refs) {
                activePage.dataset.juxCollectionsChecked = itemId;

                if (!refs || refs.length === 0) {
                    return;
                }

                var api = window.JellyfinAPI;
                if (!api || !api.cardBuilder) {
                    return;
                }

                var ids = refs.map(function (r) { return r.CollectionId; }).join(',');
                return window.ApiClient.getItems(userId, { Ids: ids, Fields: 'PrimaryImageAspectRatio' }).then(function (result) {
                    var boxSets = (result && result.Items) || [];
                    if (boxSets.length === 0) {
                        return;
                    }

                    var section = _buildCollectionsSectionSkeleton(_labels[_resolveLang()].includedIn);
                    var itemsContainer = section.querySelector('.itemsContainer');
                    itemsContainer.innerHTML = api.cardBuilder.getCardsHtml({
                        items: boxSets,
                        shape: api.getPortraitShape(false),
                        showTitle: true,
                        overlayText: false,
                        centerText: true,
                        lazy: true,
                        lines: 2
                    });
                    _loadCardImages(itemsContainer);

                    var similar = activePage.querySelector('#similarCollapsible');
                    if (similar) {
                        similar.parentNode.insertBefore(section, similar);
                    } else {
                        var detailPageContent = activePage.querySelector('.detailPageContent');
                        if (detailPageContent) {
                            detailPageContent.appendChild(section);
                        }
                    }
                });
            });
        }).catch(function (err) {
            console.error('[JellyUX] Included In check failed:', err);
        });
    }

    function _buildCollectionsSectionSkeleton(title) {
        var wrapper = document.createElement('div');
        wrapper.innerHTML =
            '<div class="verticalSection jux-collections-section">' +
            '<div class="sectionTitleContainer sectionTitleContainer-cards padded-left">' +
            '<h2 class="sectionTitle sectionTitle-cards">' + _escHtml(title) + '</h2>' +
            '</div>' +
            '<div is="emby-scroller" class="padded-top-focusscale padded-bottom-focusscale" data-centerfocus="true">' +
            '<div is="emby-itemscontainer" class="itemsContainer scrollSlider focuscontainer-x"></div>' +
            '</div>' +
            '</div>';
        return wrapper.firstElementChild;
    }

    // -------------------------------------------------------------------------
    // Collection (BoxSet) page sort
    // -------------------------------------------------------------------------

    function _tryCollectionSort() {
        var itemId = _currentDetailItemId();
        if (!itemId) {
            return;
        }

        var activePage = document.querySelector('.libraryPage:not(.hide)');
        if (!activePage) {
            return;
        }

        if (activePage.querySelector('.jux-collection-sort-btn')) {
            return;
        }

        if (!window.ApiClient) {
            return;
        }

        var userId = window.ApiClient.getCurrentUserId();
        if (!userId) {
            return;
        }

        window.ApiClient.getItem(userId, itemId).then(function (item) {
            if (!_isBoxSet(item)) {
                return;
            }

            // Confirmed live on jellyux-test: the collection's item grid is a plain .itemsContainer
            // carrying the collectionItemsContainer class, inside a .verticalSection whose own
            // .sectionTitle is the natural place for a sort button (no separate ".collectionItems"
            // wrapper on this Jellyfin Web build).
            var itemsContainer = activePage.querySelector('.itemsContainer.collectionItemsContainer');
            if (!itemsContainer) {
                return;
            }

            var section = itemsContainer.closest('.verticalSection');
            var titleContainer = section ? section.querySelector('.sectionTitleContainer, .sectionTitle') : null;
            if (!titleContainer) {
                return;
            }

            var keys = _storageKeys(itemId);
            var state = {
                sortBy: localStorage.getItem(keys.sort) || 'SortName',
                sortOrder: localStorage.getItem(keys.order) || 'Ascending'
            };

            var button = document.createElement('button');
            button.type = 'button';
            button.className = 'jux-sort-button jux-collection-sort-btn';
            button.innerHTML =
                '<span class="material-icons sort" aria-hidden="true"></span>' +
                '<span class="jux-sort-button-label">' + _escHtml(_labelFor(_sortFields(), state.sortBy)) + '</span>' +
                '<span class="material-icons arrow_drop_down" aria-hidden="true"></span>';

            button.addEventListener('click', function () {
                if (!window.JuxUI) {
                    return;
                }

                window.JuxUI.openSortDialog({
                    title: _labels[_resolveLang()].sortDialogTitle,
                    sortOptions: _sortFields(),
                    currentSortBy: state.sortBy,
                    currentSortOrder: state.sortOrder,
                    onChange: function (sortBy, sortOrder) {
                        state.sortBy = sortBy;
                        state.sortOrder = sortOrder;
                        localStorage.setItem(keys.sort, sortBy);
                        localStorage.setItem(keys.order, sortOrder);
                        button.querySelector('.jux-sort-button-label').textContent = _labelFor(_sortFields(), sortBy);
                        _resortCollection(itemId, itemsContainer, state);
                    }
                });
            });

            titleContainer.appendChild(button);

            if (state.sortBy !== 'SortName' || state.sortOrder !== 'Ascending') {
                _resortCollection(itemId, itemsContainer, state);
            }
        }).catch(function (err) {
            console.error('[JellyUX] Collection sort setup failed:', err);
        });
    }

    function _resortCollection(collectionId, itemsContainer, state) {
        var api = window.JellyfinAPI;
        if (!api || !api.cardBuilder || !window.ApiClient) {
            return;
        }

        var userId = window.ApiClient.getCurrentUserId();
        var url = window.ApiClient.getUrl('Items', {
            userId: userId,
            ParentId: collectionId,
            SortBy: state.sortBy,
            SortOrder: state.sortOrder,
            Fields: 'PrimaryImageAspectRatio'
        });

        window.ApiClient.getJSON(url).then(function (result) {
            var items = (result && result.Items) || [];
            itemsContainer.innerHTML = api.cardBuilder.getCardsHtml({
                items: items,
                shape: api.getPortraitShape(false),
                showTitle: true,
                overlayText: false,
                centerText: true,
                lazy: true,
                lines: 2
            });
            _loadCardImages(itemsContainer);
        }).catch(function (err) {
            console.error('[JellyUX] Collection re-sort fetch failed:', err);
        });
    }

    // See jux-watchlist.js -- a JUX-built card grid never gets Jellyfin's native lazy-image loading,
    // so the background image has to be applied manually.
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

    function _escHtml(str) {
        if (!str) { return ''; }
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    window.juxCollections.init();

    // Guarded UMD-lite export (same convention as jux-watchlist.js), so Vitest can exercise the pure
    // functions directly without a real browser/DOM.
    if (typeof module !== 'undefined' && module.exports) {
        module.exports = {
            _isSupportedForCollections: _isSupportedForCollections,
            _isBoxSet: _isBoxSet,
            _sortFields: _sortFields,
            _labelFor: _labelFor,
            _storageKeys: _storageKeys,
            _buildCollectionsSectionSkeleton: _buildCollectionsSectionSkeleton,
            _currentDetailItemId: _currentDetailItemId,
            _escHtml: _escHtml
        };
    }
})();
