/**
 * Theme Preloader Script
 * Must be placed in <head> before CSS to prevent flash of incorrect theme
 * This is a separate file that should be inlined or loaded early
 */
(function () {
    'use strict';

    // Get saved theme or detect system preference
    const savedTheme = localStorage.getItem('theme');
    let theme;

    if (savedTheme) {
        theme = savedTheme;
    } else {
        // Check system preference
        const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        theme = prefersDark ? 'dark' : 'light';
    }

    // Apply theme immediately to prevent flash
    document.documentElement.setAttribute('data-theme', theme);
})();
