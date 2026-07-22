import { describe, it, expect } from 'vitest';
import juxProgress from '../src/Jellyfin.Plugin.JuxHomepage/Web/jux-progress.js';

const { _escHtml, _option, _buildControlsHtml, _formatEpisodeCode, _formatTemplate } = juxProgress;

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

        expect(html).toContain('id="jux-progress-sort"');
        expect(html).toContain('value="Name" selected');
    });

    it('includes the localized empty-state message', () => {
        const htmlEn = _buildControlsHtml('en', { sortBy: 'LastPlayed' });
        const htmlFr = _buildControlsHtml('fr', { sortBy: 'LastPlayed' });

        expect(htmlEn).toContain('No series in progress yet.');
        expect(htmlFr).toContain('Aucune série en cours.');
    });
});

describe('_formatEpisodeCode', () => {
    it('formats season and episode numbers as SxxEyy, zero-padded', () => {
        expect(_formatEpisodeCode(1, 3)).toBe('S01E03');
    });

    it('does not pad numbers already two digits or more', () => {
        expect(_formatEpisodeCode(12, 34)).toBe('S12E34');
    });

    it('returns an empty string when either number is missing', () => {
        expect(_formatEpisodeCode(null, 3)).toBe('');
        expect(_formatEpisodeCode(1, null)).toBe('');
        expect(_formatEpisodeCode(undefined, undefined)).toBe('');
    });
});

describe('_formatTemplate', () => {
    it('substitutes named placeholders', () => {
        expect(_formatTemplate('{watched}/{total} episodes', { watched: 3, total: 10 })).toBe('3/10 episodes');
    });

    it('leaves unknown placeholders untouched', () => {
        expect(_formatTemplate('{missing} thing', { watched: 1 })).toBe('{missing} thing');
    });
});
