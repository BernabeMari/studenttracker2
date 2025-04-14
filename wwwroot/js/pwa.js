// Register service worker for PWA functionality
if ('serviceWorker' in navigator) {
  window.addEventListener('load', () => {
    console.log('Attempting to register service worker...');
    navigator.serviceWorker.register('/service-worker.js', {
      scope: '/'
    })
      .then(registration => {
        console.log('🎉 Service Worker registered successfully with scope:', registration.scope);
      })
      .catch(error => {
        console.error('❌ Service Worker registration failed:', error);
      });
  });
}

// Handle PWA installation
let deferredPrompt;
const installButton = document.getElementById('install-button');
const installBanner = document.getElementById('install-banner');
const bannerInstallBtn = document.getElementById('banner-install-btn');

// Log device and browser info for debugging
console.log('📱 User Agent:', navigator.userAgent);
console.log('📱 Platform:', navigator.platform);
console.log('📱 PWA Standalone:', window.matchMedia('(display-mode: standalone)').matches);

// Check if the app is running in standalone mode (installed)
const isRunningStandalone = () => {
  return window.matchMedia('(display-mode: standalone)').matches || 
         window.navigator.standalone || // For iOS
         document.referrer.includes('android-app://');
};

// Function to check if the current browser supports PWA installation
function browserSupportsPWA() {
  const ua = navigator.userAgent.toLowerCase();
  // Chrome, Edge (Chromium-based), Opera, Samsung browser
  const isCompatibleBrowser = 
    (ua.indexOf('chrome') > -1 && ua.indexOf('edge') === -1 && ua.indexOf('edg') === -1) || 
    (ua.indexOf('edge') > -1 || ua.indexOf('edg') > -1) || 
    ua.indexOf('opr') > -1 || 
    ua.indexOf('samsung') > -1;
  
  // Android or Windows or Mac
  const isCompatibleOS = 
    /android/.test(ua) || 
    /windows/.test(ua) || 
    /macintosh/.test(ua);
  
  const isHttps = window.location.protocol === 'https:' || 
                 window.location.hostname === 'localhost' || 
                 window.location.hostname === '127.0.0.1';
  
  console.log('📱 PWA support check - Compatible browser:', isCompatibleBrowser);
  console.log('📱 PWA support check - Compatible OS:', isCompatibleOS);  
  console.log('📱 PWA support check - HTTPS:', isHttps);
  
  return isCompatibleBrowser && isCompatibleOS && isHttps;
}

// Function to initialize the install button and banner
function initInstallPrompts() {
  console.log('Initializing install prompts, deferredPrompt available:', !!deferredPrompt);
  
  // Check if the app is already installed
  if (isRunningStandalone()) {
    console.log('📱 App is already installed (standalone mode)');
    return; // Don't show install prompts if already installed
  }
  
  // Check if the browser supports PWA installation
  if (!browserSupportsPWA()) {
    console.log('❌ Browser does not support PWA installation');
    return; // Don't show install prompts if not supported
  }
  
  // For iOS devices - show a custom install banner with instructions
  const isIOS = /iphone|ipad|ipod/.test(navigator.userAgent.toLowerCase());
  if (isIOS) {
    showIOSInstallInstructions();
    return;
  }
  
  // Show the install button if we have a deferred prompt
  if (deferredPrompt) {
    if (installButton) {
      installButton.style.display = 'block';
      console.log('📱 Install button shown');
    }
    
    if (installBanner) {
      installBanner.style.display = 'block';
      console.log('📱 Install banner shown');
    }
  } else {
    console.log('❌ No deferredPrompt available, can\'t show install prompts');
    // For testing purposes, show the install UI anyway
    if (installButton) installButton.style.display = 'block';
    if (installBanner) installBanner.style.display = 'block';
  }
}

