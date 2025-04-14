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
  '/js/pwa.js',
  '/images/default-profile.png',
  '/images/student.png',
  '/images/icon-192x192.png',
  '/images/icon-512x512.png',
  '/manifest.json'
];

// Helper function to normalize URLs by removing query parameters
function normalizeUrl(url) {
  const urlObj = new URL(url, self.location.origin);
  // Only keep the pathname part of the URL
  return urlObj.pathname;
}

// Install event - cache assets
self.addEventListener('install', event => {
  console.log('[Service Worker] Installing...');
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => {
        console.log('[Service Worker] Caching app assets');
        return cache.addAll(ASSETS_TO_CACHE);
      })
      .then(() => {
        console.log('[Service Worker] Install completed');
        return self.skipWaiting();
      })
  );
});

// Activate event - clean up old caches and take control immediately
self.addEventListener('activate', event => {
  console.log('[Service Worker] Activating...');
  event.waitUntil(
    caches.keys().then(cacheNames => {
      return Promise.all(
        cacheNames.filter(cacheName => {
          return cacheName !== CACHE_NAME;
        }).map(cacheName => {
          console.log('[Service Worker] Clearing old cache:', cacheName);
          return caches.delete(cacheName);
        })
      );
    })
    .then(() => {
      console.log('[Service Worker] Claiming clients');
      return self.clients.claim();
    })
  );
});

// Fetch event - serve from cache or network
self.addEventListener('fetch', event => {
  // Skip cross-origin requests like API calls
  if (!event.request.url.startsWith(self.location.origin)) {
    return;
  }
  
  // Get the normalized path without query parameters
  const normalizedUrl = normalizeUrl(event.request.url);
  
  // Handle main HTML documents specially
  if (normalizedUrl === '/' || 
      normalizedUrl === '/index.html' || 
      normalizedUrl === '/login.html' || 
      normalizedUrl === '/register.html' ||
      normalizedUrl === '/student-dashboard.html' ||
      normalizedUrl === '/parent-dashboard.html' ||
      normalizedUrl === '/teacher-dashboard.html' ||
      normalizedUrl === '/admin-dashboard.html') {
    event.respondWith(
      fetch(event.request)
        .catch(() => {
          return caches.match(normalizedUrl);
        })
    );
    return;
  }
  
  // For all other requests, try cache first, then network
  event.respondWith(
    caches.match(normalizedUrl)
      .then(cachedResponse => {
        if (cachedResponse) {
          return cachedResponse;
        }
        
        return fetch(event.request)
          .then(response => {
            // Don't cache API responses
            if (event.request.url.includes('/api/')) {
              return response;
            }
            
            // Clone the response
            let responseToCache = response.clone();
            
            // Only cache successful responses
            if (response.status === 200) {
              caches.open(CACHE_NAME)
                .then(cache => {
                  cache.put(normalizedUrl, responseToCache);
                });
            }
            
            return response;
          });
      })
  );
}); 