import { describe, it, expect } from 'vitest';
import juxMenuLinks from '../src/Jellyfin.Plugin.JuxHomepage/Web/jux-menu-links.js';

const { _buildLink, _buildSection } = juxMenuLinks;

describe('_buildLink', () => {
    it('builds a native-styled drawer link pointing at Home, with the given icon and label', () => {
        const link = _buildLink({ buttonId: 'jux-tabbtn-watchlist', icon: 'bookmark' }, 'Watchlist');

        expect(link.tagName).toBe('A');
        expect(link.getAttribute('is')).toBe('emby-linkbutton');
        expect(link.className).toBe('emby-button navMenuOption lnkMediaFolder');
        expect(link.getAttribute('href')).toBe('#/home.html');
        expect(link.querySelector('.navMenuOptionIcon').className).toContain('bookmark');
        expect(link.querySelector('.navMenuOptionText').textContent).toBe('Watchlist');
    });
});

describe('_buildSection', () => {
    it('renders a header and one link per enabled tab, in the fixed display order', () => {
        const section = _buildSection(['statistics', 'watchlist']);

        expect(section.className).toBe('jux-menu-links');
        expect(section.querySelector('.sidebarHeader')).not.toBeNull();

        const labels = Array.from(section.querySelectorAll('.navMenuOptionText')).map((el) => el.textContent);
        // watchlist comes before statistics in the fixed order, regardless of input array order.
        expect(labels).toEqual(['Watchlist', 'Statistics']);
    });

    it('skips unknown tab ids without breaking the rest of the section', () => {
        const section = _buildSection(['watchlist', 'not-a-real-tab']);
        const labels = Array.from(section.querySelectorAll('.navMenuOptionText')).map((el) => el.textContent);
        expect(labels).toEqual(['Watchlist']);
    });

    it('renders no links when no tabs are enabled', () => {
        const section = _buildSection([]);
        expect(section.querySelectorAll('a.navMenuOption').length).toBe(0);
    });

    it('renders French labels when the document language is French', () => {
        document.documentElement.lang = 'fr';
        const section = _buildSection(['progress', 'history']);
        const labels = Array.from(section.querySelectorAll('.navMenuOptionText')).map((el) => el.textContent);
        expect(labels).toEqual(['Progression', 'Historique']);
        document.documentElement.lang = 'en';
    });
});
