using Avalonia.Media.Imaging;

namespace Cycloside.Plugins.BuiltIn
{
    public class TextClipboardItem : ClipboardItem
    {
        public string Text => (string)Content;

        public TextClipboardItem(string text) : base(ClipboardContentType.Text, text)
        {
        }
    }

    public class ImageClipboardItem : ClipboardItem
    {
        public Bitmap Image => (Bitmap)Content;
        public string ImagePath { get; set; }

        public ImageClipboardItem(Bitmap image, string imagePath) : base(ClipboardContentType.Image, image)
        {
            ImagePath = imagePath;
        }
    }
}
