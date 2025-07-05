using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;

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
            string exePath = Application.ExecutablePath;

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
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while removing the context menu: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void CreateMenu(string rootKeyPath, string exePath)
        {
            using (var rootKey = Registry.ClassesRoot.OpenSubKey(rootKeyPath, true))
            {
                if (rootKey == null) return;

                using (var menuKey = rootKey.CreateSubKey(MenuName))
                {
                    menuKey.SetValue("SubCommands", "");

                    using (var lockKey = menuKey.CreateSubKey("shell\01Lock"))
                    {
                        lockKey.SetValue("", "Lock");
                        using (var commandKey = lockKey.CreateSubKey(CommandKey))
                        {
                            commandKey.SetValue("", $"\"{exePath}\" lock \"%1\"");
                        }
                    }

                    using (var unlockKey = menuKey.CreateSubKey("shell\02Unlock"))
                    {
                        unlockKey.SetValue("", "Unlock");
                        using (var commandKey = unlockKey.CreateSubKey(CommandKey))
                        {
                            commandKey.SetValue("", $"\"{exePath}\" unlock \"%1\"");
                        }
                    }
                }
            }
        }

        private static void RestartExplorer()
        {
            var result = MessageBox.Show("To see the changes, the Windows Explorer process needs to be restarted.\n\nDo you want to restart it now?", "Restart Explorer", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                foreach (var process in Process.GetProcessesByName("explorer"))
                {
                    process.Kill();
                }
            }
        }
    }
}