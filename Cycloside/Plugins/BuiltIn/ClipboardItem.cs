using System;

namespace Cycloside.Plugins.BuiltIn
{
    public enum ClipboardContentType
    {
        Text,
        Image
    }

    public class ClipboardItem
    {
        public ClipboardContentType ContentType { get; set; }
        public object Content { get; set; }
        public DateTime Timestamp { get; set; }

        public ClipboardItem(ClipboardContentType contentType, object content)
        {
            ContentType = contentType;
            Content = content;
            Timestamp = DateTime.Now;
        }
    }
}
