using System.Collections.Generic;

namespace Cycloside.Widgets;

/// <summary>
/// Enhanced widget layout for saving and loading widget configurations
/// </summary>
public class EnhancedWidgetLayout
{
    /// <summary>
    /// Current theme name
    /// </summary>
    public string Theme { get; set; } = "Default";

    /// <summary>
    /// List of widget data
    /// </summary>
    public List<WidgetData> Widgets { get; set; } = new();

    /// <summary>
    /// Layout metadata
    /// </summary>
    public LayoutMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Individual widget data for serialization
    /// </summary>
    public class WidgetData
    {
        /// <summary>
        /// Widget type name
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Unique instance identifier
        /// </summary>
        public string InstanceId { get; set; } = string.Empty;

        /// <summary>
        /// X position on canvas
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Y position on canvas
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Widget width
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// Widget height
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// Widget skin name
        /// </summary>
        public string Skin { get; set; } = "Default";

        /// <summary>
        /// Widget configuration data
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; } = new();

        /// <summary>
        /// Whether the widget is locked
        /// </summary>
        public bool IsLocked { get; set; }

        /// <summary>
        /// Widget z-index for layering
        /// </summary>
        public int ZIndex { get; set; }

        /// <summary>
        /// Widget opacity
        /// </summary>
        public double Opacity { get; set; } = 1.0;

        /// <summary>
        /// Whether the widget is visible
        /// </summary>
        public bool IsVisible { get; set; } = true;
    }

    /// <summary>
    /// Layout metadata
    /// </summary>
    public class LayoutMetadata
    {
        /// <summary>
        /// Layout name
        /// </summary>
        public string Name { get; set; } = "Default Layout";

        /// <summary>
        /// Layout description
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Creation timestamp
        /// </summary>
        public System.DateTime CreatedAt { get; set; } = System.DateTime.Now;

        /// <summary>
        /// Last modified timestamp
        /// </summary>
        public System.DateTime ModifiedAt { get; set; } = System.DateTime.Now;

        /// <summary>
        /// Layout version
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Layout author
        /// </summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// Layout tags for categorization
        /// </summary>
        public List<string> Tags { get; set; } = new();
    }
}