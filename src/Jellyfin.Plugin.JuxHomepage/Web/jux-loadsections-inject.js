// JUX loadSections splice fragment.
// This file is embedded in the plugin DLL and spliced into the Jellyfin home chunk at
// the ",loadSections:" injection point by TransformationPatches.PatchLoadSections.
// It is NOT served as a web resource.
//
// Placeholder tokens are substituted at runtime with the minified variable names
// resolved from the Jellyfin 10.11.10 web bundle:
//   {{cardbuilder}}     -> u  (u.default  = cardBuilder)
//   {{layoutmanager}}   -> n  (n.A        = layoutManager)
//   {{shapes}}          -> y  (y.UI / y.xK / y.zP = backdrop / portrait / square)
//   {{approuter}}       -> p  (p.appRouter = appRouter)
//   {{globalize}}       -> s  (s.Ay       = globalize)
//   {{this_hook}}       -> resolved dynamically from the last "var X=" before ,loadSections:
//
// The function is spliced as:
//   ,loadSections:<fragment>,originalLoadSections:
// so this.originalLoadSections is the native implementation.
function(e, t, r, o, page) {
    if (!window.JellyfinAPI) {
        window.JellyfinAPI = {
            cardBuilder: {{cardbuilder}}.default,
            layoutManager: {{layoutmanager}}.A,
            getBackdropShape: {{shapes}}.UI,
            getPortraitShape: {{shapes}}.xK,
            getSquareShape: {{shapes}}.zP,
            appRouter: {{approuter}}.appRouter,
            globalize: {{globalize}}.Ay
        };
    }
    if (window.JUXHomepage) {
        window.JUXHomepage.originalLoadSections = {{this_hook}}.originalLoadSections;
        return window.JUXHomepage.loadSections.call(this, e, t, r, o, page);
    }
    return {{this_hook}}.originalLoadSections.call(this, e, t, r, o);
}
