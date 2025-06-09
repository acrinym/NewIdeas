// ==UserScript==
// @name         In-Browser AHK (AutoHotkey-like Features)
// @namespace    http://tampermonkey.net/
// @version      0.2.2
// @description  Provides AutoHotkey-like hotstring, hotkey, workflow, and plugin functionality within the browser. With enhanced plugin settings, lifecycle hooks, and list search.
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
    const STORAGE_KEY_SETTINGS = 'inBrowserAhk_settings_v2';
    const STORAGE_KEY_PLUGIN_SETTINGS_PREFIX = 'inBrowserAhk_pluginSettings_';
    const STORAGE_KEY_PLUGIN_DATA_PREFIX = 'inBrowserAhk_pluginData_';

    const CONFIG_PANEL_ID = 'in-browser-ahk-config-panel';
    const PLUGIN_SETTINGS_MODAL_ID = 'in-browser-ahk-plugin-settings-modal';
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

    const defaultPlugins = [];

    const defaultSettings = {
        globalRecordHotkey: 'Control+Alt+Shift+R',
    };


    // --- Core Data ---
    let hotstrings = [];
    let hotkeys = [];
    let plugins = [];
    let currentSettings = { ...defaultSettings };

    let hotstringInputBuffer = '';
    let isRecordingHotkey = false;
    let capturedHotkeyForRecording = null;
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
            if (data === undefined || data === null || data === '') {
                return JSON.parse(JSON.stringify(defaultValue));
            }
            return JSON.parse(data);
        } catch (e) {
            console.error('In-Browser AHK: Error loading data for key', key, e);
            showNotification(`Error loading data for ${key}. Using defaults. Check console.`, 'error');
            return JSON.parse(JSON.stringify(defaultValue));
        }
    }

    // --- Plugin Settings and Data Storage ---
    async function loadPluginSettings(pluginId, settingDefinitions) {
        const storedSettings = await loadData(`${STORAGE_KEY_PLUGIN_SETTINGS_PREFIX}${pluginId}`, {});
        const resolvedSettings = {};
        if (Array.isArray(settingDefinitions)) {
            settingDefinitions.forEach(def => {
                resolvedSettings[def.id] = storedSettings.hasOwnProperty(def.id) ? storedSettings[def.id] : def.defaultValue;
            });
        }
        return resolvedSettings;
    }

    async function savePluginSettings(pluginId, settings) {
        await saveData(`${STORAGE_KEY_PLUGIN_SETTINGS_PREFIX}${pluginId}`, settings);
    }

    async function deletePluginSettings(pluginId) {
        try {
            await GM_deleteValue(`${STORAGE_KEY_PLUGIN_SETTINGS_PREFIX}${pluginId}`);
        } catch (e) {
            console.warn(`In-Browser AHK: Could not delete settings for plugin ${pluginId}. GM_deleteValue might not be available or an error occurred.`, e);
        }
    }

    async function getPluginStoredData(pluginId, key, defaultValue) {
        return await loadData(`${STORAGE_KEY_PLUGIN_DATA_PREFIX}${pluginId}_${key}`, defaultValue);
    }

    async function setPluginStoredData(pluginId, key, value) {
        await saveData(`${STORAGE_KEY_PLUGIN_DATA_PREFIX}${pluginId}_${key}`, value);
    }

    async function deletePluginStorageNamespace(pluginId) {
        console.warn(`In-Browser AHK: Deleting entire storage namespace for plugin ${pluginId} is not fully implemented. Settings are deleted.`);
        await deletePluginSettings(pluginId);
    }


    // --- Plugin Execution Context & Helpers ---
    function getPluginExecutionContext(plugin) {
        const pluginScopedHelpers = {
            ...customJsHelpers,
            getPluginStorage: async (key, defaultValue) => await getPluginStoredData(plugin.meta.id, key, defaultValue),
            setPluginStorage: async (key, value) => await setPluginStoredData(plugin.meta.id, key, value),
        };

        return {
            showNotification,
            GM_setClipboard,
            actions: availableActions,
            GM_setValue,
            GM_getValue,
            escapeHtml,
            helpers: pluginScopedHelpers,
            settings: plugin.currentSettings || {},
            pluginMeta: plugin.meta,
        };
    }

    async function executePluginCode(plugin, codeString, event = null, additionalArgs = {}) {
        if (!codeString || typeof codeString !== 'string') return;
        try {
            const context = getPluginExecutionContext(plugin);
            // For onSettingsChange, the 'settings' object in context should ideally be the 'newSettings'
            // The additionalArgs spread will handle passing oldSettings/newSettings correctly.
            // The 'settings' in the context will be the current settings of the plugin.
            const argsToPass = {
                ...context,
                // If newSettings is explicitly passed (like in onSettingsChange), it should be the primary 'settings' for that hook's execution scope.
                // However, plugin actions should always get the current `plugin.currentSettings`.
                // This distinction is subtle. For hooks, additionalArgs might override. For actions, context.settings is primary.
                // Let's refine: hooks get their specific args, actions get the standard context.
                // The `additionalArgs` will be directly available to the hook function.
                // The `settings` property in the context will always be `plugin.currentSettings`.
                event, // For actions
                ...additionalArgs // For hooks like onSettingsChange (oldSettings, newSettings)
            };

            const func = new Function(...Object.keys(argsToPass), codeString);
            await func(...Object.values(argsToPass));
            return true;
        } catch (e) {
            console.error(`In-Browser AHK: Error executing JS for plugin "${plugin.meta.name}" (Code: "${codeString.substring(0,100)}..."):`, e);
            showNotification(`Error in plugin ${plugin.meta.name}. Check console.`, 'error');
            return false;
        }
    }


    async function executePluginHook(plugin, hookName, hookEventArgs = {}) {
        if (plugin.meta && plugin.meta[hookName] && typeof plugin.meta[hookName] === 'string') {
            console.log(`In-Browser AHK: Executing ${hookName} for plugin ${plugin.meta.name}`);
            // Pass hookEventArgs directly, which might include oldSettings/newSettings for onSettingsChange
            // These will be available as top-level arguments in the hook's JS code.
            await executePluginCode(plugin, plugin.meta[hookName], null, hookEventArgs);
        }
    }


    // --- Action Registration & Execution ---
    function compileAndRegisterPluginAction(plugin, actionDefinition) {
        const actionKey = `plugin_${plugin.meta.id}_${actionDefinition.actionName}`;
        try {
            // The context here is for what's available at *compilation time* if the JS string was a template.
            // But more importantly, it defines the arguments for the new Function.
            const baseContextForCompilation = getPluginExecutionContext(plugin);
            const func = new Function(...Object.keys(baseContextForCompilation), 'event', actionDefinition.javascriptCode);

            availableActions[actionKey] = {
                func: async (event) => { // Actions can now be async
                    try {
                        // Get the most current context at *execution time*
                        const currentExecContext = getPluginExecutionContext(plugin);
                        await func(...Object.values(currentExecContext), event);
                    } catch (e) {
                        console.error(`In-Browser AHK: Runtime error in plugin action "${actionDefinition.actionName}" from plugin "${plugin.meta.id}":`, e);
                        showNotification(`Runtime error in action ${actionDefinition.actionName}. Check console.`, 'error');
                    }
                },
                description: actionDefinition.description || 'Plugin-defined action.',
                isPluginAction: true,
                pluginId: plugin.meta.id,
                displayName: actionDefinition.displayName || actionDefinition.actionName,
            };
            return true;
        } catch (e) {
            console.error(`In-Browser AHK: Error compiling JS for plugin action "${actionDefinition.actionName}" from plugin "${plugin.meta.id}":`, e);
            showNotification(`Error compiling action ${actionDefinition.actionName} from plugin ${plugin.meta.id}. Check console.`, 'error');
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
            if (plugin.meta.enabled !== false) {
                plugin.actions.forEach(actionDef => {
                    compileAndRegisterPluginAction(plugin, actionDef);
                });
            }
        });
    }

    const customJsHelpers = {
        clickElement: (selector) => {
            const elem = document.querySelector(selector);
            if (elem && typeof elem.click === 'function') {
                elem.click();
                return true;
            }
            showNotification(`Element not found or not clickable: ${selector}`, 'error');
            return false;
        },
        getElementValue: (selector) => {
            const elem = document.querySelector(selector);
            return elem ? elem.value : null;
        },
    };


    // --- Core Predefined Actions ---
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


    // --- Hotstring Engine ---
    function handleHotstringInput(event) {
        const target = event.target;
        if (!['INPUT', 'TEXTAREA'].includes(target.tagName) && !target.isContentEditable) {
            hotstringInputBuffer = '';
            return;
        }
        if (isRecordingHotkey) return;

        if (event.key && event.key.length === 1) {
            hotstringInputBuffer += event.key;
        } else if (event.key === 'Backspace') {
            hotstringInputBuffer = hotstringInputBuffer.slice(0, -1);
            return; // Don't process further for backspace
        } else if (event.key === ' ') {
            hotstringInputBuffer += ' ';
        } else if (['Enter', 'Tab'].includes(event.key)) {
            // Allow these to potentially trigger a hotstring if they are part of it
        } else {
            // Any other non-character key (Shift, Ctrl, Alt, Arrow keys etc.) should not be part of the buffer
            // or reset it, depending on desired behavior. For now, we just don't add them.
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

                if (target.value !== undefined) { // For INPUT and TEXTAREA
                    const cursorPos = target.selectionStart;
                    const textBefore = target.value.substring(0, cursorPos - triggerLen);
                    const textAfter = target.value.substring(cursorPos);
                    target.value = textBefore + replacementText + textAfter;
                    target.selectionStart = target.selectionEnd = textBefore.length + replacementText.length;
                } else if (target.isContentEditable) { // For contentEditable elements
                    // More robust replacement for contentEditable
                    const sel = window.getSelection();
                    if (sel.rangeCount > 0) {
                        const range = sel.getRangeAt(0);
                        // Check if the range is within the event target
                        if (target.contains(range.commonAncestorContainer)) {
                            range.setStart(range.endContainer, range.endOffset - triggerLen);
                            range.deleteContents();
                            range.insertNode(document.createTextNode(replacementText));
                            // Move cursor after inserted text
                            range.setStartAfter(range.endContainer.lastChild || range.endContainer); // Ensure cursor is after the node
                            range.collapse(true);
                            sel.removeAllRanges(); // Deselect old range
                            sel.addRange(range); // Reselect new range (collapsed)
                        } else { // Fallback if selection is weird, less precise
                            document.execCommand('insertText', false, ' '); // Placeholder to remove trigger
                            for(let i=0; i < triggerLen; i++) document.execCommand('deleteBackward', false, null);
                            document.execCommand('insertText', false, replacementText);
                        }
                    }
                }
                hotstringInputBuffer = ''; // Reset buffer after successful replacement
                event.preventDefault(); // Prevent the triggering character from being typed
                return; // Hotstring processed
            }
        }
    }

    // --- Hotkey Engine ---
    function formatPressedKeys(event) {
        const pressedKeys = [];
        if (event.ctrlKey) pressedKeys.push('Control');
        if (event.altKey) pressedKeys.push('Alt');
        if (event.shiftKey) pressedKeys.push('Shift');
        if (event.metaKey) pressedKeys.push('Meta'); // Command key on Mac, Windows key on Windows

        let keyName = event.key;
        if (keyName === ' ') keyName = 'Space';
        else if (keyName.length === 1 && keyName.match(/[a-z]/i)) keyName = keyName.toUpperCase();
        // Add normalizations for other keys if needed (e.g. ArrowUp, MediaPlayPause)
        // For now, event.key provides reasonable values for most common keys.

        // Only add the actual key if it's not a modifier itself (to avoid duplicates like "Control+Control")
        if (!['Control', 'Alt', 'Shift', 'Meta', 'Hyper', 'Super'].includes(keyName)) {
            pressedKeys.push(keyName);
        }

        const nonModifierKeyPresent = pressedKeys.some(k => !['Control', 'Alt', 'Shift', 'Meta'].includes(k));

        // A hotkey must have at least one non-modifier key, or be a single key press like "F1"
        if (pressedKeys.length === 0 || (!nonModifierKeyPresent && pressedKeys.length > 0 && pressedKeys.every(k => ['Control', 'Alt', 'Shift', 'Meta'].includes(k)) ) ) {
            // If no keys were collected, or only modifiers were collected
            if (pressedKeys.length === 0 && !['Control', 'Alt', 'Shift', 'Meta'].includes(keyName)) {
                // This handles single key presses like 'A', 'F1' which don't set modifier flags but keyName is the key
                pressedKeys.push(keyName);
            } else if (!nonModifierKeyPresent) {
                return null; // Only modifiers (e.g., Ctrl+Shift) without a main key
            }
        }
        if (pressedKeys.length === 0) return null; // Should not happen if logic above is correct

        return pressedKeys.sort().join('+');
    }


    function handleHotkey(event) {
        const target = event.target;
        const isInputTarget = ['INPUT', 'TEXTAREA'].includes(target.tagName) || target.isContentEditable;

        const pressedKeysStr = formatPressedKeys(event);
        if (!pressedKeysStr) return;

        if (isRecordingHotkey) {
            event.preventDefault();
            event.stopPropagation();
            capturedHotkeyForRecording = pressedKeysStr;
            showNotification(`Captured: ${pressedKeysStr}. Now select an action in the panel.`, 'info', 0);
            isRecordingHotkey = false;
            updateRecordHotkeyStatusUI(false);

            const panel = document.getElementById(CONFIG_PANEL_ID);
            if (panel && panel.style.display !== 'none') {
                panel.querySelector('#hk-keys').value = capturedHotkeyForRecording;
                panel.querySelector('#hk-keys').focus();
            }
            return;
        }

        if (pressedKeysStr === currentSettings.globalRecordHotkey) {
            event.preventDefault();
            event.stopPropagation();
            startRecordingHotkeyMode();
            return;
        }

        for (const hk of hotkeys) {
            if (!hk.enabled) continue;
            const normalizedHkKeys = hk.keys.split('+')
            .map(k => k.length === 1 ? k.toUpperCase() : k)
            .sort().join('+');
            const normalizedPressedKeys = pressedKeysStr.split('+').sort().join('+'); // Also sort pressed keys for robust comparison

            if (normalizedHkKeys === normalizedPressedKeys) {
                // Prevent simple character hotkeys (no modifiers) from firing in input fields
                // unless they are specific non-character keys like function keys, Escape, Tab.
                if (isInputTarget && !hk.keys.match(/Control|Alt|Meta|Shift/i) && hk.keys.split('+').length === 1) {
                    const singleKey = hk.keys.split('+')[0];
                    if (!['Escape', 'F1', 'F2', 'F3', 'F4', 'F5', 'F6', 'F7', 'F8', 'F9', 'F10', 'F11', 'F12', 'Tab', 'Enter', 'Backspace', 'Delete', 'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'PageUp', 'PageDown', 'Insert', 'Pause', 'ScrollLock', 'PrintScreen', 'CapsLock', 'NumLock'].includes(singleKey)) {
                        // If it's a letter, number, or symbol key without modifiers, and we're in an input, skip it.
                        if (singleKey.length === 1 && singleKey.match(/\S/)) { // Check if it's a single non-whitespace character
                            continue;
                        }
                    }
                }

                event.preventDefault();
                event.stopPropagation();

                if (hk.action === CUSTOM_JS_ACTION_ID && hk.customJS) {
                    try {
                        const customFunc = new Function('event', 'showNotification', 'GM_setClipboard', 'actions', 'GM_setValue', 'GM_getValue', 'escapeHtml', 'helpers', hk.customJS);
                        customFunc(event, showNotification, GM_setClipboard, availableActions, GM_setValue, GM_getValue, escapeHtml, customJsHelpers);
                    } catch (e) {
                        console.error(`In-Browser AHK: Error executing custom JS for hotkey "${hk.keys}":`, e);
                        showNotification(`Error in custom JS for ${hk.keys}: ${e.message}`, 'error');
                    }
                } else if (availableActions[hk.action] && typeof availableActions[hk.action].func === 'function') {
                    try {
                        if (availableActions[hk.action].isPluginAction) {
                            availableActions[hk.action].func(event); // Plugin actions get context from their compilation scope
                        } else {
                            // Core actions might expect a simpler context or no context
                            availableActions[hk.action].func(event, showNotification, GM_setClipboard);
                        }
                    } catch (e) {
                        console.error(`In-Browser AHK: Error executing action "${hk.action}":`, e);
                        showNotification(`Error in action: ${hk.action}`, 'error');
                    }
                } else {
                    console.warn(`In-Browser AHK: Action "${hk.action}" not found or not a function for hotkey "${hk.keys}".`);
                    showNotification(`Action "${hk.action}" not found.`, 'error');
                }
                return; // Hotkey processed
            }
        }
    }

    function startRecordingHotkeyMode() {
        isRecordingHotkey = true;
        capturedHotkeyForRecording = null;
        showNotification('Recording next hotkey... Press the desired key combination.', 'info', 0);
        updateRecordHotkeyStatusUI(true);
    }

    function updateRecordHotkeyStatusUI(isRecording) {
        const panel = document.getElementById(CONFIG_PANEL_ID);
        if (panel && panel.style.display !== 'none') {
            const recordBtn = panel.querySelector('#hk-record-btn');
            const statusDiv = panel.querySelector('#hk-record-status');
            if (recordBtn) recordBtn.textContent = isRecording ? 'Cancel Recording' : 'Record New Hotkey';
            if (statusDiv) statusDiv.textContent = isRecording ? 'Press desired hotkey combination now...' : (capturedHotkeyForRecording ? `Captured: ${capturedHotkeyForRecording}` : '');
        }

        let recordNotif = document.getElementById(RECORD_HOTKEY_NOTIFICATION_ID);
        if (isRecording) {
            if (!recordNotif) {
                recordNotif = document.createElement('div');
                recordNotif.id = RECORD_HOTKEY_NOTIFICATION_ID;
                Object.assign(recordNotif.style, {
                    position: 'fixed', top: '10px', left: '50%', transform: 'translateX(-50%)',
                    padding: '10px 20px', backgroundColor: 'rgba(0,0,200,0.8)', color: 'white',
                    zIndex: '100001', borderRadius: '5px', display: 'block'
                });
                document.body.appendChild(recordNotif);
            }
            recordNotif.textContent = 'RECORDING HOTKEY... (Press desired keys or your global record hotkey again to cancel)';
            recordNotif.style.display = 'block';
        } else {
            if (recordNotif) recordNotif.style.display = 'none';
        }
    }


    // --- UI: Configuration Panel ---
    function createConfigPanel() {
        let panel = document.getElementById(CONFIG_PANEL_ID);
        if (panel) {
            panel.style.display = panel.style.display === 'none' ? 'block' : 'none';
            if (panel.style.display === 'block') {
                refreshActionDropdown();
                renderConfigPanel(); // This will also re-apply filters if panel is re-shown
            }
            return;
        }

        panel = document.createElement('div');
        panel.id = CONFIG_PANEL_ID;
        panel.innerHTML = `
            <div class="header">
                <h2>In-Browser AHK Settings (v${GM_info.script.version})</h2>
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
                <input type="text" class="search-filter" data-list-table-body-id="hotstring-list-tbody" placeholder="Search Triggers or Replacements..." style="margin-bottom: 10px; width: calc(100% - 22px);">
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
                <input type="text" class="search-filter" data-list-table-body-id="hotkey-list-tbody" placeholder="Search Keys, Actions, or Descriptions..." style="margin-bottom: 10px; width: calc(100% - 22px);">
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
                        <code>escapeHtml(str)</code>, <code>helpers</code> (global custom utility object: ${Object.keys(customJsHelpers).join(', ')}).
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
                <input type="text" class="search-filter" data-list-table-body-id="plugin-list-tbody" placeholder="Search Names or Descriptions..." style="margin-bottom: 10px; width: calc(100% - 22px);">
                <div id="plugin-drop-zone">Drag & Drop Plugin JSON/TXT File Here</div>
                <div id="plugin-list" class="item-list"></div>
                <h4>Plugin Help</h4>
                <div class="help-text">
                    Plugins are JSON files. Structure:
                    <pre style="font-size:0.85em; padding:5px; border:1px solid #ccc; background:#fff;">${escapeHtml(`{
  "pluginMeta": {
    "id": "unique-plugin-id",
    "name": "My Plugin",
    "version": "1.0",
    "author": "You",
    "description": "What this plugin does.",
    "settings": [
      { "id": "myText", "name": "My Text Setting", "type": "text", "defaultValue": "Hello", "description": "A sample text input." },
      { "id": "myBool", "name": "Enable Awesome", "type": "checkbox", "defaultValue": true },
      { "id": "myNumber", "name": "Item Count", "type": "number", "defaultValue": 5 },
      { "id": "mySelect", "name": "Choose Option", "type": "select", "defaultValue": "b",
        "options": [{"value": "a", "label": "Option A"}, {"value": "b", "label": "Option B"}]
      }
    ],
    "onLoad": "console.log(pluginMeta.name + ' loaded with setting: ' + settings.myText); await helpers.setPluginStorage('loadedAt', Date.now());",
    "onEnable": "showNotification(pluginMeta.name + ' enabled. Option: ' + settings.mySelect, 'info');",
    "onDisable": "console.log(pluginMeta.name + ' disabled.');",
    "onSettingsChange": "console.log(pluginMeta.name + ' settings updated. New: ' + newSettings.myText + ', Old: ' + oldSettings.myText + '. Full new settings:', newSettings);"
  },
  "actions": [
    {
      "actionName": "myCoolAction",
      "displayName": "Do My Cool Thing",
      "description": "Uses plugin settings and storage.",
      "javascriptCode": "showNotification('Action from ' + pluginMeta.name + '! Setting: ' + settings.myText + ', Stored Value: ' + await helpers.getPluginStorage('someKey', 0)); await helpers.setPluginStorage('someKey', (await helpers.getPluginStorage('someKey', 0) || 0) + 1);"
    }
  ]
}`)}</pre>
                    Available in <code>javascriptCode</code> for plugin actions and hooks:
                    <code>event</code> (for actions), <code>showNotification(msg, type)</code>, <code>GM_setClipboard(data, type)</code>,
                    <code>actions</code> (all script actions), <code>GM_setValue/GM_getValue</code> (global script storage), <code>escapeHtml(str)</code>,
                    <code>helpers</code> (obj with global helpers like <code>clickElement</code>, PLUS plugin-specific <code>getPluginStorage(key, def)</code>, <code>setPluginStorage(key, val)</code>),
                    <code>settings</code> (this plugin's current settings object), <code>pluginMeta</code> (this plugin's metadata).
                    For <code>onSettingsChange</code> hook, you also get arguments <code>oldSettings</code> and <code>newSettings</code>. Plugin code can be <code>async</code>.
                </div>
            </div>

            <div id="settings-tab" class="tab-content">
                <h3>General Settings</h3>
                <label for="setting-global-record-hotkey">Global Hotkey to Start Recording:</label>
                <input type="text" id="setting-global-record-hotkey" placeholder="e.g., Control+Alt+Shift+R">
                <button id="setting-save-btn">Save Settings</button>
                 <div class="help-text">
                    Press this key combination anywhere in the browser to start "listening" for the next key combination,
                    which will then be pre-filled in the "Add/Edit Hotkey" form if the panel is open.
                </div>
            </div>

            <button id="save-all-to-storage-btn" class="save-all-btn">Save All Configurations to Storage</button>
        `;
        document.body.appendChild(panel);

        panel.querySelector('#in-browser-ahk-close-btn').addEventListener('click', () => {
            panel.style.display = 'none';
            if (isRecordingHotkey) {
                isRecordingHotkey = false;
                updateRecordHotkeyStatusUI(false);
            }
        });

        panel.querySelectorAll('.tab-button').forEach(button => {
            button.addEventListener('click', (e) => {
                panel.querySelectorAll('.tab-button, .tab-content').forEach(el => el.classList.remove('active'));
                e.target.classList.add('active');
                panel.querySelector(`#${e.target.dataset.tab}-tab`).classList.add('active');
                if (e.target.dataset.tab === 'hotkeys' || e.target.dataset.tab === 'plugins') {
                    refreshActionDropdown(); // Refresh if actions might have changed
                }
                renderConfigPanel(); // Always re-render to apply filters and update lists
            });
        });

        panel.querySelector('#hs-save-btn').addEventListener('click', saveHotstring);
        panel.querySelector('#hs-clear-btn').addEventListener('click', clearHotstringForm);

        panel.querySelector('#hk-save-btn').addEventListener('click', saveHotkey);
        panel.querySelector('#hk-clear-btn').addEventListener('click', clearHotkeyForm);
        panel.querySelector('#hk-record-btn').addEventListener('click', () => {
            if (isRecordingHotkey) {
                isRecordingHotkey = false;
                capturedHotkeyForRecording = null;
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


        const dropZone = panel.querySelector('#plugin-drop-zone');
        dropZone.addEventListener('dragover', (e) => { e.preventDefault(); e.stopPropagation(); dropZone.classList.add('dragover'); });
        dropZone.addEventListener('dragleave', (e) => { e.preventDefault(); e.stopPropagation(); dropZone.classList.remove('dragover'); });
        dropZone.addEventListener('drop', handlePluginDrop);

        panel.querySelector('#setting-save-btn').addEventListener('click', saveCurrentSettings);
        const globalRecordHotkeyInput = panel.querySelector('#setting-global-record-hotkey');
        if (globalRecordHotkeyInput) globalRecordHotkeyInput.value = currentSettings.globalRecordHotkey;


        panel.querySelector('#save-all-to-storage-btn').addEventListener('click', async () => {
            await saveData(STORAGE_KEY_HOTSTRINGS, hotstrings);
            await saveData(STORAGE_KEY_HOTKEYS, hotkeys);
            await saveData(STORAGE_KEY_PLUGINS, plugins.map(p => ({meta: p.meta, actions: p.actions, rawJson: p.rawJson})));
            for (const plugin of plugins) {
                if (plugin.currentSettings) {
                    await savePluginSettings(plugin.meta.id, plugin.currentSettings);
                }
            }
            await saveData(STORAGE_KEY_SETTINGS, currentSettings);
            showNotification('All configurations saved to Tampermonkey storage!', 'success');
            renderConfigPanel(); // Re-render to reflect saved state
        });

        refreshActionDropdown();
        renderConfigPanel(); // Initial render of lists
        setupSearchFilters(panel); // Initialize search filters after panel and lists are ready
    }

    // Function for setting up search filters
    function setupSearchFilters(panelElement) {
        panelElement.querySelectorAll('input.search-filter').forEach(inputField => {
            const tableBodyId = inputField.dataset.listTableBodyId;
            const tableBody = panelElement.querySelector(`#${tableBodyId}`);

            if (!tableBody) {
                console.warn("In-Browser AHK: Search filter setup - could not find table body with ID:", tableBodyId);
                return;
            }

            inputField.addEventListener('input', (e) => {
                const searchTerm = e.target.value.toLowerCase().trim();
                const rows = tableBody.querySelectorAll('tr');

                rows.forEach(row => {
                    let rowMatches = false;
                    if (searchTerm === '') {
                        rowMatches = true; // Show all rows if search is empty
                    } else {
                        if (tableBodyId.includes('hotstring')) {
                            const trigger = row.cells[0] ? row.cells[0].textContent.toLowerCase() : "";
                            const replacement = row.cells[1] ? row.cells[1].textContent.toLowerCase() : "";
                            rowMatches = trigger.includes(searchTerm) || replacement.includes(searchTerm);
                        } else if (tableBodyId.includes('hotkey')) {
                            const keys = row.cells[0] ? row.cells[0].textContent.toLowerCase() : "";
                            const actionText = row.cells[1] ? row.cells[1].textContent.toLowerCase() : "";
                            const description = row.cells[2] ? row.cells[2].textContent.toLowerCase() : "";
                            rowMatches = keys.includes(searchTerm) || actionText.includes(searchTerm) || description.includes(searchTerm);
                        } else if (tableBodyId.includes('plugin')) {
                            const name = row.cells[0] ? row.cells[0].textContent.toLowerCase() : "";
                            const description = row.cells[2] ? row.cells[2].textContent.toLowerCase() : ""; // Description is cell index 2
                            rowMatches = name.includes(searchTerm) || description.includes(searchTerm);
                        }
                    }
                    row.style.display = rowMatches ? '' : 'none';
                });
            });
        });
    }


    function refreshActionDropdown() {
        const panel = document.getElementById(CONFIG_PANEL_ID);
        if (!panel) return;
        const actionSelect = panel.querySelector('#hk-action');
        const currentVal = actionSelect.value;
        actionSelect.innerHTML = '';

        const customOpt = document.createElement('option');
        customOpt.value = CUSTOM_JS_ACTION_ID;
        customOpt.textContent = '-- Custom JavaScript --';
        actionSelect.appendChild(customOpt);

        const sortedActionKeys = Object.keys(availableActions).sort((a, b) => {
            const actionA = availableActions[a];
            const actionB = availableActions[b];
            const nameA = (actionA.displayName || a).toLowerCase();
            const nameB = (actionB.displayName || b).toLowerCase();
            if (actionA.isPluginAction && !actionB.isPluginAction) return 1;
            if (!actionA.isPluginAction && actionB.isPluginAction) return -1;
            return nameA.localeCompare(nameB);
        });

        for (const actionName of sortedActionKeys) {
            const action = availableActions[actionName];
            const option = document.createElement('option');
            option.value = actionName;
            let displayName = action.displayName || actionName;
            if (action.isPluginAction) {
                const plugin = plugins.find(p => p.meta.id === action.pluginId);
                displayName = `[P] ${plugin ? plugin.meta.name : action.pluginId.substring(0,10)}: ${displayName}`;
            }
            let descriptionHint = action.description || 'No description';
            if (descriptionHint.length > 70) descriptionHint = descriptionHint.substring(0, 67) + '...';
            option.textContent = `${displayName} (${descriptionHint})`;
            option.title = action.description || 'No description';
            actionSelect.appendChild(option);
        }
        actionSelect.value = currentVal;
        if (!actionSelect.value && actionSelect.options.length > 0) { // If previous val not found
            actionSelect.selectedIndex = 0; // Default to first option (usually Custom JS)
        }
        actionSelect.dispatchEvent(new Event('change')); // To update custom JS area visibility
    }


    function renderConfigPanel() {
        const panel = document.getElementById(CONFIG_PANEL_ID);
        if (!panel || panel.style.display === 'none') return;

        renderHotstringList(panel);
        renderHotkeyList(panel);
        renderPluginList(panel);
        const globalRecordHotkeyInput = panel.querySelector('#setting-global-record-hotkey');
        if (globalRecordHotkeyInput) globalRecordHotkeyInput.value = currentSettings.globalRecordHotkey;

        // Re-apply filters if any search input has a value
        panel.querySelectorAll('input.search-filter').forEach(inputField => {
            if (inputField.value) { // If there's an active search term
                inputField.dispatchEvent(new Event('input')); // Trigger the input event to re-apply filter
            }
        });
    }

    // --- Hotstring UI Functions ---
    function renderHotstringList(panel) {
        const listDiv = panel.querySelector('#hotstring-list');
        // Ensure tbody has an ID for the search filter
        listDiv.innerHTML = '<table><thead><tr><th>Trigger</th><th>Replacement</th><th>Case</th><th>Enabled</th><th>Actions</th></tr></thead><tbody id="hotstring-list-tbody"></tbody></table>';
        const tbody = listDiv.querySelector('#hotstring-list-tbody');
        if (!tbody) { console.error("hotstring-list-tbody not found!"); return; }
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
    function clearHotstringForm() {
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
    function saveHotstring() {
        const panel = document.getElementById(CONFIG_PANEL_ID);
        if (!panel) return;
        const id = panel.querySelector('#hs-id').value;
        const trigger = panel.querySelector('#hs-trigger').value.trim();
        const replacement = panel.querySelector('#hs-replacement').value;
        const caseSensitive = panel.querySelector('#hs-case-sensitive').checked;
        const enabled = panel.querySelector('#hs-enabled').checked;

        if (!trigger) { showNotification('Hotstring trigger cannot be empty.', 'error'); return; }

        if (id) {
            const index = hotstrings.findIndex(hs => hs.id === id);
            if (index > -1) hotstrings[index] = { ...hotstrings[index], trigger, replacement, caseSensitive, enabled };
        } else {
            const newId = `hs_${Date.now()}_${Math.random().toString(36).substring(2,7)}`;
            hotstrings.push({ id: newId, trigger, replacement, caseSensitive, enabled });
        }
        clearHotstringForm();
        renderHotstringList(panel); // Re-render the list
        // Re-apply filter to the hotstring list
        const hotstringSearchInput = panel.querySelector('input.search-filter[data-list-table-body-id="hotstring-list-tbody"]');
        if (hotstringSearchInput && hotstringSearchInput.value) { // Check if input exists and has a value
            hotstringSearchInput.dispatchEvent(new Event('input'));
        }

        showNotification(`Hotstring ${id ? 'updated' : 'added'}. Remember to "Save All".`);
    }
    function editHotstring(id) {
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
        panel.querySelector('#hs-trigger').focus();
    }
    function deleteHotstring(id) {
        if (!confirm('Are you sure you want to delete this hotstring?')) return;
        hotstrings = hotstrings.filter(hs => hs.id !== id);
        const panel = document.getElementById(CONFIG_PANEL_ID);
        if (panel) {
            renderHotstringList(panel); // Re-render the list
            const hotstringSearchInput = panel.querySelector('input.search-filter[data-list-table-body-id="hotstring-list-tbody"]');
            if (hotstringSearchInput && hotstringSearchInput.value) { // Check if input exists and has a value
                hotstringSearchInput.dispatchEvent(new Event('input'));
            }
        }
        showNotification('Hotstring deleted. Remember to "Save All".');
    }
    function toggleHotstringEnable(id, isEnabled) {
        const hs = hotstrings.find(h => h.id === id);
        if (hs) {
            hs.enabled = isEnabled;
            showNotification(`Hotstring ${hs.trigger} ${isEnabled ? 'enabled' : 'disabled'}. Remember to "Save All".`);
        }
    }

    // --- Hotkey UI Functions ---
    function renderHotkeyList(panel) {
        const listDiv = panel.querySelector('#hotkey-list');
        // Ensure tbody has an ID for the search filter
        listDiv.innerHTML = '<table><thead><tr><th>Keys</th><th>Action/CustomJS</th><th>Description</th><th>Enabled</th><th>Actions</th></tr></thead><tbody id="hotkey-list-tbody"></tbody></table>';
        const tbody = listDiv.querySelector('#hotkey-list-tbody');
        if (!tbody) { console.error("hotkey-list-tbody not found!"); return; }
        hotkeys.forEach((hk) => {
            const row = tbody.insertRow();
            row.dataset.id = hk.id;
            let actionDisplay = 'N/A';
            let actionTitle = hk.description || '';

            if (hk.action === CUSTOM_JS_ACTION_ID) {
                actionDisplay = `Custom JS`;
                actionTitle = hk.customJS ? `Custom JS: ${hk.customJS.substring(0,100)}${hk.customJS.length > 100 ? '...' : ''}` : 'Custom JS (No code)';
            } else if (availableActions[hk.action]) {
                actionDisplay = availableActions[hk.action].displayName || hk.action;
                if (availableActions[hk.action].isPluginAction) {
                    const plugin = plugins.find(p => p.meta.id === availableActions[hk.action].pluginId);
                    actionDisplay = `[P] ${plugin ? plugin.meta.name : 'Plugin'}: ${actionDisplay}`;
                }
                actionTitle = availableActions[hk.action].description || actionDisplay;
            }

            row.innerHTML = `
                <td>${escapeHtml(hk.keys)}</td>
                <td title="${escapeHtml(actionTitle)}">${escapeHtml(actionDisplay)}</td>
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
    function clearHotkeyForm() {
        const panel = document.getElementById(CONFIG_PANEL_ID);
        if (!panel) return;
        panel.querySelector('#hk-id').value = '';
        panel.querySelector('#hk-keys').value = '';
        if (panel.querySelector('#hk-action').options.length > 0) {
            panel.querySelector('#hk-action').selectedIndex = 0;
        }
        panel.querySelector('#hk-custom-js').value = '';
        panel.querySelector('#hk-custom-js-area').style.display = panel.querySelector('#hk-action').value === CUSTOM_JS_ACTION_ID ? 'block' : 'none';
        panel.querySelector('#hk-description').value = '';
        panel.querySelector('#hk-enabled').checked = true;
        panel.querySelector('#hk-save-btn').textContent = 'Save Hotkey';
        panel.querySelector('#hk-clear-btn').style.display = 'none';
        capturedHotkeyForRecording = null;
        updateRecordHotkeyStatusUI(false);
    }
    function saveHotkey() {
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
        if (action !== CUSTOM_JS_ACTION_ID && !availableActions[action] && action !== "") {
            showNotification('Selected action is not valid.', 'error'); return;
        }

        const hotkeyData = { keys, action, customJS, description, enabled };

        if (id) {
            const index = hotkeys.findIndex(hk => hk.id === id);
            if (index > -1) hotkeys[index] = { ...hotkeys[index], ...hotkeyData };
        } else {
            const newId = `hk_${Date.now()}_${Math.random().toString(36).substring(2,7)}`;
            hotkeys.push({ id: newId, ...hotkeyData });
        }
        clearHotkeyForm();
        renderHotkeyList(panel); // Re-render the list
        const hotkeySearchInput = panel.querySelector('input.search-filter[data-list-table-body-id="hotkey-list-tbody"]');
        if (hotkeySearchInput && hotkeySearchInput.value) { // Check if input exists and has a value
            hotkeySearchInput.dispatchEvent(new Event('input'));
        }
        showNotification(`Hotkey ${id ? 'updated' : 'added'}. Remember to "Save All".`);
    }
    function editHotkey(id) {
        const panel = document.getElementById(CONFIG_PANEL_ID);
        if (!panel) return;
        const hk = hotkeys.find(h => h.id === id);
        if (!hk) return;

        refreshActionDropdown(); // Ensure dropdown is current

        panel.querySelector('#hk-id').value = hk.id;
        panel.querySelector('#hk-keys').value = hk.keys;
        panel.querySelector('#hk-action').value = hk.action; // Set after options are populated
        panel.querySelector('#hk-custom-js').value = hk.customJS || '';
        panel.querySelector('#hk-custom-js-area').style.display = hk.action === CUSTOM_JS_ACTION_ID ? 'block' : 'none';
        panel.querySelector('#hk-description').value = hk.description;
        panel.querySelector('#hk-enabled').checked = hk.enabled;
        panel.querySelector('#hk-save-btn').textContent = 'Update Hotkey';
        panel.querySelector('#hk-clear-btn').style.display = 'inline-block';
        panel.querySelector('#hk-keys').focus();
        panel.querySelector('#hk-action').dispatchEvent(new Event('change')); // Ensure UI updates
    }
    function deleteHotkey(id) {
        if (!confirm('Are you sure you want to delete this hotkey?')) return;
        hotkeys = hotkeys.filter(hk => hk.id !== id);
        const panel = document.getElementById(CONFIG_PANEL_ID);
        if (panel) {
            renderHotkeyList(panel); // Re-render the list
            const hotkeySearchInput = panel.querySelector('input.search-filter[data-list-table-body-id="hotkey-list-tbody"]');
            if (hotkeySearchInput && hotkeySearchInput.value) { // Check if input exists and has a value
                hotkeySearchInput.dispatchEvent(new Event('input'));
            }
        }
        showNotification('Hotkey deleted. Remember to "Save All".');
    }
    function toggleHotkeyEnable(id, isEnabled) {
        const hk = hotkeys.find(h => h.id === id);
        if (hk) {
            hk.enabled = isEnabled;
            showNotification(`Hotkey ${hk.keys} ${isEnabled ? 'enabled' : 'disabled'}. Remember to "Save All".`);
        }
    }

    // --- Plugin UI & Logic Functions ---
    async function handlePluginDrop(event) {
        event.preventDefault();
        event.stopPropagation();
        const dropZone = event.target.closest('#plugin-drop-zone');
        if (dropZone) dropZone.classList.remove('dragover');

        const files = event.dataTransfer.files;
        if (files.length > 0) {
            const file = files[0];
            if (file.type === 'application/json' || file.name.endsWith('.json') || file.name.endsWith('.txt')) {
                const reader = new FileReader();
                reader.onload = async (e) => {
                    try {
                        const pluginJson = e.target.result;
                        const pluginData = JSON.parse(pluginJson);

                        if (validatePluginData(pluginData)) {
                            const existingPluginIndex = plugins.findIndex(p => p.meta.id === pluginData.pluginMeta.id);
                            let isNewPlugin = true;
                            let previousEnabledState = true;

                            if (existingPluginIndex > -1) {
                                isNewPlugin = false;
                                previousEnabledState = plugins[existingPluginIndex].meta.enabled;
                                if (!confirm(`Plugin "${pluginData.pluginMeta.name}" already exists. Overwrite? Its current enabled state and settings (if compatible) will be preserved.`)) return;
                                if (plugins[existingPluginIndex].meta.enabled) {
                                    await executePluginHook(plugins[existingPluginIndex], 'onDisable');
                                }
                                unregisterActionsForPlugin(plugins[existingPluginIndex].meta.id);
                            }

                            const loadedSettings = await loadPluginSettings(pluginData.pluginMeta.id, pluginData.pluginMeta.settings);

                            const newPluginEntry = {
                                meta: { ...pluginData.pluginMeta, enabled: previousEnabledState },
                                actions: pluginData.actions,
                                rawJson: pluginJson,
                                currentSettings: loadedSettings
                            };

                            if (existingPluginIndex > -1) {
                                plugins[existingPluginIndex] = newPluginEntry;
                            } else {
                                plugins.push(newPluginEntry);
                            }

                            if (newPluginEntry.meta.enabled !== false) {
                                newPluginEntry.actions.forEach(actionDef => compileAndRegisterPluginAction(newPluginEntry, actionDef));
                                if (isNewPlugin) {
                                    await executePluginHook(newPluginEntry, 'onLoad');
                                } else {
                                    await executePluginHook(newPluginEntry, 'onEnable'); // Re-enable if it was updated and enabled
                                }
                            }
                            renderConfigPanel(); // This will re-render lists and apply filters
                            refreshActionDropdown(); // Actions might have changed
                            showNotification(`Plugin "${pluginData.pluginMeta.name}" ${isNewPlugin ? 'loaded' : 'updated'}. Remember to "Save All".`, 'success');
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
        if (!Array.isArray(data.actions)) { // Actions array can be empty if plugin only uses hooks/settings
            data.actions = []; // Ensure it's an array for consistency
        }
        if (data.pluginMeta.settings && !Array.isArray(data.pluginMeta.settings)) {
            showNotification(`Plugin "${data.pluginMeta.name}" error: 'settings' must be an array if present.`, 'error'); return false;
        }
        if (data.pluginMeta.settings) {
            for (const setting of data.pluginMeta.settings) {
                if (!setting.id || !setting.name || !setting.type) {
                    showNotification(`Plugin "${data.pluginMeta.name}" error: Each setting must have id, name, and type.`, 'error'); return false;
                }
                if (setting.type === 'select' && (!Array.isArray(setting.options) || setting.options.some(opt => typeof opt.value === 'undefined' || typeof opt.label === 'undefined'))) {
                    showNotification(`Plugin "${data.pluginMeta.name}" error: Select setting '${setting.id}' must have valid options array.`, 'error'); return false;
                }
            }
        }
        const hooks = ['onLoad', 'onEnable', 'onDisable', 'onSettingsChange'];
        for (const hook of hooks) {
            if (data.pluginMeta[hook] && typeof data.pluginMeta[hook] !== 'string') {
                showNotification(`Plugin "${data.pluginMeta.name}" error: Hook '${hook}' must be a string of JavaScript code if defined.`, 'error'); return false;
            }
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
        // Ensure tbody has an ID for the search filter
        listDiv.innerHTML = '<table><thead><tr><th>Name</th><th>Ver.</th><th>Description</th><th>Enabled</th><th>Actions</th></tr></thead><tbody id="plugin-list-tbody"></tbody></table>';
        const tbody = listDiv.querySelector('#plugin-list-tbody');
        if (!tbody) { console.error("plugin-list-tbody not found!"); return; }
        plugins.forEach(plugin => {
            const row = tbody.insertRow();
            row.dataset.id = plugin.meta.id;
            let pluginActionsHtml = `<button class="delete-plugin-btn" data-id="${plugin.meta.id}">Delete</button>`;
            if (plugin.meta.settings && plugin.meta.settings.length > 0) {
                pluginActionsHtml = `<button class="plugin-settings-btn" data-id="${plugin.meta.id}">Settings</button> ` + pluginActionsHtml;
            }

            row.innerHTML = `
                <td>${escapeHtml(plugin.meta.name)}</td>
                <td>${escapeHtml(plugin.meta.version || 'N/A')}</td>
                <td title="${escapeHtml(plugin.meta.description || 'No description.')}">${escapeHtml(plugin.meta.description || 'No description.').substring(0,50)}${plugin.meta.description && plugin.meta.description.length > 50 ? '...' : ''}</td>
                <td><input type="checkbox" class="plugin-enable-toggle" data-id="${plugin.meta.id}" ${plugin.meta.enabled !== false ? 'checked' : ''}></td>
                <td>${pluginActionsHtml}</td>`;
        });
        listDiv.querySelectorAll('.delete-plugin-btn').forEach(btn => btn.addEventListener('click', (e) => deletePlugin(e.target.dataset.id)));
        listDiv.querySelectorAll('.plugin-enable-toggle').forEach(cb => cb.addEventListener('change', (e) => togglePluginEnable(e.target.dataset.id, e.target.checked)));
        listDiv.querySelectorAll('.plugin-settings-btn').forEach(btn => btn.addEventListener('click', (e) => openPluginSettingsModal(e.target.dataset.id)));
    }

    async function deletePlugin(pluginId) {
        const pluginToDelete = plugins.find(p => p.meta.id === pluginId);
        if (!pluginToDelete) return;
        if (!confirm(`Are you sure you want to delete the plugin "${pluginToDelete.meta.name || pluginId}"? This will also remove its settings and any data stored via helpers.getPluginStorage.`)) return;

        if (pluginToDelete.meta.enabled !== false) { // Check if it was enabled
            await executePluginHook(pluginToDelete, 'onDisable');
        }

        unregisterActionsForPlugin(pluginId);
        await deletePluginSettings(pluginId); // Delete stored settings

        plugins = plugins.filter(p => p.meta.id !== pluginId);

        const panel = document.getElementById(CONFIG_PANEL_ID);
        if (panel) {
            renderPluginList(panel); // Re-render the list
            const pluginSearchInput = panel.querySelector('input.search-filter[data-list-table-body-id="plugin-list-tbody"]');
            if (pluginSearchInput && pluginSearchInput.value) { // Check if input exists and has a value
                pluginSearchInput.dispatchEvent(new Event('input'));
            }
        }
        refreshActionDropdown(); // Actions have changed
        showNotification(`Plugin "${pluginToDelete.meta.name}" deleted. Remember to "Save All" to persist this deletion.`, 'info');
    }

    async function togglePluginEnable(pluginId, isEnabled) {
        const plugin = plugins.find(p => p.meta.id === pluginId);
        if (plugin) {
            const oldEnabledState = plugin.meta.enabled !== false; // Treat undefined as enabled
            plugin.meta.enabled = isEnabled;

            if (isEnabled) {
                // Ensure settings are loaded (should be from init or drop, but good check)
                if (!plugin.currentSettings && plugin.meta.settings && plugin.meta.settings.length > 0) {
                    plugin.currentSettings = await loadPluginSettings(plugin.meta.id, plugin.meta.settings);
                }
                plugin.actions.forEach(actionDef => compileAndRegisterPluginAction(plugin, actionDef));
                if (!oldEnabledState) { // Only call onEnable if it was previously disabled
                    await executePluginHook(plugin, 'onEnable');
                }
            } else { // Disabling
                if (oldEnabledState) { // Only call onDisable if it was previously enabled
                    await executePluginHook(plugin, 'onDisable');
                }
                unregisterActionsForPlugin(plugin.meta.id);
            }
            refreshActionDropdown(); // Actions might have changed
            const panel = document.getElementById(CONFIG_PANEL_ID);
            if (panel) renderPluginList(panel); // Update checkbox in UI
            showNotification(`Plugin "${plugin.meta.name}" ${isEnabled ? 'enabled' : 'disabled'}. Remember to "Save All".`);
        }
    }


    // --- UI: Plugin Settings Modal ---
    function openPluginSettingsModal(pluginId) {
        const plugin = plugins.find(p => p.meta.id === pluginId);
        if (!plugin || !plugin.meta.settings || plugin.meta.settings.length === 0) {
            showNotification('This plugin has no configurable settings.', 'info');
            return;
        }

        let modal = document.getElementById(PLUGIN_SETTINGS_MODAL_ID);
        if (modal) modal.remove(); // Remove if exists to ensure fresh build

        modal = document.createElement('div');
        modal.id = PLUGIN_SETTINGS_MODAL_ID;
        modal.className = 'ahk-modal';

        let settingsHtml = '';
        plugin.meta.settings.forEach(settingDef => {
            const currentValue = plugin.currentSettings.hasOwnProperty(settingDef.id)
            ? plugin.currentSettings[settingDef.id]
            : settingDef.defaultValue; // Fallback to definition's default

            settingsHtml += `<div class="setting-item">
                <label for="plugin-setting-${pluginId}-${settingDef.id}">${escapeHtml(settingDef.name)}:</label>`;
            if (settingDef.type === 'text' || settingDef.type === 'number') {
                settingsHtml += `<input type="${settingDef.type}" id="plugin-setting-${pluginId}-${settingDef.id}" data-setting-id="${settingDef.id}" value="${escapeHtml(String(currentValue))}" ${settingDef.type === 'number' ? `step="${settingDef.step || 'any'}" min="${settingDef.min || ''}" max="${settingDef.max || ''}"` : ''}>`;
            } else if (settingDef.type === 'checkbox') {
                settingsHtml += `<input type="checkbox" id="plugin-setting-${pluginId}-${settingDef.id}" data-setting-id="${settingDef.id}" ${currentValue ? 'checked' : ''}>`;
            } else if (settingDef.type === 'select') {
                settingsHtml += `<select id="plugin-setting-${pluginId}-${settingDef.id}" data-setting-id="${settingDef.id}">`;
                (settingDef.options || []).forEach(opt => {
                    settingsHtml += `<option value="${escapeHtml(opt.value)}" ${opt.value === currentValue ? 'selected' : ''}>${escapeHtml(opt.label)}</option>`;
                });
                settingsHtml += `</select>`;
            }
            if (settingDef.description) {
                settingsHtml += `<p class="setting-description">${escapeHtml(settingDef.description)}</p>`;
            }
            settingsHtml += `</div>`;
        });

        modal.innerHTML = `
            <div class="modal-content">
                <span class="modal-close-btn">&times;</span>
                <h3>Settings: ${escapeHtml(plugin.meta.name)}</h3>
                <div class="form-area">${settingsHtml}</div>
                <button id="save-plugin-settings-btn">Save Settings</button>
            </div>`;

        document.body.appendChild(modal);
        modal.style.display = 'block';

        modal.querySelector('.modal-close-btn').addEventListener('click', () => { // Corrected arrow function
            modal.style.display = 'none';
        });
        modal.querySelector('#save-plugin-settings-btn').addEventListener('click', async () => {
            const oldSettings = JSON.parse(JSON.stringify(plugin.currentSettings)); // Deep copy for the hook
            const newSettingsFromForm = {}; // Build new settings purely from form
            let settingsHaveChanged = false;

            plugin.meta.settings.forEach(settingDef => {
                const inputElement = modal.querySelector(`#plugin-setting-${pluginId}-${settingDef.id}`);
                let formValue;
                if (settingDef.type === 'checkbox') {
                    formValue = inputElement.checked;
                } else if (settingDef.type === 'number') {
                    formValue = parseFloat(inputElement.value);
                    if (isNaN(formValue)) formValue = settingDef.defaultValue; // Fallback if parsing fails
                } else {
                    formValue = inputElement.value;
                }
                newSettingsFromForm[settingDef.id] = formValue;
                // Compare with the potentially existing old setting value
                if (oldSettings[settingDef.id] !== formValue) {
                    settingsHaveChanged = true;
                }
            });

            // Update the plugin's currentSettings object by merging new values
            plugin.currentSettings = { ...plugin.currentSettings, ...newSettingsFromForm };

            await savePluginSettings(pluginId, plugin.currentSettings); // Save all current (merged) settings to GM_storage

            if (settingsHaveChanged) {
                await executePluginHook(plugin, 'onSettingsChange', { oldSettings: oldSettings, newSettings: plugin.currentSettings });
            }

            showNotification(`Settings for ${plugin.meta.name} saved. "Save All" in main panel persists other script changes.`, settingsHaveChanged ? 'success' : 'info');
            modal.style.display = 'none';
        });
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
                if (notification) notification.style.display = 'none';
            }, duration);
        }
    }

    // --- Utility: Escape HTML ---
    function escapeHtml(unsafe) {
        if (typeof unsafe !== 'string') {
            if (unsafe === null || typeof unsafe === 'undefined') return '';
            return String(unsafe);
        }
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
            #${CONFIG_PANEL_ID} {
                position: fixed; top: 50px; right: 20px; width: 650px;
                max-height: 90vh; background-color: #f0f0f0; border: 1px solid #ccc;
                border-radius: 8px; box-shadow: 0 4px 15px rgba(0,0,0,0.2);
                z-index: 99999; font-family: Arial, sans-serif; font-size: 14px;
                color: #333; display: block; overflow-y: auto;
            }
            #${CONFIG_PANEL_ID} .header {
                background-color: #e0e0e0; padding: 10px 15px; border-bottom: 1px solid #ccc;
                border-top-left-radius: 8px; border-top-right-radius: 8px;
                display: flex; justify-content: space-between; align-items: center;
            }
            #${CONFIG_PANEL_ID} h2 { margin:0; font-size: 1.2em; }
            #${CONFIG_PANEL_ID} h3 { margin: 15px 0 10px; font-size: 1.1em; color: #333; }
            #${CONFIG_PANEL_ID} h4 { margin: 10px 0 5px; font-size: 1em; color: #555; }
            #${CONFIG_PANEL_ID} .close-btn {
                background: none; border: none; font-size: 24px; font-weight: bold;
                cursor: pointer; color: #777; padding: 0 5px;
            }
            #${CONFIG_PANEL_ID} .tabs {
                display: flex; background-color: #e9e9e9; border-bottom: 1px solid #ccc;
            }
            #${CONFIG_PANEL_ID} .tab-button {
                padding: 10px 15px; cursor: pointer; border: none;
                background-color: transparent; border-right: 1px solid #ccc;
                font-size: 14px; color: #333;
            }
            #${CONFIG_PANEL_ID} .tab-button:last-child { border-right: none; }
            #${CONFIG_PANEL_ID} .tab-button.active {
                background-color: #f0f0f0; font-weight: bold; border-bottom: 2px solid #007bff;
            }
            #${CONFIG_PANEL_ID} .tab-content { padding: 15px; display: none; }
            #${CONFIG_PANEL_ID} .tab-content.active { display: block; }

            #${CONFIG_PANEL_ID} input[type="text"], #${CONFIG_PANEL_ID} select, #${CONFIG_PANEL_ID} textarea {
                width: calc(100% - 22px); padding: 8px 10px; margin-bottom: 10px;
                border: 1px solid #ccc; border-radius: 4px; box-sizing: border-box; font-size:14px;
            }
            #${CONFIG_PANEL_ID} textarea { min-height: 80px; resize: vertical; }
            #${CONFIG_PANEL_ID} label { display: block; margin-bottom: 5px; font-weight: normal; }
            #${CONFIG_PANEL_ID} label input[type="checkbox"] { margin-right: 5px; vertical-align: middle; }


            #${CONFIG_PANEL_ID} button {
                padding: 8px 15px; background-color: #007bff; color: white;
                border: none; border-radius: 4px; cursor: pointer; margin-right: 5px; font-size: 14px;
            }
            #${CONFIG_PANEL_ID} button:hover { background-color: #0056b3; }
            #${CONFIG_PANEL_ID} .save-all-btn {
                background-color: #28a745; display: block; width: calc(100% - 30px);
                margin: 20px auto 10px; padding: 12px; font-size: 16px;
            }
            #${CONFIG_PANEL_ID} .item-list { margin-bottom: 20px; max-height: 300px; overflow-y: auto; border: 1px solid #ddd; background: #fff; }
            #${CONFIG_PANEL_ID} .item-list table {
                width: 100%; border-collapse: collapse;
            }
            #${CONFIG_PANEL_ID} .item-list th, #${CONFIG_PANEL_ID} .item-list td {
                border-bottom: 1px solid #ddd; padding: 8px; text-align: left; font-size: 0.95em;
            }
            #${CONFIG_PANEL_ID} .item-list th { background-color: #e9e9e9; position: sticky; top: 0; z-index: 1; }
            #${CONFIG_PANEL_ID} .item-list td button { margin-right: 5px; padding: 4px 8px; font-size: 0.9em; }
            #${CONFIG_PANEL_ID} .edit-hs-btn, #${CONFIG_PANEL_ID} .edit-hk-btn { background-color: #ffc107; color: #333;}
            #${CONFIG_PANEL_ID} .delete-hs-btn, #${CONFIG_PANEL_ID} .delete-hk-btn, .delete-plugin-btn { background-color: #dc3545;}
            #${CONFIG_PANEL_ID} .plugin-settings-btn { background-color: #17a2b8; }


            .help-text { font-size: 0.9em; color: #555; margin-top: 5px; padding: 8px; background-color: #e9e9f9; border-radius: 4px; border: 1px solid #c8c8dd;}
            .help-text code { background-color: #d4d4e4; padding: 2px 4px; border-radius: 3px; font-family: monospace; }
            .help-text pre { white-space: pre-wrap; word-break: break-all; max-height: 200px; overflow-y:auto; }

            #plugin-drop-zone {
                border: 2px dashed #ccc; border-radius: 5px; padding: 20px; text-align: center;
                margin-bottom: 15px; background-color: #f9f9f9; color: #777;
            }
            #plugin-drop-zone.dragover { border-color: #007bff; background-color: #e7f3ff; }

            #in-browser-ahk-notification {
                position: fixed; bottom: 20px; left: 50%; transform: translateX(-50%);
                padding: 10px 20px; border-radius: 5px; color: white; z-index: 100000;
                font-size: 16px; box-shadow: 0 2px 10px rgba(0,0,0,0.2); display: none;
            }
            #in-browser-ahk-notification.info { background-color: #17a2b8; }
            #in-browser-ahk-notification.success { background-color: #28a745; }
            #in-browser-ahk-notification.error { background-color: #dc3545; }

            .ahk-modal {
                display: none; position: fixed; z-index: 100001;
                left: 0; top: 0;
                width: 100%; height: 100%; overflow: auto; background-color: rgba(0,0,0,0.4);
                font-family: Arial, sans-serif;
            }
            .ahk-modal .modal-content {
                background-color: #fefefe; margin: 10% auto; padding: 20px;
                border: 1px solid #888; width: 90%; max-width: 600px;
                border-radius: 8px; box-shadow: 0 4px 15px rgba(0,0,0,0.2);
                position: relative;
            }
            .ahk-modal .modal-close-btn {
                color: #aaa; position: absolute; right: 15px; top: 10px;
                font-size: 28px; font-weight: bold;
                cursor: pointer; line-height:1;
            }
            .ahk-modal .modal-close-btn:hover, .ahk-modal .modal-close-btn:focus {
                color: black; text-decoration: none;
            }
            .ahk-modal h3 { margin-top:0; margin-bottom: 20px; font-size: 1.2em; }
            .ahk-modal .form-area { max-height: 60vh; overflow-y: auto; padding-right: 10px; margin-bottom: 15px;}
            .ahk-modal .form-area .setting-item { margin-bottom: 15px; }
            .ahk-modal .form-area label { display: block; margin-bottom: 5px; font-weight: bold; font-size: 1em;}
            .ahk-modal .form-area input[type="text"],
            .ahk-modal .form-area input[type="number"],
            .ahk-modal .form-area select {
                width: calc(100% - 12px); padding: 8px; border: 1px solid #ccc; border-radius: 4px; font-size: 1em;
            }
            .ahk-modal .form-area input[type="checkbox"] { margin-right: 5px; vertical-align: middle; width: auto;}
            .ahk-modal .form-area .setting-description { font-size: 0.85em; color: #666; margin-top: 4px; }
            .ahk-modal button#save-plugin-settings-btn {
                 padding: 10px 15px; background-color: #007bff; color: white;
                border: none; border-radius: 4px; cursor: pointer; font-size: 1em; display: block; margin-left: auto;
            }
            .ahk-modal button#save-plugin-settings-btn:hover { background-color: #0056b3; }
        `);
    }

    // --- Initialization ---
    async function init() {
        currentSettings = await loadData(STORAGE_KEY_SETTINGS, defaultSettings);
        hotstrings = await loadData(STORAGE_KEY_HOTSTRINGS, defaultHotstrings);
        hotkeys = await loadData(STORAGE_KEY_HOTKEYS, defaultHotkeys);
        const storedPluginShells = await loadData(STORAGE_KEY_PLUGINS, defaultPlugins);

        availableActions = { ...coreActions };
        plugins = [];

        for (const pluginShell of storedPluginShells) {
            if (!pluginShell.pluginMeta || !pluginShell.pluginMeta.id || !pluginShell.actions) {
                console.warn("In-Browser AHK: Skipping invalid plugin shell from storage:", pluginShell);
                continue;
            }
            let pluginDataToValidate;
            try {
                pluginDataToValidate = pluginShell.rawJson ? JSON.parse(pluginShell.rawJson) : pluginShell;
            } catch(e) {
                console.error("In-Browser AHK: Failed to parse rawJson for stored plugin", pluginShell.pluginMeta.id, e);
                continue;
            }

            if (validatePluginData(pluginDataToValidate)) {
                const currentMetaFromValidated = pluginDataToValidate.pluginMeta;
                const pluginSettings = await loadPluginSettings(currentMetaFromValidated.id, currentMetaFromValidated.settings);

                const pluginEntry = {
                    meta: { ...currentMetaFromValidated, enabled: pluginShell.meta.enabled },
                    actions: pluginDataToValidate.actions,
                    rawJson: pluginShell.rawJson || JSON.stringify(pluginDataToValidate),
                    currentSettings: pluginSettings
                };
                plugins.push(pluginEntry);

                if (pluginEntry.meta.enabled !== false) {
                    pluginEntry.actions.forEach(actionDef => {
                        compileAndRegisterPluginAction(pluginEntry, actionDef);
                    });
                    await executePluginHook(pluginEntry, 'onLoad');
                }
            } else {
                console.warn(`In-Browser AHK: Stored plugin data for ${pluginShell.pluginMeta.id} failed validation on init. Skipping.`);
            }
        }

        addStyles();

        document.addEventListener('keyup', handleHotstringInput, true);
        document.addEventListener('keydown', handleHotkey, true);

        GM_registerMenuCommand('In-Browser AHK Settings', createConfigPanel);

        console.log(`In-Browser AHK v${GM_info.script.version} initialized.`);
        showNotification(`In-Browser AHK Active! (v${GM_info.script.version})`, 'success', 3000);
    }

    const GM_info = (typeof GM !== "undefined" && GM.info) ? GM.info : { script: { version: "0.2.2" } }; // Fallback for version

    init();

})();