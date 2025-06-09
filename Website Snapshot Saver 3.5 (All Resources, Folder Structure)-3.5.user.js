// ==UserScript==
// @name         Website Snapshot Saver 3.5 (All Resources, Folder Structure)
// @namespace    http://tampermonkey.net/
// @version      3.5
// @description  Sniff and download ALL resources (scripts, CSS, images, files, code, etc) with original folder layout. Toggle to skip >5MB files. Overlay is Shadow DOM, draggable, and self-healing.
// @author       Justin
// @match        *://*/*
// @grant        GM_xmlhttpRequest
// @connect      *
// @require      https://cdn.jsdelivr.net/npm/jszip@3.10.1/dist/jszip.min.js
// ==/UserScript==

(function() {
    'use strict';
    // JSZip loader (guaranteed)
    function ensureJSZip() {
        return new Promise((resolve, reject) => {
            if (typeof JSZip !== 'undefined') return resolve();
            const script = document.createElement('script');
            script.src = 'https://cdn.jsdelivr.net/npm/jszip@3.10.1/dist/jszip.min.js';
            script.onload = () => resolve();
            script.onerror = () => reject(new Error("Failed to load JSZip"));
            document.head.appendChild(script);
        });
    }
    // --- Self-heal: ensure overlay can't be killed by site scripts
    function keepOverlayAlive(hostId, createFn) {
        const obs = new MutationObserver(() => {
            if (!document.getElementById(hostId)) createFn();
        });
        obs.observe(document.body, { childList: true, subtree: false });
    }

    // --- Create Shadow DOM Overlay UI
    function createOverlay() {
        // Remove any existing
        const hostId = "snapshot-saver-shadow-host";
        let old = document.getElementById(hostId);
        if (old) old.remove();

        // Create Shadow host
        const host = document.createElement('div');
        host.id = hostId;
        Object.assign(host.style, {
            position: 'fixed', top: '30px', right: '30px',
            zIndex: 2147483647, width: '340px', background: 'none', pointerEvents: 'auto'
        });
        document.body.appendChild(host);
        const sd = host.attachShadow({ mode: 'open' });

        // Build overlay HTML
        sd.innerHTML = `
            <style>
                :host { all: initial; }
                .ss-container {
                    background: #fff; border: 2px solid #222; border-radius: 10px;
                    box-shadow: 0 4px 16px rgba(0,0,0,0.25); padding: 16px 20px 20px 20px;
                    min-width: 300px; min-height: 100px; font: 14px/1.5 Arial,sans-serif;
                    color: #111; cursor: grab; user-select: none; position: relative;
                }
                .ss-title { font-size: 18px; font-weight: bold; margin-bottom: 10px; }
                .ss-close { position: absolute; right: 12px; top: 10px; font-size: 22px; border: none; background: none; cursor: pointer; color: #888; }
                .ss-btn { margin: 12px 5px 0 0; padding: 4px 10px; font-size: 15px; cursor: pointer; }
                .ss-label { margin-bottom: 8px; display: flex; align-items: center; }
                .ss-checkbox { margin-right: 6px; }
                .ss-bar-wrap { height: 10px; background: #eee; border-radius: 5px; margin: 8px 0 0 0; }
                .ss-bar { height: 100%; width: 0; background: #3a9c60; border-radius: 5px; transition: width .2s; }
                .ss-status { margin: 6px 0 0 0; font-size: 14px; min-height: 18px; color: #2b2b2b; }
            </style>
            <div class="ss-container" id="dragRoot">
                <span class="ss-title">🌐 Snapshot Saver 3.5</span>
                <button class="ss-close" title="Close">&times;</button>
                <div class="ss-label">
                    <input type="checkbox" class="ss-checkbox" id="skipBig" checked>
                    <label for="skipBig" style="user-select:none;">Skip files &gt;5MB</label>
                </div>
                <button class="ss-btn" id="snapAll">Download EVERYTHING</button>
                <div class="ss-status" id="status">Ready</div>
                <div class="ss-bar-wrap"><div class="ss-bar" id="bar"></div></div>
            </div>
        `;

        // Drag support (Hermes style)
        const root = sd.getElementById('dragRoot');
        let drag = false, offsetX = 0, offsetY = 0;
        root.addEventListener('mousedown', function(e) {
            if (e.target.classList.contains('ss-close')) return;
            drag = true;
            root.style.cursor = 'grabbing';
            offsetX = e.clientX - host.offsetLeft;
            offsetY = e.clientY - host.offsetTop;
            e.preventDefault();
        });
        document.addEventListener('mousemove', function(e) {
            if (drag) {
                host.style.left = (e.clientX - offsetX) + 'px';
                host.style.top = (e.clientY - offsetY) + 'px';
            }
        });
        document.addEventListener('mouseup', function() {
            drag = false;
            root.style.cursor = 'grab';
        });

        // Close button
        sd.querySelector('.ss-close').onclick = () => host.remove();

        // Main download button
        sd.getElementById('snapAll').onclick = async function() {
            let skipBig = sd.getElementById('skipBig').checked;
            await runSuperSnapshot({ skipBig, setStatus: (msg, pct) => {
                sd.getElementById('status').textContent = msg;
                sd.getElementById('bar').style.width = (pct||0) + "%";
            }});
        };
    }

    // --- Super Resource Sniffer with Structure ---
    async function runSuperSnapshot(opts) {
        let { skipBig, setStatus } = opts;
        setStatus("Scanning all resources...", 2);

        // Helper: Convert relative/absolute URL to path (preserves folder structure)
        function urlToPath(url) {
            try {
                let u = new URL(url, location.href);
                let path = u.pathname.replace(/^\/+/, "");
                if (!path || path.endsWith('/')) path += "index.html";
                return u.hostname + "/" + path;
            } catch {
                // fallback: sanitize to flat name
                return "file/" + url.replace(/[\/:?&=]+/g, "_");
            }
        }

        // Step 1: Sniff all downloadable resources
        let seen = new Set();
        let allResources = [];
        // Elements
        [
            ['script','src'], ['link','href'], ['img','src'], ['img','srcset'],
            ['source','src'], ['audio','src'], ['video','src'], ['embed','src'],
            ['object','data'], ['iframe','src'], ['a','href']
        ].forEach(([sel, attr]) => {
            document.querySelectorAll(`${sel}[${attr}]`).forEach(el => {
                let val = el.getAttribute(attr);
                if (!val) return;
                if (attr === 'srcset') {
                    val.split(',').forEach(piece => {
                        let v = piece.trim().split(' ')[0];
                        if (v && !seen.has(v)) {
                            allResources.push(v); seen.add(v);
                        }
                    });
                } else {
                    if (!seen.has(val)) {
                        allResources.push(val); seen.add(val);
                    }
                }
            });
        });
        // Inline CSS: sniff url(...)
        document.querySelectorAll('[style]').forEach(el => {
            let bg = el.style.backgroundImage;
            if (bg) {
                let match;
                let re = /url\(["']?([^"')]+)["']?\)/g;
                while ((match = re.exec(bg))) {
                    let u = match[1];
                    if (u && !seen.has(u)) {
                        allResources.push(u); seen.add(u);
                    }
                }
            }
        });
        // Stylesheets: scan for url(...) and @import
        for (let sheet of document.styleSheets) {
            let rules;
            try { rules = sheet.cssRules || sheet.rules; } catch { continue; }
            if (!rules) continue;
            for (let r of rules) {
                if (r.cssText && /url\(/.test(r.cssText)) {
                    [...r.cssText.matchAll(/url\(["']?([^"')]+)["']?\)/g)].forEach(m => {
                        let u = m[1];
                        if (u && !seen.has(u)) {
                            allResources.push(u); seen.add(u);
                        }
                    });
                }
                // CSS @import
                if (r.href && !seen.has(r.href)) {
                    allResources.push(r.href); seen.add(r.href);
                }
            }
        }
        // Always include page itself
        allResources.unshift(location.href);

        // Step 2: Filter to external http(s), unique, no javascript/data
        let filtered = [];
        allResources.forEach(u => {
            try {
                let urlObj = new URL(u, location.href);
                if (/^https?:\/\//i.test(urlObj.href) && !filtered.includes(urlObj.href)) {
                    filtered.push(urlObj.href);
                }
            } catch {}
        });

        setStatus(`Found ${filtered.length} resources. Getting file sizes...`, 8);

        // Step 3: HEAD all resources for size/mime
        let resourceList = [];
        let skipCount = 0;
        for (let i = 0; i < filtered.length; i++) {
            let url = filtered[i];
            setStatus(`Checking ${i+1}/${filtered.length}...`, 8 + i/filtered.length*12);
            try {
                let size = null, mime = '';
                await new Promise(res => {
                    GM_xmlhttpRequest({
                        method: 'HEAD',
                        url,
                        timeout: 8000,
                        onload: function(resp) {
                            let h = resp.responseHeaders;
                            let m = h.match(/Content-Type: ([^;\r\n]+)/i);
                            mime = m ? m[1] : '';
                            let s = h.match(/Content-Length: (\d+)/i);
                            size = s ? +s[1] : null;
                            res();
                        },
                        onerror: res,
                        ontimeout: res
                    });
                });
                if (skipBig && size && size > 5 * 1024 * 1024) {
                    skipCount++;
                    continue; // Skip big file
                }
                resourceList.push({ url, size, mime, path: urlToPath(url) });
            } catch {
                resourceList.push({ url, size: null, mime: '', path: urlToPath(url), error: 'HEAD fail' });
            }
        }
        setStatus(`Downloading ${resourceList.length} files...`, 20);

        // Step 4: Download all resources as blob
        const zip = new JSZip();
        let failed = [], completed = 0;
        for (let i = 0; i < resourceList.length; i++) {
            let { url, path } = resourceList[i];
            setStatus(`Downloading: ${url.split('/').pop()} (${i+1}/${resourceList.length})`, 20 + i/resourceList.length*65);
            try {
                let blob = await new Promise((res, rej) => {
                    GM_xmlhttpRequest({
                        method: 'GET',
                        url,
                        responseType: 'blob',
                        timeout: 25000,
                        onload: r => res(r.response),
                        onerror: rej,
                        ontimeout: rej
                    });
                });
                zip.file(path, blob);
                completed++;
            } catch (e) {
                failed.push(url);
            }
        }

        // Step 5: Add summary
        zip.file('snapshot-summary.json', JSON.stringify({
            files: resourceList.map(r=>r.path),
            skipped: skipCount,
            failed: failed,
            total: resourceList.length,
            time: (new Date()).toISOString()
        }, null, 2));

        setStatus('Creating ZIP...', 98);

        // Step 6: ZIP + Download
        let blob = await zip.generateAsync(
            { type: 'blob', compression: 'STORE' },
            meta => setStatus(`Zipping...${Math.round(meta.percent||0)}%`, 98 + (meta.percent||0)*.02)
        );
        let url = URL.createObjectURL(blob);
        let a = document.createElement('a');
        a.href = url;
        a.download = `snapshot-${location.hostname}-${Date.now()}.zip`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        setTimeout(() => URL.revokeObjectURL(url), 3500);

        setStatus('Download complete!', 100);
    }

    // --- Bootstrap: ensure overlay stays alive
    createOverlay();
    keepOverlayAlive("snapshot-saver-shadow-host", createOverlay);

    // (Optional) menu command
    if (window.top === window.self && typeof GM_registerMenuCommand === 'function') {
        GM_registerMenuCommand('Show Snapshot Saver Overlay', createOverlay);
    }

})();
