import { describe, it, expect } from 'vitest';
import juxStatistics from '../src/Jellyfin.Plugin.JuxHomepage/Web/jux-statistics.js';

const { _escHtml, _tile, _buildTilesHtml } = juxStatistics;

describe('_escHtml', () => {
    it('escapes HTML special characters', () => {
        expect(_escHtml('<b>&"</b>')).toBe('&lt;b&gt;&amp;&quot;&lt;/b&gt;');
    });
});

describe('_tile', () => {
    it('renders the value and label, escaped', () => {
        const html = _tile(12, 'Films vus');
        expect(html).toContain('12');
        expect(html).toContain('Films vus');
    });

    it('escapes an unsafe label', () => {
        expect(_tile(1, '<script>')).toContain('&lt;script&gt;');
    });
});

describe('_buildTilesHtml', () => {
    it('renders all four counters with localized labels', () => {
        const stats = { MoviesWatched: 5, SeriesTracked: 3, SeriesCompleted: 1, EpisodesWatched: 42 };

        const htmlEn = _buildTilesHtml(stats, 'en');
        expect(htmlEn).toContain('5');
        expect(htmlEn).toContain('Movies watched');
        expect(htmlEn).toContain('3');
        expect(htmlEn).toContain('Series tracked');
        expect(htmlEn).toContain('1');
        expect(htmlEn).toContain('Series completed');
        expect(htmlEn).toContain('42');
        expect(htmlEn).toContain('Episodes watched');

        const htmlFr = _buildTilesHtml(stats, 'fr');
        expect(htmlFr).toContain('Films vus');
        expect(htmlFr).toContain('Séries suivies');
        expect(htmlFr).toContain('Séries terminées');
        expect(htmlFr).toContain('Épisodes vus');
    });
});
