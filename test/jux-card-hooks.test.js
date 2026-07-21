import { describe, it, expect } from 'vitest';
import juxCardHooks from '../src/Jellyfin.Plugin.JuxHomepage/Web/jux-card-hooks.js';

const { _iconName, _readCardItemInfo, _currentDetailItemId, _escHtml } = juxCardHooks;

describe('_iconName', () => {
    it('returns the filled bookmark icon when liked', () => {
        expect(_iconName(true)).toBe('bookmark');
    });

    it('returns the outline bookmark icon when not liked', () => {
        expect(_iconName(false)).toBe('bookmark_border');
    });
});

describe('_readCardItemInfo', () => {
    it('reads the item id and type from a card element\'s data attributes', () => {
        document.body.innerHTML = '<div class="card" data-id="abc123" data-type="Movie"></div>';
        const card = document.querySelector('.card');

        expect(_readCardItemInfo(card)).toEqual({ id: 'abc123', type: 'Movie' });
    });

    it('returns null when the card has no data-id', () => {
        document.body.innerHTML = '<div class="card"></div>';
        const card = document.querySelector('.card');

        expect(_readCardItemInfo(card)).toBeNull();
    });
});

describe('_currentDetailItemId', () => {
    it('extracts the item id from a details page hash', () => {
        window.location.hash = '#/details?id=abc123def456';
        expect(_currentDetailItemId()).toBe('abc123def456');
    });

    it('returns null when the hash has no id', () => {
        window.location.hash = '#/home';
        expect(_currentDetailItemId()).toBeNull();
    });
});

describe('_escHtml', () => {
    it('escapes HTML special characters', () => {
        expect(_escHtml('<b>"Tom & Jerry"</b>')).toBe('&lt;b&gt;&quot;Tom &amp; Jerry&quot;&lt;/b&gt;');
    });

    it('returns an empty string for falsy input', () => {
        expect(_escHtml('')).toBe('');
        expect(_escHtml(null)).toBe('');
    });
});
