using System.Drawing;

namespace FFLocker.Logic
{
    public enum Theme
    {
        Light,
        Dark,
        System
    }

    public class AppSettings
    {
        public Theme Theme { get; set; } = Theme.System;
        public bool IsLogVisible { get; set; } = true;
        public Point WindowLocation { get; set; } = Point.Empty;
        public Size WindowSize { get; set; } = Size.Empty;
        public bool WindowMaximized { get; set; } = false;
        public bool ContextMenuEnabled { get; set; } = false;
    }
}
