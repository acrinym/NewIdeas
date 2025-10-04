using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Cycloside.Plugins;
using Cycloside.Services;
using Cycloside.Widgets;

namespace Cycloside.Plugins.BuiltIn
{
    /// <summary>
    /// AI ASSISTANT - Intelligent code assistance and cybersecurity guidance
    /// Provides AI-powered code completion, explanations, security analysis, and learning
    /// </summary>
    public class AiAssistantPlugin : IPlugin
    {
        public string Name => "AI Assistant";
        public string Description => "Intelligent code assistance and cybersecurity guidance";
        public Version Version => new(1, 0, 0);
        public bool ForceDefaultTheme => false;

        public class AiAssistantWidget : IWidget
        {
            public string Name => "AI Assistant";

            private TabControl? _mainTabControl;
            private TextBlock? _statusText;
            private ListBox? _chatHistory;
            private TextBox? _chatInput;
            private TextBox? _codeInput;
            private ListBox? _codeSuggestions;
            private ListBox? _securityFindings;
            private TextBox? _codeGenerationInput;

            public Control BuildView()
            {
                var mainPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(10)
                };

                // Header
                var headerPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 0, 15)
                };

                var headerText = new TextBlock
                {
                    Text = "🤖 AI Assistant",
                    FontSize = 18,
                    FontWeight = FontWeight.Bold
                };

                var statusPanel = new Border
                {
                    Background = Brushes.LightGray,
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(8, 4),
                    Margin = new Thickness(15, 0, 0, 0)
                };

                _statusText = new TextBlock
                {
                    Text = "Ready",
                    FontSize = 12
                };

                statusPanel.Child = _statusText;

                headerPanel.Children.Add(headerText);
                headerPanel.Children.Add(statusPanel);

                // Main tab control
                _mainTabControl = new TabControl();

                // Chat Tab
                var chatTab = CreateChatTab();
                _mainTabControl.Items.Add(chatTab);

                // Code Analysis Tab
                var analysisTab = CreateCodeAnalysisTab();
                _mainTabControl.Items.Add(analysisTab);

                // Code Generation Tab
                var generationTab = CreateCodeGenerationTab();
                _mainTabControl.Items.Add(generationTab);

                mainPanel.Children.Add(headerPanel);
                mainPanel.Children.Add(_mainTabControl);

                var border = new Border
                {
                    Child = mainPanel,
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(10)
                };