// Function to handle the installation process
function handleInstallClick(event) {
  console.log('📱 Install button clicked');
  
  // For iOS devices, we can't trigger the install automatically
  const isIOS = /iphone|ipad|ipod/.test(navigator.userAgent.toLowerCase());
  if (isIOS) {
    showIOSInstallInstructions();
    return;
  }
  
  // Only proceed if we have a deferredPrompt
  if (!deferredPrompt) {
    console.log('❌ No installation prompt available');
    
    // Let's show a fallback instruction dialog
    showManualInstallInstructions();
    return;
  }
  
  console.log('📱 Installation prompt available, showing it now');
  
  // Hide the install prompts
  if (installButton) installButton.style.display = 'none';
  if (installBanner) installBanner.style.display = 'none';
  
  // Show the install prompt
  deferredPrompt.prompt();
  
  // Wait for the user to respond to the prompt
  deferredPrompt.userChoice.then((choiceResult) => {
    console.log('👤 User installation choice:', choiceResult.outcome);
    
    if (choiceResult.outcome === 'accepted') {
      console.log('✅ User accepted the install prompt');
    } else {
      console.log('❌ User dismissed the install prompt');
      
      // Show the prompts again if they dismissed
      if (installButton) installButton.style.display = 'block';
      if (installBanner) installBanner.style.display = 'block';
    }
    
    // Clear the deferredPrompt so it can be garbage collected
    deferredPrompt = null;
  });
}

// Function to show manual install instructions
function showManualInstallInstructions() {
  // Create modal for manual install instructions
  const modal = document.createElement('div');
  modal.style.position = 'fixed';
  modal.style.top = '0';
  modal.style.left = '0';
  modal.style.width = '100%';
  modal.style.height = '100%';
  modal.style.backgroundColor = 'rgba(0,0,0,0.5)';
  modal.style.zIndex = '2000';
  modal.style.display = 'flex';
  modal.style.alignItems = 'center';
  modal.style.justifyContent = 'center';
  
  const content = document.createElement('div');
  content.style.backgroundColor = '#fff';
  content.style.padding = '20px';
  content.style.borderRadius = '8px';
  content.style.maxWidth = '400px';
  content.style.boxShadow = '0 4px 12px rgba(0,0,0,0.15)';
  
  // Different instructions for different browsers
  const ua = navigator.userAgent.toLowerCase();
  let instructions = '';
  
  if (ua.indexOf('chrome') > -1) {
    instructions = `
      <h4>Install from Chrome browser</h4>
      <ol>
        <li>On your browser's menu (three dots in the top right), click "Install Student Tracker" or "Install app"</li>
        <li>Follow the on-screen instructions to install</li>
      </ol>
    `;
  } else if (ua.indexOf('firefox') > -1) {
    instructions = `
      <h4>Firefox doesn't fully support app installation</h4>
      <p>For the best experience, we recommend using Chrome, Edge, or Safari.</p>
    `;
  } else if (ua.indexOf('safari') > -1 && /iphone|ipad|ipod/.test(ua)) {
    instructions = `
      <h4>Install from Safari (iOS)</h4>
      <ol>
        <li>Tap the Share button at the bottom of the screen</li>
        <li>Scroll down and tap "Add to Home Screen"</li>
        <li>Tap "Add" in the top-right corner</li>
      </ol>
    `;
  } else {
    instructions = `
      <h4>Install Student Tracker</h4>
      <ol>
        <li>On your browser's menu, look for "Install Student Tracker" or "Add to Home Screen"</li>
        <li>Follow the on-screen instructions to install</li>
      </ol>
    `;
  }
  
  content.innerHTML = `
    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 15px;">
      <h3 style="margin: 0;">Install Student Tracker</h3>
      <button id="close-install-modal" style="background: none; border: none; font-size: 20px; cursor: pointer;">×</button>
    </div>
    <div>
      ${instructions}
    </div>
    <button id="ok-install-modal" style="background: #0969da; color: white; border: none; padding: 8px 16px; border-radius: 4px; font-weight: bold; margin-top: 15px; cursor: pointer;">OK</button>
  `;
  
  modal.appendChild(content);
  document.body.appendChild(modal);
  
  // Close modal handlers
  document.getElementById('close-install-modal').addEventListener('click', () => {
    document.body.removeChild(modal);
  });
  
  document.getElementById('ok-install-modal').addEventListener('click', () => {
    document.body.removeChild(modal);
  });
}

