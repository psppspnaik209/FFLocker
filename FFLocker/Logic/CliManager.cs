using FFLocker.Logic;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFLocker
{
    public static class CliManager
    {
        public static async Task Handle(string operation, string path)
        {
            Console.Clear();
            Console.WriteLine(@"
███████╗███████╗██╗      ██████╗  ██████╗██╗  ██╗███████╗██████╗ 
██╔════╝██╔════╝██║     ██╔═══██╗██╔════╝██║ ██╔╝██╔════╝██╔══██╗
█████╗  █████╗  ██║     ██║   ██║██║     █████╔╝ █████╗  ██████╔╝
██╔══╝  ██╔══╝  ██║     ██║   ██║██║     ██╔═██╗ ██╔══╝  ██╔══██╗
██║     ██║     ███████╗╚██████╔╝╚██████╗██║  ██╗███████╗██║  ██║
╚═╝     ╚═╝     ╚══════╝ ╚═════╝  ╚═════╝╚═╝  ╚═╝╚══════╝╚═╝  ╚═╝
                                                         CLI Mode
");
            try
            {
                string fullPath = Path.GetFullPath(path);

                if (operation.Equals("lock", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleLock(fullPath);
                }
                else if (operation.Equals("unlock", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleUnlock(fullPath);
                }
                else
                {
                    Console.WriteLine($"Unknown operation: {operation}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }

        private static async Task HandleLock(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                Console.WriteLine("Error: The specified file or folder does not exist.");
                return;
            }

            if (EncryptionManager.IsLocked(path))
            {
                Console.WriteLine("The selected file or folder is already locked.");
                return;
            }

            Console.Write("Enter password: ");
            var password = ReadPassword();
            Console.WriteLine(); 

            if (string.IsNullOrEmpty(password))
            {
                Console.WriteLine("Error: A password is required.");
                return;
            }

            bool useHello = false;
            if (await HelloManager.IsHelloSupportedAsync())
            {
                Console.Write("Use Windows Hello for faster unlocking? (y/n): ");
                var response = Console.ReadLine() ?? "";
                useHello = response.Trim().Equals("y", StringComparison.OrdinalIgnoreCase);
            }

            var progress = new Progress<string>(m => Console.WriteLine(m));
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                byte[]? helloKey = null;
                if (useHello)
                {
                    Console.WriteLine("Requesting Windows Hello signature to protect the master key...");
                    helloKey = await HelloManager.GenerateHelloDerivedKeyWithoutWindowAsync();

                    if (helloKey != null)
                    {
                        Console.WriteLine("Successfully derived key from Windows Hello signature.");
                    }
                    else
                    {
                        Console.WriteLine("Windows Hello operation was canceled or failed. Proceeding without it.");
                    }
                }

                using (var passwordBuffer = new SecureBuffer(Encoding.UTF8.GetByteCount(password)))
                {
                    Encoding.UTF8.GetBytes(password, 0, password.Length, passwordBuffer.Buffer, 0);
                    await Task.Run(() => EncryptionManager.Lock(path, passwordBuffer, new Progress<int>(), progress, helloKey));
                    sw.Stop();
                    Console.WriteLine($"Lock successful in {sw.Elapsed.TotalSeconds:F2}s.");
                }
            }
            finally
            {
                password = null; 
            }
        }

        private static async Task HandleUnlock(string path)
        {
            if (!EncryptionManager.IsLocked(path))
            {
                Console.WriteLine("The selected file or folder is not locked.");
                return;
            }

            bool isHelloUsed = EncryptionManager.IsHelloUsed(path);

            if (isHelloUsed)
            {
                Console.WriteLine("This item was locked with Windows Hello.");
                Console.Write("Unlock with (1) Windows Hello or (2) Password? ");
                var choice = Console.ReadLine()?.Trim();

                if (choice == "1")
                {
                    await UnlockWithHelloAsync(path);
                }
                else if (choice == "2")
                {
                    await UnlockWithPasswordAsync(path);
                }
                else
                {
                    Console.WriteLine("Invalid choice. Aborting.");
                }
            }
            else
            {
                await UnlockWithPasswordAsync(path);
            }
        }

        private static async Task UnlockWithHelloAsync(string path)
        {
            var logger = new Progress<string>(m => Console.WriteLine(m));
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                Console.WriteLine("Attempting to unlock with Windows Hello...");

                string? headerFilePath = GetHeaderFilePath(path);
                if (string.IsNullOrEmpty(headerFilePath))
                {
                    throw new InvalidOperationException("No encrypted files found to unlock.");
                }

                var header = EncryptionManager.GetFileHeader(headerFilePath);
                if (header == null || !header.IsHelloUsed)
                {
                    throw new InvalidOperationException("This item was not locked with Windows Hello or the header is corrupt.");
                }

                Console.WriteLine("Requesting Windows Hello signature to recover the master key...");
                var helloKey = await HelloManager.GenerateHelloDerivedKeyWithoutWindowAsync();
                if (helloKey == null)
                {
                    throw new Exception("Failed to unlock with Windows Hello. The operation was canceled or failed.");
                }

                var masterKeyBytes = new byte[32];
                for (int i = 0; i < 32; i++)
                {
                    masterKeyBytes[i] = (byte)(header.HelloEncryptedKey[i] ^ helloKey[i]);
                }

                using var masterKeyBuffer = new SecureBuffer(32);
                Array.Copy(masterKeyBytes, masterKeyBuffer.Buffer, 32);

                await Task.Run(() => EncryptionManager.UnlockWithMasterKey(path, masterKeyBuffer, new Progress<int>(), logger));
                sw.Stop();
                Console.WriteLine($"Unlock successful in {sw.Elapsed.TotalSeconds:F2}s.");
            }
            catch (Exception ex)
            {
                sw.Stop();
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private static async Task UnlockWithPasswordAsync(string path)
        {
            Console.Write("Enter password: ");
            var password = ReadPassword();
            Console.WriteLine();

            if (string.IsNullOrEmpty(password))
            {
                Console.WriteLine("Unlock canceled.");
                return;
            }

            var logger = new Progress<string>(m => Console.WriteLine(m));
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                using (var passwordBuffer = new SecureBuffer(Encoding.UTF8.GetByteCount(password)))
                {
                    Encoding.UTF8.GetBytes(password, 0, password.Length, passwordBuffer.Buffer, 0);
                    await Task.Run(() => EncryptionManager.Unlock(path, passwordBuffer, new Progress<int>(), logger));
                    sw.Stop();
                    Console.WriteLine($"Unlock successful in {sw.Elapsed.TotalSeconds:F2}s.");
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            finally
            {
                password = null;
            }
        }

        private static string? GetHeaderFilePath(string path)
        {
            if (Directory.Exists(path))
            {
                return Directory.EnumerateFiles(path, "*.ffl").FirstOrDefault();
            }
            else if (File.Exists(path))
            {
                return path;
            }
            return null;
        }

        private static string? ReadPassword()
        {
            var password = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    break;
                }
                if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.Length--;
                    Console.Write("\b \b"); 
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    password.Append(key.KeyChar);
                    Console.Write("*");
                }
            }
            return password.ToString();
        }
    }
}