                return border;
            }

            private TabItem CreateChatTab()
            {
                var tab = new TabItem { Header = "💬 AI Chat" };

                var panel = new StackPanel { Margin = new Thickness(15) };

                // Chat history
                var chatPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };

                var chatLabel = new TextBlock
                {
                    Text = "💬 Chat with AI:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                _chatHistory = new ListBox { Height = 300 };

                // Add sample chat messages
                _chatHistory.Items.Add("🤖 AI: Hello! I'm your AI assistant. I can help with code explanations, security analysis, and development guidance.");
                _chatHistory.Items.Add("👤 You: Can you explain how async/await works in C#?");

                chatPanel.Children.Add(chatLabel);
                chatPanel.Children.Add(_chatHistory);

                // Chat input
                var inputPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };

                _chatInput = new TextBox
                {
                    Watermark = "Ask me anything about code, security, or development...",
                    Width = 400,
                    Margin = new Thickness(0, 0, 10, 0)
                };

                var sendButton = new Button
                {
                    Content = "📤 Send",
                    Background = Brushes.DodgerBlue,
                    Foreground = Brushes.White,
                    Padding = new Thickness(15, 8)
                };
                sendButton.Click += OnSendChatMessage;

                inputPanel.Children.Add(_chatInput);
                inputPanel.Children.Add(sendButton);

                // Quick action buttons
                var actionsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };

                var explainButton = new Button
                {
                    Content = "📖 Explain Code",
                    Background = Brushes.Green,
                    Foreground = Brushes.White,
                    Padding = new Thickness(10, 5)
                };
                explainButton.Click += OnExplainCode;

                var securityButton = new Button
                {
                    Content = "🔒 Security Check",
                    Background = Brushes.Orange,
                    Foreground = Brushes.White,
                    Padding = new Thickness(10, 5)
                };
                securityButton.Click += OnSecurityCheck;

                var clearButton = new Button
                {
                    Content = "🗑️ Clear Chat",
                    Background = Brushes.Red,
                    Foreground = Brushes.White,
                    Padding = new Thickness(10, 5)
                };
                clearButton.Click += OnClearChat;

                actionsPanel.Children.Add(explainButton);
                actionsPanel.Children.Add(securityButton);
                actionsPanel.Children.Add(clearButton);

                panel.Children.Add(chatPanel);
                panel.Children.Add(inputPanel);
                panel.Children.Add(actionsPanel);

                tab.Content = panel;
                return tab;
            }

            private TabItem CreateCodeAnalysisTab()
            {
                var tab = new TabItem { Header = "🔍 Code Analysis" };

                var panel = new StackPanel { Margin = new Thickness(15) };

                // Code input
                var codePanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };

                var codeLabel = new TextBlock
                {
                    Text = "📝 Code to Analyze:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                _codeInput = new TextBox
                {
                    Text = @"using System;
using System.Data.SqlClient;

public class UserManager
{
    public void CreateUser(string username, string password)
    {
        var connectionString = ""Server=localhost;Database=users;User Id=admin;Password=secret;"";
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        var query = $""INSERT INTO Users (Username, Password) VALUES ('{username}', '{password}')"";
        using var command = new SqlCommand(query, connection);
        command.ExecuteNonQuery();
    }
}",
                    Height = 200,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap
                };

                codePanel.Children.Add(codeLabel);
                codePanel.Children.Add(_codeInput);

                // Analysis buttons
                var analysisButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };

                var analyzeButton = new Button
                {
                    Content = "🔍 Analyze Security",
                    Background = Brushes.Red,
                    Foreground = Brushes.White,
                    Padding = new Thickness(15, 8)
                };
                analyzeButton.Click += OnAnalyzeSecurity;

                var suggestionsButton = new Button
                {
                    Content = "💡 Get Suggestions",
                    Background = Brushes.Blue,
                    Foreground = Brushes.White,
                    Padding = new Thickness(15, 8)
                };
                suggestionsButton.Click += OnGetSuggestions;

                analysisButtons.Children.Add(analyzeButton);
                analysisButtons.Children.Add(suggestionsButton);

                // Results
                var resultsPanel = new StackPanel { Margin = new Thickness(0, 15, 0, 0) };

                var suggestionsLabel = new TextBlock
                {
                    Text = "💡 Code Suggestions:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                _codeSuggestions = new ListBox { Height = 200 };

                var findingsLabel = new TextBlock
                {
                    Text = "🚨 Security Findings:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 15, 0, 10)
                };

                _securityFindings = new ListBox { Height = 200 };

                resultsPanel.Children.Add(suggestionsLabel);
                resultsPanel.Children.Add(_codeSuggestions);
                resultsPanel.Children.Add(findingsLabel);
                resultsPanel.Children.Add(_securityFindings);

                panel.Children.Add(codePanel);
                panel.Children.Add(analysisButtons);
                panel.Children.Add(resultsPanel);

                tab.Content = panel;
                return tab;
            }

            private TabItem CreateCodeGenerationTab()
            {
                var tab = new TabItem { Header = "⚡ Code Generation" };

                var panel = new StackPanel { Margin = new Thickness(15) };

                // Description input
                var descPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };

                var descLabel = new TextBlock
                {
                    Text = "📝 Describe what you want to build:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                _codeGenerationInput = new TextBox
                {
                    Text = "Create a C# class that manages user authentication with password hashing and JWT token generation.",
                    Height = 80,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap
                };

                descPanel.Children.Add(descLabel);
                descPanel.Children.Add(_codeGenerationInput);

                // Generation controls
                var controlsPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };

                var languageLabel = new TextBlock { Text = "Language:", Margin = new Thickness(0, 0, 10, 0) };
                var languageCombo = new ComboBox { Width = 100 };
                languageCombo.Items.Add("C#");
                languageCombo.Items.Add("Python");
                languageCombo.Items.Add("JavaScript");
                languageCombo.SelectedIndex = 0;

                var generateButton = new Button
                {
                    Content = "⚡ Generate Code",
                    Background = Brushes.Green,
                    Foreground = Brushes.White,
                    FontWeight = FontWeight.Bold,
                    Padding = new Thickness(15, 8)
                };
                generateButton.Click += OnGenerateCode;

                controlsPanel.Children.Add(languageLabel);
                controlsPanel.Children.Add(languageCombo);
                controlsPanel.Children.Add(generateButton);

                // Generated code output
                var outputPanel = new StackPanel { Margin = new Thickness(0, 15, 0, 0) };

                var outputLabel = new TextBlock
                {
                    Text = "🎯 Generated Code:",
                    FontWeight = FontWeight.Bold,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var codeOutput = new TextBox
                {
                    Text = "// Generated code will appear here...",
                    Height = 300,
                    IsReadOnly = true,
                    Background = Brushes.FromHex("#f8f9fa"),
                    FontFamily = "Consolas"
                };

                outputPanel.Children.Add(outputLabel);
                outputPanel.Children.Add(codeOutput);

                panel.Children.Add(descPanel);
                panel.Children.Add(controlsPanel);
                panel.Children.Add(outputPanel);

                tab.Content = panel;
                return tab;
            }

            private async void OnSendChatMessage(object? sender, RoutedEventArgs e)
            {
                var message = _chatInput?.Text?.Trim();
                if (string.IsNullOrEmpty(message)) return;

                // Add user message to chat
                if (_chatHistory != null)
                {
                    _chatHistory.Items.Add($"👤 You: {message}");
                    _chatHistory.ScrollIntoView(_chatHistory.Items[^1]);
                }

                // Clear input
                if (_chatInput != null)
                    _chatInput.Text = "";

                UpdateStatus("🤖 Thinking...");

                try
                {
                    var response = await AiAssistant.SendChatMessageAsync(message);

                    if (response != null)
                    {
                        // Add AI response to chat
                        if (_chatHistory != null)
                        {
                            _chatHistory.Items.Add($"🤖 AI: {response}");
                            _chatHistory.ScrollIntoView(_chatHistory.Items[^1]);
                        }

                        UpdateStatus("✅ AI responded");
                    }
                    else
                    {
                        if (_chatHistory != null)
                        {
                            _chatHistory.Items.Add("🤖 AI: Sorry, I couldn't generate a response right now.");
                        }

                        UpdateStatus("❌ AI response failed");
                    }
                }
                catch (Exception ex)
                {
                    if (_chatHistory != null)
                    {
                        _chatHistory.Items.Add($"🤖 AI: Error: {ex.Message}");
                    }

                    UpdateStatus($"❌ Error: {ex.Message}");
                }
            }

            private async void OnExplainCode(object? sender, RoutedEventArgs e)
            {
                var code = _codeInput?.Text?.Trim();
                if (string.IsNullOrEmpty(code)) return;

                UpdateStatus("📖 Explaining code...");

                try
                {
                    var explanation = await AiAssistant.ExplainCodeAsync(code);

                    if (explanation != null)
                    {
                        if (_chatHistory != null)
                        {
                            _chatHistory.Items.Add($"🤖 AI: Here's my explanation of the code:");
                            _chatHistory.Items.Add($"🤖 AI: {explanation}");
                        }

                        UpdateStatus("✅ Code explained");
                    }
                    else
                    {
                        UpdateStatus("❌ Code explanation failed");
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus($"❌ Error: {ex.Message}");
                }
            }

            private async void OnSecurityCheck(object? sender, RoutedEventArgs e)
            {
                var code = _codeInput?.Text?.Trim();
                if (string.IsNullOrEmpty(code)) return;

                UpdateStatus("🔒 Analyzing security...");

                try
                {
                    var findings = await AiAssistant.AnalyzeCodeSecurityAsync(code);

                    if (_securityFindings != null)
                    {
                        _securityFindings.Items.Clear();
                        foreach (var finding in findings)
                        {
                            var severityColor = finding.Severity switch
                            {
                                "Critical" => Brushes.Red,
                                "High" => Brushes.Orange,
                                "Medium" => Brushes.Yellow,
                                "Low" => Brushes.Green,
                                _ => Brushes.Gray
                            };

                            _securityFindings.Items.Add($"🚨 {finding.Severity}: {finding.Title}");
                        }
                    }

                    UpdateStatus($"✅ Security analysis complete: {findings.Count} findings");
                }
                catch (Exception ex)
                {
                    UpdateStatus($"❌ Security analysis failed: {ex.Message}");
                }
            }

            private async void OnGetSuggestions(object? sender, RoutedEventArgs e)
            {
                var code = _codeInput?.Text?.Trim();
                if (string.IsNullOrEmpty(code)) return;

                UpdateStatus("💡 Getting suggestions...");

                try
                {
                    var suggestions = await AiAssistant.GetCodeSuggestionsAsync(code);

                    if (_codeSuggestions != null)
                    {
                        _codeSuggestions.Items.Clear();
                        foreach (var suggestion in suggestions)
                        {
                            _codeSuggestions.Items.Add($"💡 {suggestion.Category} ({suggestion.Priority}): {suggestion.Title}");
                        }
                    }

                    UpdateStatus($"✅ Code suggestions generated: {suggestions.Count} improvements");
                }
                catch (Exception ex)
                {
                    UpdateStatus($"❌ Suggestions failed: {ex.Message}");
                }
            }

            private async void OnGenerateCode(object? sender, RoutedEventArgs e)
            {
                var description = _codeGenerationInput?.Text?.Trim();
                if (string.IsNullOrEmpty(description)) return;

                UpdateStatus("⚡ Generating code...");

                try
                {
                    var code = await AiAssistant.GenerateCodeAsync(description);

                    if (code != null)
                    {
                        // Find the code output textbox in the generation tab
                        var tabItem = _mainTabControl?.Items.Cast<TabItem>().FirstOrDefault(t => t.Header?.ToString() == "⚡ Code Generation");
                        if (tabItem?.Content is StackPanel panel)
                        {
                            var codeOutput = panel.Children.Cast<Control>().LastOrDefault() as TextBox;
                            if (codeOutput != null)
                            {
                                codeOutput.Text = code;
                            }
                        }

                        UpdateStatus("✅ Code generated");
                    }
                    else
                    {
                        UpdateStatus("❌ Code generation failed");
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus($"❌ Code generation failed: {ex.Message}");
                }
            }

            private void OnClearChat(object? sender, RoutedEventArgs e)
            {
                if (_chatHistory != null)
                {
                    _chatHistory.Items.Clear();
                    UpdateStatus("🗑️ Chat cleared");
                }
            }

            private void UpdateStatus(string message)
            {
                if (_statusText != null)
                {
                    _statusText.Text = message;
                }

                Logger.Log($"AI Assistant: {message}");
            }
        }

        public IWidget? Widget => new AiAssistantWidget();

        public void Start()
        {
            Logger.Log("🤖 AI Assistant Plugin started - Ready for intelligent assistance!");

            // Initialize AI Assistant service
            _ = AiAssistant.InitializeAsync();
        }

        public void Stop()
        {
            Logger.Log("🛑 AI Assistant Plugin stopped");
        }
    }
}
