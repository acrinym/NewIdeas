// ==UserScript==
// @name         Website Snapshot Saver 3.6 (Full Features, fflate, ShadowDOM)
// @namespace    http://tampermonkey.net/
// @version      3.6
// @description  Download all resources with full resource picker, domain settings, classic crawl, >5MB filter, self-healing draggable Shadow DOM overlay, and ultra-fast zipping via fflate!
// @author       Justin
// @match        *://*/*
// @grant        GM_xmlhttpRequest
// @connect      *
// @require      https://cdn.jsdelivr.net/npm/fflate@0.8.1/umd/index.js
// ==/UserScript==

(function() {
    'use strict';
    const fflate = window.fflate;
    // ---- UTILS

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

    // Zip helper using fflate streams with progress callback
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
                    for (const c of chunks) { out.set(c, off); off += c.length; }
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

    // ---- DOMAIN UTILS (from 3.0)

    function getDomainRoot(url) {
        try {
            const u = new URL(url);
            const host = u.hostname;
            const parts = host.split('.');
            if (parts.length <= 2) return host;
            const tld2 = parts.slice(-2).join('.');
            const tld3 = parts.slice(-3).join('.');
            const tldish = /(\.co\.|\.ac\.)[a-z]{2,3}$/i.test(tld3) ? tld3 : tld2;
            return tldish;
        } catch(e) { return window.location.hostname; }
    }
    function sameDomain(url) {
        try {
            const root = getDomainRoot(window.location.href);
            return getDomainRoot(url) === root;
        } catch(e) { return false; }
    }
    function sameSubdomain(url) {
        try {
            const here = new URL(window.location.href);
            const test = new URL(url, here);
            return test.hostname === here.hostname;
        } catch(e) { return false; }
    }

    // ---- SMART RESOURCE SNIFFER (from 3.0, improved)

    async function smartResourceSniffer(options) {
        options = Object.assign({ includeIcons: true, includePosters: true, includeCssExtras: true }, options);
        const resList = [];
        const seen = new Set();

        function addRes(url, type, foundIn, attr, suggestedName, isBlob) {
            if (!url || seen.has(url) || url.startsWith('javascript:') || url.startsWith('data:')) return;
            if (options.stayOnSubdomain && !sameSubdomain(url)) return;
            if (!options.stayOnSubdomain && options.stayOnDomain && !sameDomain(url)) return;
            seen.add(url);
            resList.push({
                url,
                type,
                foundIn,
                attr,
                suggestedName: suggestedName || url.split('/').pop().split('?')[0] || 'file',
                isBlob: !!isBlob
            });
        }
        [
            {sel:'script[src]',type:'js',attr:'src'},
            {sel:'link[href]',type:'css',attr:'href'},
            {sel:'img[src]',type:'img',attr:'src'},
            {sel:'img[srcset]',type:'imgset',attr:'srcset'},
            {sel:'video[poster]',type:'poster',attr:'poster'},
            {sel:'source[src]',type:'media',attr:'src'},
            {sel:'source[srcset]',type:'mediaset',attr:'srcset'},
            {sel:'audio[src]',type:'audio',attr:'src'},
            {sel:'video[src]',type:'video',attr:'src'},
            {sel:'embed[src]',type:'embed',attr:'src'},
            {sel:'object[data]',type:'object',attr:'data'},
            {sel:'iframe[src]',type:'iframe',attr:'src'},
            {sel:'link[rel~="icon"][href]',type:'icon',attr:'href'},
            {sel:'link[rel="manifest"][href]',type:'manifest',attr:'href'},
            {sel:'a[href]',type:'link',attr:'href'}
        ].forEach(({sel,type,attr}) => {
            document.querySelectorAll(sel).forEach(el => {
                let raw = el.getAttribute(attr);
                if (raw) {
                    if (attr === 'srcset') {
                        raw.split(',').map(e => e.trim().split(' ')[0]).forEach(r => addRes(r,type,sel,attr,undefined));
                    } else {
                        if (sel==='a[href]') {
                            const isFile = /\.(zip|rar|7z|exe|mp3|mp4|wav|avi|mov|pdf|docx?|xlsx?|pptx?|png|jpe?g|gif|svg|webp|csv|json|xml|txt|tar|gz)$/i.test(raw);
                            const isStream = /^rtsp:|^rtmp:|^mms:|^ftp:/i.test(raw);
                            if (!isFile && !isStream) return;
                        }
                        if ((type==='icon' || type==='manifest') && !options.includeIcons) return;
                        if (type==='poster' && !options.includePosters) return;
                        if ((type==='css-import' || type==='bgimg' || type==='css-embed') && !options.includeCssExtras) return;
                        addRes(raw, type, sel, attr, undefined);
                    }
                }
            });
        });

        // Stylesheets
        for (const sheet of document.styleSheets) {
            let rules;
            try { rules = sheet.cssRules || sheet.rules; } catch(e) { continue; }
            if (!rules) continue;
            for (const rule of rules) {
                if (!rule) continue;
                if (options.includeCssExtras && rule.cssText && /@import/i.test(rule.cssText)) {
                    let imp = /@import\s+url\(['"]?([^'")]+)['"]?\)/i.exec(rule.cssText);
                    if (imp) addRes(imp[1],'css-import','@import','css-url',undefined);
                }
                if (rule.cssText && /@font-face/i.test(rule.cssText)) {
                    let m = /url\(['"]?([^'")]+)['"]?\)/i.exec(rule.cssText);
                    if (m) addRes(m[1],'font','@font-face','css-url',undefined);
                }
                if (options.includeCssExtras && rule.style && rule.style.backgroundImage) {
                    let matches = [...rule.style.backgroundImage.matchAll(/url\(['"]?([^'")]+)['"]?\)/ig)];
                    matches.forEach(match => addRes(match[1],'bgimg','css-bg','css-url',undefined));
                }
            }
        }
        if (options.includeCssExtras) {
            document.querySelectorAll('[style]').forEach(el => {
                let bg = el.style.backgroundImage;
                if (bg) {
                    let matches = [...bg.matchAll(/url\(['"]?([^'")]+)['"]?\)/ig)];
                    matches.forEach(match => addRes(match[1],'bgimg','[style]','css-url',undefined));
                }
            });
            document.querySelectorAll('style').forEach(el => {
                let text = el.textContent || '';
                let matches = [...text.matchAll(/@import\s+url\(['"]?([^'")]+)['"]?\)/ig)];
                matches.forEach(m => addRes(m[1],'css-import','<style>','css-url',undefined));
            });
        }

        // Shadow DOM
        function sniffShadow(node) {
            if (node.shadowRoot) {
                node.shadowRoot.querySelectorAll('*').forEach(child => sniffShadow(child));
                node.shadowRoot.querySelectorAll('[src],[href]').forEach(child => {
                    ['src','href'].forEach(attr => {
                        let raw = child.getAttribute(attr);
                        if (raw) addRes(raw, 'shadow', 'shadowRoot', attr, undefined);
                    });
                });
            }
        }
        document.querySelectorAll('*').forEach(sniffShadow);

        document.querySelectorAll('[src^="blob:"],[href^="blob:"]').forEach(el => {
            let src = el.getAttribute('src') || el.getAttribute('href');
            if (src) addRes(src, 'blob', el.tagName, 'src/href', undefined, true);
        });

        // Canonicalize URLs
        const absList = [];
        for (const entry of resList) {
            try { entry.url = new URL(entry.url, window.location.href).href; } catch(e) {}
            absList.push(entry);
        }
        return absList;
    }

    // ---- SHADOW DOM OVERLAY & UI (from 3.0, modernized)

    function createSnapshotShadowHost() {
        // Remove previous, if present
        const prev = document.getElementById('snapshot-shadow-host');
        if (prev) prev.remove();

        // Shadow host
        const host = document.createElement('div');
        host.id = 'snapshot-shadow-host';
        Object.assign(host.style, {
            position: 'fixed',
            top: '20px',
            right: '20px',
            zIndex: '2147483647',
            width: 'auto',
            height: 'auto',
            pointerEvents: 'auto'
        });
        document.body.appendChild(host);

        const shadow = host.attachShadow({mode: 'open'});

        // Styles
        const style = document.createElement('style');
        style.textContent = `
        :host {
            all: initial;
            display: block !important;
            position: fixed !important;
            top: 20px !important;
            right: 20px !important;
            z-index: 2147483647 !important;
        }
        .icon-container {
            background: rgba(255,255,255,0.97);
            border-radius: 50%;
            padding: 6px;
            box-shadow: 0 2px 7px rgba(0,0,0,0.24);
            pointer-events: auto;
            display: inline-block;
            cursor: grab;
        }
        .icon-container:active {
            cursor: grabbing;
            box-shadow: 0 6px 16px rgba(0,0,0,0.40);
        }
        .popup {
            display: none;
            background: #fff;
            border: 1px solid #aaa;
            border-radius: 7px;
            padding: 14px;
            box-shadow: 0 8px 28px rgba(0,0,0,0.28);
            position: absolute;
            top: 48px;
            left: 50%;
            transform: translateX(-50%);
            z-index: 2147483647;
            width: 440px;
            min-height: 210px;
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
            margin: 6px 0 3px 0;
            cursor: pointer;
        }
        .popup button {
            margin: 10px 5px 0 0;
            padding: 6px 12px;
            cursor: pointer;
        }
        .sniff-summary {
            background: #f9f9fb;
            border: 1px solid #dedee6;
            max-height: 160px;
            overflow-y: auto;
            margin: 8px 0;
            padding: 7px;
            font-size: 12px;
        }
        .sniff-summary ul {
            list-style: none;
            padding-left: 0;
            margin: 0;
        }
        .sniff-summary li {
            margin-bottom: 4px;
            word-break: break-all;
        }
        .progress {
            margin-top: 12px;
            font-size: 12px;
            color: #234;
        }
        .progress-bar {
            width: 100%;
            background: #eee;
            height: 6px;
            border-radius: 3px;
            overflow: hidden;
        }
        .progress-bar-fill {
            height: 100%;
            background: #29a96e;
            width: 0;
            transition: width 0.25s;
        }
        .domain-btns {
            margin: 7px 0;
        }
    `;
    shadow.appendChild(style);

    // Floating eye
    const iconContainer = document.createElement('div');
    iconContainer.className = 'icon-container';
    iconContainer.title = 'Show Website Snapshot Saver options';
    iconContainer.innerHTML = `
        <svg width="32" height="32" viewBox="0 0 24 24" fill="none"><path d="M12 4.5C7 4.5 2.73 7.61 1 12c1.73 4.39 6 7.5 11 7.5s9.27-3.11 11-7.5c-1.73-4.39-6-7.5-11-7.5zM12 17c-2.76 0-5-2.24-5-5s2.24-5 5-5 5 2.24 5 5-2.24 5-5 5zm0-8c-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3-1.34-3-3-3z" fill="#000"/></svg>
    `;
    shadow.appendChild(iconContainer);

    // Popup panel
    const popup = document.createElement('div');
    popup.className = 'popup';
    popup.innerHTML = `
        <div class="domain-btns">
            <label><input type="checkbox" id="stayOnSubdomain" checked> Stay on this subdomain only</label>
            <label><input type="checkbox" id="stayOnDomain"> Allow all of this domain</label>
            <label><input type="checkbox" id="allowExternalDomains"> Traverse other domains</label>
            <label><input type="checkbox" id="iconsManifests" checked> Icons & manifests</label>
            <label><input type="checkbox" id="videoPosters" checked> Video posters</label>
            <label><input type="checkbox" id="cssExtras" checked> CSS imports/background images</label>
            <label>Skip files larger than <input type="number" id="skipSize" value="5" style="width:60px;"> MB</label>
            <label>Maximum crawl depth <input type="number" id="maxDepth" min="0" style="width:50px;" placeholder="∞"></label>
        </div>
        <button id="sniffBtn" style="margin-bottom:6px;">Sniff Downloadable Resources</button>
        <button id="classicBtn" style="margin-bottom:6px;">Full Website Snapshot (Classic)</button>
        <div class="sniff-summary" id="sniffSummary">No resources sniffed yet.</div>
        <button id="saveButton" disabled>Save Selected as ZIP</button>
        <button id="cancelButton">Cancel</button>
        <div class="progress" id="progress">Ready</div>
        <div class="progress-bar"><div class="progress-bar-fill" id="progressBar"></div></div>
    `;
    shadow.appendChild(popup);

    // ===== Drag Logic =====
    let isDragging = false, offsetX, offsetY, justDragged = false;
    iconContainer.addEventListener('mousedown', (e) => {
        e.preventDefault(); e.stopPropagation();
        offsetX = e.clientX - host.offsetLeft;
        offsetY = e.clientY - host.offsetTop;
        isDragging = true; justDragged = false;
        document.body.style.userSelect = 'none';
    });
    document.addEventListener('mousemove', (e) => {
        if (isDragging) {
            justDragged = true;
            let newX = e.clientX - offsetX, newY = e.clientY - offsetY;
            const iconRect = host.getBoundingClientRect();
            const maxX = window.innerWidth - iconRect.width, maxY = window.innerHeight - iconRect.height;
            newX = Math.max(0, Math.min(newX, maxX)); newY = Math.max(0, Math.min(newY, maxY));
            host.style.left = `${newX}px`;
            host.style.top = `${newY}px`;
        }
    });
    document.addEventListener('mouseup', (e) => {
        if (isDragging) {
            isDragging = false;
            document.body.style.userSelect = 'auto';
            setTimeout(() => { justDragged = false; }, 0);
        }
    });

    // ===== Popup toggle =====
    let isPopupOpen = false;
    iconContainer.addEventListener('click', (e) => {
        e.stopPropagation();
        if (justDragged) return;
        isPopupOpen = !isPopupOpen;
        popup.classList.toggle('show', isPopupOpen);
    });

    // ===== BUTTONS & STATE =====
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

    // Domain toggle logic (mutual exclusion)
    function updateDomainToggles() {
        if (allowExternalDomains.checked) {
            stayOnDomain.checked = false;
            stayOnSubdomain.checked = false;
            stayOnDomain.disabled = true;
            stayOnSubdomain.disabled = true;
        } else if (stayOnDomain.checked) {
            stayOnSubdomain.checked = false;
            stayOnSubdomain.disabled = true;
            stayOnDomain.disabled = false;
        } else {
            stayOnSubdomain.checked = true;
            stayOnSubdomain.disabled = false;
            stayOnDomain.disabled = false;
        }
    }
    allowExternalDomains.addEventListener('change', updateDomainToggles);
    stayOnDomain.addEventListener('change', updateDomainToggles);
    stayOnSubdomain.addEventListener('change', updateDomainToggles);

    // State for smart sniffer
    let sniffedResources = [];
    let lastSnifferOpts = null;

    // ===== SNIFF BUTTON: Pre-scan resources and list =====
    sniffBtn.addEventListener('click', async () => {
        saveButton.disabled = true;
        sniffSummary.textContent = 'Scanning...';
        progressDiv.textContent = 'Sniffing page resources...';
        progressBar.style.width = '10%';
        await new Promise(r => setTimeout(r, 200));
        const opts = {
            stayOnSubdomain: stayOnSubdomain.checked,
            stayOnDomain: stayOnDomain.checked && !stayOnSubdomain.checked,
            allowExternalDomains: allowExternalDomains.checked,
            includeIcons: iconsManifests.checked,
            includePosters: videoPosters.checked,
            includeCssExtras: cssExtras.checked
        };
        lastSnifferOpts = opts;
        sniffedResources = await smartResourceSniffer(opts);
        progressBar.style.width = '40%';
        // Try to get HEAD info for size/type
        for (let i=0; i<sniffedResources.length; ++i) {
            const r = sniffedResources[i];
            await new Promise(res => {
                GM_xmlhttpRequest({
                    method: 'HEAD',
                    url: r.url,
                    timeout: 5000,
                    onload: function(resp) {
                        r.size = resp.responseHeaders.match(/Content-Length: ?(\d+)/i) ? parseInt(RegExp.$1,10) : null;
                        r.mime = resp.responseHeaders.match(/Content-Type: ?([\w\/\-\.\+]+)(;|$)/i) ? RegExp.$1 : null;
                        res();
                    },
                    onerror: res,
                    ontimeout: res
                });
            });
            progressBar.style.width = `${40 + (i/sniffedResources.length)*30}%`;
        }
        progressBar.style.width = '70%';
        // List
        if (sniffedResources.length === 0) {
            sniffSummary.innerHTML = '<em>No downloadable resources detected.</em>';
        } else {
            sniffSummary.innerHTML = `<strong>${sniffedResources.length} resources found:</strong><ul>` +
                sniffedResources.map(r =>
                                     `<li>
<input type="checkbox" class="reschk" checked data-url="${r.url}">
<b>${r.suggestedName}</b>
<span style="color:#888">[${r.type || "?"}]</span>
<small>${r.mime || ""} ${r.size ? `(${(r.size/1024).toFixed(1)}KB)` : ""}</small>
</li>`
                                    ).join('') + '</ul>';

        }
        progressDiv.textContent = 'Ready. Review and select resources to save.';
        progressBar.style.width = '100%';
        saveButton.disabled = sniffedResources.length === 0;
    });

    // ===== SAVE BUTTON: Download and zip all checked resources =====
    saveButton.addEventListener('click', async () => {
        const checkedBoxes = Array.from(shadow.querySelectorAll('.reschk')).filter(cb => cb.checked);
        const resourcesToSave = sniffedResources.filter(r => checkedBoxes.find(cb => cb.getAttribute('data-url') === r.url));
        progressDiv.textContent = 'Downloading selected resources...';
        progressBar.style.width = '0%';
        const files = {};
        const summary = [];
        const skipLimit = parseFloat(skipSizeInput.value) || 0;
        for (let i=0; i<resourcesToSave.length; ++i) {
            const r = resourcesToSave[i];
            // Skip big files
            if (skipLimit > 0 && r.size && r.size > skipLimit * 1024 * 1024) {
                summary.push({
                    url: r.url,
                    name: r.suggestedName,
                    type: r.type,
                    size: r.size,
                    mime: r.mime,
                    skipped: `File >${skipLimit}MB`
                });
                continue;
            }
            progressDiv.textContent = `Downloading: ${r.suggestedName} (${i+1}/${resourcesToSave.length})`;
            progressBar.style.width = `${(i/resourcesToSave.length)*80}%`;
            await new Promise(res => {
                GM_xmlhttpRequest({
                    method: 'GET',
                    url: r.url,
                    responseType: 'blob',
                    timeout: 20000,
                    onload: async function(resp) {
                        let uint8 = await blobToUint8Array(resp.response);
                        files[r.suggestedName] = uint8;
                        summary.push({
                            url: r.url,
                            name: r.suggestedName,
                            type: r.type,
                            size: r.size || (resp.response ? resp.response.size : null),
                            mime: r.mime || resp.response.type
                        });
                        res();
                    },
                    onerror: function() {
                        summary.push({
                            url: r.url, name: r.suggestedName, type: r.type, error: 'Failed to download'
                        });
                        res();
                    },
                    ontimeout: function() {
                        summary.push({
                            url: r.url, name: r.suggestedName, type: r.type, error: 'Timeout'
                        });
                        res();
                    }
                });
            });
        }
        files['sniffed-summary.json'] = new TextEncoder().encode(JSON.stringify(summary,null,2));
        progressBar.style.width = '90%';
        progressDiv.textContent = 'Generating zip...';
        const zipData = await zipWithProgress(files, { level: 0 }, pct => {
            progressBar.style.width = `${90 + pct * 10}%`;
            progressDiv.textContent = `Zipping: ${Math.round(pct * 100)}%`;
        }).catch(err => {
            progressDiv.textContent = 'ZIP failed: ' + err;
            alert('ZIP failed: ' + err);
            return null;
        });
        if (!zipData) return;
        let zipBlob = new Blob([zipData], { type: 'application/zip' });
        let url = URL.createObjectURL(zipBlob);
        let a = document.createElement('a');
        a.href = url;
        a.download = `website-sniffed-resources-${new Date().toISOString().replace(/:/g, '-')}.zip`;
        document.body.appendChild(a); a.click(); document.body.removeChild(a);
        setTimeout(()=>URL.revokeObjectURL(url),3500);
        progressDiv.textContent = 'ZIP saved!'; progressBar.style.width = '100%';
    });

    // ===== CLASSIC BUTTON: Run full website snapshot (deep crawl) =====
    classicBtn.addEventListener('click', async () => {
        popup.classList.remove('show');
        isPopupOpen = false;
        await runFullSnapshot({
            stayOnSubdomain: stayOnSubdomain.checked,
            stayOnDomain: stayOnDomain.checked && !stayOnSubdomain.checked,
            allowExternalDomains: allowExternalDomains.checked,
            includeIcons: iconsManifests.checked,
            includePosters: videoPosters.checked,
            includeCssExtras: cssExtras.checked,
            skipLargerThan: parseFloat(skipSizeInput.value) || 0,
            maxDepth: maxDepthInput.value ? parseInt(maxDepthInput.value, 10) : null
        });
    });

    // ===== CANCEL: Hide popup =====
    cancelButton.addEventListener('click', (e) => {
        e.stopPropagation();
        popup.classList.remove('show');
        isPopupOpen = false;
        progressDiv.textContent = 'Ready';
        progressBar.style.width = '0%';
    });
}

    // --- Self-heal overlay
    function monitorSnapshotOverlay() {
        const observer = new MutationObserver(() => {
            if (!document.getElementById('snapshot-shadow-host')) {
                setTimeout(createSnapshotShadowHost, 100);
            }
        });
        observer.observe(document.body, { childList: true, subtree: false });
    }

    // ========== CLASSIC FULL SNAPSHOT (Deep Website Crawl) ==========

    async function runFullSnapshot(domainOpts) {
        // UI overlay
        let overlay = document.getElementById('snapshot-full-overlay');
        if (!overlay) {
            overlay = document.createElement('div');
            overlay.id = 'snapshot-full-overlay';
            Object.assign(overlay.style, {
                position: 'fixed',
                left: 0, top: 0, width: '100vw', height: '100vh',
                zIndex: '2147483647',
                background: 'rgba(255,255,255,0.90)',
                color: '#123',
                fontFamily: 'Arial, sans-serif',
                fontSize: '16px',
                padding: '38px 0 0 0',
                textAlign: 'center'
            });
            overlay.innerHTML = `<div id="snapshot-status">Initializing full website snapshot...</div>
            <div style="width:70%;margin:25px auto 8px auto;height:8px;background:#eee;border-radius:4px;overflow:hidden;">
            <div id="snapshot-bar" style="height:100%;background:#29a96e;width:0%;transition:width 0.22s;"></div>
            </div>
            <button id="snapshot-cancel" style="margin-top:25px;padding:6px 18px;font-size:14px;">Cancel</button>`;
        document.body.appendChild(overlay);
        document.getElementById('snapshot-cancel').onclick = () => {
            overlay.remove();
            snapshotCancelled = true;
        };
    }
    let statusDiv = document.getElementById('snapshot-status');
    let barDiv = document.getElementById('snapshot-bar');

    // State
    let snapshotCancelled = false;
    let files = {};
    let failed = [];
    let visited = new Set();
    let totalOps = 1, doneOps = 0;

    function markDone() {
        doneOps++;
        let pct = Math.min(10 + (doneOps / totalOps) * 80, 89);
        statusDiv.textContent = `Crawling site (${doneOps}/${totalOps})...`;
        barDiv.style.width = `${pct}%`;
    }

    // Crawl logic
    async function collectResources(options, updateBar) {
        options = Object.assign({ includeIcons: true, includePosters: true, includeCssExtras: true, skipLargerThan: 0, maxDepth: null }, options);
        const toVisit = [{url: window.location.href, path: 'index.html', depth: 0}];
        const files = {};
        const visited = new Set();
        const failed = [];
        let totalOps = 1, doneOps = 0;

        function absUrl(url, base) {
            try { return new URL(url, base).href; } catch(e) { return url; }
        }
        function markDone() {
            doneOps++;
            if (typeof updateBar === 'function') {
                let pct = Math.min(10 + (doneOps / totalOps) * 80, 89);
                updateBar(`Crawling site (${doneOps}/${totalOps})...`, pct);
            }
        }

        // Main crawl loop
        while (toVisit.length > 0) {
            if (typeof updateBar === 'function') markDone();
            if (snapshotCancelled) break;
            const next = toVisit.shift();
            if (visited.has(next.url)) continue;
            visited.add(next.url);
            try {
                const html = await new Promise((resolve, reject) => {
                    GM_xmlhttpRequest({
                        method: 'GET',
                        url: next.url,
                        responseType: 'text',
                        timeout: 15000,
                        onload: r => resolve(r.response),
                        onerror: () => reject(new Error('Network error')),
                        ontimeout: () => reject(new Error('Timeout'))
                    });
                });
                files[next.path] = new TextEncoder().encode(html);

                // Find resources
                const resList = [];
                html.replace(/<script[^>]+src=['"]([^'"]+)['"]/ig, (_, src) => {
                    resList.push({type: 'script', url: absUrl(src, next.url)});
                    return '';
                });
                html.replace(/<link[^>]+href=['"]([^'"]+)['"][^>]*rel=['"]?stylesheet['"]?/ig, (_, href) => {
                    resList.push({type: 'css', url: absUrl(href, next.url)});
                    return '';
                });
                html.replace(/<img[^>]+src=['"]([^'"]+)['"]/ig, (_, src) => {
                    resList.push({type: 'img', url: absUrl(src, next.url)});
                    return '';
                });
                html.replace(/<(source|video|audio)[^>]+src=['"]([^'"]+)['"]/ig, (_, tag, src) => {
                    resList.push({type: tag, url: absUrl(src, next.url)});
                    return '';
                });
                html.replace(/<video[^>]+poster=['"]([^'"]+)['"]/ig, (_, poster) => {
                    if (options.includePosters) {
                        resList.push({type:'poster', url: absUrl(poster, next.url)});
                    }
                    return '';
                });
                html.replace(/url\(['"]?([^'")]+)['"]?\)/ig, (_, url) => {
                    if (options.includeCssExtras) {
                        resList.push({type: 'bgimg', url: absUrl(url, next.url)});
                    }
                    return '';
                });
                html.replace(/<iframe[^>]+src=['"]([^'"]+)['"]/ig, (_, src) => {
                    resList.push({type: 'iframe', url: absUrl(src, next.url)});
                    return '';
                });
                html.replace(/<link[^>]+rel=['"](?:[^'"]*icon[^'"]*)['"][^>]*href=['"]([^'"]+)['"]/ig,
                    (_, href) => {
                        if (options.includeIcons) {
                            resList.push({type: 'icon', url: absUrl(href, next.url)});
                        }
                        return '';
                    });
                html.replace(/<link[^>]+rel=['"]manifest['"][^>]*href=['"]([^'"]+)['"]/ig, (_, href) => {
                    if (options.includeIcons) {
                        resList.push({type:'manifest', url: absUrl(href, next.url)});
                    }
                    return '';
                });
                html.replace(/<a[^>]+href=['"]([^'"]+)['"]/ig, (_, href) => {
                    if (/^rtsp:|^rtmp:|^mms:|^ftp:/i.test(href) || /\.(zip|rar|7z|exe|mp3|mp4|wav|avi|mov|pdf|docx?|xlsx?|pptx?|png|jpe?g|gif|svg|webp|csv|json|xml|txt|tar|gz)$/i.test(href)) {
                        resList.push({type: 'file', url: absUrl(href, next.url)});
                    }
                    return '';
                });
                html.replace(/<style[^>]*>([\s\S]*?)<\/style>/ig, (_, css) => {
                    if (options.includeCssExtras) {
                    [...css.matchAll(/@import\s+url\(['"]?([^'"]+)['"]?\)/ig)].forEach(m => {
                        resList.push({type:'css-import', url: absUrl(m[1], next.url)});
                    });
                    [...css.matchAll(/url\(['"]?([^'")]+)['"]?\)/ig)].forEach(m => {
                        resList.push({type:'css-embed', url: absUrl(m[1], next.url)});
                    });
                    }
                    return '';
                });

                for (const r of resList) {
                    if (!visited.has(r.url) && r.url.startsWith('http')) {
                        totalOps++;
                        let allowed = false;
                        if (options.allowExternalDomains) allowed = true;
                        else if (options.stayOnDomain && sameDomain(r.url)) allowed = true;
                        else if (options.stayOnSubdomain && sameSubdomain(r.url)) allowed = true;
                        if (!allowed) continue;
                        if (r.type === 'iframe' || r.type === 'html') {
                            const iframePath = (r.url.split('//')[1] || r.url).replace(/[\\/:*?"<>|]+/g, '_') + '.html';
                            if (options.maxDepth == null || next.depth < options.maxDepth) {
                                toVisit.push({url: r.url, path: iframePath, depth: next.depth + 1});
                            }
                        } else {
                            // Download resource
                            try {
                                let skipLimit = options.skipLargerThan;
                                let head = await new Promise(res => {
                                    GM_xmlhttpRequest({
                                        method: 'HEAD',
                                        url: r.url,
                                        timeout: 8000,
                                        onload: function(resp) {
                                            let s = resp.responseHeaders.match(/Content-Length: ?(\d+)/i);
                                            let size = s ? +s[1] : null;
                                            res(size);
                                        },
                                        onerror: () => res(null),
                                        ontimeout: () => res(null)
                                    });
                                });
                                if (skipLimit && head && head > skipLimit * 1024 * 1024) {
                                    failed.push({url: r.url, reason: "Skipped (big file)"});
                                    continue;
                                }
                                const blob = await new Promise((resolve, reject) => {
                                    GM_xmlhttpRequest({
                                        method: 'GET',
                                        url: r.url,
                                        responseType: 'blob',
                                        timeout: 20000,
                                        onload: r => resolve(r.response),
                                        onerror: () => reject(new Error('Network error')),
                                        ontimeout: () => reject(new Error('Timeout'))
                                    });
                                });
                                const resPath = (r.url.split('//')[1] || r.url).replace(/[\\/:*?"<>|]+/g, '_');
                                files[resPath] = await blobToUint8Array(blob);
                            } catch (e) {
                                failed.push({url: r.url, reason: e.message});
                            }
                        }
                    }
                }
            } catch (e) {
                failed.push({url: next.url, reason: e.message});
            }
        }
        return {files, failed};
    }

    // Start full crawl!
    statusDiv.textContent = 'Collecting site data and resources...';
    let options = Object.assign({}, domainOpts);
    let {files: crawlFiles, failed: crawlFailed} = await collectResources(options, (msg, pct) => {
        statusDiv.textContent = msg;
        if (pct !== undefined) barDiv.style.width = `${pct}%`;
        if (snapshotCancelled) throw new Error('Snapshot cancelled.');
    });

    statusDiv.textContent = 'Generating ZIP archive...';
    barDiv.style.width = '97%';

    crawlFiles['snapshot-summary.json'] = new TextEncoder().encode(JSON.stringify({
        files: Object.keys(crawlFiles),
        failed: crawlFailed,
        time: (new Date()).toISOString()
    }, null, 2));

    const zipOut = await zipWithProgress(crawlFiles, { level: 0 }, pct => {
        barDiv.style.width = `${97 + pct * 3}%`;
        statusDiv.textContent = `Zipping... ${Math.round(pct * 100)}%`;
    }).catch(err => {
        statusDiv.textContent = 'ZIP failed: ' + err;
        alert('ZIP failed: ' + err);
        return null;
    });
    if (!zipOut) return;
    let zipBlob = new Blob([zipOut], { type: 'application/zip' });
    let url = URL.createObjectURL(zipBlob);
    let a = document.createElement('a');
    a.href = url;
    a.download = `website_snapshot_${new Date().toISOString().replace(/:/g, '-')}.zip`;
    document.body.appendChild(a); a.click(); document.body.removeChild(a);
    setTimeout(()=>URL.revokeObjectURL(url),3500);
    statusDiv.textContent = 'ZIP file ready. Download should start.';
    barDiv.style.width = '100%';
    setTimeout(()=>{ if(overlay) overlay.remove(); }, 3000);
}

    // ====== INIT: Start overlay & self-heal ======
    if (document.readyState === 'complete' || document.readyState === 'interactive') {
        createSnapshotShadowHost();
        monitorSnapshotOverlay();
    } else {
        window.addEventListener('DOMContentLoaded', () => {
            createSnapshotShadowHost();
            monitorSnapshotOverlay();
        }, { once: true });
    }

})();