// Function to show iOS install instructions
function showIOSInstallInstructions() {
  // Similar to showManualInstallInstructions but iOS-specific
  const modal = document.createElement('div');
  modal.style.position = 'fixed';
  modal.style.top = '0';
  modal.style.left = '0';
  modal.style.width = '100%';
  modal.style.height = '100%';
  modal.style.backgroundColor = 'rgba(0,0,0,0.5)';
  modal.style.zIndex = '2000';
  modal.style.display = 'flex';
  modal.style.alignItems = 'center';
  modal.style.justifyContent = 'center';
  
  const content = document.createElement('div');
  content.style.backgroundColor = '#fff';
  content.style.padding = '20px';
  content.style.borderRadius = '8px';
  content.style.maxWidth = '400px';
  content.style.boxShadow = '0 4px 12px rgba(0,0,0,0.15)';
  
  content.innerHTML = `
    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 15px;">
      <h3 style="margin: 0;">Install on iOS</h3>
      <button id="close-ios-modal" style="background: none; border: none; font-size: 20px; cursor: pointer;">×</button>
    </div>
    <div>
      <h4>Install Student Tracker on your iOS device</h4>
      <ol>
        <li>Tap the Share button <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 12v8a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-8"></path><polyline points="16 6 12 2 8 6"></polyline><line x1="12" y1="2" x2="12" y2="15"></line></svg> at the bottom of the screen</li>
        <li>Scroll down and tap "Add to Home Screen"</li>
        <li>Tap "Add" in the top-right corner</li>
      </ol>
      <p>The app will appear on your home screen like any other app.</p>
    </div>
    <button id="ok-ios-modal" style="background: #0969da; color: white; border: none; padding: 8px 16px; border-radius: 4px; font-weight: bold; margin-top: 15px; cursor: pointer;">OK</button>
  `;
  
  modal.appendChild(content);
  document.body.appendChild(modal);
  
  // Close modal handlers
  document.getElementById('close-ios-modal').addEventListener('click', () => {
    document.body.removeChild(modal);
  });
  
  document.getElementById('ok-ios-modal').addEventListener('click', () => {
    document.body.removeChild(modal);
  });
}

// Watch for the beforeinstallprompt event
window.addEventListener('beforeinstallprompt', (e) => {
  console.log('📱 beforeinstallprompt event fired!');
  
  // Prevent Chrome 67 and earlier from automatically showing the prompt
  e.preventDefault();
  
  // Stash the event so it can be triggered later
  deferredPrompt = e;
  
  // Initialize the install prompts
  initInstallPrompts();
});

// Setup install button click handlers
if (installButton) {
  installButton.addEventListener('click', handleInstallClick);
  console.log('📱 Install button handler attached');
}

if (bannerInstallBtn) {
  bannerInstallBtn.addEventListener('click', handleInstallClick);
  console.log('📱 Banner install button handler attached');
}

// Handle the "appinstalled" event
window.addEventListener('appinstalled', (evt) => {
  console.log('✅ Application was successfully installed');
  
  // Hide the install prompts after successful installation
  if (installButton) installButton.style.display = 'none';
  if (installBanner) installBanner.style.display = 'none';
  
  // Maybe show a success message
  const successMsg = document.createElement('div');
  successMsg.style.position = 'fixed';
  successMsg.style.bottom = '20px';
  successMsg.style.left = '50%';
  successMsg.style.transform = 'translateX(-50%)';
  successMsg.style.backgroundColor = '#4CAF50';
  successMsg.style.color = 'white';
  successMsg.style.padding = '10px 20px';
  successMsg.style.borderRadius = '5px';
  successMsg.style.zIndex = '1000';
  successMsg.style.boxShadow = '0 2px 5px rgba(0,0,0,0.2)';
  successMsg.textContent = 'App installed successfully!';
  
  document.body.appendChild(successMsg);
  
  // Remove the success message after 3 seconds
  setTimeout(() => {
    if (document.body.contains(successMsg)) {
      document.body.removeChild(successMsg);
    }
  }, 3000);
});

// Initialize installation prompts on page load
document.addEventListener('DOMContentLoaded', () => {
  // Check PWA installability criteria
  const ua = navigator.userAgent.toLowerCase();
  console.log('PWA Installability:');
  console.log('- Using HTTPS:', window.location.protocol === 'https:' || window.location.hostname === 'localhost');
  console.log('- Has service worker:', 'serviceWorker' in navigator);
  console.log('- Has web manifest:', !!document.querySelector('link[rel="manifest"]'));
  console.log('- Meets display requirements:', true);
  console.log('- Compatible browser:', !(/firefox|trident|msie/i.test(ua)));
  
  // Initialize install UI based on current state
  initInstallPrompts();
}); 