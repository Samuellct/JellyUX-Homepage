import { describe, it, expect } from 'vitest';
import juxCardHooks from '../src/Jellyfin.Plugin.JuxHomepage/Web/jux-card-hooks.js';

const { _iconName, _readCardItemInfo } = juxCardHooks;

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
