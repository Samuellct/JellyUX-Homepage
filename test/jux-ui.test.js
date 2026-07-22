import { describe, it, expect } from 'vitest';
import juxUi from '../src/Jellyfin.Plugin.JuxHomepage/Web/jux-ui.js';

const { showEmpty, buildProgressBar, buildStatCard, _escHtml, _buildRadio, _buildRadioGroup } = juxUi;

describe('_escHtml', () => {
    it('escapes HTML special characters', () => {
        expect(_escHtml('<b>Tom & "Jerry"</b>')).toBe('&lt;b&gt;Tom &amp; &quot;Jerry&quot;&lt;/b&gt;');
    });

    it('returns an empty string for nullish input', () => {
        expect(_escHtml(null)).toBe('');
        expect(_escHtml(undefined)).toBe('');
    });
});

describe('showEmpty', () => {
    it('renders an icon, title, and subtitle into the container', () => {
        document.body.innerHTML = '<div id="pane"></div>';
        const container = document.getElementById('pane');

        showEmpty(container, { icon: 'bookmark_border', title: 'Empty', subtitle: 'Nothing here yet.' });

        expect(container.querySelector('.jux-empty-icon .material-icons').className).toContain('bookmark_border');
        expect(container.querySelector('.jux-empty-title').textContent).toBe('Empty');
        expect(container.querySelector('.jux-empty-subtitle').textContent).toBe('Nothing here yet.');
    });

    it('omits the subtitle element when none is given', () => {
        document.body.innerHTML = '<div id="pane"></div>';
        const container = document.getElementById('pane');

        showEmpty(container, { icon: 'info', title: 'Empty' });

        expect(container.querySelector('.jux-empty-subtitle')).toBeNull();
    });

    it('escapes title and subtitle text', () => {
        document.body.innerHTML = '<div id="pane"></div>';
        const container = document.getElementById('pane');

        showEmpty(container, { icon: 'info', title: '<script>', subtitle: '<img>' });

        expect(container.innerHTML).toContain('&lt;script&gt;');
        expect(container.innerHTML).toContain('&lt;img&gt;');
    });
});

describe('buildProgressBar', () => {
    it('renders one chunk per episode, marking the watched ones', () => {
        const html = buildProgressBar(3, 5);
        const div = document.createElement('div');
        div.innerHTML = html;

        const chunks = div.querySelectorAll('.jux-progress-chunk');
        expect(chunks.length).toBe(5);
        expect(div.querySelectorAll('.jux-progress-chunk.watched').length).toBe(3);
        expect(div.querySelectorAll('.jux-progress-chunk.unwatched').length).toBe(2);
    });

    it('adds a completed badge and class when watched equals total', () => {
        const html = buildProgressBar(5, 5);
        expect(html).toContain('jux-progress-bar-completed');
        expect(html).toContain('jux-progress-complete-badge');
    });

    it('does not add a completed badge when partially watched', () => {
        const html = buildProgressBar(2, 5);
        expect(html).not.toContain('jux-progress-complete-badge');
    });

    it('handles a zero-total series without throwing', () => {
        const html = buildProgressBar(0, 0);
        expect(html).toContain('jux-progress-bar');
        expect(html).not.toContain('jux-progress-chunk');
    });
});

describe('buildStatCard', () => {
    it('renders the icon, value, and label', () => {
        const html = buildStatCard('movie', 12, 'Movies watched');
        const div = document.createElement('div');
        div.innerHTML = html;

        expect(div.querySelector('.jux-stat-icon').className).toContain('movie');
        expect(div.querySelector('.jux-stat-value').textContent).toBe('12');
        expect(div.querySelector('.jux-stat-label').textContent).toBe('Movies watched');
    });

    it('escapes an unsafe label', () => {
        expect(buildStatCard('movie', 1, '<script>')).toContain('&lt;script&gt;');
    });
});

describe('_buildRadio', () => {
    it('marks the checked option and escapes the label', () => {
        const html = _buildRadio('sortBy', 'Name', '<b>Name</b>', true);
        expect(html).toContain('value="Name"');
        expect(html).toContain('checked');
        expect(html).toContain('&lt;b&gt;Name&lt;/b&gt;');
    });

    it('does not mark an unchecked option', () => {
        const html = _buildRadio('sortBy', 'Name', 'Name', false);
        expect(html).not.toContain('checked');
    });

    it('uses a unique clip-path id per call to avoid SVG id collisions', () => {
        const first = _buildRadio('sortBy', 'A', 'A', false);
        const second = _buildRadio('sortBy', 'B', 'B', false);
        const firstId = first.match(/clipPath id="([^"]+)"/)[1];
        const secondId = second.match(/clipPath id="([^"]+)"/)[1];
        expect(firstId).not.toBe(secondId);
    });
});

describe('_buildRadioGroup', () => {
    it('renders one radio per option with the current value checked', () => {
        const html = _buildRadioGroup([{ value: 'A', label: 'A' }, { value: 'B', label: 'B' }], 'B', 'group');
        const div = document.createElement('div');
        div.innerHTML = html;

        const inputs = div.querySelectorAll('input[type="radio"]');
        expect(inputs.length).toBe(2);
        expect(inputs[0].checked).toBe(false);
        expect(inputs[1].checked).toBe(true);
    });
});
