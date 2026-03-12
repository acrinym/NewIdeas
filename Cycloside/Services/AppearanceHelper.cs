using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit;

namespace Cycloside.Services
{
    public enum SemanticButtonRole
    {
        Accent,
        Success,
        Danger,
        Warning,
        Neutral
    }

    /// <summary>
    /// Applies shared semantic classes to code-built UI so themes and skins can restyle it.
    /// </summary>
    public static class AppearanceHelper
    {
        private static readonly string[] ButtonRoleClasses =
        {
            "accent",
            "success",
            "danger",
            "warning",
            "neutral"
        };

        public static void ApplyButtonRole(Button button, SemanticButtonRole role)
        {
            RemoveClasses(button, ButtonRoleClasses);

            var className = role switch
            {
                SemanticButtonRole.Accent => "accent",
                SemanticButtonRole.Success => "success",
                SemanticButtonRole.Danger => "danger",
                SemanticButtonRole.Warning => "warning",
                _ => "neutral"
            };

            AddClass(button, className);
        }

        public static void ApplyCardSurface(Border border)
        {
            AddClass(border, "surface-card");
        }

        public static void ApplyStatusChip(Border border)
        {
            AddClass(border, "status-chip");
        }

        public static void ApplyWarningPanel(Border border)
        {
            AddClass(border, "warning-panel");
        }

        public static void ApplyAccentStrip(Border border)
        {
            AddClass(border, "accent-strip");
        }

        public static void ApplyAccentDivider(GridSplitter splitter)
        {
            AddClass(splitter, "accent-divider");
        }

        public static void ApplySecondaryText(TextBlock textBlock)
        {
            AddClass(textBlock, "secondary");
        }

        public static void ApplyWarningText(TextBlock textBlock)
        {
            AddClass(textBlock, "warning");
        }

        public static void ApplyInverseText(TextBlock textBlock)
        {
            AddClass(textBlock, "inverse");
        }

        public static void ApplyCodeEditor(TextEditor editor)
        {
            AddClass(editor, "code-editor");
        }

        private static void AddClass(StyledElement element, string className)
        {
            if (!element.Classes.Contains(className))
            {
                element.Classes.Add(className);
            }
        }

        private static void RemoveClasses(StyledElement element, string[] classNames)
        {
            foreach (var className in classNames)
            {
                if (element.Classes.Contains(className))
                {
                    element.Classes.Remove(className);
                }
            }
        }
    }
}
