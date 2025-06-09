// ==UserScript==
// @name         Website Snapshot Saver
// @namespace    http://tampermonkey.net/
// @version      2.5
// @description  Saves full website pages (HTML, JS, CSS, images) and form elements into a zip file, optimized for BMC Helix with fixed drag and popup
// @author       You
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
            top: '20px',
            right: '20px',
            zIndex: '2147483647',
            cursor: 'move',
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
            }
            .popup {
                display: none;
                background: #fff;
                border: 1px solid #ccc;
                border-radius: 5px;
                padding: 10px;
                box-shadow: 0 4px 8px rgba(0,0,0,0.2);
                position: absolute;
                top: 40px;
                left: 0;
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

        // Draggable icon
        let isDragging = false;
        let offsetX, offsetY;

        iconContainer.addEventListener('mousedown', (e) => {
            e.preventDefault();
            e.stopPropagation();
            const rect = container.getBoundingClientRect();
            offsetX = e.clientX - rect.left;
            offsetY = e.clientY - rect.top;
            isDragging = true;
            console.log(`Drag start: clientX=${e.clientX}, rect.left=${rect.left}, offsetX=${offsetX}`);
            targetDocument.body.style.userSelect = 'none';
        });

        shadow.addEventListener('mousemove', (e) => {
            if (isDragging) {
                e.preventDefault();
                e.stopPropagation();
                const newX = e.clientX - offsetX;
                const newY = e.clientY - offsetY;
                const rect = container.getBoundingClientRect();
                const maxX = window.innerWidth - rect.width;
                const maxY = window.innerHeight - rect.height;
                const boundedX = Math.max(0, Math.min(newX, maxX));
                const boundedY = Math.max(0, Math.min(newY, maxY));
                container.style.left = `${boundedX}px`;
                container.style.top = `${boundedY}px`;
                container.style.right = 'auto';
                console.log(`Dragging: clientX=${e.clientX}, newX=${newX}, boundedX=${boundedX}`);
            }
        });

        shadow.addEventListener('mouseup', (e) => {
            e.stopPropagation();
            isDragging = false;
            targetDocument.body.style.userSelect = 'auto';
            console.log('Drag end');
        });

        // Popup toggle
        let isPopupOpen = false;
        iconContainer.addEventListener('click', (e) => {
            e.stopPropagation();
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
                popup.classList.remove('show');
                isPopupOpen = false;
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
            if (doc.body) {
                createFloatingEye(doc);
            } else {
                doc.addEventListener('DOMContentLoaded', () => createFloatingEye(doc), { once: true });
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
                        injectIntoDocument(iframeDoc);
                    }
                } catch (e) {
                    console.warn(`Cannot access iframe: ${e}`);
                }
            });
        };
        checkIframes();
        setInterval(checkIframes, 2000);

        // Monitor DOM changes
        const observer = new MutationObserver(() => {
            if (!document.querySelector('[id^="website-snapshot-container"]')) {
                console.warn('Icon container removed, reinjecting');
                injectIntoDocument(document);
            }
        });
        observer.observe(document.body, { childList: true, subtree: true });
    };

    // Extract TLD
    function extractTLD(hostname) {
        const parts = hostname.split('.');
        return parts.length > 2 ? parts.slice(-2).join('.') : hostname;
    }

    // Detect max connections
    async function detectConnectionLimit(url) {
        const urlObj = new URL(url);
        const tld = extractTLD(urlObj.hostname);
        const testUrl = `https://${tld}/favicon.ico`;
        let maxConnections = 1;
        let rateLimited = false;

        for (let i = 2; i <= 10 && !rateLimited; i++) {
            const promises = Array(i).fill().map(() =>
                GM_xmlhttpRequest({
                    method: 'HEAD',
                    url: testUrl,
                    onload: res => ({ status: res.status }),
                    onerror: () => ({ status: 0 })
                })
            );
            const results = await Promise.all(promises);
            rateLimited = results.some(res => res.status === 429 || res.status === 0);
            if (!rateLimited) maxConnections = i;
        }
        return maxConnections;
    }

    // Fetch queue
    class FetchQueue {
        constructor(maxConcurrent) {
            this.maxConcurrent = maxConcurrent;
            this.current = 0;
            this.queue = [];
        }

        async enqueue(fetchTask) {
            if (this.current >= this.maxConcurrent) {
                await new Promise(resolve => this.queue.push(resolve));
            }
            this.current++;
            try {
                return await fetchTask();
            } finally {
                this.current--;
                if (this.queue.length) this.queue.shift()();
            }
        }
    }

    // Capture network log
    function captureNetworkLog(options) {
        const networkLog = [];
        const baseUrl = window.location.origin;
        const imageExtensions = ['.png', '.jpg', '.jpeg', '.gif', '.webp', '.svg'];
        const cookies = document.cookie || '';

        function isImageUrl(url) {
            return imageExtensions.some(ext => url.toLowerCase().endsWith(ext));
        }

        function getQueryParams(url) {
            try {
                const urlObj = new URL(url);
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
            if (!isImageUrl(url) && (options.externalResources || new URL(url, baseUrl).origin === baseUrl)) {
                const startTime = performance.now();
                const method = init.method || 'GET';
                const headers = init.headers || {};
                const initiator = document.activeElement || null;
                const domDetails = initiator ? {
                    tag: initiator.tagName,
                    id: initiator.id || null,
                    class: initiator.className || null
                } : {};

                try {
                    const response = await originalFetch(input, init);
                    let body = '';
                    if (response.headers.get('content-type')?.includes('json')) {
                        body = await response.clone().text();
                    }
                    networkLog.push({
                        url,
                        method,
                        headers: Object.fromEntries(new Headers(headers).entries()),
                        cookies: headers['Cookie'] || cookies,
                        queryParams: getQueryParams(url),
                        status: response.status,
                        contentType: response.headers.get('content-type') || '',
                        timestamp: new Date().toISOString(),
                        duration: performance.now() - startTime,
                        initiatorType: 'fetch',
                        domDetails,
                        responseBody: body
                    });
                    return response;
                } catch (error) {
                    networkLog.push({
                        url,
                        method,
                        headers: Object.fromEntries(new Headers(headers).entries()),
                        cookies: headers['Cookie'] || cookies,
                        queryParams: getQueryParams(url),
                        status: 0,
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

        const originalWebSocket = window.WebSocket;
        window.WebSocket = function(url, protocols) {
            const ws = new originalWebSocket(url, protocols);
            ws.addEventListener('message', event => {
                networkLog.push({
                    url,
                    method: 'WebSocket',
                    data: event.data,
                    timestamp: new Date().toISOString(),
                    initiatorType: 'websocket'
                });
            });
            return ws;
        };

        return {
            networkLog,
            cleanup: () => {
                window.fetch = originalFetch;
                window.WebSocket = originalWebSocket;
            }
        };
    }

    // Parse resources from HTML
    function parseResourcesFromHtml(html, baseUrl, options, isDom = false) {
        const resources = new Map();
        const links = new Set();

        if (typeof html !== 'string') {
            console.warn('Invalid HTML content:', html);
            return { resources, links };
        }

        const patterns = [
            { regex: /<link[^>]+href=["'](.*?)["']/gi, attr: 'href', type: 'link' },
            { regex: /<script[^>]+src=["'](.*?)["']/gi, attr: 'src', type: 'script' },
            { regex: /<img[^>]+src=["'](.*?)["']/gi, attr: 'src', type: 'image' },
            { regex: /<picture[^>]*>[\s\S]*?<source[^>]+srcset=["'](.*?)["']/gi, attr: 'srcset', type: 'image' },
            { regex: /<audio[^>]+src=["'](.*?)["']/gi, attr: 'src', type: 'audio' },
            { regex: /<video[^>]+src=["'](.*?)["']/gi, attr: 'src', type: 'video' },
            { regex: /<source[^>]+src=["'](.*?)["']/gi, attr: 'src', type: 'media' },
            { regex: /<embed[^>]+src=["'](.*?)["']/gi, attr: 'src', type: 'embed' },
            { regex: /<object[^>]+data=["'](.*?)["']/gi, attr: 'src', type: 'object' },
            { regex: /<a[^>]+href=["'](.*?)["']/gi, attr: 'href', type: 'link' },
            { regex: /@import\s+url\(["'](.*?)["']\)/gi, attr: 'url', type: 'css-import' },
            { regex: /url\(["'](.*?)["']\)/gi, attr: 'url', type: 'css-url' }
        ];

        for (const { regex, attr, type } of patterns) {
            let match;
            while ((match = regex.exec(html)) !== null) {
                const url = match[1];
                try {
                    const fullUrl = new URL(url, baseUrl);
                    if (!options.externalResources && fullUrl.origin !== baseUrl) continue;
                    const path = fullUrl.pathname;
                    const resourcePath = path.substring(0, path.lastIndexOf('/')) || '/';
                    if (attr === 'href' && type === 'link' && regex.source.includes('<a')) {
                        if (options.sameDomainLinks && fullUrl.origin === baseUrl) {
                            links.add(fullUrl.href);
                        }
                        resources.set(fullUrl.href, { path: resourcePath + '/' + path.split('/').pop(), type: 'link' });
                    } else {
                        resources.set(fullUrl.href, { path: resourcePath + '/' + path.split('/').pop(), type });
                    }
                } catch {
                    console.warn(`Invalid URL skipped: ${url}`);
                }
            }
        }

        return { resources, links };
    }

    // Serialize full page
    function serializeFullPage() {
        const serializeNode = (node, indent = 0) => {
            if (!node) return '';
            let html = '';
            if (node.nodeType === Node.ELEMENT_NODE) {
                html += ' '.repeat(indent) + node.outerHTML + '\n';
                if (node.shadowRoot) {
                    html += ' '.repeat(indent) + '<!-- Shadow DOM -->\n';
                    html += serializeNode(node.shadowRoot, indent + 2);
                }
            } else if (node.nodeType === Node.DOCUMENT_NODE) {
                html += node.documentElement.outerHTML + '\n';
            }
            return html;
        };

        let fullHtml = `<!-- Main Document -->\n${serializeNode(document)}`;

        document.querySelectorAll('iframe').forEach(iframe => {
            try {
                const iframeDoc = iframe.contentDocument || iframe.contentWindow.document;
                if (iframeDoc) {
                    fullHtml += `<!-- Iframe: ${iframe.src || 'inline'} -->\n${serializeNode(iframeDoc, 2)}`;
                }
            } catch (e) {
                console.warn(`Cannot access iframe: ${e}`);
            }
        });

        const observer = new MutationObserver(mutations => {
            mutations.forEach(mutation => {
                mutation.addedNodes.forEach(node => {
                    if (node.nodeType === Node.ELEMENT_NODE) {
                        fullHtml += `<!-- Dynamic Node -->\n${serializeNode(node, 2)}`;
                    }
                });
            });
        });
        observer.observe(document.body, { childList: true, subtree: true });
        setTimeout(() => observer.disconnect(), 5000);

        return fullHtml;
    }

    // Sniff form elements
    function sniffFormElements() {
        const elements = [];
        const formElements = document.querySelectorAll('input, textarea, select, button');
        formElements.forEach(el => {
            const labelEl = el.closest('label') || document.querySelector(`label[for="${el.id}"]`);
            const label = labelEl?.textContent.trim() ||
                          el.getAttribute('aria-label') ||
                          el.getAttribute('aria-labelledby') ||
                          el.closest('fieldset')?.querySelector('legend')?.textContent.trim() ||
                          '';
            const selector = el.id ? `#${el.id}` :
                            el.name ? `[name="${el.name}"]` :
                            el.tagName.toLowerCase();
            elements.push({
                tag: el.tagName,
                name: el.name || '',
                id: el.id || '',
                label: label || '',
                selector,
                type: el.type || '',
                value: el.value || '',
                disabled: el.disabled || false,
                required: el.required || false,
                formId: el.closest('form')?.id || ''
            });
        });
        return elements;
    }

    // Collect resources
    async function collectResources(options, progressDiv, progressBar, fetchQueue) {
        const baseUrl = window.location.origin;
        const failedResources = [];
        const visited = new Set();
        const allResources = new Map();
        const allLinks = new Set();
        const { networkLog, cleanup } = options.networkTraffic ? captureNetworkLog(options) : { networkLog: [], cleanup: () => {} };
        const files = {};

        async function fetchWithRetries(url, maxRetries = 2, timeoutMs = 10000) {
            let retries = 0;
            while (retries <= maxRetries) {
                const controller = new AbortController();
                const timeout = setTimeout(() => controller.abort(), timeoutMs);
                try {
                    const response = await fetchQueue.enqueue(() =>
                        GM_xmlhttpRequest({
                            method: 'GET',
                            url,
                            responseType: 'blob',
                            signal: controller.signal,
                            onload: res => ({ response: new Response(res.response, { status: res.status, headers: new Headers(res.responseHeaders) }) }),
                            onerror: () => { throw new Error('GM_xmlhttpRequest failed'); }
                        })
                    );
                    clearTimeout(timeout);
                    if (!response.response.ok) throw new Error(`HTTP ${response.response.status}`);
                    return response.response;
                } catch (error) {
                    clearTimeout(timeout);
                    retries++;
                    if (retries > maxRetries) {
                        throw error;
                    }
                    await new Promise(resolve => setTimeout(resolve, 1000));
                }
            }
        }

        async function spiderPage(url, depth = 0) {
            if (visited.has(url) || depth > 1) return;
            visited.add(url);
            try {
                const response = await fetchWithRetries(url);
                const contentType = response.headers.get('content-type') || '';
                const filePath = new URL(url).pathname.split('/').pop() || 'index.html';
                let content = await response.text();
                const resourceType = contentType.includes('text/html') ? 'HTML' :
                                    contentType.includes('javascript') || url.endsWith('.js') ? 'JavaScript' :
                                    contentType.includes('css') || url.endsWith('.css') ? 'CSS' :
                                    contentType.includes('image') ? 'Image' : 'Other';
                content = `/*${resourceType}*/\n${content}`;
                files[filePath] = content;

                if (contentType.includes('text/html')) {
                    const { resources, links } = parseResourcesFromHtml(content, url, options);
                    resources.forEach((info, resourceUrl) => allResources.set(resourceUrl, info));
                    links.forEach(link => allLinks.add(link));
                }
            } catch (error) {
                failedResources.push({ url, error: error.message });
            }
        }

        if (options.fullPage) {
            progressDiv.textContent = 'Serializing full page...';
            files['full-page.html'] = `/*FullPage*/\n${serializeFullPage()}`;
        }

        if (options.sniffElements) {
            progressDiv.textContent = 'Sniffing form elements...';
            const formElements = sniffFormElements();
            files['form-elements.json'] = `/*FormElements*/\n${JSON.stringify(formElements, null, 2)}`;
        }

        await spiderPage(window.location.href);
        const { resources: domResources, links: domLinks } = parseResourcesFromHtml(document.documentElement.outerHTML, baseUrl, options, true);
        domResources.forEach((info, url) => allResources.set(url, info));
        domLinks.forEach(link => allLinks.add(link));

        let fetchedCount = 0;
        const totalCount = allResources.size + allLinks.size + (options.networkTraffic ? 1 : 0);
        progressBar.style.width = '0%';

        async function fetchResource(url, resourceInfo) {
            try {
                const response = await fetchWithRetries(url);
                const contentType = response.headers.get('content-type') || '';
                let data = await response.blob();
                const filePath = resourceInfo.path.split('/').pop();
                let resourceType = resourceInfo.type;

                if (resourceType === 'script' || contentType.includes('javascript') || url.endsWith('.js')) resourceType = 'JavaScript';
                else if (resourceType === 'link' && (contentType.includes('css') || url.endsWith('.css'))) resourceType = 'CSS';
                else if (resourceType === 'image' || contentType.includes('image')) resourceType = 'Image';
                else if (resourceType === 'audio' || contentType.includes('audio')) resourceType = 'Audio';
                else if (resourceType === 'video' || contentType.includes('video')) resourceType = 'Video';
                else resourceType = 'Other';

                let fileContent;
                if (options.base64Images && contentType.includes('image')) {
                    const reader = new FileReader();
                    fileContent = await new Promise(resolve => {
                        reader.onload = () => resolve(reader.result.split(',')[1]);
                        reader.readAsDataURL(data);
                    });
                    fileContent = `/*${resourceType}*/\n${fileContent}`;
                    files[filePath + '.base64.txt'] = fileContent;
                } else {
                    fileContent = await data.text();
                    fileContent = `/*${resourceType}*/\n${fileContent}`;
                    files[filePath] = fileContent;
                }

                fetchedCount++;
                progressDiv.textContent = `Fetching resources: ${fetchedCount}/${totalCount}`;
                progressBar.style.width = `${(fetchedCount / totalCount) * 100}%`;
            } catch (error) {
                console.warn(`Failed to fetch ${url}: ${error}`);
                failedResources.push({ url, path: resourceInfo.path, error: error.message });
            }
        }

        const fetchPromises = [];
        allResources.forEach((resourceInfo, url) => {
            fetchPromises.push(fetchResource(url, resourceInfo).catch(() => null));
        });

        for (const link of allLinks) {
            await spiderPage(link, 1);
            fetchedCount++;
            progressDiv.textContent = `Spidering links: ${fetchedCount}/${totalCount}`;
            progressBar.style.width = `${(fetchedCount / totalCount) * 100}%`;
        }

        if (options.networkTraffic) {
            files['network-log.json'] = `/*NetworkLog*/\n${JSON.stringify(networkLog, null, 2)}`;
            fetchedCount++;
            progressDiv.textContent = `Saved network log: ${fetchedCount}/${totalCount}`;
            progressBar.style.width = `${(fetchedCount / totalCount) * 100}%`;
            cleanup();
        }

        if (failedResources.length > 0) {
            files['failed-resources.json'] = `/*FailedResources*/\n${JSON.stringify(failedResources, null, 2)}`;
        }

        await Promise.all(fetchPromises);
        return files;
    }

    // Main snapshot function
    async function takeSnapshot(options, progressDiv, progressBar) {
        console.log('takeSnapshot called with options:', options);
        try {
            if (options.debugNoZip) {
                progressDiv.textContent = 'Collecting resources (debug mode, no zip)...';
                const files = await collectResources(options, progressDiv, progressBar, new FetchQueue(10));
                const debugOutput = JSON.stringify(files, null, 2);
                const blob = new Blob([debugOutput], { type: 'application/json' });
                const url = URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = `website-snapshot-debug-${new Date().toISOString()}.json`;
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                URL.revokeObjectURL(url);
                progressDiv.textContent = 'Debug output saved!';
                progressBar.style.width = '100%';
                return;
            }

            if (typeof JSZip === 'undefined') {
                throw new Error('JSZip is not defined - library failed to load');
            }

            progressDiv.textContent = 'Detecting connection limits...';
            const maxConnections = options.throttleConnections
                ? await detectConnectionLimit(window.location.href)
                : 10;
            const fetchQueue = new FetchQueue(maxConnections);

            progressDiv.textContent = 'Collecting resources and network log...';
            const files = await collectResources(options, progressDiv, progressBar, fetchQueue);

            progressDiv.textContent = 'Generating zip archive...';
            const zip = new JSZip();
            for (const [filePath, content] of Object.entries(files)) {
                zip.file(filePath, content);
            }

            const zipBlob = await zip.generateAsync({ type: 'blob' });
            const url = URL.createObjectURL(zipBlob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `website-snapshot-${new Date().toISOString()}.zip`;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            URL.revokeObjectURL(url);

            progressDiv.textContent = 'Download complete!';
            progressBar.style.width = '100%';
        } catch (error) {
            console.error('Snapshot failed:', error);
            progressDiv.textContent = `Error: Snapshot failed - ${error.message}`;
            progressBar.style.width = '0%';
            throw error;
        }
    }

    // Initialize script
    ensureJSZip()
        .then(() => {
            console.log('Starting Website Snapshot Saver');
            setupEyeIcon();
        })
        .catch(error => {
            console.error('Failed to initialize script:', error);
            alert('Website Snapshot Saver failed to load JSZip: ' + error.message);
        });
})();