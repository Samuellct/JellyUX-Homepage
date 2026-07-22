import { describe, it, expect } from 'vitest';
import juxCollections from '../src/Jellyfin.Plugin.JuxHomepage/Web/jux-collections.js';

const {
    _isSupportedForCollections,
    _isBoxSet,
    _sortFields,
    _labelFor,
    _storageKeys,
    _buildCollectionsSectionSkeleton,
    _currentDetailItemId,
    _escHtml
} = juxCollections;

describe('_isSupportedForCollections', () => {
    it('returns true for Movie and Series items', () => {
        expect(_isSupportedForCollections({ Type: 'Movie' })).toBe(true);
        expect(_isSupportedForCollections({ Type: 'Series' })).toBe(true);
    });

    it('returns false for other item types', () => {
        expect(_isSupportedForCollections({ Type: 'BoxSet' })).toBe(false);
        expect(_isSupportedForCollections({ Type: 'Episode' })).toBe(false);
    });

    it('returns false for a nullish item', () => {
        expect(_isSupportedForCollections(null)).toBe(false);
    });
});

describe('_isBoxSet', () => {
    it('returns true only for BoxSet items', () => {
        expect(_isBoxSet({ Type: 'BoxSet' })).toBe(true);
        expect(_isBoxSet({ Type: 'Movie' })).toBe(false);
        expect(_isBoxSet(null)).toBe(false);
    });
});

describe('_sortFields', () => {
    it('returns all 5 sort criteria, including both community and critic rating', () => {
        const fields = _sortFields();
        expect(fields).toHaveLength(5);
        const values = fields.map((f) => f.value);
        expect(values).toEqual(['SortName', 'PremiereDate', 'DateCreated', 'CommunityRating', 'CriticRating']);
    });
});

describe('_labelFor', () => {
    it('returns the label matching the current value', () => {
        const fields = _sortFields();
        expect(_labelFor(fields, 'PremiereDate')).toBe('Release Date');
    });

    it('falls back to the first option when the value is unknown', () => {
        const fields = _sortFields();
        expect(_labelFor(fields, 'Unknown')).toBe(fields[0].label);
    });
});

describe('_storageKeys', () => {
    it('builds jux-prefixed localStorage keys scoped to the collection id', () => {
        expect(_storageKeys('abc-123')).toEqual({
            sort: 'jux-collection-sort-abc-123',
            order: 'jux-collection-order-abc-123'
        });
    });
});

describe('_buildCollectionsSectionSkeleton', () => {
    it('renders an escaped title and an empty items container', () => {
        const section = _buildCollectionsSectionSkeleton('Included In');

        expect(section.className).toContain('jux-collections-section');
        expect(section.querySelector('.sectionTitle').textContent).toBe('Included In');
        const itemsContainer = section.querySelector('.itemsContainer');
        expect(itemsContainer).not.toBeNull();
        expect(itemsContainer.children.length).toBe(0);
    });

    it('escapes the title', () => {
        const section = _buildCollectionsSectionSkeleton('<script>Alert</script>');
        expect(section.innerHTML).not.toContain('<script>Alert</script>');
    });
});

describe('_currentDetailItemId', () => {
    it('extracts the id from the location hash', () => {
        window.location.hash = '#/details?id=abc123-def';
        expect(_currentDetailItemId()).toBe('abc123-def');
    });

    it('returns null when no id is present', () => {
        window.location.hash = '#/home';
        expect(_currentDetailItemId()).toBeNull();
    });
});

describe('_escHtml', () => {
    it('escapes HTML special characters', () => {
        expect(_escHtml('<b>Tom & "Jerry"</b>')).toBe('&lt;b&gt;Tom &amp; &quot;Jerry&quot;&lt;/b&gt;');
    });

    it('returns an empty string for nullish input', () => {
        expect(_escHtml(null)).toBe('');
        expect(_escHtml(undefined)).toBe('');
    });
});
