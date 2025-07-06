using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Security.Principal;

namespace FFLocker
{
    public static class RegistryManager
    {
        private const string MenuName = "FFLocker";
        private const string FileKey = @"*\shell";
        private const string FolderKey = @"Directory\shell";
        private const string CommandKey = "command";

        public static bool IsAdmin()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public static bool IsContextMenuEnabled()
        {
            try
            {
                using (var key = Registry.ClassesRoot.OpenSubKey(FileKey))
                {
                    return key?.OpenSubKey(MenuName) != null;
                }
            }
            catch { return false; }
        }

        public static void AddContextMenu()
        {
            string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath)) return;

            // For files
            CreateMenu(FileKey, exePath);

            // For folders
            CreateMenu(FolderKey, exePath);

            RestartExplorer();
        }

        public static void RemoveContextMenu()
        {
            try
            {
                using (var key = Registry.ClassesRoot.OpenSubKey(FileKey, true))
                {
                    key?.DeleteSubKeyTree(MenuName, false);
                }
                using (var key = Registry.ClassesRoot.OpenSubKey(FolderKey, true))
                {
                    key?.DeleteSubKeyTree(MenuName, false);
                }

                RestartExplorer();
            }
            catch (Exception)
            {
                // TODO: Show message box
            }
        }

        private static void CreateMenu(string rootKeyPath, string exePath)
        {
            using (var rootKey = Registry.ClassesRoot.OpenSubKey(rootKeyPath, true))
            {
                if (rootKey == null) return;

                // Create the main FFLocker menu item
                using (var menuKey = rootKey.CreateSubKey(MenuName))
                {
                    menuKey.SetValue("MUIVerb", MenuName);
                    menuKey.SetValue("Icon", $"\"{exePath}\"");
                    menuKey.SetValue("SubCommands", ""); // Indicates a submenu

                    // The container for the sub-commands
                    using (var shellKey = menuKey.CreateSubKey("shell"))
                    {
                        // "Lock" sub-command
                        using (var lockKey = shellKey.CreateSubKey("01Lock"))
                        {
                            lockKey.SetValue("MUIVerb", "Lock");
                            using (var commandKey = lockKey.CreateSubKey(CommandKey))
                            {
                                commandKey.SetValue("", $"\"{exePath}\" lock \"%1\"");
                            }
                        }

                        // "Unlock" sub-command
                        using (var unlockKey = shellKey.CreateSubKey("02Unlock"))
                        {
                            unlockKey.SetValue("MUIVerb", "Unlock");
                            using (var commandKey = unlockKey.CreateSubKey(CommandKey))
                            {
                                commandKey.SetValue("", $"\"{exePath}\" unlock \"%1\"");
                            }
                        }
                    }
                }
            }
        }

        private static void RestartExplorer()
        {
            // TODO: Show message box
        }
    }
}
