namespace Cycloside.Services
{
    /// <summary>
    /// Shared constants for theme-related validation and resolution.
    /// </summary>
    internal static class ThemeConstants
    {
        public const int MaxDependencyDepth = 10;
        public const int MaxXmlEntityCharacters = 1024;
        public const int FileUriPrefixLength = 8; // "file:///"
    }
}
