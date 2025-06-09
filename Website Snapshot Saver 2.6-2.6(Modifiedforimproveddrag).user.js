// ==UserScript==
// @name         Website Snapshot Saver 2.6
// @namespace    http://tampermonkey.net/
// @version      2.6 (Modified for improved drag)
// @description  Saves full website pages (HTML, JS, CSS, images) and form elements into a zip file, optimized for BMC Helix with fixed drag and popup
// @author       You (Justin & AI)
// @match        *://*/*
// @grant        GM_xmlhttpRequest
// @require      https://cdn.jsdelivr.net/npm/jszip@3.10.1/dist/jszip.min.js
// ==/UserScript==

/* eslint-disable no-undef */
/* global JSZip */

(function() {
    'use strict';

    // Ensure JSZip is loaded with fallback
    function ensureJSZip() {
        return new Promise((resolve, reject) => {
            if (typeof JSZip !== 'undefined') {
                console.log('JSZip loaded via @require');
                resolve();
                return;
            }

            console.warn('JSZip not found, attempting fallback');
            GM_xmlhttpRequest({
                method: 'GET',
                url: 'https://unpkg.com/jszip@3.10.1/dist/jszip.min.js',
                onload: function(response) {
                    try {
                        const script = document.createElement('script');
                        script.textContent = response.responseText;
                        (document.head || document.documentElement).appendChild(script);
                        setTimeout(() => {
                            if (typeof JSZip !== 'undefined') {
                                console.log('JSZip loaded via fallback');
                                resolve();
                            } else {
                                reject(new Error('JSZip failed to load via fallback'));
                            }
                        }, 100);
                    } catch (e) {
                        reject(new Error(`JSZip load error: ${e.message}`));
                    }
                },
                onerror: function() {
                    reject(new Error('Failed to fetch JSZip from fallback URL'));
                }
            });
        });
    }

    // Create floating eye icon and popup
    const createFloatingEye = (targetDocument = document, retryCount = 0) => {
        const maxRetries = 5;
        if (!targetDocument.body && retryCount < maxRetries) {
            console.warn(`document.body not available in ${targetDocument === document ? 'main' : 'iframe'} document, retrying (${retryCount + 1}/${maxRetries})`);
            setTimeout(() => createFloatingEye(targetDocument, retryCount + 1), 500);
            return;
        }

        const container = targetDocument.createElement('div');
        container.id = `website-snapshot-container-${Math.random().toString(36).slice(2)}`;
        Object.assign(container.style, {
            position: 'fixed',
            top: '20px',        // Initial position
            right: '20px',       // Initial position
            zIndex: '2147483647',
            // cursor: 'move', // Will be set on iconContainer instead
            pointerEvents: 'auto',
            display: 'block !important',
            visibility: 'visible !important',
            width: 'auto',
            height: 'auto'
        });

        const shadow = container.attachShadow({ mode: 'open' });
        const style = targetDocument.createElement('style');
        style.textContent = `
            :host {
                all: initial;
                display: block !important;
                visibility: visible !important;
                position: relative;
                z-index: 2147483647;
            }
            .icon-container {
                background: rgba(255, 255, 255, 0.8);
                border-radius: 50%;
                padding: 5px;
                box-shadow: 0 2px 5px rgba(0,0,0,0.3);
                pointer-events: auto;
                display: inline-block;
                cursor: grab; /* Indicate draggable */
            }
            .icon-container:active {
                cursor: grabbing; /* Indicate dragging */
            }
            .popup {
                display: none;
                background: #fff;
                border: 1px solid #ccc;
                border-radius: 5px;
                padding: 10px;
                box-shadow: 0 4px 8px rgba(0,0,0,0.2);
                position: absolute;
                top: 40px; /* Relative to icon-container */
                left: 50%; /* Center relative to icon-container */
                transform: translateX(-50%);
                z-index: 2147483647;
                width: 300px;
                min-height: 200px;
                font-family: Arial, sans-serif;
                font-size: 14px;
                pointer-events: auto;
                overflow: visible;
            }
            .popup.show {
                display: block !important;
                visibility: visible !important;
            }
            .popup label {
                display: block;
                margin: 5px 0;
                cursor: help;
            }
            .popup button {
                margin: 10px 5px 0 0;
                padding: 5px 10px;
                cursor: pointer;
            }
            .popup select {
                margin: 5px 0;
                width: 100%;
            }
            .progress {
                margin-top: 10px;
                font-size: 12px;
                color: #333;
            }
            .progress-bar {
                width: 100%;
                background: #eee;
                height: 5px;
                border-radius: 3px;
                overflow: hidden;
            }
            .progress-bar-fill {
                height: 100%;
                background: #4caf50;
                width: 0;
                transition: width 0.3s;
            }
        `;
        shadow.appendChild(style);

        const iconContainer = targetDocument.createElement('div');
        iconContainer.className = 'icon-container';
        iconContainer.title = 'Toggle snapshot options';
        iconContainer.innerHTML = `
            <svg width="32" height="32" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M12 4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5zM12 17c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5zm0-8c-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3-1.34-3-3-3z" fill="#000"/>
            </svg>
        `;
        shadow.appendChild(iconContainer);

        const popup = targetDocument.createElement('div');
        popup.className = 'popup';
        popup.innerHTML = `
            <label title="Save images as Base64-encoded text files."><input type="checkbox" id="base64Images"> Save images as Base64</label>
            <label title="Log network activity and capture streams."><input type="checkbox" id="networkTraffic"> Include network traffic</label>
            <label title="Include external resources (e.g., CDNs)."><input type="checkbox" id="externalResources" checked> Include external resources</label>
            <label title="Follow same-domain links."><input type="checkbox" id="sameDomainLinks"> Follow same-domain links</label>
            <label title="Throttle connections to avoid rate-limiting."><input type="checkbox" id="throttleConnections" checked> Throttle connections</label>
            <label title="Save full page HTML."><input type="checkbox" id="fullPage" checked> Capture full page</label>
            <label title="Save form elements."><input type="checkbox" id="sniffElements" checked> Sniff form elements</label>
            <label title="Debug without zipping."><input type="checkbox" id="debugNoZip"> Debug: Save as JSON</label>
            <button id="saveButton" title="Save website as zip.">Save Website</button>
            <button id="cancelButton" title="Close popup.">Cancel</button>
            <div class="progress" id="progress">Ready</div>
            <div class="progress-bar"><div class="progress-bar-fill" id="progressBar"></div></div>
        `;
        shadow.appendChild(popup);

        // Draggable icon logic
        let isDragging = false;
        let offsetX, offsetY;
        let justDragged = false; // To prevent click after drag

        iconContainer.addEventListener('mousedown', (e) => {
            e.preventDefault(); // Prevent default actions like text selection if any
            e.stopPropagation();

            // If the container is positioned with 'right', convert to 'left'/'top'
            // before calculating offset to prevent jump.
            const style = window.getComputedStyle(container);
            if (style.right !== 'auto' || !container.style.left) {
                const currentRect = container.getBoundingClientRect();
                container.style.left = currentRect.left + 'px';
                container.style.top = currentRect.top + 'px';
                container.style.right = 'auto';
                container.style.bottom = 'auto'; // Also good practice
            }

            offsetX = e.clientX - container.offsetLeft;
            offsetY = e.clientY - container.offsetTop;

            isDragging = true;
            justDragged = false; // Reset for current drag operation
            targetDocument.body.style.userSelect = 'none'; // Prevent text selection on the page
            console.log(`Drag start: clientX=${e.clientX}, container.offsetLeft=${container.offsetLeft}, offsetX=${offsetX}`);
        });

        targetDocument.addEventListener('mousemove', (e) => {
            if (isDragging) {
                justDragged = true; // Indicate that a drag movement has occurred
                // e.preventDefault(); // Usually not needed for document move, but can be added if issues
                // e.stopPropagation(); // Be cautious with stopPropagation on document listeners

                let newX = e.clientX - offsetX;
                let newY = e.clientY - offsetY;

                // Boundary checks (optional, but good for keeping it on screen)
                // Ensure rect.width/height are available, or use fixed values if known
                const iconRect = container.getBoundingClientRect(); // Get current dimensions
                const maxX = window.innerWidth - iconRect.width;
                const maxY = window.innerHeight - iconRect.height;

                newX = Math.max(0, Math.min(newX, maxX));
                newY = Math.max(0, Math.min(newY, maxY));

                container.style.left = `${newX}px`;
                container.style.top = `${newY}px`;
                // container.style.right is already 'auto' from mousedown
                console.log(`Dragging: clientX=${e.clientX}, newX=${newX}`);
            }
        });

        targetDocument.addEventListener('mouseup', (e) => {
            if (isDragging) {
                // e.stopPropagation(); // Be cautious
                isDragging = false;
                targetDocument.body.style.userSelect = 'auto'; // Re-enable text selection
                console.log('Drag end');
                // If justDragged is true, click event on iconContainer will be ignored.
                // Reset justDragged after a very short delay to allow for separate clicks later.
                setTimeout(() => {
                    justDragged = false;
                }, 0);
            }
        });

        // Popup toggle
        let isPopupOpen = false;
        iconContainer.addEventListener('click', (e) => {
            e.stopPropagation(); // Always good to stop propagation for UI elements
            if (justDragged) {
                // If a drag just happened, don't toggle the popup.
                // justDragged will be reset by the mouseup timeout.
                return;
            }
            isPopupOpen = !isPopupOpen;
            popup.classList.toggle('show', isPopupOpen);
            console.log(`Popup toggled: ${isPopupOpen ? 'open' : 'closed'}`);
        });

        // Button handlers
        const saveButton = popup.querySelector('#saveButton');
        const cancelButton = popup.querySelector('#cancelButton');
        const progressDiv = popup.querySelector('#progress');
        const progressBar = popup.querySelector('#progressBar');

        saveButton.addEventListener('click', async (e) => {
            e.stopPropagation();
            const options = {
                base64Images: popup.querySelector('#base64Images').checked,
                networkTraffic: popup.querySelector('#networkTraffic').checked,
                externalResources: popup.querySelector('#externalResources').checked,
                sameDomainLinks: popup.querySelector('#sameDomainLinks').checked,
                throttleConnections: popup.querySelector('#throttleConnections').checked,
                fullPage: popup.querySelector('#fullPage').checked,
                sniffElements: popup.querySelector('#sniffElements').checked,
                debugNoZip: popup.querySelector('#debugNoZip').checked
            };
            saveButton.disabled = true;
            try {
                await takeSnapshot(options, progressDiv, progressBar);
            } catch (error) {
                console.error('Snapshot failed:', error);
                progressDiv.textContent = `Error: ${error.message}`;
            } finally {
                saveButton.disabled = false;
                // Do not close popup automatically on save, user might want to see status or save again
                // popup.classList.remove('show');
                // isPopupOpen = false;
            }
        });

        cancelButton.addEventListener('click', (e) => {
            e.stopPropagation();
            popup.classList.remove('show');
            isPopupOpen = false;
            progressDiv.textContent = 'Ready';
            progressBar.style.width = '0%';
        });

        try {
            (targetDocument.body || targetDocument.documentElement).appendChild(container);
            console.log(`Eye icon appended to ${targetDocument === document ? 'main' : 'iframe'} document`);
        } catch (error) {
            console.error(`Failed to append eye icon: ${error}`);
            if (retryCount < maxRetries) {
                setTimeout(() => createFloatingEye(targetDocument, retryCount + 1), 500);
            }
        }
    };

    // Setup eye icon in main document and iframes
    const setupEyeIcon = () => {
        const injectIntoDocument = (doc) => {
            // Check if already injected to prevent duplicates if script runs multiple times or DOM is manipulated
            if (doc.querySelector('[id^="website-snapshot-container"]')) {
                return;
            }
            if (doc.body) {
                createFloatingEye(doc);
            } else {
                // Fallback if body is not yet ready
                doc.addEventListener('DOMContentLoaded', () => {
                    if (!doc.querySelector('[id^="website-snapshot-container"]')) {
                         createFloatingEye(doc);
                    }
                }, { once: true });
            }
        };

        // Main document
        injectIntoDocument(document);

        // Iframes
        const checkIframes = () => {
            document.querySelectorAll('iframe').forEach(iframe => {
                try {
                    const iframeDoc = iframe.contentDocument || iframe.contentWindow.document;
                    if (iframeDoc && !iframeDoc.querySelector('[id^="website-snapshot-container"]')) {
                        // Check if the iframe source is accessible (same-origin or permissive CORS)
                        // and if it's a HTML document before trying to inject.
                        if (iframe.src && iframe.src !== 'about:blank' && !iframe.src.startsWith('javascript:')) {
                           try {
                               // A quick check if we can even access the body.
                               // This might still fail for cross-origin iframes that haven't errored out yet.
                               if (iframeDoc.body) {
                                   injectIntoDocument(iframeDoc);
                               }
                           } catch (e) {
                                console.warn(`Cannot inject into iframe due to access restrictions: ${iframe.src || 'inline iframe'}`, e.message);
                           }
                        } else if (!iframe.src || iframe.src === 'about:blank') { // For srcdoc or blank iframes
                            injectIntoDocument(iframeDoc);
                        }
                    }
                } catch (e) {
                    // This catch is for when accessing contentDocument/contentWindow itself fails (cross-origin)
                    // console.warn(`Cannot access iframe content: ${iframe.src || 'inline iframe'}`, e.message);
                }
            });
        };

        // Initial check and then periodic checks for new iframes
        checkIframes();
        setInterval(checkIframes, 3000); // Increased interval slightly

        // Monitor DOM changes for icon removal (e.g., by SPA navigations)
        if (document.body) { // Ensure body exists before observing
            const observer = new MutationObserver(() => {
                if (!document.querySelector('[id^="website-snapshot-container"]')) {
                    console.warn('Icon container removed from main document, reinjecting');
                    injectIntoDocument(document);
                }
            });
            observer.observe(document.body, { childList: true, subtree: false }); // Observe body directly for major changes
        }
    };

    // --- UNMODIFIED FUNCTIONS BELOW THIS LINE ---
    // (extractTLD, detectConnectionLimit, FetchQueue, captureNetworkLog,
    //  parseResourcesFromHtml, serializeFullPage, sniffFormElements,
    //  collectResources, takeSnapshot)
    // These functions are assumed to be correct as per the original script and
    // are not directly related to the drag-and-drop fix.
    // For brevity, I will not repeat them here but assume they are appended from your original script.
    // Please make sure to include the rest of your original script functions from this point onwards.

    // Extract TLD
    function extractTLD(hostname) {
        const parts = hostname.split('.');
        return parts.length > 2 ? parts.slice(-2).join('.') : hostname;
    }

    // Detect max connections
    async function detectConnectionLimit(url) {
        const urlObj = new URL(url);
        const tld = extractTLD(urlObj.hostname);
        const testUrl = `https://${tld}/favicon.ico`; // Consider http/https
        let maxConnections = 1;
        let rateLimited = false;

        for (let i = 2; i <= 10 && !rateLimited; i++) {
            const promises = Array(i).fill().map(() =>
                new Promise((resolve) =>
                    GM_xmlhttpRequest({
                        method: 'HEAD',
                        url: testUrl,
                        timeout: 3000, // Add a timeout for unresponsive servers
                        onload: res => resolve({ status: res.status }),
                        onerror: () => resolve({ status: 0 }), // Network error or CORS blocked
                        ontimeout: () => resolve({ status: 0 }) // Timeout
                    })
                )
            );
            // Await all promises to complete for this batch
            const results = await Promise.all(promises);
            rateLimited = results.some(res => res.status === 429 || res.status === 0);
            if (!rateLimited) maxConnections = i;
        }
        console.log(`Detected max connections: ${maxConnections}`);
        return maxConnections;
    }

    // Fetch queue
    class FetchQueue {
        constructor(maxConcurrent) {
            this.maxConcurrent = Math.max(1, maxConcurrent); // Ensure at least 1
            this.current = 0;
            this.queue = [];
            console.log(`WorkspaceQueue initialized with maxConcurrent: ${this.maxConcurrent}`);
        }

        async enqueue(fetchTask) {
            return new Promise(async (resolveTask, rejectTask) => {
                const runner = async () => {
                    if (this.current >= this.maxConcurrent) {
                        await new Promise(resolveQueue => this.queue.push(resolveQueue));
                    }
                    this.current++;
                    try {
                        const result = await fetchTask();
                        resolveTask(result);
                    } catch (error) {
                        rejectTask(error);
                    } finally {
                        this.current--;
                        if (this.queue.length > 0) {
                            const nextInQueue = this.queue.shift();
                            if (nextInQueue) nextInQueue();
                        }
                    }
                };
                runner();
            });
        }
    }


    // Capture network log
    function captureNetworkLog(options) {
        const networkLog = [];
        const baseUrl = window.location.origin;
        const imageExtensions = ['.png', '.jpg', '.jpeg', '.gif', '.webp', '.svg', '.ico']; // Added .ico
        const cookies = document.cookie || '';

        function isImageUrl(url) {
            try {
                const path = new URL(url, baseUrl).pathname;
                return imageExtensions.some(ext => path.toLowerCase().endsWith(ext));
            } catch {
                return false; // Invalid URL
            }
        }

        function getQueryParams(url) {
            try {
                const urlObj = new URL(url, baseUrl); // Ensure base URL for relative paths
                const params = {};
                urlObj.searchParams.forEach((value, key) => params[key] = value);
                return params;
            } catch {
                return {};
            }
        }

        const originalFetch = window.fetch;
        window.fetch = async function(input, init = {}) {
            const url = typeof input === 'string' ? input : input.url;
            const absoluteUrl = new URL(url, baseUrl).href; // Resolve to absolute URL

            if (!isImageUrl(absoluteUrl) && (options.externalResources || new URL(absoluteUrl).origin === baseUrl)) {
                const startTime = performance.now();
                const method = init.method || 'GET';
                const headers = init.headers || {};
                // Initiator logic might be complex to get accurately; simplifying
                const initiator = document.activeElement ? document.activeElement.tagName : 'unknown';
                const domDetails = initiator !== 'unknown' ? {
                    tag: initiator,
                    id: document.activeElement.id || null,
                    class: document.activeElement.className || null
                } : {};

                try {
                    const response = await originalFetch(input, init);
                    let responseBodyText = '';
                    // Clone response to read body, as it can only be read once
                    const clonedResponse = response.clone();
                    if (clonedResponse.headers.get('content-type')?.includes('json') || clonedResponse.headers.get('content-type')?.includes('text')) {
                        try {
                            responseBodyText = await clonedResponse.text();
                        } catch (e) {
                            console.warn('Error reading response body for network log:', e);
                            responseBodyText = '[Error reading body]';
                        }
                    } else {
                        responseBodyText = '[Non-text/json response body]';
                    }

                    networkLog.push({
                        url: absoluteUrl,
                        method,
                        headers: Object.fromEntries(new Headers(headers).entries()),
                        cookies: headers['Cookie'] || cookies, // Attempt to get request cookies
                        queryParams: getQueryParams(absoluteUrl),
                        status: response.status,
                        contentType: response.headers.get('content-type') || '',
                        timestamp: new Date().toISOString(),
                        duration: performance.now() - startTime,
                        initiatorType: 'fetch',
                        domDetails,
                        responseBody: responseBodyText
                    });
                    return response;
                } catch (error) {
                    networkLog.push({
                        url: absoluteUrl,
                        method,
                        headers: Object.fromEntries(new Headers(headers).entries()),
                        cookies: headers['Cookie'] || cookies,
                        queryParams: getQueryParams(absoluteUrl),
                        status: 0, // Indicate error
                        contentType: '',
                        timestamp: new Date().toISOString(),
                        duration: performance.now() - startTime,
                        initiatorType: 'fetch',
                        domDetails,
                        error: error.message
                    });
                    throw error;
                }
            }
            return originalFetch(input, init);
        };

        // Basic WebSocket logging (messages sent/received)
        const originalWebSocket = window.WebSocket;
        window.WebSocket = function(url, protocols) {
            const wsInstance = protocols ? new originalWebSocket(url, protocols) : new originalWebSocket(url);
            const absoluteUrl = new URL(url, baseUrl).href;

            wsInstance.addEventListener('open', () => {
                networkLog.push({
                    url: absoluteUrl,
                    method: 'WebSocket',
                    type: 'event',
                    event: 'open',
                    timestamp: new Date().toISOString(),
                    initiatorType: 'websocket'
                });
            });

            wsInstance.addEventListener('message', event => {
                networkLog.push({
                    url: absoluteUrl,
                    method: 'WebSocket',
                    type: 'message',
                    direction: 'received',
                    data: event.data, // Could be string, Blob, ArrayBuffer
                    timestamp: new Date().toISOString(),
                    initiatorType: 'websocket'
                });
            });

            wsInstance.addEventListener('error', (event) => {
                networkLog.push({
                    url: absoluteUrl,
                    method: 'WebSocket',
                    type: 'event',
                    event: 'error',
                    errorDetails: event.message || 'WebSocket Error',
                    timestamp: new Date().toISOString(),
                    initiatorType: 'websocket'
                });
            });

            wsInstance.addEventListener('close', (event) => {
                 networkLog.push({
                    url: absoluteUrl,
                    method: 'WebSocket',
                    type: 'event',
                    event: 'close',
                    code: event.code,
                    reason: event.reason,
                    wasClean: event.wasClean,
                    timestamp: new Date().toISOString(),
                    initiatorType: 'websocket'
                });
            });

            const originalSend = wsInstance.send;
            wsInstance.send = function(data) {
                networkLog.push({
                    url: absoluteUrl,
                    method: 'WebSocket',
                    type: 'message',
                    direction: 'sent',
                    data: data,
                    timestamp: new Date().toISOString(),
                    initiatorType: 'websocket'
                });
                return originalSend.call(this, data);
            };
            return wsInstance;
        };


        return {
            networkLog,
            cleanup: () => {
                window.fetch = originalFetch;
                window.WebSocket = originalWebSocket;
            }
        };
    }

    // Parse resources from HTML (or CSS content)
    function parseResourcesFromHtml(content, baseUrl, options, isDomOrCss = false) {
        const resources = new Map(); // Using Map to store { url: { path, type } }
        const links = new Set();     // For <a href> links if options.sameDomainLinks

        if (typeof content !== 'string') {
            console.warn('Invalid content for parsing resources:', content);
            return { resources, links };
        }

        const patterns = [
            // HTML patterns
            { regex: /<link[^>]+href=["']([^"']+)["']/gi, type: 'link_href' }, // For CSS files typically
            { regex: /<script[^>]+src=["']([^"']+)["']/gi, type: 'script_src' },
            { regex: /<img[^>]+src=["']([^"']+)["']/gi, type: 'image_src' },
            { regex: /<img[^>]+srcset=["']([^"']+)["']/gi, type: 'image_srcset' }, // Complex, grab whole srcset
            { regex: /<source[^>]+src=["']([^"']+)["']/gi, type: 'source_src' }, // For <audio>, <video>, <picture>
            { regex: /<source[^>]+srcset=["']([^"']+)["']/gi, type: 'source_srcset' }, // For <picture>
            { regex: /<audio[^>]+src=["']([^"']+)["']/gi, type: 'audio_src' },
            { regex: /<video[^>]+src=["']([^"']+)["']/gi, type: 'video_src' },
            { regex: /<embed[^>]+src=["']([^"']+)["']/gi, type: 'embed_src' },
            { regex: /<object[^>]+data=["']([^"']+)["']/gi, type: 'object_data' },
            { regex: /<iframe[^>]+src=["']([^"']+)["']/gi, type: 'iframe_src' },
            { regex: /<a[^>]+href=["']([^"']+)["']/gi, type: 'a_href' }, // For spidering if enabled
            // CSS patterns (url() an @import)
            { regex: /@import\s+(?:url\()?["']([^"']+)["']\)?/gi, type: 'css_import' },
            { regex: /url\(["']?([^"')]+?)["']?\)/gi, type: 'css_url' }, // More permissive for unquoted URLs
            // Inline style attributes
            { regex: /style=["'][^"']*url\(["']?([^"')]+?)["']?\)[^"']*["']/gi, type: 'inline_style_url'}
        ];

        patterns.forEach(({ regex, type }) => {
            let match;
            while ((match = regex.exec(content)) !== null) {
                const rawUrl = match[1].trim();
                if (!rawUrl || rawUrl.startsWith('data:') || rawUrl.startsWith('blob:') || rawUrl.startsWith('javascript:')) {
                    continue;
                }

                // Handle srcset by splitting and taking the first URL as representative (simplification)
                let urlsToProcess = [rawUrl];
                if (type.includes('srcset')) {
                    urlsToProcess = rawUrl.split(',').map(part => part.trim().split(/\s+/)[0]).filter(Boolean);
                }

                for (const url of urlsToProcess) {
                    try {
                        const fullUrl = new URL(url, baseUrl).href; // Resolve to absolute URL

                        if (!options.externalResources && new URL(fullUrl).origin !== new URL(baseUrl).origin) {
                            continue;
                        }

                        // Determine resource type more specifically
                        let resourceType = 'unknown';
                        if (type.startsWith('css_') || (type === 'link_href' && (url.includes('.css') || match[0].toLowerCase().includes('rel="stylesheet"')))) {
                            resourceType = 'CSS';
                        } else if (type.startsWith('script_')) {
                            resourceType = 'JavaScript';
                        } else if (type.startsWith('image_') || /\.(png|jpg|jpeg|gif|webp|svg|ico)(?:\?|$)/i.test(url)) {
                            resourceType = 'Image';
                        } else if (type.startsWith('audio_') || /\.(mp3|ogg|wav)(?:\?|$)/i.test(url)) {
                            resourceType = 'Audio';
                        } else if (type.startsWith('video_') || /\.(mp4|webm|ogg)(?:\?|$)/i.test(url)) {
                            resourceType = 'Video';
                        } else if (type === 'iframe_src') {
                            resourceType = 'Iframe'; // Could be a page to spider or just link
                        } else if (type === 'a_href') {
                            if (options.sameDomainLinks && new URL(fullUrl).origin === new URL(baseUrl).origin) {
                                links.add(fullUrl); // Add to links for spidering
                            }
                            resourceType = 'LinkedPage'; // Still add as a resource if we want to list it
                            // continue; // Or skip adding <a href> to general resources if only for spidering
                        }

                        // Create a relative path for storing the file
                        const urlPath = new URL(fullUrl).pathname;
                        const fileName = urlPath.substring(urlPath.lastIndexOf('/') + 1) || (resourceType === 'CSS' ? 'style.css' : 'index.html');
                        // Simple path: place all in root for now, or create basic structure
                        const savePath = fileName; // Could be enhanced to mimic dir structure


                        if (!resources.has(fullUrl)) {
                             resources.set(fullUrl, { path: savePath, type: resourceType });
                        }

                    } catch (e) {
                        console.warn(`Skipping invalid resource URL: "${url}" from ${type}. Error: ${e.message}`);
                    }
                }
            }
        });
        return { resources, links };
    }

    // Serialize full page (improved)
    function serializeFullPage(targetDoc = document) {
        // Clone the document to avoid modifying the live DOM, and to get a snapshot
        const clonedDoc = targetDoc.cloneNode(true);

        // Remove the script's own UI from the clone to prevent saving it
        const scriptUI = clonedDoc.querySelector('[id^="website-snapshot-container"]');
        if (scriptUI) {
            scriptUI.remove();
        }
        // Remove any no-script tags as they might interfere when viewed locally
        clonedDoc.querySelectorAll('noscript').forEach(ns => ns.remove());

        // Convert relative URLs to absolute (optional, but good for portability)
        // This is a complex task; for now, we rely on browsers handling relative paths
        // when the HTML is saved alongside its resources.

        // Get the doctype
        let doctypeStr = "";
        if (targetDoc.doctype) {
            doctypeStr = new XMLSerializer().serializeToString(targetDoc.doctype);
        }

        return doctypeStr + '\n' + clonedDoc.documentElement.outerHTML;
    }

    // Sniff form elements
    function sniffFormElements(targetDoc = document) {
        const elements = [];
        const formElements = targetDoc.querySelectorAll('input, textarea, select, button');
        formElements.forEach(el => {
            const labelEl = el.closest('label') || (el.id ? targetDoc.querySelector(`label[for="${el.id}"]`) : null);
            const label = labelEl?.textContent.trim() ||
                          el.getAttribute('aria-label') ||
                          el.getAttribute('placeholder') || // Use placeholder as a fallback for label
                          el.name || // Use name as a fallback
                          '';

            // Create a more robust selector
            let selector = el.tagName.toLowerCase();
            if (el.id) {
                selector += `#${CSS.escape(el.id)}`;
            } else if (el.name) {
                selector += `[name="${CSS.escape(el.name)}"]`;
            }
            // Could add class-based selectors as fallback if needed, but gets complex

            elements.push({
                tag: el.tagName,
                name: el.name || '',
                id: el.id || '',
                label: label || '',
                selector,
                type: el.type || '',
                value: el.tagName.toLowerCase() === 'textarea' ? el.textContent : el.value, // Get textarea content correctly
                checked: el.checked || false, // For checkboxes/radio
                disabled: el.disabled || false,
                required: el.required || false,
                formId: el.form?.id || el.closest('form')?.id || '',
                outerHTML: el.outerHTML // Include the element's HTML for context
            });
        });
        return elements;
    }


    // Collect resources
    async function collectResources(options, progressDiv, progressBar, fetchQueue, targetDoc = document) {
        const baseUrl = targetDoc.location.origin;
        const pageUrl = targetDoc.location.href;
        const failedResources = [];
        const visitedUrls = new Set(); // To avoid re-fetching same resource URL
        const files = {}; // Store as { 'path/filename.ext': content_or_blob }

        const { networkLog, cleanup: cleanupNetworkCapture } = options.networkTraffic ? captureNetworkLog(options) : { networkLog: [], cleanup: () => {} };

        let totalOperations = 0;
        let completedOperations = 0;

        function updateProgress() {
            completedOperations++;
            if (totalOperations > 0) {
                const percentage = (completedOperations / totalOperations) * 100;
                progressBar.style.width = `${percentage}%`;
                progressDiv.textContent = `Processing: ${completedOperations}/${totalOperations} items (${Math.round(percentage)}%)`;
            }
        }

        async function fetchResource(url, savePath, resourceType) {
            if (visitedUrls.has(url) || url.startsWith('data:') || url.startsWith('blob:')) {
                return;
            }
            visitedUrls.add(url);
            totalOperations++; // Increment for each new resource to fetch

            try {
                progressDiv.textContent = `Workspaceing ${resourceType}: ${savePath}...`;
                const response = await fetchQueue.enqueue(() =>
                    new Promise((resolve, reject) =>
                        GM_xmlhttpRequest({
                            method: 'GET',
                            url: url,
                            responseType: (resourceType === 'Image' || resourceType === 'Audio' || resourceType === 'Video' || !options.base64Images) ? 'blob' : 'text',
                            timeout: 15000, // 15 second timeout
                            onload: res => {
                                if (res.status >= 200 && res.status < 300) {
                                    resolve(res);
                                } else {
                                    reject(new Error(`HTTP ${res.status} for ${url}`));
                                }
                            },
                            onerror: () => reject(new Error(`Network error fetching ${url}`)),
                            ontimeout: () => reject(new Error(`Timeout fetching ${url}`))
                        })
                    )
                );

                let content;
                if (response.responseType === 'blob' || response.response instanceof Blob) {
                     if (options.base64Images && resourceType === 'Image') {
                        content = await new Promise((resolve, reject) => {
                            const reader = new FileReader();
                            reader.onloadend = () => resolve(reader.result.split(',')[1]); // Get Base64 part
                            reader.onerror = reject;
                            reader.readAsDataURL(response.response);
                        });
                        files[savePath + '.base64.txt'] = `/*Base64Encoded ${resourceType}*/\n${content}`;
                    } else {
                        content = response.response; // Store as Blob
                        files[savePath] = content;
                    }
                } else { // Text response
                    content = response.responseText;
                    files[savePath] = `/*${resourceType}*/\n${content}`;
                }
                console.log(`Workspaceed ${resourceType}: ${url} -> ${savePath}`);

            } catch (error) {
                console.warn(`Failed to fetch ${resourceType} ${url}: ${error.message}`);
                failedResources.push({ url, path: savePath, error: error.message, type: resourceType });
            } finally {
                updateProgress();
            }
        }

        // 1. Save main HTML
        if (options.fullPage) {
            totalOperations++;
            progressDiv.textContent = 'Serializing main page HTML...';
            try {
                files['index.html'] = `/*FullPageHTML*/\n${serializeFullPage(targetDoc)}`;
            } catch(e) {
                console.error("Error serializing main page:", e);
                files['index.html'] = ``;
                failedResources.push({url: pageUrl, path: 'index.html', error: `Serialization error: ${e.message}`, type: 'HTML'});
            }
            updateProgress();
        }

        // 2. Sniff form elements
        if (options.sniffElements) {
            totalOperations++;
            progressDiv.textContent = 'Sniffing form elements...';
            try {
                const formElements = sniffFormElements(targetDoc);
                files['form-elements.json'] = `/*FormElements*/\n${JSON.stringify(formElements, null, 2)}`;
            } catch(e) {
                console.error("Error sniffing form elements:", e);
                files['form-elements.json'] = `/* Error sniffing forms: ${e.message} */`;
                failedResources.push({url: pageUrl, path: 'form-elements.json', error: `Form sniffing error: ${e.message}`, type: 'JSON'});
            }
            updateProgress();
        }

        // 3. Parse and fetch resources from main document HTML
        progressDiv.textContent = 'Parsing resources from main document...';
        const { resources: mainDocResources, links: mainDocLinks } = parseResourcesFromHtml(targetDoc.documentElement.outerHTML, pageUrl, options, true);

        const resourceFetchPromises = [];

        mainDocResources.forEach((info, url) => {
            resourceFetchPromises.push(fetchResource(url, info.path, info.type));
        });

        // TODO: Spidering for options.sameDomainLinks (complex, defer for now or simplify)
        // For now, we just fetched resources from the main page.

        // Wait for all initial resource fetches from main page to be queued
        await Promise.allSettled(resourceFetchPromises);

        // Wait for FetchQueue to complete all its tasks (hacky way, better if FetchQueue had an 'idle' event)
        await new Promise(resolve => {
            const interval = setInterval(() => {
                if (fetchQueue.current === 0 && fetchQueue.queue.length === 0 && completedOperations >= totalOperations - fetchQueue.current - fetchQueue.queue.length) {
                    clearInterval(interval);
                    resolve();
                }
            }, 100);
        });


        // 4. Add network log if enabled
        if (options.networkTraffic) {
            files['network-log.json'] = `/*NetworkLog*/\n${JSON.stringify(networkLog, null, 2)}`;
            cleanupNetworkCapture(); // Stop capturing network requests
        }

        // 5. Add failed resources log
        if (failedResources.length > 0) {
            files['failed-resources.json'] = `/*FailedResources*/\n${JSON.stringify(failedResources, null, 2)}`;
        }

        progressBar.style.width = '100%';
        progressDiv.textContent = `Processing complete. ${completedOperations}/${totalOperations} items.`;
        return files;
    }


    // Main snapshot function
    async function takeSnapshot(options, progressDiv, progressBar) {
        console.log('takeSnapshot called with options:', options);
        progressDiv.textContent = 'Starting snapshot...';
        progressBar.style.width = '0%';

        try {
            if (typeof JSZip === 'undefined') {
                throw new Error('JSZip is not defined - library failed to load');
            }

            progressDiv.textContent = 'Detecting connection limits...';
            const maxConnections = options.throttleConnections
                ? await detectConnectionLimit(window.location.href)
                : 10; // Default if not throttling
            const fetchQueue = new FetchQueue(maxConnections);

            progressDiv.textContent = 'Collecting page data and resources...';
            // Pass the main document context to collectResources
            const files = await collectResources(options, progressDiv, progressBar, fetchQueue, document);

            if (options.debugNoZip) {
                progressDiv.textContent = 'Preparing debug output (JSON)...';
                // For debug mode, convert Blobs to Base64 strings for readability in JSON
                const debugFiles = {};
                for (const [filePath, content] of Object.entries(files)) {
                    if (content instanceof Blob) {
                        try {
                            debugFiles[filePath] = await new Promise((resolve, reject) => {
                                const reader = new FileReader();
                                reader.onloadend = () => resolve(`data:${content.type};base64,${reader.result.split(',')[1]}`);
                                reader.onerror = reject;
                                reader.readAsDataURL(content);
                            });
                        } catch (e) {
                            debugFiles[filePath] = `[Error converting Blob to Base64: ${e.message}]`;
                        }
                    } else {
                        debugFiles[filePath] = content;
                    }
                }
                const debugOutput = JSON.stringify(debugFiles, null, 2);
                const blob = new Blob([debugOutput], { type: 'application/json' });
                const url = URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = `website-snapshot-debug-${new Date().toISOString().replace(/:/g, '-')}.json`;
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                URL.revokeObjectURL(url);
                progressDiv.textContent = 'Debug output saved as JSON!';
                progressBar.style.width = '100%';
                return;
            }


            progressDiv.textContent = 'Generating zip archive...';
            progressBar.style.width = '95%'; // Indicate zipping started
            const zip = new JSZip();
            for (const [filePath, content] of Object.entries(files)) {
                zip.file(filePath, content); // JSZip can handle Blobs directly
            }

            const zipBlob = await zip.generateAsync({ type: 'blob', compression: "STORE" }); (metadata) => {
                // Update progress during zipping (optional, can be slow)
                // progressBar.style.width = `${95 + (metadata.percent * 0.05)}%`;
                // progressDiv.textContent = `Zipping: ${Math.round(metadata.percent)}%`;
            });
            const url = URL.createObjectURL(zipBlob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `website-snapshot-${new Date().toISOString().replace(/:/g, '-')}.zip`;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            URL.revokeObjectURL(url);

            progressDiv.textContent = 'Download complete!';
            progressBar.style.width = '100%';
        } catch (error) {
            console.error('Snapshot failed:', error);
            progressDiv.textContent = `Error: Snapshot failed - ${error.message}`;
            progressBar.style.width = '0%'; // Reset progress on error
            throw error; // Re-throw for handling by caller if needed
        }
    }

    // Initialize script
    ensureJSZip()
        .then(() => {
            console.log('Starting Website Snapshot Saver (v2.6 Modified for drag)');
            // Delay setupEyeIcon slightly to ensure page is more likely to be ready
            if (document.readyState === 'complete' || document.readyState === 'interactive') {
                setupEyeIcon();
            } else {
                window.addEventListener('DOMContentLoaded', setupEyeIcon, { once: true });
            }
        })
        .catch(error => {
            console.error('Failed to initialize Website Snapshot Saver:', error);
            if (window.confirm('Website Snapshot Saver failed to load critical component (JSZip). See console for details. Reload page and try again?')) {
                // window.location.reload(); // Could be too aggressive
            }
        });
})();