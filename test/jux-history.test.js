import { describe, it, expect } from 'vitest';
import juxHistory from '../src/Jellyfin.Plugin.JuxHomepage/Web/jux-history.js';

const { _escHtml, _option, _buildControlsHtml } = juxHistory;

describe('_escHtml', () => {
    it('escapes HTML special characters', () => {
        expect(_escHtml('<b>Tom & "Jerry"</b>')).toBe('&lt;b&gt;Tom &amp; &quot;Jerry&quot;&lt;/b&gt;');
    });

    it('returns an empty string for nullish input', () => {
        expect(_escHtml(null)).toBe('');
        expect(_escHtml(undefined)).toBe('');
    });
});

describe('_option', () => {
    it('marks the option matching the current value as selected', () => {
        expect(_option('LastPlayed', 'Dernier vu', 'LastPlayed')).toBe('<option value="LastPlayed" selected>Dernier vu</option>');
    });

    it('does not mark an option that does not match the current value', () => {
        expect(_option('Name', 'Nom', 'LastPlayed')).toBe('<option value="Name">Nom</option>');
    });
});

describe('_buildControlsHtml', () => {
    it('renders the sort control with the current state pre-selected', () => {
        const html = _buildControlsHtml('en', { sortBy: 'Name' });

        expect(html).toContain('id="jux-history-sort"');
        expect(html).toContain('value="Name" selected');
    });

    it('includes the localized empty-state message', () => {
        const htmlEn = _buildControlsHtml('en', { sortBy: 'LastPlayed' });
        const htmlFr = _buildControlsHtml('fr', { sortBy: 'LastPlayed' });

        expect(htmlEn).toContain('No watched movies yet.');
        expect(htmlFr).toContain('Aucun film vu pour le moment.');
    });
});
