#!/bin/bash

# Script to add Category property to all plugins
# Based on PLUGIN_AUDIT.md categorization

PLUGINS_DIR="/home/user/NewIdeas/Cycloside/Plugins/BuiltIn"

# Function to add category after ForceDefaultTheme line
add_category() {
    local file="$1"
    local category="$2"
    local line_to_add="        public PluginCategory Category => PluginCategory.${category};"

    # Check if Category already exists
    if grep -q "public PluginCategory Category" "$file"; then
        echo "  ⚠️  Category already exists in $(basename $file)"
        return
    fi

    # Find the line with ForceDefaultTheme and add Category after it
    if grep -q "public bool ForceDefaultTheme" "$file"; then
        sed -i "/public bool ForceDefaultTheme/a\\${line_to_add}" "$file"
        echo "  ✅ Added $category to $(basename $file)"
    else
        echo "  ❌ Could not find ForceDefaultTheme in $(basename $file)"
    fi
}

echo "🎯 Applying Plugin Categories..."
echo ""

# CORE DESKTOP CUSTOMIZATION (5 plugins)
echo "📦 Core Desktop Customization (5 plugins):"
add_category "$PLUGINS_DIR/WallpaperPlugin.cs" "DesktopCustomization"
add_category "$PLUGINS_DIR/WidgetHostPlugin.cs" "DesktopCustomization"
add_category "$PLUGINS_DIR/DateTimeOverlayPlugin.cs" "DesktopCustomization"
add_category "$PLUGINS_DIR/ScreenSaverPlugin.cs" "DesktopCustomization"
add_category "$PLUGINS_DIR/ManagedVisHostPlugin.cs" "DesktopCustomization"
echo ""

# RETRO COMPUTING (4 plugins)
echo "🎮 Retro Computing (4 plugins):"
add_category "$PLUGINS_DIR/JezzballPlugin.cs" "RetroComputing"
add_category "$PLUGINS_DIR/QBasicRetroIDEPlugin.cs" "RetroComputing"
add_category "$PLUGINS_DIR/ModTrackerPlugin.cs" "RetroComputing"
add_category "$PLUGINS_DIR/MP3PlayerPlugin.cs" "Entertainment"
echo ""

# TINKERER TOOLS (4 plugins)
echo "🛠️  Tinkerer Tools (4 plugins):"
add_category "$PLUGINS_DIR/MacroPlugin.cs" "TinkererTools"
add_category "$PLUGINS_DIR/FileWatcherPlugin.cs" "TinkererTools"
add_category "$PLUGINS_DIR/TaskSchedulerPlugin.cs" "TinkererTools"
add_category "$PLUGINS_DIR/HardwareMonitorPlugin.cs" "TinkererTools"
echo ""

# UTILITIES (9 plugins)
echo "📁 Utilities (9 plugins):"
add_category "$PLUGINS_DIR/FileExplorerPlugin.cs" "Utilities"
add_category "$PLUGINS_DIR/TextEditorPlugin.cs" "Utilities"
add_category "$PLUGINS_DIR/ClipboardManagerPlugin.cs" "Utilities"
add_category "$PLUGINS_DIR/NotificationCenterPlugin.cs" "Utilities"
add_category "$PLUGINS_DIR/LogViewerPlugin.cs" "Utilities"
add_category "$PLUGINS_DIR/DiskUsagePlugin.cs" "Utilities"
add_category "$PLUGINS_DIR/EnvironmentEditorPlugin.cs" "Utilities"
add_category "$PLUGINS_DIR/QuickLauncherPlugin.cs" "Utilities"
add_category "$PLUGINS_DIR/CharacterMapPlugin.cs" "Utilities"
add_category "$PLUGINS_DIR/EncryptionPlugin.cs" "Utilities"
echo ""

# DEVELOPMENT (7 plugins)
echo "💻 Development Tools (7 plugins):"
add_category "$PLUGINS_DIR/TerminalPlugin.cs" "Development"
add_category "$PLUGINS_DIR/PowerShellTerminalPlugin.cs" "Development"
add_category "$PLUGINS_DIR/HackerTerminalPlugin.cs" "Development"
add_category "$PLUGINS_DIR/AdvancedCodeEditorPlugin.cs" "Development"
add_category "$PLUGINS_DIR/ApiTestingPlugin.cs" "Development"
add_category "$PLUGINS_DIR/DatabaseManagerPlugin.cs" "Development"
add_category "$PLUGINS_DIR/AiAssistantPlugin.cs" "Development"
echo ""

# SECURITY (6 plugins - mark for archival)
echo "🔒 Security Tools (6 plugins - ARCHIVE):"
add_category "$PLUGINS_DIR/NetworkToolsPlugin.cs" "Security"
add_category "$PLUGINS_DIR/VulnerabilityScannerPlugin.cs" "Security"
add_category "$PLUGINS_DIR/ExploitDatabasePlugin.cs" "Security"
add_category "$PLUGINS_DIR/ExploitDevToolsPlugin.cs" "Security"
add_category "$PLUGINS_DIR/DigitalForensicsPlugin.cs" "Security"
add_category "$PLUGINS_DIR/HackersParadisePlugin.cs" "Security"
echo ""

echo "✅ Done! All plugins categorized."
echo ""
echo "📊 Summary:"
echo "  - Core Desktop: 5 plugins (always enabled)"
echo "  - Retro Computing: 4 plugins (always enabled)"
echo "  - Tinkerer Tools: 4 plugins (enabled by default)"
echo "  - Utilities: 10 plugins (enabled by default)"
echo "  - Development: 7 plugins (disabled by default)"
echo "  - Security: 6 plugins (disabled by default, consider archiving)"
echo ""
echo "🚀 Next: Build and test!"
