import { defineConfig } from 'vitest/config';

// TODO_V2.md Phase 15.3: jsdom is needed only for the small handful of pure/near-pure functions in
// jux-homepage.js that touch window.location/document (see test/jux-homepage.test.js). The plugin
// has no bundler and no other front-end test surface today.
export default defineConfig({
    test: {
        environment: 'jsdom'
    }
});
