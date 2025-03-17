// Service Worker for Student Tracker PWA
const CACHE_NAME = 'student-tracker-v1';
const ASSETS_TO_CACHE = [
  '/',
  '/index.html',
  '/login.html',
  '/register.html',
  '/student-dashboard.html',
  '/parent-dashboard.html',
  '/teacher-dashboard.html',
  '/admin-dashboard.html',
  '/css/bootstrap.min.css',
  '/css/styles.css',
  '/js/bootstrap.bundle.min.js',
  '/js/jquery-3.6.0.min.js',
  '/js/signalr/dist/browser/signalr.min.js',
  '/images/default-profile.png',
  '/images/icon-192x192.png',
  '/images/icon-512x512.png',
  '/manifest.json'
];

// Install event - cache assets
self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => {
        console.log('Caching app assets');
        return cache.addAll(ASSETS_TO_CACHE);
      })
  );
});

// Activate event - clean up old caches
self.addEventListener('activate', event => {
  event.waitUntil(
    caches.keys().then(cacheNames => {
      return Promise.all(
        cacheNames.filter(cacheName => {
          return cacheName !== CACHE_NAME;
        }).map(cacheName => {
          return caches.delete(cacheName);
        })
      );
    })
  );
});

// Fetch event - serve from cache or network
self.addEventListener('fetch', event => {
  // Skip cross-origin requests like API calls
  if (event.request.url.startsWith(self.location.origin)) {
    event.respondWith(
      caches.match(event.request)
        .then(cachedResponse => {
          if (cachedResponse) {
            return cachedResponse;
          }
          
          return fetch(event.request)
            .then(response => {
              // Don't cache API responses
              if (!event.request.url.includes('/api/')) {
                // Clone the response
                let responseToCache = response.clone();
                
                caches.open(CACHE_NAME)
                  .then(cache => {
                    cache.put(event.request, responseToCache);
                  });
              }
              
              return response;
            });
        })
    );
  }
}); 