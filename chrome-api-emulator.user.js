// ==UserScript==
// @name         Chrome API Emulator
// @namespace    http://tampermonkey.net/
// @version      0.1
// @description  Emulates a subset of the Chrome extension API (storage.sync, context menus, runtime messaging) with drag-and-drop plugin loading.
// @match        *://*/*
// @grant        GM_getValue
// @grant        GM_setValue
// @grant        GM_registerMenuCommand
// @grant        GM_listValues
// ==/UserScript==

(function() {
    'use strict';

    // --- Runtime Messaging ---
    const runtime = {
        _listeners: [],
        onMessage: {
            addListener(fn) { runtime._listeners.push(fn); }
        },
        sendMessage(message, callback) {
            runtime._listeners.forEach(fn => fn(message, {}, callback));
            // broadcast to other tabs
            localStorage.setItem('tm_api_msg', JSON.stringify(message));
            localStorage.removeItem('tm_api_msg');
        }
    };

    window.addEventListener('storage', (e) => {
        if (e.key === 'tm_api_msg' && e.newValue) {
            const msg = JSON.parse(e.newValue);
            runtime._listeners.forEach(fn => fn(msg, {}, ()=>{}));
        }
    });

    // --- Storage Sync ---
    const storageSync = {
        get(keys, cb) {
            const result = {};
            if (Array.isArray(keys)) {
                keys.forEach(k => result[k] = GM_getValue(k));
            } else if (typeof keys === 'object') {
                Object.keys(keys).forEach(k => result[k] = GM_getValue(k, keys[k]));
            } else if (typeof keys === 'string') {
                result[keys] = GM_getValue(keys);
            } else {
                GM_listValues().forEach(k => result[k] = GM_getValue(k));
            }
            cb && cb(result);
            return Promise.resolve(result);
        },
        set(obj, cb) {
            Object.entries(obj).forEach(([k,v]) => GM_setValue(k, v));
            cb && cb();
            return Promise.resolve();
        }
    };

    // --- Context Menus ---
    const contextMenus = {
        create(options) {
            const id = options.id || ('menu_'+Date.now());
            GM_registerMenuCommand(options.title, () => {
                options.onclick && options.onclick();
                runtime.sendMessage({type:'contextMenu', menuItemId:id});
            });
            return id;
        }
    };

    const chromeAPI = {
        runtime,
        storage: { sync: storageSync },
        contextMenus,
        _plugins: []
    };

    window.chrome = chromeAPI;

    // --- Drag and Drop Plugin Loader ---
    const dz = document.createElement('div');
    dz.textContent = 'Drop JS Plugin Here';
    Object.assign(dz.style, {
        position:'fixed', bottom:'10px', right:'10px', padding:'6px',
        border:'1px dashed #888', background:'#fff', zIndex:99999, fontSize:'12px'
    });
    document.body.appendChild(dz);

    dz.addEventListener('dragover', e => { e.preventDefault(); dz.style.borderColor='#0b7'; });
    dz.addEventListener('dragleave', e => { dz.style.borderColor='#888'; });
    dz.addEventListener('drop', e => {
        e.preventDefault();
        dz.style.borderColor='#888';
        const file = e.dataTransfer.files[0];
        if (!file) return;
        const reader = new FileReader();
        reader.onload = () => {
            try {
                const code = reader.result;
                new Function('chrome', code)(chromeAPI);
                chromeAPI._plugins.push({name:file.name, code});
                alert('Loaded plugin: '+file.name);
            } catch(err) {
                alert('Plugin error: '+err.message);
            }
        };
        reader.readAsText(file);
    });
})();

