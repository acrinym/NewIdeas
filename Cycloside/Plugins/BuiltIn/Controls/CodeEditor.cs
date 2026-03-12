using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Controls.Primitives;

namespace Cycloside.Plugins.BuiltIn.Controls
{
    /// <summary>
    /// Advanced Code Editor with syntax highlighting, line numbers, and IDE features
    /// </summary>
    public class CodeEditor : UserControl
    {
        private Grid _mainGrid = null!;
        private TextBox _lineNumbersBox = null!;
        private Canvas _foldingGutter = null!;
        private ScrollViewer _editorScrollViewer = null!;
        private TextBox _editorTextBox = null!;
        private Canvas _highlightCanvas = null!;
        private ListBox _autocompleteListBox = null!;
        private Popup _autocompletePopup = null!;
        private string _currentLanguage = "C#";
        private readonly Dictionary<string, SyntaxHighlighter> _highlighters;
        private readonly Dictionary<string, List<string>> _autocompleteSuggestions;
        
        // Code folding
        private HashSet<int> _foldedRegions = new HashSet<int>();
        private Dictionary<int, int> _foldableRegions = new Dictionary<int, int>(); // start line -> end line
        
        // Minimap
        private Canvas _minimap = null!;
        
        // Find and Replace
        private Grid _findReplacePanel = null!;
        private TextBox _findTextBox = null!;
        private TextBox _replaceTextBox = null!;
        private Button _findNextButton = null!;
        private Button _findPreviousButton = null!;
        private Button _replaceButton = null!;
        private Button _replaceAllButton = null!;
        private Button _closeFindButton = null!;
        private CheckBox _regexCheckBox = null!;
        private CheckBox _caseSensitiveCheckBox = null!;
        private CheckBox _wholeWordCheckBox = null!;
        private List<(int start, int length)> _searchResults = new List<(int, int)>();
        private int _currentSearchIndex = -1;

        public static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<CodeEditor, string>(nameof(Text), string.Empty);

        public static readonly StyledProperty<string> LanguageProperty =
            AvaloniaProperty.Register<CodeEditor, string>(nameof(Language), "C#");

        public static readonly StyledProperty<bool> ShowLineNumbersProperty =
            AvaloniaProperty.Register<CodeEditor, bool>(nameof(ShowLineNumbers), true);

        public string Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string Language
        {
            get => GetValue(LanguageProperty);
            set => SetValue(LanguageProperty, value);
        }

        public bool ShowLineNumbers
        {
            get => GetValue(ShowLineNumbersProperty);
            set => SetValue(ShowLineNumbersProperty, value);
        }

        public CodeEditor()
        {
            _highlighters = new Dictionary<string, SyntaxHighlighter>
            {
                ["C#"] = new CSharpSyntaxHighlighter(),
                ["Python"] = new PythonSyntaxHighlighter(),
                ["JavaScript"] = new JavaScriptSyntaxHighlighter(),
                ["HTML"] = new HtmlSyntaxHighlighter(),
                ["CSS"] = new CssSyntaxHighlighter(),
                ["JSON"] = new JsonSyntaxHighlighter()
            };

            _autocompleteSuggestions = new Dictionary<string, List<string>>
            {
                ["C#"] = new List<string> { "class", "interface", "namespace", "using", "public", "private", "protected", "internal", "static", "void", "int", "string", "bool", "double", "float", "decimal", "var", "if", "else", "for", "foreach", "while", "do", "switch", "case", "break", "continue", "return", "try", "catch", "finally", "throw", "new", "this", "base", "override", "virtual", "abstract", "sealed", "readonly", "const", "async", "await", "Task", "List", "Dictionary", "Array", "Console.WriteLine", "ToString", "GetType", "Equals", "GetHashCode" },
                ["JavaScript"] = new List<string> { "function", "var", "let", "const", "if", "else", "for", "while", "do", "switch", "case", "break", "continue", "return", "try", "catch", "finally", "throw", "new", "this", "class", "extends", "import", "export", "default", "async", "await", "Promise", "console.log", "document", "window", "Array", "Object", "String", "Number", "Boolean", "null", "undefined", "true", "false" },
                ["Python"] = new List<string> { "def", "class", "if", "elif", "else", "for", "while", "try", "except", "finally", "with", "as", "import", "from", "return", "yield", "break", "continue", "pass", "lambda", "and", "or", "not", "in", "is", "True", "False", "None", "self", "super", "print", "len", "range", "enumerate", "zip", "map", "filter", "list", "dict", "tuple", "set", "str", "int", "float", "bool" },
                ["HTML"] = new List<string> { "html", "head", "body", "div", "span", "p", "h1", "h2", "h3", "h4", "h5", "h6", "a", "img", "ul", "ol", "li", "table", "tr", "td", "th", "form", "input", "button", "select", "option", "textarea", "label", "br", "hr", "meta", "link", "script", "style", "title" },
                ["CSS"] = new List<string> { "color", "background", "font-size", "font-family", "margin", "padding", "border", "width", "height", "display", "position", "top", "left", "right", "bottom", "float", "clear", "text-align", "vertical-align", "line-height", "font-weight", "font-style", "text-decoration", "overflow", "z-index", "opacity", "visibility", "cursor", "box-shadow", "border-radius" },
                ["JSON"] = new List<string> { "true", "false", "null" }
            };

            InitializeComponent();
            SetupEventHandlers();
        }

