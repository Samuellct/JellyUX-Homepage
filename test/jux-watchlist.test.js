import { describe, it, expect } from 'vitest';
import juxWatchlist from '../src/Jellyfin.Plugin.JuxHomepage/Web/jux-watchlist.js';

const { _escHtml, _option, _buildControlsHtml } = juxWatchlist;

describe('_escHtml', () => {
    it('escapes ampersands, angle brackets, and quotes', () => {
        expect(_escHtml('<b>Tom & "Jerry"</b>')).toBe('&lt;b&gt;Tom &amp; &quot;Jerry&quot;&lt;/b&gt;');
    });

    it('returns an empty string for empty or nullish input', () => {
        expect(_escHtml('')).toBe('');
        expect(_escHtml(null)).toBe('');
        expect(_escHtml(undefined)).toBe('');
    });
});

describe('_option', () => {
    it('marks the option matching the current value as selected', () => {
        expect(_option('Name', 'Nom', 'Name')).toBe('<option value="Name" selected>Nom</option>');
    });

    it('does not mark an option that does not match the current value', () => {
        expect(_option('Name', 'Nom', 'DateAdded')).toBe('<option value="Name">Nom</option>');
    });

    it('escapes both value and label', () => {
        expect(_option('<x>', '<y>', 'z')).toBe('<option value="&lt;x&gt;">&lt;y&gt;</option>');
    });
});

describe('_buildControlsHtml', () => {
    it('renders both select controls with the current state pre-selected', () => {
        const html = _buildControlsHtml('en', { sortBy: 'Name', includeItemTypes: 'Movie' });

        expect(html).toContain('id="jux-watchlist-sort"');
        expect(html).toContain('id="jux-watchlist-type"');
        expect(html).toContain('value="Name" selected');
        expect(html).toContain('value="Movie" selected');
    });

    it('includes the localized empty-state message', () => {
        const htmlEn = _buildControlsHtml('en', { sortBy: 'DateAdded', includeItemTypes: 'All' });
        const htmlFr = _buildControlsHtml('fr', { sortBy: 'DateAdded', includeItemTypes: 'All' });

        expect(htmlEn).toContain('Your watchlist is empty.');
        expect(htmlFr).toContain('Ta watchlist est vide.');
    });
});
