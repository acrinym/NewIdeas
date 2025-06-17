// ==UserScript==
// @name         Website Snapshot Saver 3.7 (Merged & Deduped)
// @namespace    http://tampermonkey.net/
// @version      3.7
// @description  Download all resources with a full resource picker, domain settings, classic crawl, file size filter, self-healing draggable Shadow DOM overlay, and ultra-fast zipping via fflate. Merged and improved by an assistant.
// @author       Justin & Assistant
// @match        *://*/*
// @grant        GM_xmlhttpRequest
// @connect      *
// @require      https://cdn.jsdelivr.net/npm/fflate@0.8.1/umd/index.js
// ==/UserScript==

(function() {
    'use strict';
    const fflate = window.fflate;
    const SIZE_KEY = 'snapshotSkipSizeMB';
    let savedSize = parseFloat(localStorage.getItem(SIZE_KEY));
    if (!Number.isFinite(savedSize) || savedSize <= 0) savedSize = 5;

    // ---- UTILITY FUNCTIONS ----

    /**
     * Converts a Blob to a Uint8Array.
     * @param {Blob} blob - The blob to convert.
     * @returns {Promise<Uint8Array>}
     */
    function blobToUint8Array(blob) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = function(e) {
                resolve(new Uint8Array(e.target.result));
            };
            reader.onerror = reject;
            reader.readAsArrayBuffer(blob);
        });
    }

    /**
     * Zips files using fflate's streaming API with a progress callback.
     * @param {Object.<string, Uint8Array>} files - An object where keys are filenames and values are file data.
     * @param {object} opts - fflate options.
     * @param {function(number): void} progressCb - Callback for zip progress (0 to 1).
     * @returns {Promise<Uint8Array>} The zipped data.
     */
    function zipWithProgress(files, opts, progressCb) {
        return new Promise((resolve, reject) => {
            const chunks = [];
            const zip = new fflate.Zip((err, dat, final) => {
                if (err) {
                    reject(err);
                    return;
                }
                chunks.push(dat);
                if (final) {
                    const total = chunks.reduce((n, c) => n + c.length, 0);
                    const out = new Uint8Array(total);
                    let off = 0;
                    for (const c of chunks) {
                        out.set(c, off);
                        off += c.length;
                    }
                    resolve(out);
                }
            });

            const names = Object.keys(files);
            let done = 0;
            names.forEach(name => {
                const data = files[name];
                const stream = new fflate.AsyncZipDeflate(name, opts);
                zip.add(stream);
                const orig = stream.ondata;
                stream.ondata = function(err, dat, final) {
                    orig.call(this, err, dat, final);
                    if (final) {
                        done++;
                        if (progressCb) progressCb(done / names.length);
                    }
                };
                stream.push(data, true);
            });
            zip.end();
        });
    }


    // ---- DOMAIN UTILITY FUNCTIONS ----

    function getDomainRoot(url) {
        try {
            const u = new URL(url);
            const host = u.hostname;
            const parts = host.split('.');
            if (parts.length <= 2) return host;
            const tld2 = parts.slice(-2).join('.');
            const tld3 = parts.slice(-3).join('.');
            // Common second-level domains
            return /(\.co\.|\.ac\.|\.com\.)[a-z]{2,3}$/i.test(tld3) ? tld3 : tld2;
        } catch (e) {
            return window.location.hostname;
        }
    }

    function sameDomain(url) {
        try {
            return getDomainRoot(url) === getDomainRoot(window.location.href);
        } catch (e) {
            return false;
        }
    }

    function sameSubdomain(url) {
        try {
            const here = new URL(window.location.href);
            const test = new URL(url, here);
            return test.hostname === here.hostname;
        } catch (e) {
            return false;
        }
    }


    // ---- SMART RESOURCE SNIFFER ----

    async function smartResourceSniffer(options) {
        options = Object.assign({
            includeIcons: true,
            includePosters: true,
            includeCssExtras: true
        }, options);
        const resList = [];
        const seen = new Set();

        function addRes(url, type, foundIn, attr, suggestedName, isBlob) {
            if (!url || seen.has(url) || url.startsWith('javascript:') || url.startsWith('data:')) return;

            // Apply domain restrictions
            const absoluteUrl = new URL(url, window.location.href).href;
            if (options.stayOnSubdomain && !sameSubdomain(absoluteUrl)) return;
            if (options.stayOnDomain && !sameDomain(absoluteUrl)) return;

            seen.add(absoluteUrl);
            resList.push({
                url: absoluteUrl,
                type,
                foundIn,
                attr,
                suggestedName: suggestedName || absoluteUrl.split('/').pop().split('?')[0] || 'file',
                isBlob: !!isBlob
            });
        }

        const selectors = [
            { sel: 'script[src]', type: 'js', attr: 'src' },
            { sel: 'link[href][rel~="stylesheet"]', type: 'css', attr: 'href' },
            { sel: 'img[src]', type: 'img', attr: 'src' },
            { sel: 'img[srcset]', type: 'imgset', attr: 'srcset' },
            { sel: 'video[poster]', type: 'poster', attr: 'poster' },
            { sel: 'source[src]', type: 'media', attr: 'src' },
            { sel: 'source[srcset]', type: 'mediaset', attr: 'srcset' },
            { sel: 'audio[src]', type: 'audio', attr: 'src' },
            { sel: 'video[src]', type: 'video', attr: 'src' },
            { sel: 'embed[src]', type: 'embed', attr: 'src' },
            { sel: 'object[data]', type: 'object', attr: 'data' },
            { sel: 'iframe[src]', type: 'iframe', attr: 'src' },
            { sel: 'link[rel~="icon"][href]', type: 'icon', attr: 'href' },
            { sel: 'link[rel="manifest"][href]', type: 'manifest', attr: 'href' },
            { sel: 'a[href]', type: 'link', attr: 'href' }
        ];

        selectors.forEach(({ sel, type, attr }) => {
            document.querySelectorAll(sel).forEach(el => {
                let raw = el.getAttribute(attr);
                if (!raw) return;

                // Option checks
                if ((type === 'icon' || type === 'manifest') && !options.includeIcons) return;
                if (type === 'poster' && !options.includePosters) return;

                if (attr === 'srcset') {
                    raw.split(',').map(e => e.trim().split(' ')[0]).forEach(r => addRes(r, type, sel, attr));
                } else if (sel === 'a[href]') {
                    const isFile = /\.(zip|rar|7z|exe|mp3|mp4|wav|avi|mov|pdf|docx?|xlsx?|pptx?|png|jpe?g|gif|svg|webp|csv|json|xml|txt|tar|gz)$/i.test(raw);
                    const isStream = /^rtsp:|^rtmp:|^mms:|^ftp:/i.test(raw);
                    if (isFile || isStream) {
                        addRes(raw, type, sel, attr);
                    }
                } else {
                    addRes(raw, type, sel, attr);
                }
            });
        });

        // Search through stylesheets for embedded resources
        if (options.includeCssExtras) {
            for (const sheet of document.styleSheets) {
                let rules;
                try {
                    rules = sheet.cssRules || sheet.rules;
                } catch (e) {
                    continue;
                }
                if (!rules) continue;

                for (const rule of rules) {
                    if (!rule) continue;
                    const cssText = rule.cssText || '';
                    if (/@import/i.test(cssText)) {
                        let imp = /@import\s+url\(['"]?([^'")]+)['"]?\)/i.exec(cssText);
                        if (imp) addRes(imp[1], 'css-import', '@import', 'css-url');
                    }
                    if (/@font-face/i.test(cssText)) {
                        let m = /url\(['"]?([^'")]+)['"]?\)/i.exec(cssText);
                        if (m) addRes(m[1], 'font', '@font-face', 'css-url');
                    }
                    if (rule.style && rule.style.backgroundImage) {
                        [...rule.style.backgroundImage.matchAll(/url\(['"]?([^'")]+)['"]?\)/ig)]
                        .forEach(match => addRes(match[1], 'bgimg', 'css-bg', 'css-url'));
                    }
                }
            }
            // Inline styles
            document.querySelectorAll('[style]').forEach(el => {
                if (el.style.backgroundImage) {
                    [...el.style.backgroundImage.matchAll(/url\(['"]?([^'")]+)['"]?\)/ig)]
                    .forEach(match => addRes(match[1], 'bgimg', '[style]', 'css-url'));
                }
            });
            // <style> tags
            document.querySelectorAll('style').forEach(el => {
                [...el.textContent.matchAll(/@import\s+url\(['"]?([^'"]+)['"]?\)/ig)]
                .forEach(m => addRes(m[1], 'css-import', '<style>', 'css-url'));
            });
        }


        // Find resources in Shadow DOM
        function sniffShadow(node) {
            if (node.shadowRoot) {
                // Recursively check elements inside the shadow root
                node.shadowRoot.querySelectorAll('*').forEach(sniffShadow);
                // Find resources directly within the shadow root
                node.shadowRoot.querySelectorAll('[src],[href]').forEach(child => {
                    ['src', 'href'].forEach(attr => {
                        let raw = child.getAttribute(attr);
                        if (raw) addRes(raw, 'shadow', 'shadowRoot', attr);
                    });
                });
            }
        }
        document.querySelectorAll('*').forEach(sniffShadow);

        // Find blob resources
        document.querySelectorAll('[src^="blob:"],[href^="blob:"]').forEach(el => {
            let src = el.getAttribute('src') || el.getAttribute('href');
            if (src) addRes(src, 'blob', el.tagName, 'src/href', undefined, true);
        });

        // Final list is already canonicalized by addRes
        return resList;
    }


    // ---- SHADOW DOM OVERLAY & UI ----

    function createSnapshotShadowHost() {
        // Remove previous instance if it exists
        const prev = document.getElementById('snapshot-shadow-host');
        if (prev) prev.remove();

        const host = document.createElement('div');
        host.id = 'snapshot-shadow-host';
        document.body.appendChild(host);

        const shadow = host.attachShadow({
            mode: 'open'
        });

        const style = document.createElement('style');
        style.textContent = `
        :host {
            all: initial; /* Reset all inherited styles */
            display: block !important;
            position: fixed !important;
            top: 20px !important;
            right: 20px !important;
            z-index: 2147483647 !important;
            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
            font-size: 14px;
        }
        .icon-container {
            background: rgba(255,255,255,0.97);
            border-radius: 50%;
            padding: 6px;
            box-shadow: 0 2px 7px rgba(0,0,0,0.24);
            pointer-events: auto;
            display: inline-block;
            cursor: grab;
            transition: box-shadow 0.2s;
        }
        .icon-container:active {
            cursor: grabbing;
            box-shadow: 0 6px 16px rgba(0,0,0,0.40);
        }
        .popup {
            display: none;
            background: #fff;
            border: 1px solid #ccc;
            border-radius: 8px;
            padding: 16px;
            box-shadow: 0 8px 28px rgba(0,0,0,0.28);
            position: absolute;
            top: 52px;
            right: 0;
            z-index: 2147483647;
            width: 460px;
            min-height: 220px;
            pointer-events: auto;
        }
        .popup.show { display: block !important; }
        .popup label { display: block; margin: 8px 0; cursor: pointer; user-select: none; }
        .popup input[type=checkbox] { margin-right: 6px; vertical-align: middle; }
        .popup input[type=number], .popup input[type=text] {
             width: 60px; padding: 4px; border: 1px solid #ccc; border-radius: 4px;
        }
        .popup button {
            margin: 10px 5px 0 0;
            padding: 8px 14px;
            cursor: pointer;
            border: 1px solid #aaa;
            border-radius: 5px;
            background-color: #f0f0f0;
        }
        .popup button:hover { background-color: #e0e0e0; }
        .popup button:disabled { cursor: not-allowed; opacity: 0.6; }
        .sniff-summary {
            background: #f9f9fb;
            border: 1px solid #dedee6;
            border-radius: 4px;
            max-height: 180px;
            overflow-y: auto;
            margin: 12px 0;
            padding: 8px;
            font-size: 12px;
        }
        .sniff-summary ul { list-style: none; padding-left: 0; margin: 0; }
        .sniff-summary li { margin-bottom: 5px; word-break: break-all; }
        .progress { margin-top: 12px; font-size: 13px; color: #333; }
        .progress-bar { width: 100%; background: #eee; height: 8px; border-radius: 4px; overflow: hidden; margin-top: 4px; }
        .progress-bar-fill { height: 100%; background: #29a96e; width: 0; transition: width 0.25s; }
        hr { border: 0; border-top: 1px solid #eee; margin: 12px 0; }
        `;
        shadow.appendChild(style);

        const iconContainer = document.createElement('div');
        iconContainer.className = 'icon-container';
        iconContainer.title = 'Show Website Snapshot Saver options';
        iconContainer.innerHTML = `<svg width="32" height="32" viewBox="0 0 24 24" fill="none"><path d="M12 4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5zM12 17c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5zm0-8c-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3-1.34-3-3-3z" fill="#000"/></svg>`;
        shadow.appendChild(iconContainer);

        const popup = document.createElement('div');
        popup.className = 'popup';
        popup.innerHTML = `
            <div>
                <label><input type="checkbox" id="stayOnSubdomain" checked> Stay on this subdomain only</label>
                <label><input type="checkbox" id="stayOnDomain"> Allow all of this domain</label>
                <label><input type="checkbox" id="allowExternalDomains"> Traverse other domains (crawl only)</label>
            </div>
            <hr>
            <div>
                <label><input type="checkbox" id="iconsManifests" checked> Include Icons & manifests</label>
                <label><input type="checkbox" id="videoPosters" checked> Include Video posters</label>
                <label><input type="checkbox" id="cssExtras" checked> Include CSS imports & background images</label>
            </div>
            <hr>
            <div>
                <label>Skip files larger than <input type="number" id="skipSize" value="${savedSize}" min="1" style="width:5em;"> MB</label>
                <label>Max crawl depth: <input type="number" id="maxDepth" min="0" style="width:4em;" placeholder="∞"></label>
                <label>User Agent: <input type="text" id="userAgent" placeholder="Browser default" style="width:12em"></label>
            </div>
            <hr>
            <button id="sniffBtn">Sniff Current Page Resources</button>
            <button id="classicBtn">Full Website Snapshot (Crawl)</button>
            <div class="sniff-summary" id="sniffSummary">No resources sniffed yet.</div>
            <button id="saveButton" disabled>Save Selected as ZIP</button>
            <button id="cancelButton">Close</button>
            <div class="progress" id="progress">Ready</div>
            <div class="progress-bar"><div class="progress-bar-fill" id="progressBar"></div></div>
        `;
        shadow.appendChild(popup);

        // --- Drag Logic ---
        let isDragging = false,
            offsetX, offsetY, justDragged = false;
        iconContainer.addEventListener('mousedown', (e) => {
            e.preventDefault();
            e.stopPropagation();
            const rect = host.getBoundingClientRect();
            host.style.left = `${rect.left}px`;
            host.style.top = `${rect.top}px`;
            host.style.right = 'auto';
            isDragging = true;
            justDragged = false;
            offsetX = e.clientX - rect.left;
            offsetY = e.clientY - rect.top;
            document.body.style.userSelect = 'none';
        });
        document.addEventListener('mousemove', (e) => {
            if (isDragging) {
                justDragged = true;
                let newX = e.clientX - offsetX;
                let newY = e.clientY - offsetY;
                const rect = host.getBoundingClientRect();
                const maxX = window.innerWidth - rect.width;
                const maxY = window.innerHeight - rect.height;
                host.style.left = `${Math.max(0, Math.min(newX, maxX))}px`;
                host.style.top = `${Math.max(0, Math.min(newY, maxY))}px`;
            }
        });
        document.addEventListener('mouseup', () => {
            if (isDragging) {
                isDragging = false;
                document.body.style.userSelect = '';
                setTimeout(() => { justDragged = false; }, 0);
            }
        });

        // --- Popup Toggle ---
        let isPopupOpen = false;
        iconContainer.addEventListener('click', (e) => {
            if (justDragged) return;
            e.stopPropagation();
            isPopupOpen = !isPopupOpen;
            popup.classList.toggle('show', isPopupOpen);
        });

        // --- UI Element References & Event Listeners ---
        const sniffBtn = popup.querySelector('#sniffBtn');
        const classicBtn = popup.querySelector('#classicBtn');
        const saveButton = popup.querySelector('#saveButton');
        const cancelButton = popup.querySelector('#cancelButton');
        const sniffSummary = popup.querySelector('#sniffSummary');
        const progressDiv = popup.querySelector('#progress');
        const progressBar = popup.querySelector('#progressBar');
        const stayOnSubdomain = popup.querySelector('#stayOnSubdomain');
        const stayOnDomain = popup.querySelector('#stayOnDomain');
        const allowExternalDomains = popup.querySelector('#allowExternalDomains');
        const iconsManifests = popup.querySelector('#iconsManifests');
        const videoPosters = popup.querySelector('#videoPosters');
        const cssExtras = popup.querySelector('#cssExtras');
        const skipSizeInput = popup.querySelector('#skipSize');
        const maxDepthInput = popup.querySelector('#maxDepth');
        const userAgentInput = popup.querySelector('#userAgent');

        let sniffedResources = [];

        // --- Logic for UI interactions ---

        skipSizeInput.addEventListener('change', () => {
            localStorage.setItem(SIZE_KEY, skipSizeInput.value);
        });

        // Mutual exclusion for domain settings
        function updateDomainToggles(event) {
            const source = event.target;
            if (source === allowExternalDomains && source.checked) {
                stayOnDomain.checked = stayOnSubdomain.checked = false;
            } else if (source === stayOnDomain && source.checked) {
                allowExternalDomains.checked = stayOnSubdomain.checked = false;
            } else if (source === stayOnSubdomain && source.checked) {
                allowExternalDomains.checked = stayOnDomain.checked = false;
            }
            // Ensure at least one is checked
            if (!allowExternalDomains.checked && !stayOnDomain.checked && !stayOnSubdomain.checked) {
                stayOnSubdomain.checked = true;
            }
        }
        [allowExternalDomains, stayOnDomain, stayOnSubdomain].forEach(el => {
            el.addEventListener('change', updateDomainToggles);
        });


        // Sniff Button Action
        sniffBtn.addEventListener('click', async () => {
            saveButton.disabled = true;
            sniffSummary.innerHTML = '<i>Scanning...</i>';
            progressDiv.textContent = 'Sniffing page resources...';
            progressBar.style.width = '10%';

            const opts = {
                stayOnSubdomain: stayOnSubdomain.checked,
                stayOnDomain: stayOnDomain.checked,
                // allowExternalDomains is not used by sniffer, only crawler
                includeIcons: iconsManifests.checked,
                includePosters: videoPosters.checked,
                includeCssExtras: cssExtras.checked,
                userAgent: userAgentInput.value.trim()
            };

            sniffedResources = await smartResourceSniffer(opts);
            progressBar.style.width = '40%';
            
            // Get HEAD info for size/type
            for (let i = 0; i < sniffedResources.length; ++i) {
                const r = sniffedResources[i];
                if (r.isBlob) continue; // Cannot HEAD a blob URL
                await new Promise(res => {
                    GM_xmlhttpRequest({
                        method: 'HEAD',
                        url: r.url,
                        timeout: 5000,
                        headers: opts.userAgent ? { 'User-Agent': opts.userAgent } : {},
                        onload: (resp) => {
                            const sizeHeader = resp.responseHeaders.match(/content-length: ?(\d+)/i);
                            const typeHeader = resp.responseHeaders.match(/content-type: ?([\w\/\-\.\+]+)/i);
                            r.size = sizeHeader ? parseInt(sizeHeader[1], 10) : null;
                            r.mime = typeHeader ? typeHeader[1] : null;
                            res();
                        },
                        onerror: res,
                        ontimeout: res
                    });
                });
                progressBar.style.width = `${40 + (i / sniffedResources.length) * 30}%`;
            }

            progressBar.style.width = '70%';
            if (sniffedResources.length === 0) {
                sniffSummary.innerHTML = '<em>No downloadable resources detected with current settings.</em>';
            } else {
                sniffSummary.innerHTML = `<strong>${sniffedResources.length} resources found:</strong><ul>` +
                    sniffedResources.map(r =>
                        `<li>
                           <input type="checkbox" class="reschk" checked data-url="${r.url}">
                           <strong>${r.suggestedName}</strong>
                           <span style="color:#888;">[${r.type}]</span>
                           <small>${r.mime || ''} ${r.size ? `(${(r.size / 1024).toFixed(1)}KB)` : ''}</small>
                         </li>`
                    ).join('') + '</ul>';
            }
            progressDiv.textContent = 'Ready. Review resources to save.';
            progressBar.style.width = '100%';
            saveButton.disabled = sniffedResources.length === 0;
        });

        // Save Button Action
        saveButton.addEventListener('click', async () => {
            const checkedUrls = new Set(
                Array.from(shadow.querySelectorAll('.reschk:checked')).map(cb => cb.dataset.url)
            );
            const resourcesToSave = sniffedResources.filter(r => checkedUrls.has(r.url));
            if (resourcesToSave.length === 0) {
                 alert('No resources selected to save.');
                 return;
            }

            progressDiv.textContent = 'Downloading selected resources...';
            progressBar.style.width = '0%';
            const files = {};
            const summary = [];
            const skipLimit = parseFloat(skipSizeInput.value) * 1024 * 1024;

            for (let i = 0; i < resourcesToSave.length; ++i) {
                const r = resourcesToSave[i];
                if (skipLimit > 0 && r.size && r.size > skipLimit) {
                    summary.push({ ...r, skipped: `File >${skipSizeInput.value}MB` });
                    continue;
                }
                
                progressDiv.textContent = `Downloading: ${r.suggestedName} (${i + 1}/${resourcesToSave.length})`;
                progressBar.style.width = `${(i / resourcesToSave.length) * 80}%`;

                await new Promise(resolve => {
                    GM_xmlhttpRequest({
                        method: 'GET',
                        url: r.url,
                        responseType: 'blob',
                        timeout: 20000,
                        headers: userAgentInput.value.trim() ? { 'User-Agent': userAgentInput.value.trim() } : {},
                        onload: async (resp) => {
                            files[r.suggestedName] = await blobToUint8Array(resp.response);
                            summary.push({ ...r, size: r.size || resp.response.size, mime: r.mime || resp.response.type });
                            resolve();
                        },
                        onerror: () => {
                            summary.push({ ...r, error: 'Failed to download' });
                            resolve();
                        },
                        ontimeout: () => {
                            summary.push({ ...r, error: 'Timeout' });
                            resolve();
                        }
                    });
                });
            }

            files['sniffed-summary.json'] = new TextEncoder().encode(JSON.stringify(summary, null, 2));
            progressBar.style.width = '90%';
            progressDiv.textContent = 'Generating zip...';

            const zipData = await zipWithProgress(files, { level: 0 }, pct => {
                progressBar.style.width = `${90 + pct * 10}%`;
                progressDiv.textContent = `Zipping: ${Math.round(pct * 100)}%`;
            }).catch(err => {
                progressDiv.textContent = 'ZIP failed: ' + err;
                console.error('ZIP failed:', err);
                return null;
            });

            if (!zipData) return;
            let zipBlob = new Blob([zipData], { type: 'application/zip' });
            let url = URL.createObjectURL(zipBlob);
            let a = document.createElement('a');
            a.href = url;
            a.download = `website-sniffed-resources-${new Date().toISOString().replace(/:/g, '-')}.zip`;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            setTimeout(() => URL.revokeObjectURL(url), 5000);
            progressDiv.textContent = 'ZIP saved!';
            progressBar.style.width = '100%';
        });

        // Classic Crawl Button Action
        classicBtn.addEventListener('click', async () => {
            popup.classList.remove('show');
            isPopupOpen = false;
            await runFullSnapshot({
                stayOnSubdomain: stayOnSubdomain.checked,
                stayOnDomain: stayOnDomain.checked,
                allowExternalDomains: allowExternalDomains.checked,
                includeIcons: iconsManifests.checked,
                includePosters: videoPosters.checked,
                includeCssExtras: cssExtras.checked,
                skipLargerThan: parseFloat(skipSizeInput.value) || 0,
                maxDepth: maxDepthInput.value ? parseInt(maxDepthInput.value, 10) : Infinity,
                userAgent: userAgentInput.value.trim()
            });
        });

        // Cancel/Close Button Action
        cancelButton.addEventListener('click', (e) => {
            e.stopPropagation();
            popup.classList.remove('show');
            isPopupOpen = false;
        });
    }

    // --- Self-healing logic for the overlay ---
    function monitorSnapshotOverlay() {
        const observer = new MutationObserver(() => {
            if (!document.getElementById('snapshot-shadow-host')) {
                // Recreate if removed by the page's JS
                setTimeout(createSnapshotShadowHost, 100);
            }
        });
        observer.observe(document.body, {
            childList: true,
            subtree: false
        });
    }

    // ========== CLASSIC FULL SNAPSHOT (DEEP CRAWL) ==========

    async function runFullSnapshot(options) {
        let overlay = document.getElementById('snapshot-full-overlay');
        if (overlay) overlay.remove(); // Clean up previous
        
        overlay = document.createElement('div');
        overlay.id = 'snapshot-full-overlay';
        Object.assign(overlay.style, {
            position: 'fixed',
            left: 0, top: 0, width: '100vw', height: '100vh',
            zIndex: '2147483647',
            background: 'rgba(255,255,255,0.95)',
            color: '#123',
            fontFamily: 'Arial, sans-serif',
            fontSize: '16px',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            justifyContent: 'center',
            textAlign: 'center'
        });
        overlay.innerHTML = `
            <div id="snapshot-status" style="margin-bottom: 20px;">Initializing full website snapshot...</div>
            <div style="width:70%;max-width:500px;height:8px;background:#eee;border-radius:4px;overflow:hidden;">
                <div id="snapshot-bar" style="height:100%;background:#29a96e;width:0%;transition:width 0.22s;"></div>
            </div>
            <button id="snapshot-cancel" style="margin-top:25px;padding:8px 20px;font-size:14px; cursor:pointer;">Cancel</button>`;
        document.body.appendChild(overlay);

        const statusDiv = document.getElementById('snapshot-status');
        const barDiv = document.getElementById('snapshot-bar');
        const cancelBtn = document.getElementById('snapshot-cancel');

        let snapshotCancelled = false;
        cancelBtn.onclick = () => {
            snapshotCancelled = true;
            statusDiv.textContent = 'Snapshot cancelled by user.';
            setTimeout(() => { if (overlay) overlay.remove(); }, 2000);
        };
        
        try {
            const { files: crawlFiles, failed: crawlFailed } = await collectResources(options, (msg, pct) => {
                if (snapshotCancelled) throw new Error('Snapshot cancelled.');
                statusDiv.textContent = msg;
                if (pct !== undefined) barDiv.style.width = `${pct}%`;
            });

            if (snapshotCancelled) return;

            statusDiv.textContent = 'Generating ZIP archive...';
            barDiv.style.width = '95%';

            crawlFiles['snapshot-summary.json'] = new TextEncoder().encode(JSON.stringify({
                options,
                files: Object.keys(crawlFiles).length,
                failed: crawlFailed.length,
                failedList: crawlFailed,
                time: new Date().toISOString()
            }, null, 2));

            const zipOut = await zipWithProgress(crawlFiles, { level: 0 }, pct => {
                barDiv.style.width = `${95 + pct * 5}%`;
                statusDiv.textContent = `Zipping... ${Math.round(pct * 100)}%`;
            });
            
            let zipBlob = new Blob([zipOut], { type: 'application/zip' });
            let url = URL.createObjectURL(zipBlob);
            let a = document.createElement('a');
            a.href = url;
            a.download = `website_snapshot_${window.location.hostname}_${new Date().toISOString().replace(/:/g, '-')}.zip`;
            a.click();
            URL.revokeObjectURL(url);
            
            statusDiv.textContent = 'ZIP file ready. Download has started.';
            barDiv.style.width = '100%';
            setTimeout(() => { if (overlay) overlay.remove(); }, 3000);

        } catch (e) {
            if (e.message !== 'Snapshot cancelled.') {
                statusDiv.textContent = `An error occurred: ${e.message}`;
                console.error("Snapshot failed:", e);
            }
            setTimeout(() => { if (overlay) overlay.remove(); }, 3000);
        }
    }


    async function collectResources(options, updateBar) {
        const toVisit = [{ url: window.location.href, path: 'index.html', depth: 0 }];
        const files = {};
        const visited = new Set();
        const failed = [];
        let totalOps = 1, doneOps = 0;

        const absUrl = (url, base) => { try { return new URL(url, base).href; } catch(e) { return url; }};
        const updateProgress = () => {
            doneOps++;
            const pct = Math.min(10 + (doneOps / totalOps) * 85, 95);
            updateBar(`Crawling site (${doneOps}/${totalOps} items)...`, pct);
        };

        const regexes = {
             scripts: /<script[^>]+src=['"]([^'"]+)['"]/ig,
             stylesheets: /<link[^>]+href=['"]([^'"]+)['"][^>]*rel=['"]?stylesheet['"]?/ig,
             images: /<img[^>]+src=['"]([^'"]+)['"]/ig,
             media: /<(source|video|audio)[^>]+src=['"]([^'"]+)['"]/ig,
             posters: /<video[^>]+poster=['"]([^'"]+)['"]/ig,
             cssUrl: /url\(['"]?([^'")]+)['"]?\)/ig,
             iframes: /<iframe[^>]+src=['"]([^'"]+)['"]/ig,
             icons: /<link[^>]+rel=['"](?:[^'"]*icon[^'"]*)['"][^>]*href=['"]([^'"]+)['"]/ig,
             manifests: /<link[^>]+rel=['"]manifest['"][^>]*href=['"]([^'"]+)['"]/ig,
             links: /<a[^>]+href=['"]([^'"]+)['"]/ig,
             cssImports: /@import\s+url\(['"]?([^'"]+)['"]?\)/ig,
        };
        
        const processQueue = async (item) => {
            if (!item || item.depth > options.maxDepth || visited.has(item.url)) {
                 updateProgress();
                 return;
            }
            visited.add(item.url);

            try {
                const pageContent = await new Promise((resolve, reject) => {
                    GM_xmlhttpRequest({
                        method: 'GET', url: item.url, responseType: 'text', timeout: 15000,
                        headers: options.userAgent ? { 'User-Agent': options.userAgent } : {},
                        onload: r => resolve(r.responseText),
                        onerror: () => reject(new Error('Network error')),
                        ontimeout: () => reject(new Error('Timeout'))
                    });
                });
                
                if (item.path.endsWith('.html')) {
                    files[item.path] = new TextEncoder().encode(pageContent);
                }

                const resourcesFound = [];
                // Simple regex parsing for speed during crawl
                const find = (regex, type) => {
                    [...pageContent.matchAll(regex)].forEach(match => {
                         resourcesFound.push({ type, url: absUrl(match[1], item.url) });
                    });
                };
                
                find(regexes.scripts, 'script');
                find(regexes.stylesheets, 'css');
                find(regexes.images, 'img');
                find(regexes.media, 'media');
                if (options.includePosters) find(regexes.posters, 'poster');
                if (options.includeCssExtras) find(regexes.cssUrl, 'bgimg');
                find(regexes.iframes, 'iframe');
                if (options.includeIcons) {
                    find(regexes.icons, 'icon');
                    find(regexes.manifests, 'manifest');
                }

                for (const res of resourcesFound) {
                    if (visited.has(res.url) || !res.url.startsWith('http')) continue;
                    
                    let allowed = options.allowExternalDomains || 
                                 (options.stayOnDomain && sameDomain(res.url)) ||
                                 (options.stayOnSubdomain && sameSubdomain(res.url));
                    if (!allowed) continue;

                    totalOps++;
                    
                    if (res.type === 'iframe' || res.type === 'html') {
                         toVisit.push({ url: res.url, path: (res.url.split('//')[1] || res.url).replace(/[\/\\:*?"<>|]+/g, '_') + '.html', depth: item.depth + 1 });
                    } else {
                        // All other resources are added to a download queue
                        downloadQueue.push(res);
                    }
                }
                updateProgress();

            } catch(e) {
                failed.push({ url: item.url, reason: e.message });
                updateProgress();
            }
        };

        const downloadQueue = [];
        
        while(toVisit.length > 0) {
            await processQueue(toVisit.shift());
        }

        // Now download all collected resource URLs
        updateBar(`Downloading ${downloadQueue.length} resources...`, 90);
        await Promise.all(downloadQueue.map(async (res) => {
            if (visited.has(res.url)) return;
            visited.add(res.url);
            try {
                const resPath = (res.url.split('//')[1] || res.url).replace(/[\/\\:*?"<>|]+/g, '_');
                const data = await new Promise((resolve, reject) => {
                     GM_xmlhttpRequest({
                        method: 'GET', url: res.url, responseType: 'blob', timeout: 20000,
                        headers: options.userAgent ? { 'User-Agent': options.userAgent } : {},
                        onload: async r => {
                            if (options.skipLargerThan > 0 && r.response.size > options.skipLargerThan * 1024 * 1024) {
                                return reject(new Error('Skipped (big file)'));
                            }
                            resolve(await blobToUint8Array(r.response));
                        },
                        onerror: () => reject(new Error('Network error')),
                        ontimeout: () => reject(new Error('Timeout'))
                    });
                });
                files[resPath] = data;
            } catch(e) {
                failed.push({ url: res.url, reason: e.message });
            }
        }));

        return { files, failed };
    }


    // ====== SCRIPT INITIALIZATION ======
    if (document.readyState === 'complete' || document.readyState === 'interactive') {
        createSnapshotShadowHost();
        monitorSnapshotOverlay();
    } else {
        window.addEventListener('DOMContentLoaded', () => {
            createSnapshotShadowHost();
            monitorSnapshotOverlay();
        }, {
            once: true
        });
    }

})();