        private void InitializeComponent()
        {
            _mainGrid = new Grid();
            _mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); // Find/Replace panel
            _mainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star)); // Editor area
            _mainGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto)); // Line numbers
            _mainGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto)); // Code folding gutter
            _mainGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star)); // Editor
            _mainGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(120))); // Minimap

            // Line numbers
            _lineNumbersBox = new TextBox
            {
                IsReadOnly = true,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                Background = new SolidColorBrush(Color.Parse("#f8f9fa")),
                BorderThickness = new Thickness(0, 0, 1, 0),
                BorderBrush = new SolidColorBrush(Color.Parse("#e1e5e9")),
                Padding = new Thickness(8, 4),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                TextAlignment = TextAlignment.Right,
                Width = 60
            };
            Grid.SetColumn(_lineNumbersBox, 0);

            // Code folding gutter
            _foldingGutter = new Canvas
            {
                Background = new SolidColorBrush(Color.Parse("#f0f0f0")),
                Width = 20,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
            };
            Grid.SetColumn(_foldingGutter, 1);

            // Editor with syntax highlighting
            var editorGrid = new Grid();
            
            _editorTextBox = new TextBox
            {
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                AcceptsReturn = true,
                AcceptsTab = true,
                TextWrapping = TextWrapping.NoWrap,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8, 4),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
            };

            _highlightCanvas = new Canvas
            {
                Background = Brushes.White,
                IsHitTestVisible = false
            };

            editorGrid.Children.Add(_highlightCanvas);
            editorGrid.Children.Add(_editorTextBox);

            _editorScrollViewer = new ScrollViewer
            {
                Content = editorGrid,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetColumn(_editorScrollViewer, 2);

            // Minimap
            _minimap = new Canvas
            {
                Background = new SolidColorBrush(Color.Parse("#f8f8f8")),
                Width = 120,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                ClipToBounds = true
            };
            Grid.SetColumn(_minimap, 3);

            // Set all editor elements to row 1
            Grid.SetRow(_lineNumbersBox, 1);
            Grid.SetRow(_foldingGutter, 1);
            Grid.SetRow(_editorScrollViewer, 1);
            Grid.SetRow(_minimap, 1);

            // Create find/replace panel
            CreateFindReplacePanel();

            _mainGrid.Children.Add(_findReplacePanel);
            _mainGrid.Children.Add(_lineNumbersBox);
            _mainGrid.Children.Add(_foldingGutter);
            _mainGrid.Children.Add(_editorScrollViewer);
            _mainGrid.Children.Add(_minimap);

            // Create autocomplete popup
            _autocompleteListBox = new ListBox
            {
                Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                MaxHeight = 200,
                MinWidth = 200
            };
            
            _autocompletePopup = new Popup
            {
                Child = _autocompleteListBox,
                IsOpen = false,
                PlacementTarget = _editorTextBox,
                Placement = PlacementMode.Bottom
            };

            Content = _mainGrid;
        }

        private void CreateFindReplacePanel()
        {
            _findReplacePanel = new Grid
            {
                Background = new SolidColorBrush(Color.Parse("#f0f0f0")),
                IsVisible = false,
                Height = 80
            };
            
            // Set panel to span all columns and be in row 0
            Grid.SetRow(_findReplacePanel, 0);
            Grid.SetColumnSpan(_findReplacePanel, 4);
            
            // Create panel layout
            _findReplacePanel.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _findReplacePanel.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _findReplacePanel.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto)); // Labels
            _findReplacePanel.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(200))); // Text boxes
            _findReplacePanel.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto)); // Buttons
            _findReplacePanel.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto)); // Options
            _findReplacePanel.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star)); // Spacer
            _findReplacePanel.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto)); // Close button
            
            // Find row
            var findLabel = new TextBlock { Text = "Find:", Margin = new Thickness(8, 8, 4, 4), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            Grid.SetRow(findLabel, 0);
            Grid.SetColumn(findLabel, 0);
            
            _findTextBox = new TextBox { Margin = new Thickness(4, 4, 4, 4), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            Grid.SetRow(_findTextBox, 0);
            Grid.SetColumn(_findTextBox, 1);
            
            var findButtonsPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Margin = new Thickness(4) };
            _findNextButton = new Button { Content = "Next", Margin = new Thickness(2, 0), Padding = new Thickness(8, 4) };
            _findPreviousButton = new Button { Content = "Previous", Margin = new Thickness(2, 0), Padding = new Thickness(8, 4) };
            findButtonsPanel.Children.Add(_findNextButton);
            findButtonsPanel.Children.Add(_findPreviousButton);
            Grid.SetRow(findButtonsPanel, 0);
            Grid.SetColumn(findButtonsPanel, 2);
            
            // Replace row
            var replaceLabel = new TextBlock { Text = "Replace:", Margin = new Thickness(8, 4, 4, 8), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            Grid.SetRow(replaceLabel, 1);
            Grid.SetColumn(replaceLabel, 0);
            
            _replaceTextBox = new TextBox { Margin = new Thickness(4, 4, 4, 8), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            Grid.SetRow(_replaceTextBox, 1);
            Grid.SetColumn(_replaceTextBox, 1);
            
            var replaceButtonsPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Margin = new Thickness(4, 4, 4, 8) };
            _replaceButton = new Button { Content = "Replace", Margin = new Thickness(2, 0), Padding = new Thickness(8, 4) };
            _replaceAllButton = new Button { Content = "Replace All", Margin = new Thickness(2, 0), Padding = new Thickness(8, 4) };
            replaceButtonsPanel.Children.Add(_replaceButton);
            replaceButtonsPanel.Children.Add(_replaceAllButton);
            Grid.SetRow(replaceButtonsPanel, 1);
            Grid.SetColumn(replaceButtonsPanel, 2);
            
            // Options panel
            var optionsPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Vertical, Margin = new Thickness(8, 4) };
            _regexCheckBox = new CheckBox { Content = "Regex", Margin = new Thickness(0, 1) };
            _caseSensitiveCheckBox = new CheckBox { Content = "Case Sensitive", Margin = new Thickness(0, 1) };
            _wholeWordCheckBox = new CheckBox { Content = "Whole Word", Margin = new Thickness(0, 1) };
            optionsPanel.Children.Add(_regexCheckBox);
            optionsPanel.Children.Add(_caseSensitiveCheckBox);
            optionsPanel.Children.Add(_wholeWordCheckBox);
            Grid.SetRow(optionsPanel, 0);
            Grid.SetColumn(optionsPanel, 3);
            Grid.SetRowSpan(optionsPanel, 2);
            
            // Close button
            _closeFindButton = new Button { Content = "×", Margin = new Thickness(4), Padding = new Thickness(8, 4), FontWeight = FontWeight.Bold };
            Grid.SetRow(_closeFindButton, 0);
            Grid.SetColumn(_closeFindButton, 5);
            
            // Add all elements to panel
            _findReplacePanel.Children.Add(findLabel);
            _findReplacePanel.Children.Add(_findTextBox);
            _findReplacePanel.Children.Add(findButtonsPanel);
            _findReplacePanel.Children.Add(replaceLabel);
            _findReplacePanel.Children.Add(_replaceTextBox);
            _findReplacePanel.Children.Add(replaceButtonsPanel);
            _findReplacePanel.Children.Add(optionsPanel);
            _findReplacePanel.Children.Add(_closeFindButton);
        }

        private void SetupEventHandlers()
        {
            _editorTextBox.TextChanged += OnTextChanged;
            _editorTextBox.KeyDown += OnKeyDown;
            _editorTextBox.PropertyChanged += OnEditorPropertyChanged;
            
            // Synchronize scrolling between line numbers and editor
            _editorScrollViewer.ScrollChanged += OnEditorScrollChanged;
            
            // Autocomplete event handlers
            _autocompleteListBox.SelectionChanged += OnAutocompleteSelectionChanged;
            _autocompleteListBox.KeyDown += OnAutocompleteKeyDown;
            
            // Code folding event handlers
            _foldingGutter.PointerPressed += OnFoldingGutterClick;
            
            // Minimap event handlers
            _minimap.PointerPressed += OnMinimapClick;
            
            // Find/Replace event handlers
            _findTextBox.TextChanged += OnFindTextChanged;
            _findNextButton.Click += OnFindNext;
            _findPreviousButton.Click += OnFindPrevious;
            _replaceButton.Click += OnReplace;
            _replaceAllButton.Click += OnReplaceAll;
            _closeFindButton.Click += OnCloseFindReplace;
            _findTextBox.KeyDown += OnFindTextBoxKeyDown;
            _replaceTextBox.KeyDown += OnReplaceTextBoxKeyDown;
            
            this.PropertyChanged += (s, e) =>
            {
                if (e.Property == TextProperty)
                {
                    _editorTextBox.Text = Text;
                    UpdateSyntaxHighlighting();
                    UpdateLineNumbers();
                }
                else if (e.Property == LanguageProperty)
                {
                    _currentLanguage = Language;
                    UpdateSyntaxHighlighting();
                }
                else if (e.Property == ShowLineNumbersProperty)
                {
                    _lineNumbersBox.IsVisible = ShowLineNumbers;
                }
            };
        }
        
        private void OnEditorScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            // Synchronize line numbers scroll with editor scroll
            // For now, we'll implement this when we have a proper line numbers scroll viewer
            // This is a placeholder for future scroll synchronization
            
            // Update minimap viewport
            UpdateMinimapViewport();
        }
        
        private void OnAutocompleteSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_autocompleteListBox.SelectedItem is string selectedItem)
            {
                InsertAutocompleteText(selectedItem);
                _autocompletePopup.IsOpen = false;
            }
        }
        
        private void OnAutocompleteKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _autocompletePopup.IsOpen = false;
                _editorTextBox.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                if (_autocompleteListBox.SelectedItem is string selectedItem)
                {
                    InsertAutocompleteText(selectedItem);
                    _autocompletePopup.IsOpen = false;
                    e.Handled = true;
                }
            }
        }
        
        private void ShowAutocomplete(string currentWord)
        {
            if (!_autocompleteSuggestions.ContainsKey(_currentLanguage))
                return;
                
            var suggestions = _autocompleteSuggestions[_currentLanguage]
                .Where(s => s.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s)
                .ToList();
                
            if (suggestions.Count > 0)
            {
                _autocompleteListBox.ItemsSource = suggestions;
                _autocompleteListBox.SelectedIndex = 0;
                _autocompletePopup.IsOpen = true;
            }
            else
            {
                _autocompletePopup.IsOpen = false;
            }
        }
        
        private void InsertAutocompleteText(string text)
        {
            var caretIndex = _editorTextBox.CaretIndex;
            var currentText = _editorTextBox.Text ?? "";
            
            // Find the start of the current word
            var wordStart = caretIndex;
            while (wordStart > 0 && char.IsLetterOrDigit(currentText[wordStart - 1]))
            {
                wordStart--;
            }
            
            // Replace the current word with the selected suggestion
            var newText = currentText.Substring(0, wordStart) + text + currentText.Substring(caretIndex);
            _editorTextBox.Text = newText;
            _editorTextBox.CaretIndex = wordStart + text.Length;
        }
        
        private string GetCurrentWord()
        {
            var caretIndex = _editorTextBox.CaretIndex;
            var text = _editorTextBox.Text ?? "";
            
            if (caretIndex == 0 || caretIndex > text.Length)
                return "";
                
            var wordStart = caretIndex;
            while (wordStart > 0 && char.IsLetterOrDigit(text[wordStart - 1]))
            {
                wordStart--;
            }
            
            return text.Substring(wordStart, caretIndex - wordStart);
        }

        private void OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            Text = _editorTextBox.Text ?? string.Empty;
            
            Dispatcher.UIThread.Post(() =>
            {
                UpdateSyntaxHighlighting();
                UpdateLineNumbers();
                DetectFoldableRegions();
                UpdateFoldingGutter();
                UpdateMinimap();
                
                // Trigger autocomplete if the user is typing a word
                var currentWord = GetCurrentWord();
                if (currentWord.Length >= 2) // Show autocomplete after 2 characters
                {
                    ShowAutocomplete(currentWord);
                }
                else if (currentWord.Length == 0)
                {
                    _autocompletePopup.IsOpen = false;
                }
            }, DispatcherPriority.Background);
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            // Handle autocomplete navigation
            if (_autocompletePopup.IsOpen)
            {
                if (e.Key == Key.Down)
                {
                    var currentIndex = _autocompleteListBox.SelectedIndex;
                    if (currentIndex < _autocompleteListBox.ItemCount - 1)
                    {
                        _autocompleteListBox.SelectedIndex = currentIndex + 1;
                    }
                    e.Handled = true;
                    return;
                }
                else if (e.Key == Key.Up)
                {
                    var currentIndex = _autocompleteListBox.SelectedIndex;
                    if (currentIndex > 0)
                    {
                        _autocompleteListBox.SelectedIndex = currentIndex - 1;
                    }
                    e.Handled = true;
                    return;
                }
                else if (e.Key == Key.Escape)
                {
                    _autocompletePopup.IsOpen = false;
                    e.Handled = true;
                    return;
                }
                else if (e.Key == Key.Enter || e.Key == Key.Tab)
                {
                    if (_autocompleteListBox.SelectedItem is string selectedItem)
                    {
                        InsertAutocompleteText(selectedItem);
                        _autocompletePopup.IsOpen = false;
                        e.Handled = true;
                        return;
                    }
                }
            }
            
            // Trigger autocomplete on Ctrl+Space
            if (e.Key == Key.Space && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                var currentWord = GetCurrentWord();
                ShowAutocomplete(currentWord);
                e.Handled = true;
            }
            
            // Handle bracket and quote auto-closing
            if (ShouldAutoClose(e.Key))
            {
                HandleAutoClosing(e);
                return;
            }
            
            // Handle other special keys
            if (e.Key == Key.Tab)
            {
                HandleTabKey(e);
            }
            else if (e.Key == Key.Enter)
            {
                HandleEnterKey(e);
            }
        }

        private void HandleTabKey(KeyEventArgs e)
        {
            var caretIndex = _editorTextBox.CaretIndex;
            var text = _editorTextBox.Text ?? string.Empty;
            
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                // Shift+Tab: Decrease indentation
                var lineStart = text.LastIndexOf('\n', caretIndex - 1) + 1;
                var lineEnd = text.IndexOf('\n', caretIndex);
                if (lineEnd == -1) lineEnd = text.Length;
                
                var currentLine = text.Substring(lineStart, lineEnd - lineStart);
                if (currentLine.StartsWith("    "))
                {
                    var newLine = currentLine.Substring(4);
                    var newText = text.Substring(0, lineStart) + newLine + text.Substring(lineEnd);
                    _editorTextBox.Text = newText;
                    _editorTextBox.CaretIndex = Math.Max(lineStart, caretIndex - 4);
                    e.Handled = true;
                }
                else if (currentLine.StartsWith("\t"))
                {
                    var newLine = currentLine.Substring(1);
                    var newText = text.Substring(0, lineStart) + newLine + text.Substring(lineEnd);
                    _editorTextBox.Text = newText;
                    _editorTextBox.CaretIndex = Math.Max(lineStart, caretIndex - 1);
                    e.Handled = true;
                }
            }
            else
            {
                // Tab: Insert 4 spaces
                var newText = text.Insert(caretIndex, "    ");
                _editorTextBox.Text = newText;
                _editorTextBox.CaretIndex = caretIndex + 4;
                e.Handled = true;
            }
        }

        private void HandleEnterKey(KeyEventArgs e)
        {
            // Auto-indentation on new line
            var caretIndex = _editorTextBox.CaretIndex;
            var text = _editorTextBox.Text ?? string.Empty;
            
            if (caretIndex > 0)
            {
                // Find the current line
                var lineStart = text.LastIndexOf('\n', caretIndex - 1) + 1;
                var currentLine = text.Substring(lineStart, caretIndex - lineStart);
                
                // Calculate indentation of current line
                var indentation = GetLineIndentation(currentLine);
                
                // Check if we need extra indentation (after opening braces, etc.)
                var extraIndent = ShouldIncreaseIndentation(currentLine.Trim());
                
                // Create new line with proper indentation
                var newLineText = "\n" + indentation + (extraIndent ? "    " : "");
                var newText = text.Insert(caretIndex, newLineText);
                
                _editorTextBox.Text = newText;
                _editorTextBox.CaretIndex = caretIndex + newLineText.Length;
                e.Handled = true;
            }
        }

        private string GetLineIndentation(string line)
        {
            var indentation = string.Empty;
            foreach (var c in line)
            {
                if (c == ' ' || c == '\t')
                    indentation += c;
                else
                    break;
            }
            return indentation;
        }

        private bool ShouldIncreaseIndentation(string line)
        {
            // Check if line ends with characters that should increase indentation
            var trimmedLine = line.TrimEnd();
            if (string.IsNullOrEmpty(trimmedLine))
                return false;

            // Common patterns that increase indentation
            return trimmedLine.EndsWith("{") ||
                   trimmedLine.EndsWith(":") ||
                   trimmedLine.StartsWith("if ") ||
                   trimmedLine.StartsWith("else") ||
                   trimmedLine.StartsWith("for ") ||
                   trimmedLine.StartsWith("while ") ||
                   trimmedLine.StartsWith("foreach ") ||
                   trimmedLine.StartsWith("try") ||
                   trimmedLine.StartsWith("catch") ||
                   trimmedLine.StartsWith("finally") ||
                   trimmedLine.StartsWith("switch ") ||
                   trimmedLine.StartsWith("case ") ||
                   trimmedLine.StartsWith("default:") ||
                   trimmedLine.StartsWith("class ") ||
                   trimmedLine.StartsWith("interface ") ||
                   trimmedLine.StartsWith("namespace ") ||
                   trimmedLine.StartsWith("public ") ||
                   trimmedLine.StartsWith("private ") ||
                   trimmedLine.StartsWith("protected ") ||
                   trimmedLine.StartsWith("internal ");
        }

        private bool ShouldAutoClose(Key key)
        {
            return key == Key.D9 || // ( with Shift
                   key == Key.OemOpenBrackets || // [ and { with Shift
                   key == Key.OemQuotes; // " and '
        }

        private void HandleAutoClosing(KeyEventArgs e)
        {
            var caretIndex = _editorTextBox.CaretIndex;
            var text = _editorTextBox.Text ?? string.Empty;
            
            string openChar = "";
            string closeChar = "";
            
            // Determine what characters to insert based on the key
            if (e.Key == Key.D9 && e.KeyModifiers.HasFlag(KeyModifiers.Shift)) // (
            {
                openChar = "(";
                closeChar = ")";
            }
            else if (e.Key == Key.OemOpenBrackets && !e.KeyModifiers.HasFlag(KeyModifiers.Shift)) // [
            {
                openChar = "[";
                closeChar = "]";
            }
            else if (e.Key == Key.OemOpenBrackets && e.KeyModifiers.HasFlag(KeyModifiers.Shift)) // {
            {
                openChar = "{";
                closeChar = "}";
            }
            else if (e.Key == Key.OemQuotes && !e.KeyModifiers.HasFlag(KeyModifiers.Shift)) // "
            {
                openChar = "\"";
                closeChar = "\"";
            }
            else if (e.Key == Key.OemQuotes && e.KeyModifiers.HasFlag(KeyModifiers.Shift)) // '
            {
                openChar = "'";
                closeChar = "'";
            }
            
            if (!string.IsNullOrEmpty(openChar))
            {
                // Check if we should auto-close (not inside a string or comment)
                if (ShouldAutoCloseAtPosition(text, caretIndex))
                {
                    var newText = text.Insert(caretIndex, openChar + closeChar);
                    _editorTextBox.Text = newText;
                    _editorTextBox.CaretIndex = caretIndex + 1; // Position cursor between the brackets
                    e.Handled = true;
                }
            }
        }

        private bool ShouldAutoCloseAtPosition(string text, int position)
        {
            // Simple check to avoid auto-closing inside strings or comments
            var beforeCaret = text.Substring(0, position);
            var quoteCount = beforeCaret.Count(c => c == '"');
            var singleQuoteCount = beforeCaret.Count(c => c == '\'');
            
            // Don't auto-close if we're inside a string
            if (quoteCount % 2 == 1 || singleQuoteCount % 2 == 1)
                return false;
                
            // Don't auto-close if we're in a comment
            var lastLineStart = beforeCaret.LastIndexOf('\n') + 1;
            var currentLine = beforeCaret.Substring(lastLineStart);
            if (currentLine.Contains("//"))
                return false;
                
            return true;
         }

        private void OnEditorPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name == "CaretIndex")
            {
                HighlightMatchingBrackets();
            }
        }

        private void HighlightMatchingBrackets()
        {
            var text = _editorTextBox.Text ?? string.Empty;
            var caretIndex = _editorTextBox.CaretIndex;
            
            if (caretIndex <= 0 || caretIndex > text.Length)
                return;
                
            // Check character at caret and before caret
            var charAtCaret = caretIndex < text.Length ? text[caretIndex] : '\0';
            var charBeforeCaret = caretIndex > 0 ? text[caretIndex - 1] : '\0';
            
            int bracketPos = -1;
            char bracketChar = '\0';
            
            // Determine which bracket to match
            if (IsBracket(charAtCaret))
            {
                bracketPos = caretIndex;
                bracketChar = charAtCaret;
            }
            else if (IsBracket(charBeforeCaret))
            {
                bracketPos = caretIndex - 1;
                bracketChar = charBeforeCaret;
            }
            
            if (bracketPos >= 0)
            {
                var matchingPos = FindMatchingBracket(text, bracketPos, bracketChar);
                if (matchingPos >= 0)
                {
                    // Here you would highlight the brackets
                    // For now, we'll just store the positions for potential future use
                    // In a full implementation, you'd modify the text formatting
                }
            }
        }

        private bool IsBracket(char c)
        {
            return c == '(' || c == ')' || c == '[' || c == ']' || c == '{' || c == '}';
        }

        private int FindMatchingBracket(string text, int position, char bracket)
        {
            var openBrackets = new char[] { '(', '[', '{' };
            var closeBrackets = new char[] { ')', ']', '}' };
            
            bool isOpenBracket = Array.IndexOf(openBrackets, bracket) >= 0;
            char matchingBracket;
            int direction;
            
            if (isOpenBracket)
            {
                var index = Array.IndexOf(openBrackets, bracket);
                matchingBracket = closeBrackets[index];
                direction = 1; // Search forward
            }
            else
            {
                var index = Array.IndexOf(closeBrackets, bracket);
                matchingBracket = openBrackets[index];
                direction = -1; // Search backward
            }
            
            int count = 1;
            int pos = position + direction;
            
            while (pos >= 0 && pos < text.Length && count > 0)
            {
                var currentChar = text[pos];
                
                if (currentChar == bracket)
                    count++;
                else if (currentChar == matchingBracket)
                    count--;
                    
                if (count == 0)
                    return pos;
                    
                pos += direction;
            }
            
            return -1; // No matching bracket found
         }

        private void OnFoldingGutterClick(object? sender, PointerPressedEventArgs e)
        {
            var position = e.GetPosition(_foldingGutter);
            var lineHeight = 20; // Approximate line height
            var clickedLine = (int)(position.Y / lineHeight) + 1;
            
            if (_foldableRegions.ContainsKey(clickedLine))
            {
                if (_foldedRegions.Contains(clickedLine))
                {
                    UnfoldRegion(clickedLine);
                }
                else
                {
                    FoldRegion(clickedLine);
                }
                UpdateFoldingGutter();
            }
        }

        private void FoldRegion(int startLine)
        {
            if (_foldableRegions.ContainsKey(startLine))
            {
                _foldedRegions.Add(startLine);
                UpdateEditorDisplay();
            }
        }

        private void UnfoldRegion(int startLine)
        {
            _foldedRegions.Remove(startLine);
            UpdateEditorDisplay();
        }

        private void UpdateEditorDisplay()
        {
            // In a full implementation, this would hide/show lines based on folded regions
            // For now, we'll just update the visual indicators
            UpdateFoldingGutter();
        }

        private void DetectFoldableRegions()
        {
            _foldableRegions.Clear();
            var text = _editorTextBox.Text ?? string.Empty;
            var lines = text.Split('\n');
            
            var stack = new Stack<int>();
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                
                // Detect opening braces or function/class declarations
                if (line.EndsWith("{") || 
                    line.StartsWith("function ") ||
                    line.StartsWith("class ") ||
                    line.StartsWith("if ") ||
                    line.StartsWith("for ") ||
                    line.StartsWith("while ") ||
                    line.StartsWith("switch ") ||
                    line.StartsWith("try ") ||
                    line.StartsWith("namespace "))
                {
                    stack.Push(i + 1); // Line numbers are 1-based
                }
                else if (line.StartsWith("}") && stack.Count > 0)
                {
                    var startLine = stack.Pop();
                    if (i + 1 - startLine > 1) // Only foldable if more than 1 line
                    {
                        _foldableRegions[startLine] = i + 1;
                    }
                }
            }
        }

        private void UpdateFoldingGutter()
        {
            _foldingGutter.Children.Clear();
            
            foreach (var region in _foldableRegions)
            {
                var startLine = region.Key;
                var isFolded = _foldedRegions.Contains(startLine);
                
                var button = new Button
                {
                    Content = isFolded ? "+" : "-",
                    Width = 16,
                    Height = 16,
                    FontSize = 10,
                    Padding = new Thickness(0),
                    Background = new SolidColorBrush(Color.Parse("#e0e0e0")),
                    BorderBrush = new SolidColorBrush(Color.Parse("#c0c0c0")),
                    BorderThickness = new Thickness(1)
                };
                
                Canvas.SetTop(button, (startLine - 1) * 20); // Approximate line height
                Canvas.SetLeft(button, 2);
                
                button.Click += (s, e) =>
                {
                    if (isFolded)
                        UnfoldRegion(startLine);
                    else
                        FoldRegion(startLine);
                };
                
                _foldingGutter.Children.Add(button);
            }
         }

        private void OnMinimapClick(object? sender, PointerPressedEventArgs e)
        {
            var position = e.GetPosition(_minimap);
            var text = _editorTextBox.Text ?? string.Empty;
            var lines = text.Split('\n');
            
            // Calculate which line was clicked based on minimap height
            var lineHeight = Math.Max(1, _minimap.Bounds.Height / Math.Max(1, lines.Length));
            var clickedLine = (int)(position.Y / lineHeight);
            
            // Scroll to the clicked line
            if (clickedLine >= 0 && clickedLine < lines.Length)
            {
                ScrollToLine(clickedLine);
            }
        }

        private void ScrollToLine(int lineNumber)
        {
            var text = _editorTextBox.Text ?? string.Empty;
            var lines = text.Split('\n');
            
            if (lineNumber >= 0 && lineNumber < lines.Length)
            {
                // Calculate character position for the line
                var charPosition = 0;
                for (int i = 0; i < lineNumber; i++)
                {
                    charPosition += lines[i].Length + 1; // +1 for newline
                }
                
                _editorTextBox.CaretIndex = charPosition;
                _editorTextBox.Focus();
                
                // Scroll the editor to show the line
                var lineHeight = 20; // Approximate line height
                var targetOffset = lineNumber * lineHeight;
                _editorScrollViewer.Offset = new Vector(0, targetOffset);
            }
        }

        private void UpdateMinimap()
        {
            _minimap.Children.Clear();
            
            var text = _editorTextBox.Text ?? string.Empty;
            var lines = text.Split('\n');
            
            if (lines.Length == 0) return;
            
            var minimapHeight = _minimap.Bounds.Height;
            var lineHeight = Math.Max(1, minimapHeight / Math.Max(1, lines.Length));
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                // Create a simplified representation of the line
                var lineRect = new Rectangle
                {
                    Width = Math.Min(100, line.Length * 0.8), // Approximate width based on content
                    Height = Math.Max(1, lineHeight - 1),
                    Fill = GetLineColor(line),
                    Stroke = null
                };
                
                Canvas.SetTop(lineRect, i * lineHeight);
                Canvas.SetLeft(lineRect, 2);
                
                _minimap.Children.Add(lineRect);
            }
            
            UpdateMinimapViewport();
        }

        private IBrush GetLineColor(string line)
        {
            var trimmed = line.Trim();
            
            // Color code based on content type
            if (trimmed.StartsWith("//") || trimmed.StartsWith("/*"))
                return new SolidColorBrush(Color.Parse("#008000")); // Green for comments
            else if (trimmed.Contains("class ") || trimmed.Contains("function ") || trimmed.Contains("namespace "))
                return new SolidColorBrush(Color.Parse("#0000FF")); // Blue for declarations
            else if (trimmed.Contains("{") || trimmed.Contains("}"))
                return new SolidColorBrush(Color.Parse("#800080")); // Purple for braces
            else if (trimmed.Contains("if ") || trimmed.Contains("for ") || trimmed.Contains("while "))
                return new SolidColorBrush(Color.Parse("#FF8000")); // Orange for control flow
            else if (!string.IsNullOrWhiteSpace(trimmed))
                return new SolidColorBrush(Color.Parse("#404040")); // Dark gray for regular code
            else
                return new SolidColorBrush(Color.Parse("#E0E0E0")); // Light gray for empty lines
        }

        private void UpdateMinimapViewport()
        {
            // Remove existing viewport indicator
            var existingViewport = _minimap.Children.OfType<Rectangle>()
                .FirstOrDefault(r => r.Name == "viewport");
            if (existingViewport != null)
                _minimap.Children.Remove(existingViewport);
            
            var text = _editorTextBox.Text ?? string.Empty;
            var lines = text.Split('\n');
            
            if (lines.Length == 0) return;
            
            var minimapHeight = _minimap.Bounds.Height;
            var lineHeight = Math.Max(1, minimapHeight / Math.Max(1, lines.Length));
            
            // Calculate visible area
            var scrollOffset = _editorScrollViewer.Offset.Y;
            var viewportHeight = _editorScrollViewer.Viewport.Height;
            var editorLineHeight = 20; // Approximate line height in editor
            
            var visibleStartLine = (int)(scrollOffset / editorLineHeight);
            var visibleEndLine = (int)((scrollOffset + viewportHeight) / editorLineHeight);
            
            // Create viewport indicator
            var viewportRect = new Rectangle
            {
                Name = "viewport",
                Width = 116,
                Height = Math.Max(2, (visibleEndLine - visibleStartLine) * lineHeight),
                Fill = new SolidColorBrush(Color.Parse("#4080FF80")), // Semi-transparent blue
                Stroke = new SolidColorBrush(Color.Parse("#4080FF")),
                StrokeThickness = 1
            };
            
            Canvas.SetTop(viewportRect, visibleStartLine * lineHeight);
            Canvas.SetLeft(viewportRect, 2);
            
            _minimap.Children.Add(viewportRect);
        }

        private void UpdateLineNumbers()
        {
            if (!ShowLineNumbers)
            {
                _lineNumbersBox.IsVisible = false;
                return;
            }

            _lineNumbersBox.IsVisible = true;
            var lines = Text?.Split('\n') ?? new string[0];
            var lineCount = Math.Max(1, lines.Length);
            var lineNumbers = new System.Text.StringBuilder();
            
            // Calculate the width needed for line numbers based on the maximum line number
            var maxLineNumberWidth = lineCount.ToString().Length;
            
            for (int i = 1; i <= lineCount; i++)
            {
                // Right-align line numbers with consistent width
                lineNumbers.AppendLine(i.ToString().PadLeft(maxLineNumberWidth));
            }
            
            _lineNumbersBox.Text = lineNumbers.ToString().TrimEnd();
            
            // Update line numbers box width based on content
            var charWidth = 8; // Approximate character width in pixels
            _lineNumbersBox.Width = (maxLineNumberWidth + 1) * charWidth + 10; // Add padding
        }

        private void UpdateSyntaxHighlighting()
        {
            if (!_highlighters.TryGetValue(_currentLanguage, out var highlighter))
                return;

            var text = _editorTextBox.Text ?? string.Empty;
            _highlightCanvas.Children.Clear();

            // This is a simplified version - in a real implementation,
            // you'd render highlighted text blocks on the canvas
            var tokens = highlighter.Tokenize(text);
            
            // For now, just log the tokens (in a real implementation,
            // you'd render them with appropriate colors)
            foreach (var token in tokens.Take(10)) // Limit for performance
            {
                // Create colored text blocks and position them on the canvas
                // This would require more complex text measurement and positioning
            }
        }

        private void OnFindTextChanged(object? sender, TextChangedEventArgs e)
        {
            PerformSearch();
        }

        private void PerformSearch()
        {
            try
            {
                _searchResults.Clear();
                _currentSearchIndex = -1;
                ClearSearchHighlights();

                var searchText = _findTextBox?.Text;
                if (string.IsNullOrEmpty(searchText)) return;

                var text = _editorTextBox.Text ?? "";
                var isRegex = _regexCheckBox?.IsChecked == true;
                var isCaseSensitive = _caseSensitiveCheckBox?.IsChecked == true;
                var isWholeWord = _wholeWordCheckBox?.IsChecked == true;

                if (isRegex)
                {
                    var options = RegexOptions.None;
                    if (!isCaseSensitive) options |= RegexOptions.IgnoreCase;

                    var regex = new Regex(searchText, options);
                    var matches = regex.Matches(text);

                    foreach (Match match in matches)
                    {
                        _searchResults.Add((match.Index, match.Length));
                    }
                }
                else
                {
                    var comparison = isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                    var startIndex = 0;

                    while (true)
                    {
                        var index = text.IndexOf(searchText, startIndex, comparison);
                        if (index == -1) break;

                        if (isWholeWord)
                        {
                            var isWordStart = index == 0 || !char.IsLetterOrDigit(text[index - 1]);
                            var isWordEnd = index + searchText.Length >= text.Length || 
                                          !char.IsLetterOrDigit(text[index + searchText.Length]);
                            
                            if (isWordStart && isWordEnd)
                            {
                                _searchResults.Add((index, searchText.Length));
                            }
                        }
                        else
                        {
                            _searchResults.Add((index, searchText.Length));
                        }

                        startIndex = index + 1;
                    }
                }

                if (_searchResults.Count > 0)
                {
                    _currentSearchIndex = 0;
                    HighlightSearchResults();
                    ScrollToSearchResult();
                }
            }
            catch (Exception)
            {
                // Handle regex errors gracefully
                _searchResults.Clear();
                _currentSearchIndex = -1;
                ClearSearchHighlights();
            }
        }

        public void FindNext()
        {
            if (_searchResults.Count == 0) return;
            
            _currentSearchIndex = (_currentSearchIndex + 1) % _searchResults.Count;
            HighlightSearchResults();
            ScrollToSearchResult();
        }

        public void FindPrevious()
        {
            if (_searchResults.Count == 0) return;
            
            _currentSearchIndex = _currentSearchIndex <= 0 ? _searchResults.Count - 1 : _currentSearchIndex - 1;
            HighlightSearchResults();
            ScrollToSearchResult();
        }

        private void HighlightSearchResults()
        {
            ClearSearchHighlights();
            
            for (int i = 0; i < _searchResults.Count; i++)
            {
                var result = _searchResults[i];
                var isCurrentResult = i == _currentSearchIndex;
                AddSearchHighlight(result.start, result.length, isCurrentResult);
            }
        }

        private void AddSearchHighlight(int start, int length, bool isCurrent)
        {
            // Create highlight rectangle
            var highlight = new Rectangle
            {
                Fill = isCurrent ? Brushes.Orange : Brushes.Yellow,
                Opacity = 0.3,
                Width = length * 8, // Approximate character width
                Height = 20 // Line height
            };

            // Position the highlight (this is simplified - real implementation would need proper text measurement)
            var line = GetLineFromPosition(start);
            var column = GetColumnFromPosition(start);
            
            Canvas.SetLeft(highlight, column * 8);
            Canvas.SetTop(highlight, line * 20);
            
            _highlightCanvas.Children.Add(highlight);
        }

        private void ClearSearchHighlights()
        {
            _highlightCanvas.Children.Clear();
        }

        private void ScrollToSearchResult()
        {
            if (_searchResults.Count == 0 || _currentSearchIndex < 0) return;
            
            var result = _searchResults[_currentSearchIndex];
            var line = GetLineFromPosition(result.start);
            
            // Calculate target scroll position
            var lineHeight = 20; // Approximate line height
            var viewportHeight = _editorScrollViewer.Viewport.Height;
            var targetY = line * lineHeight;
            
            // Center the line in the viewport
            if (targetY < _editorScrollViewer.Offset.Y || 
                targetY > _editorScrollViewer.Offset.Y + viewportHeight)
            {
                _editorScrollViewer.Offset = new Vector(_editorScrollViewer.Offset.X, 
                    Math.Max(0, targetY - viewportHeight / 2));
            }
        }

        private void OnFindNext(object? sender, RoutedEventArgs e)
        {
            FindNext();
        }

        private void OnFindPrevious(object? sender, RoutedEventArgs e)
        {
            FindPrevious();
        }

        private void OnReplace(object? sender, RoutedEventArgs e)
        {
            if (_searchResults.Count == 0 || _currentSearchIndex < 0) return;
            
            var replaceText = _replaceTextBox?.Text ?? "";
            var result = _searchResults[_currentSearchIndex];
            
            var text = _editorTextBox.Text ?? "";
            var newText = text.Substring(0, result.start) + replaceText + text.Substring(result.start + result.length);
            _editorTextBox.Text = newText;
            
            // Update search results
            PerformSearch();
        }

        private void OnReplaceAll(object? sender, RoutedEventArgs e)
        {
            if (_searchResults.Count == 0) return;
            
            var replaceText = _replaceTextBox?.Text ?? "";
            var text = _editorTextBox.Text ?? "";
            
            // Replace from end to start to maintain indices
            for (int i = _searchResults.Count - 1; i >= 0; i--)
            {
                var result = _searchResults[i];
                text = text.Substring(0, result.start) + replaceText + text.Substring(result.start + result.length);
            }
            
            _editorTextBox.Text = text;
            PerformSearch();
        }

        private void OnCloseFindReplace(object? sender, RoutedEventArgs e)
        {
            HideFindReplace();
        }

        private void OnFindTextBoxKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FindNext();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                HideFindReplace();
                e.Handled = true;
            }
        }

        private void OnReplaceTextBoxKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OnReplace(sender, e);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                HideFindReplace();
                e.Handled = true;
            }
        }

        private int GetLineFromPosition(int index)
        {
            var text = _editorTextBox.Text ?? string.Empty;
            if (index <= 0) return 0;
            if (index > text.Length) index = text.Length;
            int line = 0;
            for (int i = 0; i < index; i++)
            {
                if (text[i] == '\n') line++;
            }
            return line;
        }

        private int GetColumnFromPosition(int index)
        {
            var text = _editorTextBox.Text ?? string.Empty;
            if (index <= 0) return 0;
            if (index > text.Length) index = text.Length;
            int lastNewline = text.LastIndexOf('\n', Math.Max(0, index - 1));
            return lastNewline >= 0 ? index - lastNewline - 1 : index;
        }

        public void ShowFindReplace()
        {
            if (_findReplacePanel != null)
            {
                _findReplacePanel.IsVisible = true;
            }
            _findTextBox?.Focus();
        }

        public void HideFindReplace()
        {
            if (_findReplacePanel != null)
            {
                _findReplacePanel.IsVisible = false;
            }
            _searchResults.Clear();
            _currentSearchIndex = -1;
            ClearSearchHighlights();
        }
    
    }

    // Syntax highlighting infrastructure
    public abstract class SyntaxHighlighter
    {
        public abstract List<SyntaxToken> Tokenize(string text);
    }

    public class SyntaxToken
    {
        public string Text { get; set; } = string.Empty;
        public TokenType Type { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
    }

    public enum TokenType
    {
        Text,
        Keyword,
        String,
        Comment,
        Number,
        Operator,
        Identifier,
        Type
    }

    public class CSharpSyntaxHighlighter : SyntaxHighlighter
    {
        private readonly string[] _keywords = {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this",
            "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
            "using", "virtual", "void", "volatile", "while"
        };

        public override List<SyntaxToken> Tokenize(string text)
        {
            var tokens = new List<SyntaxToken>();
            
            // Simple regex-based tokenization
            var patterns = new[]
            {
                (@"//.*$", TokenType.Comment),
                (@"/\*[\s\S]*?\*/", TokenType.Comment),
                (@"""(?:[^""\\]|\\.)*""", TokenType.String),
                (@"'(?:[^'\\]|\\.)*'", TokenType.String),
                (@"\b\d+\.?\d*\b", TokenType.Number),
                (@"\b(" + string.Join("|", _keywords) + @")\b", TokenType.Keyword),
                (@"[+\-*/=<>!&|^%~?:;,.()\[\]{}]", TokenType.Operator),
                (@"\b[a-zA-Z_][a-zA-Z0-9_]*\b", TokenType.Identifier)
            };

            foreach (var (pattern, tokenType) in patterns)
            {
                var matches = Regex.Matches(text, pattern, RegexOptions.Multiline);
                foreach (Match match in matches)
                {
                    tokens.Add(new SyntaxToken
                    {
                        Text = match.Value,
                        Type = tokenType,
                        Start = match.Index,
                        Length = match.Length
                    });
                }
            }

            return tokens.OrderBy(t => t.Start).ToList();
        }
    }





    // Placeholder implementations for other languages
    public class PythonSyntaxHighlighter : SyntaxHighlighter
    {
        public override List<SyntaxToken> Tokenize(string text) => new List<SyntaxToken>();
    }

    public class JavaScriptSyntaxHighlighter : SyntaxHighlighter
    {
        public override List<SyntaxToken> Tokenize(string text) => new List<SyntaxToken>();
    }

    public class HtmlSyntaxHighlighter : SyntaxHighlighter
    {
        public override List<SyntaxToken> Tokenize(string text) => new List<SyntaxToken>();
    }

    public class CssSyntaxHighlighter : SyntaxHighlighter
    {
        public override List<SyntaxToken> Tokenize(string text) => new List<SyntaxToken>();
    }

    public class JsonSyntaxHighlighter : SyntaxHighlighter
    {
        public override List<SyntaxToken> Tokenize(string text) => new List<SyntaxToken>();
    }
}
