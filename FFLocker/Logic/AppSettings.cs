using System.Drawing;

namespace FFLocker
{
    public class AppSettings
    {
        public bool DarkMode { get; set; } = false;
        public Point WindowLocation { get; set; } = Point.Empty;
        public Size WindowSize { get; set; } = Size.Empty;
        public bool WindowMaximized { get; set; } = false;
        public bool ContextMenuEnabled { get; set; } = false;
    }
}
