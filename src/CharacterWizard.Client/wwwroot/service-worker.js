// Development-only service worker.
// In development the browser fetches all resources from the dev server;
// this file exists solely so the browser can register a service worker and
// prompt "Add to Home Screen" during local testing.
// The published build replaces this with service-worker.published.js which
// performs full offline caching.

self.addEventListener('fetch', () => { });
