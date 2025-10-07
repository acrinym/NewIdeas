using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Cycloside.Services
{
    /// <summary>
    /// API TESTING SERVICE - Comprehensive REST API testing and web service analysis toolkit
    /// Provides HTTP request testing, response analysis, authentication handling, and API documentation
    /// </summary>
    public static class ApiTestingService
    {
        public static event EventHandler<ApiRequestEventArgs>? ApiRequestExecuted;
        public static event EventHandler<ApiCollectionEventArgs>? ApiCollectionUpdated;

        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly ObservableCollection<ApiRequest> _requestHistory = new();
        private static readonly ObservableCollection<ApiCollection> _collections = new();
        private static readonly string _collectionsPath = Path.Combine(AppContext.BaseDirectory, "Data", "api-collections.json");

        public static ObservableCollection<ApiRequest> RequestHistory => _requestHistory;
        public static ObservableCollection<ApiCollection> Collections => _collections;

        static ApiTestingService()
        {
            // Configure HTTP client
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Cycloside-API-Testing/1.0");
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Initialize API testing service
        /// </summary>
        public static async Task InitializeAsync()
        {
            Logger.Log("🌐 Initializing API Testing Service...");

            try
            {
                await LoadCollectionsAsync();
                Logger.Log($"✅ API Testing Service initialized with {_collections.Count} collections");
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ API Testing Service initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Execute HTTP request
        /// </summary>
        public static async Task<ApiResponse> ExecuteRequestAsync(ApiRequest request)
        {
            var response = new ApiResponse
            {
                RequestId = Guid.NewGuid().ToString(),
                Request = request,
                StartTime = DateTime.Now
            };

            try
            {
                Logger.Log($"🌐 Executing API request: {request.Method} {request.Url}");

                // Create HTTP request message
                var httpRequest = new HttpRequestMessage
                {
                    Method = new HttpMethod(request.Method),
                    RequestUri = new Uri(request.Url)
                };

                // Add headers
                if (request.Headers != null)
                {
                    foreach (var header in request.Headers)
                    {
                        if (!httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value))
                        {
                            httpRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                    }
                }

                // Add body content
                if (!string.IsNullOrEmpty(request.Body))
                {
                    var contentType = request.Headers?.FirstOrDefault(h => h.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)).Value
                        ?? "application/json";

                    httpRequest.Content = new StringContent(request.Body, Encoding.UTF8, contentType);
                }

                // Execute request
                var httpResponse = await _httpClient.SendAsync(httpRequest);
                response.EndTime = DateTime.Now;
                response.Duration = response.EndTime - response.StartTime;

                // Read response
                response.StatusCode = (int)httpResponse.StatusCode;
                response.StatusMessage = httpResponse.ReasonPhrase;

                // Get response headers
                response.Headers = new Dictionary<string, string>();
                foreach (var header in httpResponse.Headers)
                {
                    response.Headers[header.Key] = string.Join(", ", header.Value);
                }

                // Get response content
                var responseContent = await httpResponse.Content.ReadAsStringAsync();
                response.Content = responseContent;

                // Try to parse as JSON for better display
                try
                {
                    var jsonElement = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    response.JsonContent = jsonElement;
                    response.IsJson = true;
                }
                catch
                {
                    response.IsJson = false;
                }

                response.Size = Encoding.UTF8.GetByteCount(responseContent);
                response.Success = httpResponse.IsSuccessStatusCode;

                // Add to history
                Dispatcher.UIThread.Post(() =>
                {
                    _requestHistory.Add(new ApiRequest
                    {
                        Id = response.RequestId,
                        Name = request.Name,
                        Method = request.Method,
                        Url = request.Url,
                        Headers = request.Headers,
                        Body = request.Body,
                        ExecutedAt = response.StartTime
                    });

                    // Keep only recent 100 requests
                    if (_requestHistory.Count > 100)
                    {
                        _requestHistory.RemoveAt(0);
                    }
                });

                OnApiRequestExecuted(response);
                Logger.Log($"✅ API request executed: {request.Method} {request.Url} - {response.StatusCode} in {response.Duration.TotalMilliseconds}ms");
            }
            catch (Exception ex)
            {
                response.EndTime = DateTime.Now;
                response.Duration = response.EndTime - response.StartTime;
                response.Success = false;
                response.Error = ex.Message;

                Logger.Log($"❌ API request failed: {ex.Message}");
            }

            return response;
        }

        /// <summary>
        /// Create new API collection
        /// </summary>
        public static async Task<ApiCollection> CreateCollectionAsync(string name, string description = "")
        {
            var collection = new ApiCollection
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Description = description,
                CreatedAt = DateTime.Now,
                Requests = new List<ApiRequest>()
            };

            _collections.Add(collection);
            await SaveCollectionsAsync();

            OnApiCollectionUpdated();
            Logger.Log($"✅ API collection created: {name}");

            return collection;
        }

        /// <summary>
        /// Add request to collection
        /// </summary>
        public static void AddRequestToCollection(string collectionId, ApiRequest request)
        {
            var collection = _collections.FirstOrDefault(c => c.Id == collectionId);
            if (collection != null)
            {
                request.CollectionId = collectionId;
                collection.Requests.Add(request);
                SaveCollectionsAsync();
                OnApiCollectionUpdated();
            }
        }

        /// <summary>
        /// Remove request from collection
        /// </summary>
        public static void RemoveRequestFromCollection(string collectionId, string requestId)
        {
            var collection = _collections.FirstOrDefault(c => c.Id == collectionId);
            if (collection != null)
            {
                collection.Requests.RemoveAll(r => r.Id == requestId);
                SaveCollectionsAsync();
                OnApiCollectionUpdated();
            }
        }

        /// <summary>
        /// Delete collection
        /// </summary>
        public static void DeleteCollection(string collectionId)
        {
            var collection = _collections.FirstOrDefault(c => c.Id == collectionId);
            if (collection != null)
            {
                _collections.Remove(collection);
                SaveCollectionsAsync();
                OnApiCollectionUpdated();
                Logger.Log($"✅ API collection deleted: {collection.Name}");
            }
        }

        /// <summary>
        /// Test authentication for API endpoint
        /// </summary>
        public static async Task<AuthenticationTestResult> TestAuthenticationAsync(string url, AuthenticationType authType, Dictionary<string, string> authParams)
        {
            var result = new AuthenticationTestResult
            {
                Url = url,
                AuthType = authType,
                TestTime = DateTime.Now
            };

            try
            {
                var request = new ApiRequest
                {
                    Method = "GET",
                    Url = url,
                    Headers = new Dictionary<string, string>()
                };

                // Configure authentication
                switch (authType)
                {
                    case AuthenticationType.BearerToken:
                        if (authParams.TryGetValue("token", out var token))
                        {
                            request.Headers["Authorization"] = $"Bearer {token}";
                        }
                        break;

                    case AuthenticationType.BasicAuth:
                        if (authParams.TryGetValue("username", out var username) &&
                            authParams.TryGetValue("password", out var password))
                        {
                            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                            request.Headers["Authorization"] = $"Basic {credentials}";
                        }
                        break;

                    case AuthenticationType.ApiKey:
                        if (authParams.TryGetValue("key", out var apiKey) &&
                            authParams.TryGetValue("header", out var header))
                        {
                            request.Headers[header] = apiKey;
                        }
                        break;

                    case AuthenticationType.OAuth2:
                        // OAuth2 implementation would go here
                        result.Success = false;
                        result.Error = "OAuth2 not yet implemented";
                        return result;
                }

                var response = await ExecuteRequestAsync(request);
                result.Success = response.Success;
                result.StatusCode = response.StatusCode;
                result.ResponseTime = response.Duration;

                if (response.Success)
                {
                    result.Message = $"Authentication successful - {response.StatusCode}";
                }
                else
                {
                    result.Message = $"Authentication failed - {response.StatusCode}: {response.Error}";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Generate API documentation from collection
        /// </summary>
        public static string GenerateApiDocumentation(ApiCollection collection)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"# {collection.Name}");
            sb.AppendLine();
            sb.AppendLine(collection.Description);
            sb.AppendLine();

            sb.AppendLine("## Endpoints");
            sb.AppendLine();

            foreach (var request in collection.Requests.OrderBy(r => r.Method).ThenBy(r => r.Url))
            {
                sb.AppendLine($"### {request.Method} {request.Url}");
                sb.AppendLine();
                sb.AppendLine($"**Description:** {request.Description ?? "No description"}");
                sb.AppendLine();

                if (request.Headers != null && request.Headers.Any())
                {
                    sb.AppendLine("**Headers:**");
                    foreach (var header in request.Headers)
                    {
                        sb.AppendLine($"- {header.Key}: {header.Value}");
                    }
                    sb.AppendLine();
                }

                if (!string.IsNullOrEmpty(request.Body))
                {
                    sb.AppendLine("**Request Body:**");
                    sb.AppendLine("```json");
                    sb.AppendLine(request.Body);
                    sb.AppendLine("```");
                    sb.AppendLine();
                }

                sb.AppendLine("---");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Import collection from file
        /// </summary>
        public static async Task<bool> ImportCollectionAsync(string filePath)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var collections = JsonSerializer.Deserialize<List<ApiCollection>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (collections != null)
                {
                    foreach (var collection in collections)
                    {
                        _collections.Add(collection);
                    }

                    await SaveCollectionsAsync();
                    OnApiCollectionUpdated();
                    Logger.Log($"✅ Imported {collections.Count} API collections from {filePath}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Failed to import API collections: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Export collection to file
        /// </summary>
        public static async Task<bool> ExportCollectionAsync(string collectionId, string filePath)
        {
            try
            {
                var collection = _collections.FirstOrDefault(c => c.Id == collectionId);
                if (collection == null) return false;

                var collections = new List<ApiCollection> { collection };
                var json = JsonSerializer.Serialize(collections, new JsonSerializerOptions { WriteIndented = true });

                await File.WriteAllTextAsync(filePath, json);
                Logger.Log($"✅ Exported API collection to {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Failed to export API collection: {ex.Message}");
                return false;
            }
        }

        // Helper methods
        private static async Task LoadCollectionsAsync()
        {
            try
            {
                if (File.Exists(_collectionsPath))
                {
                    var json = await File.ReadAllTextAsync(_collectionsPath);
                    var collections = JsonSerializer.Deserialize<List<ApiCollection>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (collections != null)
                    {
                        _collections.Clear();
                        foreach (var collection in collections)
                        {
                            _collections.Add(collection);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Failed to load API collections: {ex.Message}");
            }
        }

        private static async Task SaveCollectionsAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_collections.ToList(), new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_collectionsPath, json);
            }
            catch (Exception ex)
            {
                Logger.Log($"❌ Failed to save API collections: {ex.Message}");
            }
        }

        // Event handlers
        private static void OnApiRequestExecuted(ApiResponse response)
        {
            ApiRequestExecuted?.Invoke(null, new ApiRequestEventArgs(response));
        }

        private static void OnApiCollectionUpdated()
        {
            ApiCollectionUpdated?.Invoke(null, new ApiCollectionEventArgs(_collections.ToList()));
        }
    }

    // Data models
    public class ApiRequest
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Method { get; set; } = "GET";
        public string Url { get; set; } = "";
        public string Description { get; set; } = "";
        public Dictionary<string, string>? Headers { get; set; }
        public string Body { get; set; } = "";
        public string CollectionId { get; set; } = "";
        public DateTime ExecutedAt { get; set; }
    }

    public class ApiResponse
    {
        public string RequestId { get; set; } = "";
        public ApiRequest Request { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int StatusCode { get; set; }
        public string? StatusMessage { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
        public string Content { get; set; } = "";
        public JsonElement? JsonContent { get; set; }
        public bool IsJson { get; set; }
        public long Size { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
    }

    public class ApiCollection
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public List<ApiRequest> Requests { get; set; } = new();
    }

    public class AuthenticationTestResult
    {
        public string Url { get; set; } = "";
        public AuthenticationType AuthType { get; set; }
        public DateTime TestTime { get; set; }
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
    }

    public enum AuthenticationType
    {
        None,
        BearerToken,
        BasicAuth,
        ApiKey,
        OAuth2
    }

    // Event args
    public class ApiRequestEventArgs : EventArgs
    {
        public ApiResponse Response { get; }

        public ApiRequestEventArgs(ApiResponse response)
        {
            Response = response;
        }
    }

    public class ApiCollectionEventArgs : EventArgs
    {
        public List<ApiCollection> Collections { get; }

        public ApiCollectionEventArgs(List<ApiCollection> collections)
        {
            Collections = collections;
        }
    }
}
