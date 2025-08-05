using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Cycloside.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Cycloside.Plugins.BuiltIn
{
    /// <summary>
    /// Simple character map for copying special symbols and emoji.
    /// </summary>
    public partial class CharacterMapPlugin : ObservableObject, IPlugin
    {
        private CharacterMapWindow? _window;

        // --- IPlugin Properties ---
        public string Name => "Character Map";
        public string Description => "Browse characters and copy them to the clipboard.";
        public Version Version => new Version(1, 0, 0);
        public Widgets.IWidget? Widget => null;
        public bool ForceDefaultTheme => false;

        // --- Observable collections ---
        public ObservableCollection<string> FontNames { get; } = new();
        public ObservableCollection<string> Characters { get; } = new();

        [ObservableProperty]
        private string? _selectedFont;

        [ObservableProperty]
        private string _previewCharacter = string.Empty;

        [ObservableProperty]
        private string _textBuffer = string.Empty;

        // --- Plugin lifecycle ---
        public void Start()
        {
            LoadFonts();
            LoadCharacters();

            _window = new CharacterMapWindow { DataContext = this };
            ThemeManager.ApplyForPlugin(_window, this);
            WindowEffectsManager.Instance.ApplyConfiguredEffects(_window, nameof(CharacterMapPlugin));
            _window.Show();
        }

        public void Stop()
        {
            _window?.Close();
            _window = null;
        }

        // --- Commands ---
        [RelayCommand]
        private async Task CharacterSelected(string? ch)
        {
            if (string.IsNullOrEmpty(ch)) return;
            PreviewCharacter = ch;
            TextBuffer += ch;
            var clipboard = Application.Current?.GetMainTopLevel()?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(ch);
            }
        }

        [RelayCommand]
        private async Task CopyBuffer()
        {
            if (string.IsNullOrEmpty(TextBuffer)) return;
            var clipboard = Application.Current?.GetMainTopLevel()?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(TextBuffer);
            }
        }

        [RelayCommand]
        private void ClearBuffer() => TextBuffer = string.Empty;

        // --- Helpers ---
        private void LoadFonts()
        {
            FontNames.Clear();
            foreach (var ff in FontManager.Current.SystemFonts)
            {
                FontNames.Add(ff.Name);
            }
            SelectedFont = FontNames.FirstOrDefault();
        }

        private void LoadCharacters()
        {
            Characters.Clear();
            for (int i = 0x20; i <= 0x2FFF; i++)
            {
                var ch = char.ConvertFromUtf32(i);
                Characters.Add(ch);
            }
            for (int i = 0x1F600; i <= 0x1F64F; i++)
            {
                var ch = char.ConvertFromUtf32(i);
                Characters.Add(ch);
            }
        }
    }
}
