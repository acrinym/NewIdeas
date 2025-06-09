// ==UserScript==
// @name         In-Browser AHK (AutoHotkey-like Features)
// @namespace    http://tampermonkey.net/
// @version      0.2
// @description  Provides AutoHotkey-like hotstring, hotkey, workflow, and plugin functionality within the browser.
// @author       Justin Gargano (Edited by Gemini)
// @match        *://*/*
// @grant        GM_setValue
// @grant        GM_getValue
// @grant        GM_addStyle
// @grant        GM_registerMenuCommand
// @grant        GM_setClipboard
// @run-at       document-idle
// ==/UserScript==

(function() {
    'use strict';

    // --- Configuration & Constants ---
    const STORAGE_KEY_HOTSTRINGS = 'inBrowserAhk_hotstrings_v2';
    const STORAGE_KEY_HOTKEYS = 'inBrowserAhk_hotkeys_v2';
    const STORAGE_KEY_PLUGINS = 'inBrowserAhk_plugins_v2';
    const STORAGE_KEY_SETTINGS = 'inBrowserAhk_settings_v2'; // For general settings like record hotkey

    const CONFIG_PANEL_ID = 'in-browser-ahk-config-panel';
    const MAX_HOTSTRING_BUFFER = 20;
    const CUSTOM_JS_ACTION_ID = '_custom_javascript_';
    const RECORD_HOTKEY_NOTIFICATION_ID = 'in-browser-ahk-record-notification';

    // --- Default Data & Settings ---
    const defaultHotstrings = [
        { id: `hs_${Date.now()}_${Math.random().toString(36).substring(2,7)}`, trigger: 'btw', replacement: 'by the way', caseSensitive: false, enabled: true },
        { id: `hs_${Date.now()+1}_${Math.random().toString(36).substring(2,7)}`, trigger: 'omw', replacement: 'on my way', caseSensitive: false, enabled: true },
        { id: `hs_${Date.now()+2}_${Math.random().toString(36).substring(2,7)}`, trigger: ';email', replacement: 'your.email@example.com', caseSensitive: true, enabled: true },
    ];

    const defaultHotkeys = [
        { id: `hk_${Date.now()}_${Math.random().toString(36).substring(2,7)}`, keys: 'Control+Alt+C', action: 'copyCurrentUrl', description: 'Copy Current URL', enabled: true, customJS: null },
        { id: `hk_${Date.now()+1}_${Math.random().toString(36).substring(2,7)}`, keys: 'Control+Alt+G', action: 'googleSelectedText', description: 'Google Selected Text', enabled: true, customJS: null },
        { id: `hk_${Date.now()+2}_${Math.random().toString(36).substring(2,7)}`, keys: 'Alt+Shift+H', action: 'highlightAllLinks', description: 'Highlight All Links on Page', enabled: true, customJS: null },
    ];

    const defaultPlugins = []; // No default plugins initially

    const defaultSettings = {
        globalRecordHotkey: 'Control+Alt+Shift+R', // Configurable global hotkey to start recording
    };


    // --- Core Data ---
    let hotstrings = [];
    let hotkeys = [];
    let plugins = []; // Holds loaded plugin objects { meta: {}, actions: [{}], rawJson: "..." }
    let currentSettings = { ...defaultSettings };

    let hotstringInputBuffer = '';
    let isRecordingHotkey = false;
    let capturedHotkeyForRecording = null;

    // --- Global Actions Object (Core + Plugin Actions) ---
    // This object will be populated by core actions and actions from enabled plugins
    let availableActions = {};


    // --- Storage Functions ---
    async function saveData(key, data) {
        try {
            await GM_setValue(key, JSON.stringify(data));
        } catch (e) {
            console.error('In-Browser AHK: Error saving data for key', key, e);
            showNotification(`Error saving data for ${key}. Check console.`, 'error');
        }
    }

    async function loadData(key, defaultValue) {
        try {
            const data = await GM_getValue(key);
            if (data === undefined || data === null || data === '') { // Check for empty string explicitly
                 return JSON.parse(JSON.stringify(defaultValue)); // Deep copy default value
            }
            return JSON.parse(data);
        } catch (e) {
            console.error('In-Browser AHK: Error loading data for key', key, e);
            showNotification(`Error loading data for ${key}. Using defaults. Check console.`, 'error');
            return JSON.parse(JSON.stringify(defaultValue)); // Deep copy default value on error
        }
    }

    // --- Action Registration & Execution ---
    function compileAndRegisterPluginAction(pluginId, actionDefinition) {
        const actionKey = `plugin_${pluginId}_${actionDefinition.actionName}`;
        try {
            // These are the arguments that will be available inside the plugin's JS code
            const func = new Function('event', 'showNotification', 'GM_setClipboard', 'actions', 'GM_setValue', 'GM_getValue', 'escapeHtml', 'helpers', actionDefinition.javascriptCode);
            availableActions[actionKey] = {
                func: func,
                description: actionDefinition.description || 'Plugin-defined action.',
                isPluginAction: true,
                pluginId: pluginId,
                displayName: actionDefinition.displayName || actionDefinition.actionName,
            };
            return true;
        } catch (e) {
            console.error(`In-Browser AHK: Error compiling JS for plugin action "${actionDefinition.actionName}" from plugin "${pluginId}":`, e);
            showNotification(`Error compiling action ${actionDefinition.actionName} from plugin ${pluginId}. Check console.`, 'error');
            return false;
        }
    }

    function unregisterActionsForPlugin(pluginId) {
        for (const key in availableActions) {
            if (availableActions[key].isPluginAction && availableActions[key].pluginId === pluginId) {
                delete availableActions[key];
            }
        }
    }

    function registerAllPluginActions() {
        plugins.forEach(plugin => {
            if (plugin.meta.enabled !== false) { // Enabled by default or if explicitly true
                plugin.actions.forEach(actionDef => {
                    compileAndRegisterPluginAction(plugin.meta.id, actionDef);
                });
            }
        });
    }

    // Helper functions that can be passed to custom JS
    const customJsHelpers = {
        // Example: A function to click an element by selector
        clickElement: (selector) => {
            const elem = document.querySelector(selector);
            if (elem && typeof elem.click === 'function') {
                elem.click();
                return true;
            }
            showNotification(`Element not found or not clickable: ${selector}`, 'error');
            return false;
        },
        // Example: Get value of an input
        getElementValue: (selector) => {
            const elem = document.querySelector(selector);
            return elem ? elem.value : null;
        },
        // Add more helpers as needed
    };


    // --- Core Predefined Actions ---
    // These are the script's built-in actions.
    const coreActions = {
        copyCurrentUrl: {
            func: () => {
                const url = window.location.href;
                GM_setClipboard(url, 'text');
                showNotification(`Copied URL: ${url}`);
            },
            description: 'Copies the current page URL to the clipboard.'
        },
        googleSelectedText: {
            func: () => {
                const selectedText = window.getSelection().toString().trim();
                if (selectedText) {
                    const googleUrl = `https://www.google.com/search?q=${encodeURIComponent(selectedText)}`;
                    window.open(googleUrl, '_blank');
                } else {
                    showNotification('No text selected to Google.');
                }
            },
            description: 'Searches Google for the currently selected text.'
        },
        highlightAllLinks: {
            func: () => {
                const links = document.querySelectorAll('a');
                links.forEach(link => {
                    link.style.backgroundColor = link.style.backgroundColor === 'yellow' ? '' : 'yellow';
                });
                showNotification(`Toggled highlight for ${links.length} links.`);
            },
            description: 'Toggles a yellow background on all links on the page.'
        },
        reloadPage: { func: () => window.location.reload(), description: 'Reload the current page.' },
        goBack: { func: () => window.history.back(), description: 'Navigate to the previous page.' },
        goForward: { func: () => window.history.forward(), description: 'Navigate to the next page.' },
        scrollToTop: { func: () => window.scrollTo({top: 0, behavior: 'smooth'}), description: 'Scroll to the top of the page.' },
        scrollToBottom: { func: () => window.scrollTo({top: document.body.scrollHeight, behavior: 'smooth'}), description: 'Scroll to the bottom of the page.' },
    };


    // --- Hotstring Engine (Mostly unchanged, added ID to objects) ---
    function handleHotstringInput(event) {
        const target = event.target;
        if (!['INPUT', 'TEXTAREA'].includes(target.tagName) && !target.isContentEditable) {
            hotstringInputBuffer = '';
            return;
        }
        if (isRecordingHotkey) return; // Don't process hotstrings while recording a hotkey

        if (event.key && event.key.length === 1) {
            hotstringInputBuffer += event.key;
        } else if (event.key === 'Backspace') {
            hotstringInputBuffer = hotstringInputBuffer.slice(0, -1);
            return;
        } else if (event.key === ' ') {
             hotstringInputBuffer += ' ';
        } else if (['Enter', 'Tab'].includes(event.key)) {
            // Allow these to potentially trigger
        } else {
            return;
        }

        if (hotstringInputBuffer.length > MAX_HOTSTRING_BUFFER) {
            hotstringInputBuffer = hotstringInputBuffer.slice(-MAX_HOTSTRING_BUFFER);
        }

        for (const hs of hotstrings) {
            if (!hs.enabled) continue;
            let trigger = hs.trigger;
            let currentBuffer = hotstringInputBuffer;
            if (!hs.caseSensitive) {
                trigger = trigger.toLowerCase();
                currentBuffer = currentBuffer.toLowerCase();
            }

            if (currentBuffer.endsWith(trigger)) {
                const replacementText = hs.replacement;
                const triggerLen = hs.trigger.length;

                if (target.value !== undefined) {
                    const cursorPos = target.selectionStart;
                    target.value = target.value.substring(0, cursorPos - triggerLen) + replacementText + target.value.substring(cursorPos);
                    target.selectionStart = target.selectionEnd = cursorPos - triggerLen + replacementText.length;
                } else if (target.isContentEditable) {
                    document.execCommand('insertText', false, ' '); // Remove trigger by backspacing then inserting
                    for(let i=0; i < triggerLen; i++) document.execCommand('deleteBackward', false, null);
                    document.execCommand('insertText', false, replacementText);
                }
                hotstringInputBuffer = '';
                event.preventDefault();
                return;
            }
        }
        if (hotstringInputBuffer.endsWith(' ') && event.key === ' ') {
            hotstringInputBuffer = hotstringInputBuffer.slice(0, -1);
        }
    }

    // --- Hotkey Engine (Updated for custom JS and recording) ---
    function formatPressedKeys(event) {
        const pressedKeys = [];
        if (event.ctrlKey) pressedKeys.push('Control');
        if (event.altKey) pressedKeys.push('Alt');
        if (event.shiftKey) pressedKeys.push('Shift');
        if (event.metaKey) pressedKeys.push('Meta');

        let keyName = event.key;
        if (keyName.length === 1 && keyName.match(/[a-z]/i)) keyName = keyName.toUpperCase();
        if (!['Control', 'Alt', 'Shift', 'Meta', 'Hyper', 'Super'].includes(keyName)) pressedKeys.push(keyName);

        // Ensure there's at least one non-modifier key if modifiers are present
        if (pressedKeys.length > 0 && !pressedKeys.some(k => !['Control', 'Alt', 'Shift', 'Meta'].includes(k))) {
            if (!['Control', 'Alt', 'Shift', 'Meta'].includes(keyName)) { // only add if it's not a modifier itself
                 // This case should be rare if logic above is correct
            } else {
                return null; // Only modifiers pressed
            }
        }
        if (pressedKeys.length === 0) return null; // No valid keys

        return pressedKeys.sort().join('+');
    }


    function handleHotkey(event) {
        const pressedKeysStr = formatPressedKeys(event);
        if (!pressedKeysStr) return;

        // Handle Hotkey Recording Mode
        if (isRecordingHotkey) {
            event.preventDefault();
            event.stopPropagation();
            capturedHotkeyForRecording = pressedKeysStr;
            showNotification(`Captured: ${pressedKeysStr}. Now select an action in the panel.`, 'info', 0); // Persist until next action
            isRecordingHotkey = false; // Stop recording after one capture
            updateRecordHotkeyStatusUI(false); // Update UI if panel is open

            // If config panel is open, update the input field
            const panel = document.getElementById(CONFIG_PANEL_ID);
            if (panel && panel.style.display !== 'none') {
                panel.querySelector('#hk-keys').value = capturedHotkeyForRecording;
                panel.querySelector('#hk-keys').focus(); // Focus for immediate action selection
            }
            return;
        }

        // Check for Global Record Hotkey
        if (pressedKeysStr === currentSettings.globalRecordHotkey) {
            event.preventDefault();
            event.stopPropagation();
            startRecordingHotkeyMode();
            return;
        }

        // Normal Hotkey Execution
        for (const hk of hotkeys) {
            if (!hk.enabled) continue;
            const normalizedHkKeys = hk.keys.split('+').map(k => k.length === 1 ? k.toUpperCase() : k).sort().join('+');

            if (normalizedHkKeys === pressedKeysStr) {
                event.preventDefault();
                event.stopPropagation();

                if (hk.action === CUSTOM_JS_ACTION_ID && hk.customJS) {
                    try {
                        // Arguments for custom JS: event, showNotification, GM_setClipboard, all available actions, GM_setValue, GM_getValue, escapeHtml, custom helpers
                        const customFunc = new Function('event', 'showNotification', 'GM_setClipboard', 'actions', 'GM_setValue', 'GM_getValue', 'escapeHtml', 'helpers', hk.customJS);
                        customFunc(event, showNotification, GM_setClipboard, availableActions, GM_setValue, GM_getValue, escapeHtml, customJsHelpers);
                    } catch (e) {
                        console.error(`In-Browser AHK: Error executing custom JS for hotkey "${hk.keys}":`, e);
                        showNotification(`Error in custom JS for ${hk.keys}: ${e.message}`, 'error');
                    }
                } else if (availableActions[hk.action] && typeof availableActions[hk.action].func === 'function') {
                    try {
                        // Pass context to plugin actions as well
                        availableActions[hk.action].func(event, showNotification, GM_setClipboard, availableActions, GM_setValue, GM_getValue, escapeHtml, customJsHelpers);
                    } catch (e) {
                        console.error(`In-Browser AHK: Error executing action "${hk.action}":`, e);
                        showNotification(`Error in action: ${hk.action}`, 'error');
                    }
                } else {
                    console.warn(`In-Browser AHK: Action "${hk.action}" not found or not a function.`);
                    showNotification(`Action "${hk.action}" not found.`, 'error');
                }
                return;
            }
        }
    }

    function startRecordingHotkeyMode() {
        isRecordingHotkey = true;
        capturedHotkeyForRecording = null;
        showNotification('Recording next hotkey... Press the desired key combination.', 'info', 0); // Persist
        updateRecordHotkeyStatusUI(true);
    }

    function updateRecordHotkeyStatusUI(isRecording) {
        const panel = document.getElementById(CONFIG_PANEL_ID);
        if (panel && panel.style.display !== 'none') {
            const recordBtn = panel.querySelector('#hk-record-btn');
            const statusDiv = panel.querySelector('#hk-record-status');
            if (recordBtn) recordBtn.textContent = isRecording ? 'Cancel Recording' : 'Record New Hotkey';
            if (statusDiv) statusDiv.textContent = isRecording ? 'Press desired hotkey combination now...' : '';
        }

        let recordNotif = document.getElementById(RECORD_HOTKEY_NOTIFICATION_ID);
        if (isRecording) {
            if (!recordNotif) {
                recordNotif = document.createElement('div');
                recordNotif.id = RECORD_HOTKEY_NOTIFICATION_ID;
                recordNotif.style.position = 'fixed';
                recordNotif.style.top = '10px';
                recordNotif.style.left = '50%';
                recordNotif.style.transform = 'translateX(-50%)';
                recordNotif.style.padding = '10px 20px';
                recordNotif.style.backgroundColor = 'rgba(0,0,200,0.8)';
                recordNotif.style.color = 'white';
                recordNotif.style.zIndex = '100001';
                recordNotif.style.borderRadius = '5px';
                document.body.appendChild(recordNotif);
            }
            recordNotif.textContent = 'RECORDING HOTKEY... (Press desired keys or your global record hotkey again to cancel)';
            recordNotif.style.display = 'block';
        } else {
            if (recordNotif) recordNotif.style.display = 'none';
        }
    }


    // --- UI: Configuration Panel (Significantly updated) ---
    function createConfigPanel() {
        let panel = document.getElementById(CONFIG_PANEL_ID);
        if (panel) {
            panel.style.display = panel.style.display === 'none' ? 'block' : 'none';
            if (panel.style.display === 'block') refreshActionDropdown(); // Refresh if opening
            return;
        }

        panel = document.createElement('div');
        panel.id = CONFIG_PANEL_ID;
        panel.innerHTML = `
            <div class="header">
                <h2>In-Browser AHK Settings (v0.2)</h2>
                <button id="in-browser-ahk-close-btn" class="close-btn">&times;</button>
            </div>
            <div class="tabs">
                <button class="tab-button active" data-tab="hotstrings">Hotstrings</button>
                <button class="tab-button" data-tab="hotkeys">Hotkeys</button>
                <button class="tab-button" data-tab="plugins">Plugins</button>
                <button class="tab-button" data-tab="settings">Settings</button>
            </div>

            <div id="hotstrings-tab" class="tab-content active">
                <h3>Manage Hotstrings</h3>
                <div id="hotstring-list" class="item-list"></div>
                <h4>Add/Edit Hotstring</h4>
                <input type="hidden" id="hs-id">
                <input type="text" id="hs-trigger" placeholder="Trigger (e.g., btw)">
                <input type="text" id="hs-replacement" placeholder="Replacement (e.g., by the way)">
                <label><input type="checkbox" id="hs-case-sensitive"> Case Sensitive</label>
                <label><input type="checkbox" id="hs-enabled" checked> Enabled</label>
                <button id="hs-save-btn">Save Hotstring</button>
                <button id="hs-clear-btn" style="display:none;">Clear Form</button>
            </div>

            <div id="hotkeys-tab" class="tab-content">
                <h3>Manage Hotkeys</h3>
                <button id="hk-record-btn">Record New Hotkey</button> <span id="hk-record-status" style="margin-left:10px; color: #007bff;"></span>
                <div id="hotkey-list" class="item-list"></div>
                <h4>Add/Edit Hotkey</h4>
                <input type="hidden" id="hk-id">
                <label for="hk-keys">Keys (e.g., Control+Alt+C):</label>
                <input type="text" id="hk-keys" placeholder="Press 'Record' or type manually">

                <label for="hk-action">Action:</label>
                <select id="hk-action"></select>

                <div id="hk-custom-js-area" style="display:none; margin-top:10px;">
                    <label for="hk-custom-js">Custom JavaScript Code:</label>
                    <textarea id="hk-custom-js" rows="5" placeholder="Enter your JavaScript code here..."></textarea>
                    <div class="help-text">
                        Available in your JS: <code>event</code>, <code>showNotification(msg, type)</code>, <code>GM_setClipboard(data, type)</code>,
                        <code>actions</code> (object of all available actions), <code>GM_setValue(key,val)</code>, <code>GM_getValue(key,def)</code>,
                        <code>escapeHtml(str)</code>, <code>helpers</code> (custom utility object).
                        <br><strong>Warning:</strong> Custom JS runs with script privileges. Use with caution.
                        <br>Example: <code>showNotification('My custom hotkey fired!', 'success');</code>
                    </div>
                </div>

                <label for="hk-description">Description (optional):</label>
                <input type="text" id="hk-description" placeholder="Brief description of what it does">
                <label><input type="checkbox" id="hk-enabled" checked> Enabled</label>
                <button id="hk-save-btn">Save Hotkey</button>
                <button id="hk-clear-btn" style="display:none;">Clear Form</button>
            </div>

            <div id="plugins-tab" class="tab-content">
                <h3>Manage Plugins</h3>
                <div id="plugin-drop-zone">Drag & Drop Plugin JSON/TXT File Here</div>
                <div id="plugin-list" class="item-list"></div>
                <h4>Plugin Help</h4>
                <div class="help-text">
                    Plugins are JSON files that add new actions. Structure:
                    <pre style="font-size:0.9em; padding:5px; border:1px solid #ccc; background:#fff;">${escapeHtml(`{
  "pluginMeta": {
    "id": "unique-plugin-id",
    "name": "My Plugin",
    "version": "1.0",
    "author": "You",
    "description": "What this plugin does."
  },
  "actions": [
    {
      "actionName": "myCoolAction", // unique within plugin
      "displayName": "Do My Cool Thing", // shows in dropdown
      "description": "Detailed description of the action.",
      "javascriptCode": "showNotification('My plugin action: ' + event.type);"
    }
  ]
}`)}</pre>
                    Available in <code>javascriptCode</code>: same as Custom JS for Hotkeys.
                    <br><em>Advanced:</em> You can use <code>fetch()</code> inside <code>javascriptCode</code> to load external libraries at your own risk.
                </div>
            </div>

            <div id="settings-tab" class="tab-content">
                <h3>General Settings</h3>
                <label for="setting-global-record-hotkey">Global Hotkey to Start Recording:</label>
                <input type="text" id="setting-global-record-hotkey" placeholder="e.g., Control+Alt+Shift+R">
                <button id="setting-save-btn">Save Settings</button>
                 <div class="help-text">
                    Press this key combination anywhere in the browser to start "listening" for the next key combination,
                    which will then be pre-filled in the "Add/Edit Hotkey" form.
                </div>
            </div>

            <button id="save-all-to-storage-btn" class="save-all-btn">Save All Configurations to Storage</button>
        `;
        document.body.appendChild(panel);

        // Event Listeners
        panel.querySelector('#in-browser-ahk-close-btn').addEventListener('click', () => {
            panel.style.display = 'none';
            if (isRecordingHotkey) { // Cancel recording if panel is closed
                isRecordingHotkey = false;
                updateRecordHotkeyStatusUI(false);
            }
        });

        // Tabs
        panel.querySelectorAll('.tab-button').forEach(button => {
            button.addEventListener('click', (e) => {
                panel.querySelectorAll('.tab-button, .tab-content').forEach(el => el.classList.remove('active'));
                e.target.classList.add('active');
                panel.querySelector(`#${e.target.dataset.tab}-tab`).classList.add('active');
                if (e.target.dataset.tab === 'hotkeys') refreshActionDropdown(); // Refresh if switching to hotkeys tab
            });
        });

        // Hotstrings
        panel.querySelector('#hs-save-btn').addEventListener('click', saveHotstring);
        panel.querySelector('#hs-clear-btn').addEventListener('click', clearHotstringForm);

        // Hotkeys
        panel.querySelector('#hk-save-btn').addEventListener('click', saveHotkey);
        panel.querySelector('#hk-clear-btn').addEventListener('click', clearHotkeyForm);
        panel.querySelector('#hk-record-btn').addEventListener('click', () => {
            if (isRecordingHotkey) {
                isRecordingHotkey = false;
                updateRecordHotkeyStatusUI(false);
                showNotification('Hotkey recording cancelled.', 'info');
            } else {
                startRecordingHotkeyMode();
            }
        });
        panel.querySelector('#hk-action').addEventListener('change', (e) => {
            const customJsArea = panel.querySelector('#hk-custom-js-area');
            customJsArea.style.display = e.target.value === CUSTOM_JS_ACTION_ID ? 'block' : 'none';
        });


        // Plugins
        const dropZone = panel.querySelector('#plugin-drop-zone');
        dropZone.addEventListener('dragover', (e) => { e.preventDefault(); e.stopPropagation(); dropZone.classList.add('dragover'); });
        dropZone.addEventListener('dragleave', (e) => { e.preventDefault(); e.stopPropagation(); dropZone.classList.remove('dragover'); });
        dropZone.addEventListener('drop', handlePluginDrop);

        // Settings
        panel.querySelector('#setting-save-btn').addEventListener('click', saveCurrentSettings);
        panel.querySelector('#setting-global-record-hotkey').value = currentSettings.globalRecordHotkey;


        // Save All Button
        panel.querySelector('#save-all-to-storage-btn').addEventListener('click', async () => {
            await saveData(STORAGE_KEY_HOTSTRINGS, hotstrings);
            await saveData(STORAGE_KEY_HOTKEYS, hotkeys);
            await saveData(STORAGE_KEY_PLUGINS, plugins.map(p => ({meta: p.meta, actions: p.actions, rawJson: p.rawJson}))); // Don't save compiled functions
            await saveData(STORAGE_KEY_SETTINGS, currentSettings);
            showNotification('All configurations saved to Tampermonkey storage!', 'success');
            renderConfigPanel(); // Re-render to reflect saved state and ensure lists are up-to-date
        });

        refreshActionDropdown(); // Initial population
        renderConfigPanel(); // Initial render of lists
    }

    function refreshActionDropdown() {
        const panel = document.getElementById(CONFIG_PANEL_ID);
        if (!panel) return;
        const actionSelect = panel.querySelector('#hk-action');
        const currentVal = actionSelect.value; // Preserve selection if possible
        actionSelect.innerHTML = ''; // Clear existing options

        // Add option for Custom JS
        const customOpt = document.createElement('option');
        customOpt.value = CUSTOM_JS_ACTION_ID;
        customOpt.textContent = '-- Custom JavaScript --';
        actionSelect.appendChild(customOpt);

        // Add core and plugin actions
        for (const actionName in availableActions) {
            const action = availableActions[actionName];
            const option = document.createElement('option');
            option.value = actionName;
            let displayName = action.displayName || actionName;
            if (action.isPluginAction) displayName = `[Plugin: ${action.pluginId.substring(0,10)}] ${displayName}`;
            option.textContent = `${displayName} (${action.description || 'No description'})`;
            actionSelect.appendChild(option);
        }
        actionSelect.value = currentVal; // Try to restore previous selection
         if (!actionSelect.value && actionSelect.options.length > 0) { // if previous val not found, select first
            actionSelect.selectedIndex = 0;
        }
        // Trigger change to show/hide custom JS area if needed
        actionSelect.dispatchEvent(new Event('change'));
    }


    function renderConfigPanel() { // Re-renders all lists
        const panel = document.getElementById(CONFIG_PANEL_ID);
        if (!panel || panel.style.display === 'none') return;

        renderHotstringList(panel);
        renderHotkeyList(panel);
        renderPluginList(panel);
        panel.querySelector('#setting-global-record-hotkey').value = currentSettings.globalRecordHotkey; // Ensure settings field is current
    }

    // --- Hotstring UI Functions (with ID handling) ---
    function renderHotstringList(panel) {
        const listDiv = panel.querySelector('#hotstring-list');
        listDiv.innerHTML = '<table><thead><tr><th>Trigger</th><th>Replacement</th><th>Case</th><th>Enabled</th><th>Actions</th></tr></thead><tbody></tbody></table>';
        const tbody = listDiv.querySelector('tbody');
        hotstrings.forEach((hs) => {
            const row = tbody.insertRow();
            row.dataset.id = hs.id;
            row.innerHTML = `
                <td>${escapeHtml(hs.trigger)}</td>
                <td>${escapeHtml(hs.replacement)}</td>
                <td>${hs.caseSensitive ? 'Yes' : 'No'}</td>
                <td><input type="checkbox" class="hs-enable-toggle" data-id="${hs.id}" ${hs.enabled ? 'checked' : ''}></td>
                <td>
                    <button class="edit-hs-btn" data-id="${hs.id}">Edit</button>
                    <button class="delete-hs-btn" data-id="${hs.id}">Delete</button>
                </td>`;
        });
        listDiv.querySelectorAll('.delete-hs-btn').forEach(btn => btn.addEventListener('click', (e) => deleteHotstring(e.target.dataset.id)));
        listDiv.querySelectorAll('.edit-hs-btn').forEach(btn => btn.addEventListener('click', (e) => editHotstring(e.target.dataset.id)));
        listDiv.querySelectorAll('.hs-enable-toggle').forEach(cb => cb.addEventListener('change', (e) => toggleHotstringEnable(e.target.dataset.id, e.target.checked)));
    }
    function clearHotstringForm() { /* ... */
        const panel = document.getElementById(CONFIG_PANEL_ID);
        if (!panel) return;
        panel.querySelector('#hs-id').value = '';
        panel.querySelector('#hs-trigger').value = '';
        panel.querySelector('#hs-replacement').value = '';
        panel.querySelector('#hs-case-sensitive').checked = false;
        panel.querySelector('#hs-enabled').checked = true;
        panel.querySelector('#hs-save-btn').textContent = 'Save Hotstring';
        panel.querySelector('#hs-clear-btn').style.display = 'none';
    }
    function saveHotstring() { /* ... */
        const panel = document.getElementById(CONFIG_PANEL_ID);
        if (!panel) return;
        const id = panel.querySelector('#hs-id').value;
        const trigger = panel.querySelector('#hs-trigger').value.trim();
        const replacement = panel.querySelector('#hs-replacement').value;
        const caseSensitive = panel.querySelector('#hs-case-sensitive').checked;
        const enabled = panel.querySelector('#hs-enabled').checked;

        if (!trigger) { showNotification('Hotstring trigger cannot be empty.', 'error'); return; }

        if (id) { // Edit existing
            const index = hotstrings.findIndex(hs => hs.id === id);
            if (index > -1) hotstrings[index] = { ...hotstrings[index], trigger, replacement, caseSensitive, enabled };
        } else { // Add new
            const newId = `hs_${Date.now()}_${Math.random().toString(36).substring(2,7)}`;
            hotstrings.push({ id: newId, trigger, replacement, caseSensitive, enabled });
        }
        clearHotstringForm();
        renderHotstringList(panel);
        showNotification(`Hotstring ${id ? 'updated' : 'added'}. Remember to "Save All".`);
    }
    function editHotstring(id) { /* ... */
        const panel = document.getElementById(CONFIG_PANEL_ID);
        if (!panel) return;
        const hs = hotstrings.find(h => h.id === id);
        if (!hs) return;
        panel.querySelector('#hs-id').value = hs.id;
        panel.querySelector('#hs-trigger').value = hs.trigger;
        panel.querySelector('#hs-replacement').value = hs.replacement;
        panel.querySelector('#hs-case-sensitive').checked = hs.caseSensitive;
        panel.querySelector('#hs-enabled').checked = hs.enabled;
        panel.querySelector('#hs-save-btn').textContent = 'Update Hotstring';
        panel.querySelector('#hs-clear-btn').style.display = 'inline-block';
    }
    function deleteHotstring(id) { /* ... */
        if (!confirm('Are you sure you want to delete this hotstring?')) return;
        hotstrings = hotstrings.filter(hs => hs.id !== id);
        const panel = document.getElementById(CONFIG_PANEL_ID);
        if (panel) renderHotstringList(panel);
        showNotification('Hotstring deleted. Remember to "Save All".');
    }
    function toggleHotstringEnable(id, isEnabled) { /* ... */
        const hs = hotstrings.find(h => h.id === id);
        if (hs) {
            hs.enabled = isEnabled;
            showNotification(`Hotstring ${hs.trigger} ${isEnabled ? 'enabled' : 'disabled'}. Remember to "Save All".`);
        }
    }

    // --- Hotkey UI Functions (with ID, custom JS) ---
    function renderHotkeyList(panel) {
        const listDiv = panel.querySelector('#hotkey-list');
        listDiv.innerHTML = '<table><thead><tr><th>Keys</th><th>Action/CustomJS</th><th>Description</th><th>Enabled</th><th>Actions</th></tr></thead><tbody></tbody></table>';
        const tbody = listDiv.querySelector('tbody');
        hotkeys.forEach((hk) => {
            const row = tbody.insertRow();
            row.dataset.id = hk.id;
            let actionDisplay = 'N/A';
            if (hk.action === CUSTOM_JS_ACTION_ID) {
                actionDisplay = `Custom JS (hover for code)`;
            } else if (availableActions[hk.action]) {
                actionDisplay = availableActions[hk.action].displayName || hk.action;
            }

            row.innerHTML = `
                <td>${escapeHtml(hk.keys)}</td>
                <td title="${hk.customJS ? escapeHtml(hk.customJS) : (availableActions[hk.action]?.description || '')}">${escapeHtml(actionDisplay)}</td>
                <td>${escapeHtml(hk.description)}</td>
                <td><input type="checkbox" class="hk-enable-toggle" data-id="${hk.id}" ${hk.enabled ? 'checked' : ''}></td>
                <td>
                    <button class="edit-hk-btn" data-id="${hk.id}">Edit</button>
                    <button class="delete-hk-btn" data-id="${hk.id}">Delete</button>
                </td>`;
        });
        listDiv.querySelectorAll('.delete-hk-btn').forEach(btn => btn.addEventListener('click', (e) => deleteHotkey(e.target.dataset.id)));
        listDiv.querySelectorAll('.edit-hk-btn').forEach(btn => btn.addEventListener('click', (e) => editHotkey(e.target.dataset.id)));
        listDiv.querySelectorAll('.hk-enable-toggle').forEach(cb => cb.addEventListener('change', (e) => toggleHotkeyEnable(e.target.dataset.id, e.target.checked)));
    }
    function clearHotkeyForm() { /* ... */
        const panel = document.getElementById(CONFIG_PANEL_ID);
        if (!panel) return;
        panel.querySelector('#hk-id').value = '';
        panel.querySelector('#hk-keys').value = '';
        panel.querySelector('#hk-action').value = panel.querySelector('#hk-action').options[0]?.value || ''; // Default to first or custom
        panel.querySelector('#hk-custom-js').value = '';
        panel.querySelector('#hk-custom-js-area').style.display = panel.querySelector('#hk-action').value === CUSTOM_JS_ACTION_ID ? 'block' : 'none';
        panel.querySelector('#hk-description').value = '';
        panel.querySelector('#hk-enabled').checked = true;
        panel.querySelector('#hk-save-btn').textContent = 'Save Hotkey';
        panel.querySelector('#hk-clear-btn').style.display = 'none';
        capturedHotkeyForRecording = null; // Clear any captured hotkey
        updateRecordHotkeyStatusUI(false);
    }
    function saveHotkey() { /* ... */
        const panel = document.getElementById(CONFIG_PANEL_ID);
        if (!panel) return;
        const id = panel.querySelector('#hk-id').value;
        const keys = panel.querySelector('#hk-keys').value.trim();
        const action = panel.querySelector('#hk-action').value;
        const customJS = action === CUSTOM_JS_ACTION_ID ? panel.querySelector('#hk-custom-js').value : null;
        const description = panel.querySelector('#hk-description').value.trim();
        const enabled = panel.querySelector('#hk-enabled').checked;

        if (!keys) { showNotification('Hotkey keys cannot be empty.', 'error'); return; }
        if (action === CUSTOM_JS_ACTION_ID && !customJS) { showNotification('Custom JavaScript cannot be empty if selected.', 'error'); return; }
        if (action !== CUSTOM_JS_ACTION_ID && !availableActions[action] && action !== "") { // allow empty action if user is clearing
             showNotification('Selected action is not valid.', 'error'); return;
        }


        const hotkeyData = { keys, action, customJS, description, enabled };

        if (id) { // Edit existing
            const index = hotkeys.findIndex(hk => hk.id === id);
            if (index > -1) hotkeys[index] = { ...hotkeys[index], ...hotkeyData };
        } else { // Add new
            const newId = `hk_${Date.now()}_${Math.random().toString(36).substring(2,7)}`;
            hotkeys.push({ id: newId, ...hotkeyData });
        }
        clearHotkeyForm();
        renderHotkeyList(panel);
        showNotification(`Hotkey ${id ? 'updated' : 'added'}. Remember to "Save All".`);
    }
    function editHotkey(id) { /* ... */
        const panel = document.getElementById(CONFIG_PANEL_ID);
        if (!panel) return;
        const hk = hotkeys.find(h => h.id === id);
        if (!hk) return;
        panel.querySelector('#hk-id').value = hk.id;
        panel.querySelector('#hk-keys').value = hk.keys;
        panel.querySelector('#hk-action').value = hk.action;
        panel.querySelector('#hk-custom-js').value = hk.customJS || '';
        panel.querySelector('#hk-custom-js-area').style.display = hk.action === CUSTOM_JS_ACTION_ID ? 'block' : 'none';
        panel.querySelector('#hk-description').value = hk.description;
        panel.querySelector('#hk-enabled').checked = hk.enabled;
        panel.querySelector('#hk-save-btn').textContent = 'Update Hotkey';
        panel.querySelector('#hk-clear-btn').style.display = 'inline-block';
        refreshActionDropdown(); // Ensure dropdown is current before setting value
        panel.querySelector('#hk-action').value = hk.action; // Set after refresh
        panel.querySelector('#hk-action').dispatchEvent(new Event('change')); // Trigger display of custom JS area
    }
    function deleteHotkey(id) { /* ... */
        if (!confirm('Are you sure you want to delete this hotkey?')) return;
        hotkeys = hotkeys.filter(hk => hk.id !== id);
        const panel = document.getElementById(CONFIG_PANEL_ID);
        if (panel) renderHotkeyList(panel);
        showNotification('Hotkey deleted. Remember to "Save All".');
    }
    function toggleHotkeyEnable(id, isEnabled) { /* ... */
        const hk = hotkeys.find(h => h.id === id);
        if (hk) {
            hk.enabled = isEnabled;
            showNotification(`Hotkey ${hk.keys} ${isEnabled ? 'enabled' : 'disabled'}. Remember to "Save All".`);
        }
    }

    // --- Plugin UI & Logic Functions ---
    function handlePluginDrop(event) {
        event.preventDefault();
        event.stopPropagation();
        const dropZone = event.target.closest('#plugin-drop-zone');
        if (dropZone) dropZone.classList.remove('dragover');

        const files = event.dataTransfer.files;
        if (files.length > 0) {
            const file = files[0];
            if (file.type === 'application/json' || file.name.endsWith('.json') || file.name.endsWith('.txt')) {
                const reader = new FileReader();
                reader.onload = (e) => {
                    try {
                        const pluginJson = e.target.result;
                        const pluginData = JSON.parse(pluginJson);
                        if (validatePluginData(pluginData)) {
                            const existingPluginIndex = plugins.findIndex(p => p.meta.id === pluginData.pluginMeta.id);
                            const newPluginEntry = {
                                meta: { ...pluginData.pluginMeta, enabled: true }, // Ensure enabled field exists
                                actions: pluginData.actions,
                                rawJson: pluginJson // Store raw for saving
                            };

                            if (existingPluginIndex > -1) { // Update existing
                                if (!confirm(`Plugin "${pluginData.pluginMeta.name}" already exists. Overwrite?`)) return;
                                unregisterActionsForPlugin(plugins[existingPluginIndex].meta.id); // Unregister old actions
                                plugins[existingPluginIndex] = newPluginEntry;
                            } else { // Add new
                                plugins.push(newPluginEntry);
                            }

                            compileAndRegisterPluginAction(newPluginEntry.meta.id, newPluginEntry.actions[0]); // Example: re-register first action, ideally all
                            registerAllPluginActions(); // Re-register all actions from all enabled plugins

                            renderConfigPanel();
                            refreshActionDropdown();
                            showNotification(`Plugin "${pluginData.pluginMeta.name}" loaded. Remember to "Save All".`, 'success');
                        }
                    } catch (err) {
                        console.error("Error processing plugin file:", err);
                        showNotification(`Error processing plugin: ${err.message}`, 'error');
                    }
                };
                reader.readAsText(file);
            } else {
                showNotification('Invalid file type. Please drop a JSON or TXT file.', 'error');
            }
        }
    }

    function validatePluginData(data) {
        if (!data.pluginMeta || !data.pluginMeta.id || !data.pluginMeta.name) {
            showNotification('Plugin error: Missing pluginMeta.id or pluginMeta.name.', 'error'); return false;
        }
        if (!Array.isArray(data.actions) || data.actions.length === 0) {
            showNotification(`Plugin "${data.pluginMeta.name}" error: 'actions' array is missing or empty.`, 'error'); return false;
        }
        for (const action of data.actions) {
            if (!action.actionName || !action.javascriptCode) {
                showNotification(`Plugin "${data.pluginMeta.name}" error: Action missing actionName or javascriptCode.`, 'error'); return false;
            }
        }
        return true;
    }

    function renderPluginList(panel) {
        const listDiv = panel.querySelector('#plugin-list');
        listDiv.innerHTML = '<table><thead><tr><th>Name</th><th>Version</th><th>Description</th><th>Enabled</th><th>Actions</th></tr></thead><tbody></tbody></table>';
        const tbody = listDiv.querySelector('tbody');
        plugins.forEach(plugin => {
            const row = tbody.insertRow();
            row.dataset.id = plugin.meta.id;
            row.innerHTML = `
                <td>${escapeHtml(plugin.meta.name)}</td>
                <td>${escapeHtml(plugin.meta.version || 'N/A')}</td>
                <td title="${plugin.actions.map(a => a.displayName || a.actionName).join(', ')}">${escapeHtml(plugin.meta.description || 'No description.')}</td>
                <td><input type="checkbox" class="plugin-enable-toggle" data-id="${plugin.meta.id}" ${plugin.meta.enabled !== false ? 'checked' : ''}></td>
                <td><button class="delete-plugin-btn" data-id="${plugin.meta.id}">Delete</button></td>`;
        });
        listDiv.querySelectorAll('.delete-plugin-btn').forEach(btn => btn.addEventListener('click', (e) => deletePlugin(e.target.dataset.id)));
        listDiv.querySelectorAll('.plugin-enable-toggle').forEach(cb => cb.addEventListener('change', (e) => togglePluginEnable(e.target.dataset.id, e.target.checked)));
    }

    function deletePlugin(pluginId) {
        if (!confirm(`Are you sure you want to delete the plugin "${plugins.find(p=>p.meta.id === pluginId)?.meta.name || pluginId}"?`)) return;
        unregisterActionsForPlugin(pluginId);
        plugins = plugins.filter(p => p.meta.id !== pluginId);
        renderConfigPanel();
        refreshActionDropdown();
        showNotification('Plugin deleted. Remember to "Save All".');
    }

    function togglePluginEnable(pluginId, isEnabled) {
        const plugin = plugins.find(p => p.meta.id === pluginId);
        if (plugin) {
            plugin.meta.enabled = isEnabled;
            if (isEnabled) {
                plugin.actions.forEach(actionDef => compileAndRegisterPluginAction(plugin.meta.id, actionDef));
            } else {
                unregisterActionsForPlugin(plugin.meta.id);
            }
            refreshActionDropdown();
            showNotification(`Plugin "${plugin.meta.name}" ${isEnabled ? 'enabled' : 'disabled'}. Remember to "Save All".`);
        }
    }

    // --- Settings UI Functions ---
    function saveCurrentSettings() {
        const panel = document.getElementById(CONFIG_PANEL_ID);
        if (!panel) return;
        const newGlobalRecordHotkey = panel.querySelector('#setting-global-record-hotkey').value.trim();
        if (newGlobalRecordHotkey) {
            currentSettings.globalRecordHotkey = newGlobalRecordHotkey;
            showNotification('Settings updated. Remember to "Save All".');
        } else {
            showNotification('Global Record Hotkey cannot be empty.', 'error');
        }
    }


    // --- Utility: Show Notification ---
    let notificationTimeout;
    function showNotification(message, type = 'info', duration = 3000) {
        let notification = document.getElementById('in-browser-ahk-notification');
        if (!notification) {
            notification = document.createElement('div');
            notification.id = 'in-browser-ahk-notification';
            document.body.appendChild(notification);
        }
        notification.textContent = message;
        notification.className = `ahk-notification ${type}`;
        notification.style.display = 'block';

        clearTimeout(notificationTimeout);
        if (duration > 0) {
            notificationTimeout = setTimeout(() => {
                notification.style.display = 'none';
            }, duration);
        }
    }

    // --- Utility: Escape HTML ---
    function escapeHtml(unsafe) {
        if (typeof unsafe !== 'string') return String(unsafe); // Coerce to string
        return unsafe
             .replace(/&/g, "&amp;")
             .replace(/</g, "&lt;")
             .replace(/>/g, "&gt;")
             .replace(/"/g, "&quot;")
             .replace(/'/g, "&#039;");
    }

    // --- Styles ---
    function addStyles() {
        GM_addStyle(`
            #${CONFIG_PANEL_ID} { /* ... existing styles ... */
                position: fixed; top: 50px; right: 20px; width: 650px; /* Wider */
                max-height: 90vh; background-color: #f0f0f0; border: 1px solid #ccc;
                border-radius: 8px; box-shadow: 0 4px 15px rgba(0,0,0,0.2);
                z-index: 99999; font-family: Arial, sans-serif; font-size: 14px;
                color: #333; display: block; overflow-y: auto;
            }
            #${CONFIG_PANEL_ID} .header { /* ... */
                background-color: #e0e0e0; padding: 10px 15px; border-bottom: 1px solid #ccc;
                border-top-left-radius: 8px; border-top-right-radius: 8px;
                display: flex; justify-content: space-between; align-items: center;
            }
            #${CONFIG_PANEL_ID} h2 { font-size: 1.2em; }
            #${CONFIG_PANEL_ID} h3 { margin: 15px 0 10px; font-size: 1.1em; }
            #${CONFIG_PANEL_ID} h4 { margin: 10px 0 5px; font-size: 1em; color: #555; }
            #${CONFIG_PANEL_ID} .close-btn { /* ... */
                background: none; border: none; font-size: 24px; font-weight: bold;
                cursor: pointer; color: #777;
            }
            #${CONFIG_PANEL_ID} .tabs { /* ... */
                display: flex; background-color: #e9e9e9; border-bottom: 1px solid #ccc;
            }
            #${CONFIG_PANEL_ID} .tab-button { /* ... */
                padding: 10px 15px; cursor: pointer; border: none;
                background-color: transparent; border-right: 1px solid #ccc;
            }
            #${CONFIG_PANEL_ID} .tab-button.active { /* ... */
                background-color: #f0f0f0; font-weight: bold;
            }
            #${CONFIG_PANEL_ID} .tab-content { padding: 15px; display: none; }
            #${CONFIG_PANEL_ID} .tab-content.active { display: block; }

            #${CONFIG_PANEL_ID} input[type="text"], #${CONFIG_PANEL_ID} select, #${CONFIG_PANEL_ID} textarea {
                width: calc(100% - 22px); padding: 8px 10px; margin-bottom: 10px;
                border: 1px solid #ccc; border-radius: 4px; box-sizing: border-box;
            }
            #${CONFIG_PANEL_ID} textarea { min-height: 80px; resize: vertical; }
            #${CONFIG_PANEL_ID} label { display: block; margin-bottom: 5px; font-weight: bold; }
            #${CONFIG_PANEL_ID} label input[type="checkbox"] { margin-right: 5px; vertical-align: middle; font-weight:normal;}
            #${CONFIG_PANEL_ID} .inline-label { display: inline-block; margin-right: 15px; font-weight:normal;}


            #${CONFIG_PANEL_ID} button { /* ... */
                padding: 8px 15px; background-color: #007bff; color: white;
                border: none; border-radius: 4px; cursor: pointer; margin-right: 5px; font-size: 14px;
            }
            #${CONFIG_PANEL_ID} button:hover { background-color: #0056b3; }
            #${CONFIG_PANEL_ID} .save-all-btn { /* ... */
                background-color: #28a745; display: block; width: calc(100% - 30px);
                margin: 20px auto 10px; padding: 12px; font-size: 16px;
            }
            #${CONFIG_PANEL_ID} .item-list table { /* ... */
                width: 100%; border-collapse: collapse; margin-bottom: 15px;
            }
            #${CONFIG_PANEL_ID} .item-list th, #${CONFIG_PANEL_ID} .item-list td { /* ... */
                border: 1px solid #ddd; padding: 8px; text-align: left; font-size: 0.95em;
            }
            #${CONFIG_PANEL_ID} .item-list th { background-color: #e9e9e9; }
            #${CONFIG_PANEL_ID} .edit-hs-btn, #${CONFIG_PANEL_ID} .edit-hk-btn { background-color: #ffc107; color: #333;}
            #${CONFIG_PANEL_ID} .delete-hs-btn, #${CONFIG_PANEL_ID} .delete-hk-btn, .delete-plugin-btn { background-color: #dc3545;}

            .help-text { font-size: 0.9em; color: #555; margin-top: 5px; padding: 8px; background-color: #e9e9f9; border-radius: 4px; border: 1px solid #c8c8dd;}
            .help-text code { background-color: #d4d4e4; padding: 2px 4px; border-radius: 3px; font-family: monospace; }
            .help-text pre { white-space: pre-wrap; word-break: break-all; max-height: 150px; overflow-y:auto; }

            #plugin-drop-zone {
                border: 2px dashed #ccc; border-radius: 5px; padding: 20px; text-align: center;
                margin-bottom: 15px; background-color: #f9f9f9; color: #777;
            }
            #plugin-drop-zone.dragover { border-color: #007bff; background-color: #e7f3ff; }

            #in-browser-ahk-notification { /* ... existing styles ... */
                position: fixed; bottom: 20px; left: 50%; transform: translateX(-50%);
                padding: 10px 20px; border-radius: 5px; color: white; z-index: 100000;
                font-size: 16px; box-shadow: 0 2px 10px rgba(0,0,0,0.2); display: none;
            }
            #in-browser-ahk-notification.info { background-color: #17a2b8; }
            #in-browser-ahk-notification.success { background-color: #28a745; }
            #in-browser-ahk-notification.error { background-color: #dc3545; }
        `);
    }

    // --- Initialization ---
    async function init() {
        // Load all data
        currentSettings = await loadData(STORAGE_KEY_SETTINGS, defaultSettings);
        hotstrings = await loadData(STORAGE_KEY_HOTSTRINGS, defaultHotstrings);
        hotkeys = await loadData(STORAGE_KEY_HOTKEYS, defaultHotkeys);
        const storedPlugins = await loadData(STORAGE_KEY_PLUGINS, defaultPlugins);

        // Initialize availableActions with core actions
        availableActions = { ...coreActions }; // Shallow copy is fine as functions are redefined if plugin reloads

        // Process stored plugins
        plugins = []; // Reset before loading
        storedPlugins.forEach(pluginData => {
            if (validatePluginData(pluginData)) { // Validate again on load
                 const pluginEntry = {
                    meta: { ...pluginData.pluginMeta }, // Ensure enabled field is handled
                    actions: pluginData.actions,
                    rawJson: pluginData.rawJson || JSON.stringify(pluginData) // Store raw for saving
                };
                plugins.push(pluginEntry);
                if (pluginEntry.meta.enabled !== false) { // If enabled is not explicitly false
                    pluginEntry.actions.forEach(actionDef => {
                        compileAndRegisterPluginAction(pluginEntry.meta.id, actionDef);
                    });
                }
            }
        });


        addStyles();

        document.addEventListener('keyup', handleHotstringInput, true);
        document.addEventListener('keydown', handleHotkey, true);

        GM_registerMenuCommand('In-Browser AHK Settings', createConfigPanel);

        console.log('In-Browser AHK v0.2 initialized.');
        showNotification('In-Browser AHK Active! (v0.2)', 'success');
    }

    init();

})();
