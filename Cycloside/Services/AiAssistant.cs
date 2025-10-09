using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Cycloside.Services
{
    /// <summary>
    /// AI Assistant Event Arguments
    /// </summary>
    public class AiResponseEventArgs : EventArgs
    {
        public string Response { get; }

        public AiResponseEventArgs(string response)
        {
            Response = response;
        }
    }

    public class AiErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; }

        public AiErrorEventArgs(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }

    public class CodeSuggestionEventArgs : EventArgs
    {
        public CodeSuggestion Suggestion { get; }

        public CodeSuggestionEventArgs(CodeSuggestion suggestion)
        {
            Suggestion = suggestion;
        }
    }

    public class SecurityAnalysisEventArgs : EventArgs
    {
        public List<SecurityFinding> Findings { get; }

        public SecurityAnalysisEventArgs(List<SecurityFinding> findings)
        {
            Findings = findings;
        }
    }

    /// <summary>
    /// AI ASSISTANT - Intelligent code assistance and cybersecurity guidance
    /// Provides AI-powered code completion, explanations, security analysis, and learning
    /// </summary>
    public static partial class AiAssistant
    {
        public static event EventHandler<AiResponseEventArgs>? AiResponseReceived;
        public static event EventHandler<AiErrorEventArgs>? AiErrorOccurred;
        public static event EventHandler<CodeSuggestionEventArgs>? CodeSuggestionReceived;
        public static event EventHandler<SecurityAnalysisEventArgs>? SecurityAnalysisCompleted;

        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly string _apiEndpoint = "https://api.openai.com/v1/chat/completions";
        private static string _apiKey = ""; // Would be loaded from secure config
        private static readonly ObservableCollection<ChatMessage> _chatHistory = new();
        private static readonly ObservableCollection<CodeSuggestion> _codeSuggestions = new();
        private static readonly ObservableCollection<SecurityFinding> _securityFindings = new();

        public static ObservableCollection<ChatMessage> ChatHistory => _chatHistory;
        public static ObservableCollection<CodeSuggestion> CodeSuggestions => _codeSuggestions;
        public static ObservableCollection<SecurityFinding> SecurityFindings => _securityFindings;

        public static bool IsInitialized => !string.IsNullOrEmpty(_apiKey);
        public static bool IsProcessing => _currentRequest != null;

        private static Task<string?>? _currentRequest;

        /// <summary>
        /// Initialize AI Assistant with API configuration
        /// </summary>
        public static async Task InitializeAsync()
        {
            Logger.Log("🤖 Initializing AI Assistant...");

            try
            {
                // Load API key from secure configuration
                _apiKey = await LoadApiKeyAsync();

                if (!IsInitialized)
                {
                    Logger.Log("⚠️ AI Assistant: No API key configured - AI features disabled");
                    return;
                }

                // Test API connectivity
                await Task.Run(async () =>
                {
                    var isOnline = await TestApiConnectionAsync();

                    if (isOnline)
                    {
                        Logger.Log("✅ AI Assistant initialized successfully");
                    }
                    else
                    {
                        Logger.Log("❌ AI Assistant: API connection test failed");
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ AI Assistant initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Load API key from secure configuration
        /// </summary>
        private static Task<string> LoadApiKeyAsync()
        {
            // In production, this would load from encrypted config file
            // For now, return empty string to disable AI features
            return Task.FromResult("");
        }

        /// <summary>
        /// Test API connectivity
        /// </summary>
        private static async Task<bool> TestApiConnectionAsync()
        {
            try
            {
                var response = await SendChatMessageAsync("Hello, this is a test message. Please respond with 'API connection successful'.");
                return response != null && response.Contains("API connection successful", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Send chat message to AI
        /// </summary>
        public static async Task<string?> SendChatMessageAsync(string message)
        {
            if (!IsInitialized)
            {
                OnAiError("AI Assistant not configured - API key required");
                return null;
            }

            if (IsProcessing)
            {
                OnAiError("AI Assistant is currently processing another request");
                return null;
            }

            try
            {
                Logger.Log($"🤖 Sending message to AI: {message[..Math.Min(message.Length, 50)]}...");

                var chatMessage = new ChatMessage
                {
                    Role = "user",
                    Content = message
                };

                // Add to chat history
                _chatHistory.Add(chatMessage);

                // Send to AI API
                _currentRequest = SendToAiApiAsync(chatMessage);
                var response = await _currentRequest;

                if (response != null)
                {
                    Logger.Log("✅ AI response received successfully");

                    // Add AI response to chat history
                    var aiResponse = new ChatMessage
                    {
                        Role = "assistant",
                        Content = response
                    };
                    _chatHistory.Add(aiResponse);

                    OnAiResponseReceived(response);
                    return response;
                }
                else
                {
                    OnAiError("No response received from AI");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ AI chat error: {ex.Message}");
                OnAiError($"AI chat failed: {ex.Message}");
                return null;
            }
            finally
            {
                _currentRequest = null;
            }
        }

        /// <summary>
        /// Send message to AI API
        /// </summary>
        private static async Task<string?> SendToAiApiAsync(ChatMessage message)
        {
            try
            {
                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[] { new { role = message.Role, content = message.Content } },
                    max_tokens = 1000,
                    temperature = 0.7
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Cycloside-AI-Assistant");

                var response = await _httpClient.PostAsync(_apiEndpoint, content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<AiApiResponse>(responseJson);

                return apiResponse?.Choices?.FirstOrDefault()?.Message?.Content;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ AI API error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Send prompt to AI API with a system role
        /// </summary>
        private static async Task<string?> SendToAiApiAsync(string prompt, string systemRole)
        {
            try
            {
                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new { role = "system", content = systemRole },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 1500,
                    temperature = 0.3
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Cycloside-AI-Assistant");

                var response = await _httpClient.PostAsync(_apiEndpoint, content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<AiApiResponse>(responseJson);

                return apiResponse?.Choices?.FirstOrDefault()?.Message?.Content;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ AI API error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Analyze code for security vulnerabilities
        /// </summary>
        public static async Task<List<SecurityFinding>> AnalyzeCodeSecurityAsync(string code, string language = "csharp")
        {
            var findings = new List<SecurityFinding>();

            try
            {
                Logger.Log($"🔒 Analyzing code security for {language}...");

                var prompt = $@"Analyze the following {language} code for security vulnerabilities, best practices violations, and potential exploits:

{code}

Please provide a detailed security analysis including:
1. SQL injection vulnerabilities
2. XSS (Cross-Site Scripting) risks
3. Buffer overflow potential
4. Authentication/authorization issues
5. Cryptography weaknesses
6. Input validation problems
7. Memory safety issues
8. Race conditions
9. Information disclosure risks
10. Configuration security issues

For each finding, provide:
- Severity level (Critical/High/Medium/Low)
- Description of the vulnerability
- Impact assessment
- Remediation recommendations
- Code location/line numbers if applicable

Format your response as a structured security report.";

                var response = await SendChatMessageAsync(prompt);

                if (response != null)
                {
                    // Parse AI response into structured findings
                    findings = ParseSecurityFindings(response);

                    // Add to findings collection
                    foreach (var finding in findings)
                    {
                        _securityFindings.Add(finding);
                    }

                    Logger.Log($"🔒 Security analysis completed: {findings.Count} findings");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Security analysis failed: {ex.Message}");
            }

            return findings;
        }

        /// <summary>
        /// Get code suggestions for improvement
        /// </summary>
        public static async Task<List<CodeSuggestion>> GetCodeSuggestionsAsync(string code, string language = "csharp")
        {
            var suggestions = new List<CodeSuggestion>();

            try
            {
                Logger.Log($"💡 Getting code suggestions for {language}...");

                var prompt = $@"Analyze the following {language} code and provide specific, actionable suggestions for improvement:

{code}

Please provide suggestions for:
1. Performance optimizations
2. Code readability improvements
3. Best practices implementation
4. Error handling enhancements
5. Security improvements
6. Maintainability enhancements
7. Documentation additions

For each suggestion, provide:
- Category (Performance/Security/Readability/Maintainability/etc.)
- Priority (High/Medium/Low)
- Description of the improvement
- Code example showing the suggested change
- Explanation of why this improves the code

Format your response as a structured list of suggestions with clear examples.";

                var response = await SendChatMessageAsync(prompt);

                if (response != null)
                {
                    // Parse AI response into structured suggestions
                    suggestions = ParseCodeSuggestions(response);

                    // Add to suggestions collection
                    foreach (var suggestion in suggestions)
                    {
                        _codeSuggestions.Add(suggestion);
                    }

                    Logger.Log($"💡 Code suggestions generated: {suggestions.Count} improvements");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Code suggestions failed: {ex.Message}");
            }

            return suggestions;
        }

        /// <summary>
        /// Explain code functionality
        /// </summary>
        public static async Task<string?> ExplainCodeAsync(string code, string language = "csharp")
        {
            try
            {
                Logger.Log($"📖 Explaining {language} code...");

                var prompt = $@"Please explain what the following {language} code does in clear, understandable terms:

{code}

Your explanation should include:
1. Overall purpose and functionality
2. Key components and their roles
3. Data flow and processing logic
4. Important algorithms or patterns used
5. Potential use cases and applications
6. Any notable design decisions or trade-offs

Write this as if explaining to a junior developer who wants to understand the code's purpose and implementation.";

                return await SendChatMessageAsync(prompt);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Code explanation failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generate code based on description
        /// </summary>
        public static async Task<string?> GenerateCodeAsync(string description, string language = "csharp")
        {
            try
            {
                Logger.Log($"⚡ Generating {language} code for: {description[..Math.Min(description.Length, 50)]}...");

                var prompt = $@"Please write {language} code that implements the following functionality:

{description}

Requirements:
1. Use proper {language} syntax and conventions
2. Include appropriate error handling
3. Add meaningful comments
4. Follow best practices for {language}
5. Make the code production-ready and maintainable

Provide a complete, working code example with all necessary imports and structure.";

                return await SendChatMessageAsync(prompt);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Code generation failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get cybersecurity best practices
        /// </summary>
        public static async Task<string?> GetSecurityBestPracticesAsync(string context = "general")
        {
            try
            {
                Logger.Log($"🔒 Getting security best practices for: {context}");

                var prompt = $@"Provide comprehensive cybersecurity best practices for {context} development and operations.

Cover these areas:
1. Authentication and authorization
2. Input validation and sanitization
3. Secure data storage and transmission
4. Error handling and logging
5. Network security
6. Cryptography usage
7. Session management
8. Access control
9. Security testing and monitoring
10. Incident response and recovery

Provide specific, actionable recommendations with code examples where applicable.";

                return await SendChatMessageAsync(prompt);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Security best practices query failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parse security findings from AI response
        /// </summary>
        private static List<SecurityFinding> ParseSecurityFindings(string response)
        {
            var findings = new List<SecurityFinding>();

            try
            {
                // Simple parsing - in production would be more sophisticated
                var lines = response.Split('\n');
                var currentFinding = new SecurityFinding();

                foreach (var line in lines)
                {
                    if (line.StartsWith("1.") || line.StartsWith("2.") || line.StartsWith("3.") ||
                        line.StartsWith("4.") || line.StartsWith("5.") || line.StartsWith("6.") ||
                        line.StartsWith("7.") || line.StartsWith("8.") || line.StartsWith("9.") ||
                        line.StartsWith("10."))
                    {
                        if (currentFinding.Description != null)
                        {
                            findings.Add(currentFinding);
                        }

                        currentFinding = new SecurityFinding
                        {
                            Title = line.Trim(),
                            Severity = "Medium", // Default
                            Description = ""
                        };
                    }
                    else if (line.Contains("Critical") || line.Contains("High") || line.Contains("Medium") || line.Contains("Low"))
                    {
                        if (line.Contains("Critical"))
                            currentFinding.Severity = "Critical";
                        else if (line.Contains("High"))
                            currentFinding.Severity = "High";
                        else if (line.Contains("Medium"))
                            currentFinding.Severity = "Medium";
                        else if (line.Contains("Low"))
                            currentFinding.Severity = "Low";
                    }
                    else if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("=") && !line.StartsWith("-"))
                    {
                        if (currentFinding.Description == null)
                            currentFinding.Description = line.Trim();
                        else
                            currentFinding.Description += " " + line.Trim();
                    }
                }

                if (currentFinding.Description != null)
                {
                    findings.Add(currentFinding);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"⚠️ Failed to parse security findings: {ex.Message}");
            }

            return findings;
        }

        /// <summary>
        /// Parse code suggestions from AI response
        /// </summary>
        private static List<CodeSuggestion> ParseCodeSuggestions(string response)
        {
            var suggestions = new List<CodeSuggestion>();

            try
            {
                // Simple parsing - in production would be more sophisticated
                var lines = response.Split('\n');
                var currentSuggestion = new CodeSuggestion();

                foreach (var line in lines)
                {
                    if (line.StartsWith("1.") || line.StartsWith("2.") || line.StartsWith("3.") ||
                        line.StartsWith("4.") || line.StartsWith("5.") || line.StartsWith("6.") ||
                        line.StartsWith("7.") || line.StartsWith("8.") || line.StartsWith("9.") ||
                        line.StartsWith("10."))
                    {
                        if (currentSuggestion.Description != null)
                        {
                            suggestions.Add(currentSuggestion);
                        }

                        currentSuggestion = new CodeSuggestion
                        {
                            Title = line.Trim(),
                            Category = "General",
                            Priority = "Medium",
                            Description = ""
                        };
                    }
                    else if (line.Contains("Performance") || line.Contains("Security") || line.Contains("Readability") || line.Contains("Maintainability"))
                    {
                        currentSuggestion.Category = line.Trim();
                    }
                    else if (line.Contains("High") || line.Contains("Medium") || line.Contains("Low"))
                    {
                        currentSuggestion.Priority = line.Trim();
                    }
                    else if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("=") && !line.StartsWith("-"))
                    {
                        if (currentSuggestion.Description == null)
                            currentSuggestion.Description = line.Trim();
                        else
                            currentSuggestion.Description += " " + line.Trim();
                    }
                }

                if (currentSuggestion.Description != null)
                {
                    suggestions.Add(currentSuggestion);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"⚠️ Failed to parse code suggestions: {ex.Message}");
            }

            return suggestions;
        }

        /// <summary>
        /// Clear chat history
        /// </summary>
        public static void ClearChatHistory()
        {
            _chatHistory.Clear();
            Logger.Log("🗑️ Chat history cleared");
        }

        /// <summary>
        /// Clear code suggestions
        /// </summary>
        public static void ClearCodeSuggestions()
        {
            _codeSuggestions.Clear();
            Logger.Log("🗑️ Code suggestions cleared");
        }

        /// <summary>
        /// Clear security findings
        /// </summary>
        public static void ClearSecurityFindings()
        {
            _securityFindings.Clear();
            Logger.Log("🗑️ Security findings cleared");
        }

        // Event handlers
        private static void OnAiResponseReceived(string response)
        {
            AiResponseReceived?.Invoke(null, new AiResponseEventArgs(response));
        }

        private static void OnAiError(string error)
        {
            AiErrorOccurred?.Invoke(null, new AiErrorEventArgs(error));
        }

        private static void OnCodeSuggestionReceived(CodeSuggestion suggestion)
        {
            CodeSuggestionReceived?.Invoke(null, new CodeSuggestionEventArgs(suggestion));
        }

        private static void OnSecurityAnalysisCompleted(List<SecurityFinding> findings)
        {
            SecurityAnalysisCompleted?.Invoke(null, new SecurityAnalysisEventArgs(findings));
        }
    }

    // Data models
    public class ChatMessage
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = "";
    }

    public class CodeSuggestion
    {
        public string Title { get; set; } = "";
        public string Category { get; set; } = "General";
        public string Priority { get; set; } = "Medium";
        public string Description { get; set; } = "";
        public string? CodeExample { get; set; }
    }

    public class SecurityFinding
    {
        public string Title { get; set; } = "";
        public string Severity { get; set; } = "Medium";
        public string Description { get; set; } = "";
        public string? Impact { get; set; }
        public string? Remediation { get; set; }
        public string? CodeLocation { get; set; }
    }

    // API models
    public class AiApiResponse
    {
        public List<AiChoice>? Choices { get; set; }
    }

    public class AiChoice
    {
        public AiMessage? Message { get; set; }
    }

    public class AiMessage
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
    }

    public static partial class AiAssistant
    {
        /// <summary>
        /// Analyze code for security vulnerabilities in real-time
        /// </summary>
        public static async Task<List<SecurityFinding>> AnalyzeCodeRealTimeAsync(string code, string language = "csharp")
        {
            var prompt = $@"Analyze this {language} code for security vulnerabilities and provide detailed findings:

Code:
{code}

Please provide a JSON response with the following structure:
{{
    ""vulnerabilities"": [
        {{
            ""type"": ""SQL Injection|Path Traversal|XSS|CSRF|Authentication Bypass|Authorization Bypass|Cryptography|Input Validation|Session Management|Configuration"",
            ""severity"": ""Critical|High|Medium|Low|Info"",
            ""line"": number,
            ""description"": ""detailed explanation"",
            ""recommendation"": ""how to fix"",
            ""cwe"": ""CWE-XXX"",
            ""owasp"": ""OWASP category""
        }}
    ],
    ""overallRisk"": ""Critical|High|Medium|Low|Info"",
    ""recommendations"": [""general security recommendations""]
}}";

            try
            {
                var response = await SendToAiApiAsync(prompt, "You are a cybersecurity expert analyzing code for security vulnerabilities.");
                if (response == null) return new List<SecurityFinding>();

                // Parse JSON response and convert to SecurityFinding objects
                var findings = ParseSecurityFindings(response);
                return findings;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ AI security analysis failed: {ex.Message}");
                return new List<SecurityFinding>();
            }
        }

        /// <summary>
        /// Analyze code performance and provide optimization suggestions
        /// </summary>
        public static async Task<List<PerformanceSuggestion>> AnalyzeCodePerformanceAsync(string code, string language = "csharp")
        {
            var prompt = $@"Analyze this {language} code for performance issues and provide optimization suggestions:

Code:
{code}

Please provide a JSON response with the following structure:
{{
    ""performanceIssues"": [
        {{
            ""type"": ""Memory|CPU|I/O|Algorithm|Design"",
            ""severity"": ""Critical|High|Medium|Low|Info"",
            ""line"": number,
            ""description"": ""performance issue description"",
            ""impact"": ""High|Medium|Low"",
            ""optimization"": ""specific optimization suggestion"",
            ""estimatedImprovement"": ""percentage or metric""
        }}
    ],
    ""overallPerformance"": ""Excellent|Good|Average|Poor|Critical"",
    ""recommendations"": [""general performance recommendations""]
}}";

            try
            {
                var response = await SendToAiApiAsync(prompt, "You are a performance optimization expert analyzing code for efficiency.");
                if (response == null) return new List<PerformanceSuggestion>();

                var suggestions = ParsePerformanceSuggestions(response);
                return suggestions;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ AI performance analysis failed: {ex.Message}");
                return new List<PerformanceSuggestion>();
            }
        }

        /// <summary>
        /// Analyze code quality and provide improvement suggestions
        /// </summary>
        public static async Task<List<CodeQualitySuggestion>> AnalyzeCodeQualityAsync(string code, string language = "csharp")
        {
            var prompt = $@"Analyze this {language} code for quality issues and provide improvement suggestions:

Code:
{code}

Please provide a JSON response with the following structure:
{{
    ""qualityIssues"": [
        {{
            ""type"": ""Readability|Maintainability|Testability|Architecture|Design|Documentation"",
            ""severity"": ""Critical|High|Medium|Low|Info"",
            ""line"": number,
            ""description"": ""quality issue description"",
            ""suggestion"": ""specific improvement suggestion"",
            ""benefit"": ""expected benefit""
        }}
    ],
    ""overallQuality"": ""Excellent|Good|Average|Poor|Critical"",
    ""recommendations"": [""general quality recommendations""]
}}";

            try
            {
                var response = await SendToAiApiAsync(prompt, "You are a code quality expert providing detailed analysis and improvement suggestions.");
                if (response == null) return new List<CodeQualitySuggestion>();

                var suggestions = ParseCodeQualitySuggestions(response);
                return suggestions;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ AI quality analysis failed: {ex.Message}");
                return new List<CodeQualitySuggestion>();
            }
        }

        /// <summary>
        /// Get code complexity metrics and refactoring suggestions
        /// </summary>
        public static async Task<ComplexityAnalysis> AnalyzeCodeComplexityAsync(string code, string language = "csharp")
        {
            var prompt = $@"Analyze this {language} code for complexity and provide refactoring suggestions:

Code:
{code}

Please provide a JSON response with the following structure:
{{
    ""cyclomaticComplexity"": number,
    ""cognitiveComplexity"": number,
    ""linesOfCode"": number,
    ""maintainabilityIndex"": number,
    ""complexityIssues"": [
        {{
            ""method"": ""method name"",
            ""complexity"": number,
            ""suggestion"": ""refactoring suggestion""
        }}
    ],
    ""refactoringRecommendations"": [""general refactoring suggestions""],
    ""riskLevel"": ""Low|Medium|High|Critical""
}}";

            try
            {
                var response = await SendToAiApiAsync(prompt, "You are a software architecture expert analyzing code complexity and providing refactoring guidance.");
                if (response == null) return new ComplexityAnalysis();

                var analysis = ParseComplexityAnalysis(response);
                return analysis;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ AI complexity analysis failed: {ex.Message}");
                return new ComplexityAnalysis();
            }
        }

        /// <summary>
        /// Parse performance suggestions from AI response
        /// </summary>
        private static List<PerformanceSuggestion> ParsePerformanceSuggestions(string response)
        {
            var suggestions = new List<PerformanceSuggestion>();

            try
            {
                // Simple parsing for demo - in production would use proper JSON parsing
                var lines = response.Split('\n');
                var currentSuggestion = new PerformanceSuggestion();

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (line.Contains("Performance Issue") || line.Contains("Optimization"))
                    {
                        if (currentSuggestion.Description != null)
                        {
                            suggestions.Add(currentSuggestion);
                        }

                        currentSuggestion = new PerformanceSuggestion
                        {
                            Description = line.Trim()
                        };
                    }
                    else if (line.Contains("Type:") || line.Contains("Impact:") || line.Contains("Line:"))
                    {
                        if (line.Contains("Type:")) currentSuggestion.Type = line.Replace("Type:", "").Trim();
                        if (line.Contains("Impact:")) currentSuggestion.Impact = line.Replace("Impact:", "").Trim();
                        if (line.Contains("Line:")) currentSuggestion.Line = int.TryParse(line.Replace("Line:", "").Trim(), out var lineNum) ? lineNum : 0;
                    }
                }

                if (currentSuggestion.Description != null)
                {
                    suggestions.Add(currentSuggestion);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"⚠️ Failed to parse performance suggestions: {ex.Message}");
            }

            return suggestions;
        }

        /// <summary>
        /// Parse code quality suggestions from AI response
        /// </summary>
        private static List<CodeQualitySuggestion> ParseCodeQualitySuggestions(string response)
        {
            var suggestions = new List<CodeQualitySuggestion>();

            try
            {
                // Simple parsing for demo - in production would use proper JSON parsing
                var lines = response.Split('\n');
                var currentSuggestion = new CodeQualitySuggestion();

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (line.Contains("Quality Issue") || line.Contains("Improvement"))
                    {
                        if (currentSuggestion.Description != null)
                        {
                            suggestions.Add(currentSuggestion);
                        }

                        currentSuggestion = new CodeQualitySuggestion
                        {
                            Description = line.Trim()
                        };
                    }
                    else if (line.Contains("Type:") || line.Contains("Benefit:") || line.Contains("Line:"))
                    {
                        if (line.Contains("Type:")) currentSuggestion.Type = line.Replace("Type:", "").Trim();
                        if (line.Contains("Benefit:")) currentSuggestion.Benefit = line.Replace("Benefit:", "").Trim();
                        if (line.Contains("Line:")) currentSuggestion.Line = int.TryParse(line.Replace("Line:", "").Trim(), out var lineNum) ? lineNum : 0;
                    }
                }

                if (currentSuggestion.Description != null)
                {
                    suggestions.Add(currentSuggestion);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"⚠️ Failed to parse code quality suggestions: {ex.Message}");
            }

            return suggestions;
        }

        /// <summary>
        /// Parse complexity analysis from AI response
        /// </summary>
        private static ComplexityAnalysis ParseComplexityAnalysis(string response)
        {
            var analysis = new ComplexityAnalysis();

            try
            {
                // Simple parsing for demo - in production would use proper JSON parsing
                var lines = response.Split('\n');

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (line.Contains("Complexity:") && line.Contains("Cyclomatic"))
                        analysis.CyclomaticComplexity = int.TryParse(line.Replace("Cyclomatic Complexity:", "").Trim(), out var cc) ? cc : 0;
                    else if (line.Contains("Complexity:") && line.Contains("Cognitive"))
                        analysis.CognitiveComplexity = int.TryParse(line.Replace("Cognitive Complexity:", "").Trim(), out var cc) ? cc : 0;
                    else if (line.Contains("Lines of Code:"))
                        analysis.LinesOfCode = int.TryParse(line.Replace("Lines of Code:", "").Trim(), out var loc) ? loc : 0;
                    else if (line.Contains("Maintainability Index:"))
                        analysis.MaintainabilityIndex = int.TryParse(line.Replace("Maintainability Index:", "").Trim(), out var mi) ? mi : 0;
                    else if (line.Contains("Risk Level:"))
                        analysis.RiskLevel = line.Replace("Risk Level:", "").Trim();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"⚠️ Failed to parse complexity analysis: {ex.Message}");
            }

            return analysis;
        }
    }

    /// <summary>
    /// Performance optimization suggestion
    /// </summary>
    public class PerformanceSuggestion
    {
        public string Type { get; set; } = "";
        public string Severity { get; set; } = "";
        public int Line { get; set; }
        public string Description { get; set; } = "";
        public string Impact { get; set; } = "";
        public string Optimization { get; set; } = "";
        public string EstimatedImprovement { get; set; } = "";
    }

    /// <summary>
    /// Code quality improvement suggestion
    /// </summary>
    public class CodeQualitySuggestion
    {
        public string Type { get; set; } = "";
        public string Severity { get; set; } = "";
        public int Line { get; set; }
        public string Description { get; set; } = "";
        public string Suggestion { get; set; } = "";
        public string Benefit { get; set; } = "";
    }

    /// <summary>
    /// Code complexity analysis results
    /// </summary>
    public class ComplexityAnalysis
    {
        public int CyclomaticComplexity { get; set; }
        public int CognitiveComplexity { get; set; }
        public int LinesOfCode { get; set; }
        public int MaintainabilityIndex { get; set; }
        public string RiskLevel { get; set; } = "";
        public List<string> RefactoringRecommendations { get; set; } = new();
    }
}

