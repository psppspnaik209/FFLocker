using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FFLocker
{
    class FileMapping
    {
        public string Real { get; set; } = string.Empty;
        public string Obf { get; set; } = string.Empty;
        public byte[] FileSalt { get; set; } = Array.Empty<byte>();
        public string Hash { get; set; } = string.Empty;
    }

    class MetadataContainer
    {
        public string Version { get; set; } = "2.0";
        public byte[] GlobalSalt { get; set; } = Array.Empty<byte>();
        public List<FileMapping> Files { get; set; } = new List<FileMapping>();
        public List<FileMapping> Directories { get; set; } = new List<FileMapping>();
        public long CreatedTime { get; set; }
        public string Checksum { get; set; } = string.Empty;
    }

    class EncryptionConfig
    {
        public int ChunkSize { get; set; } = 1024 * 1024;
        public int MaxParallelism { get; set; } = Environment.ProcessorCount;
        public int BufferSize { get; set; } = 1024 * 1024;
        public int SecureRandomLength { get; set; } = 32;
        public int KeyDerivationIterations { get; set; } = 600_000;
    }

    class SecureBuffer : IDisposable
    {
        private byte[] _buffer;
        private GCHandle _handle;
        private bool _disposed;

        public SecureBuffer(int size)
        {
            _buffer = new byte[size];
            _handle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
            CryptographicOperations.ZeroMemory(_buffer);
        }

        public byte[] Buffer => _disposed ? throw new ObjectDisposedException(nameof(SecureBuffer)) : _buffer;
        public int Length => _disposed ? throw new ObjectDisposedException(nameof(SecureBuffer)) : _buffer.Length;

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_buffer != null)
                {
                    CryptographicOperations.ZeroMemory(_buffer);
                    if (_handle.IsAllocated)
                        _handle.Free();
                    _buffer = null!;
                }
                _disposed = true;
            }
        }
    }

    class SecureCrypto : IDisposable
    {
        private readonly RandomNumberGenerator _rng;
        private bool _disposed;

        public SecureCrypto()
        {
            _rng = RandomNumberGenerator.Create();
        }

        public void GetBytes(byte[] buffer)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SecureCrypto));
            _rng.GetBytes(buffer);
        }

        public void GetBytes(Span<byte> buffer)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SecureCrypto));
            _rng.GetBytes(buffer);
        }

        public string GenerateSecureFilename(int length)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SecureCrypto));
            
            var randomBytes = new byte[length];
            _rng.GetBytes(randomBytes);
            
            var base64 = Convert.ToBase64String(randomBytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
                
            return base64.Length > length ? base64.Substring(0, length) : base64;
        }

        public byte[] GenerateFileSalt()
        {
            var salt = new byte[32];
            _rng.GetBytes(salt);
            return salt;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _rng?.Dispose();
                _disposed = true;
            }
        }
    }

    class KeyDerivation : IDisposable
    {
        private bool _disposed;

        public byte[] DeriveKey(byte[] password, byte[] salt, int iterations, int keyLength)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(KeyDerivation));
            
            using var kdf = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            return kdf.GetBytes(keyLength);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
    
    class SecurePasswordReader
    {
        public static SecureBuffer ReadPassword()
        {
            var passwordChars = new List<char>();
            
            try
            {
                ConsoleKeyInfo key;
                while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
                {
                    if (key.Key == ConsoleKey.Backspace && passwordChars.Count > 0)
                    {
                        passwordChars.RemoveAt(passwordChars.Count - 1);
                        Console.Write("\b \b");
                    }
                    else if (key.KeyChar != '\0' && key.KeyChar != '\b')
                    {
                        passwordChars.Add(key.KeyChar);
                        Console.Write("*");
                    }
                }
                Console.WriteLine();

                var passwordString = new string(passwordChars.ToArray());
                var passwordBytes = Encoding.UTF8.GetBytes(passwordString);
                
                var secureBuffer = new SecureBuffer(passwordBytes.Length);
                Array.Copy(passwordBytes, secureBuffer.Buffer, passwordBytes.Length);
                
                CryptographicOperations.ZeroMemory(passwordBytes);
                passwordChars.Clear();
                
                return secureBuffer;
            }
            finally
            {
                passwordChars.Clear();
            }
        }
    }

    class DualMetadataManager
    {
        private const string PRIMARY_CONTAINER = ".fflmeta";
        private const string BACKUP_CONTAINER = ".fflbkup";
        private const string RECOVERY_CONTAINER = ".fflrcvr";

        public static void SaveMetadata(string root, MetadataContainer metadata, byte[] masterKey, SecureCrypto crypto, IProgress<string> logger)
        {
            var containers = new[]
            {
                Path.Combine(root, PRIMARY_CONTAINER),
                Path.Combine(root, BACKUP_CONTAINER), 
                Path.Combine(root, RECOVERY_CONTAINER)
            };

            var metadataJson = JsonSerializer.SerializeToUtf8Bytes(metadata);
            
            try
            {
                for (int i = 0; i < containers.Length; i++)
                {
                    var iv = new byte[12];
                    crypto.GetBytes(iv);
                    
                    var encryptedData = new byte[metadataJson.Length];
                    var tag = new byte[16];
                    
                    using (var aes = new AesGcm(masterKey))
                    {
                        aes.Encrypt(iv, metadataJson, encryptedData, tag);
                    }
                    
                    var content = new StringBuilder();
                    content.AppendLine("# FFLocker Dual-Redundant Metadata Container");
                    content.AppendLine($"# Container Type: {(i == 0 ? "Primary" : i == 1 ? "Backup" : "Recovery")}");
                    content.AppendLine($"# Created: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                    content.AppendLine($"version:2.0");
                    content.AppendLine($"container_id:{i}");
                    content.AppendLine($"global_salt:{Convert.ToBase64String(metadata.GlobalSalt)}");
                    content.AppendLine($"iv:{Convert.ToBase64String(iv)}");
                    content.AppendLine($"tag:{Convert.ToBase64String(tag)}");
                    content.AppendLine($"data:{Convert.ToBase64String(encryptedData)}");
                    
                    var tempPath = containers[i] + ".tmp";
                    File.WriteAllText(tempPath, content.ToString());
                    File.Move(tempPath, containers[i]);
                    
                    logger.Report($"Saved {(i == 0 ? "primary" : i == 1 ? "backup" : "recovery")} metadata container");
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(metadataJson);
            }
        }

        public static MetadataContainer? LoadMetadata(string root, byte[] password, IProgress<string> logger)
        {
            var containers = new[]
            {
                Path.Combine(root, PRIMARY_CONTAINER),
                Path.Combine(root, BACKUP_CONTAINER),
                Path.Combine(root, RECOVERY_CONTAINER)
            };

            using var keyDerivation = new KeyDerivation();

            for (int i = 0; i < containers.Length; i++)
            {
                var containerPath = containers[i];
                if (!File.Exists(containerPath)) continue;

                try
                {
                    logger.Report($"Attempting to load {(i == 0 ? "primary" : i == 1 ? "backup" : "recovery")} metadata container...");
                    
                    var lines = File.ReadAllLines(containerPath);
                    
                    var globalSaltB64 = lines.First(l => l.StartsWith("global_salt:")).Substring(12);
                    var ivB64 = lines.First(l => l.StartsWith("iv:")).Substring(3);
                    var tagB64 = lines.First(l => l.StartsWith("tag:")).Substring(4);
                    var dataB64 = lines.First(l => l.StartsWith("data:")).Substring(5);
                    
                    var globalSalt = Convert.FromBase64String(globalSaltB64);
                    var iv = Convert.FromBase64String(ivB64);
                    var tag = Convert.FromBase64String(tagB64);
                    var encryptedData = Convert.FromBase64String(dataB64);
                    
                    var masterKey = keyDerivation.DeriveKey(password, globalSalt, 600_000, 32);
                    
                    try
                    {
                        var decryptedData = new byte[encryptedData.Length];
                        using (var aes = new AesGcm(masterKey))
                        {
                            aes.Decrypt(iv, encryptedData, tag, decryptedData);
                        }
                        
                        var metadata = JsonSerializer.Deserialize<MetadataContainer>(decryptedData);
                        
                        CryptographicOperations.ZeroMemory(decryptedData);
                        CryptographicOperations.ZeroMemory(masterKey);
                        
                        logger.Report($"Successfully loaded {(i == 0 ? "primary" : i == 1 ? "backup" : "recovery")} metadata container");
                        return metadata;
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(masterKey);
                    }
                }
                catch (Exception ex)
                {
                    logger.Report($"Failed to load {(i == 0 ? "primary" : i == 1 ? "backup" : "recovery")} container: {ex.Message}");
                    continue;
                }
            }
            
            return null;
        }

        public static bool IsLocked(string path)
        {
            if (File.Exists(path))
            {
                return path.EndsWith(".ffl", StringComparison.OrdinalIgnoreCase);
            }
            if (Directory.Exists(path))
            {
                return File.Exists(Path.Combine(path, PRIMARY_CONTAINER)) ||
                       File.Exists(Path.Combine(path, BACKUP_CONTAINER)) ||
                       File.Exists(Path.Combine(path, RECOVERY_CONTAINER));
            }
            return false;
        }

        public static void CleanupContainers(string root, IProgress<string> logger)
        {
            var containers = new[]
            {
                Path.Combine(root, PRIMARY_CONTAINER),
                Path.Combine(root, BACKUP_CONTAINER),
                Path.Combine(root, RECOVERY_CONTAINER)
            };

            foreach (var container in containers)
            {
                try
                {
                    if (File.Exists(container))
                        File.Delete(container);
                }
                catch (Exception ex)
                {
                    logger.Report($"Warning: Could not delete container {Path.GetFileName(container)}: {ex.Message}");
                }
            }
        }
    }

    class Program
    {
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();
        private const int ATTACH_PARENT_PROCESS = -1;

        private const int SALT_SIZE = 32;
        private const int NONCE_SIZE = 12;
        private const int TAG_SIZE = 16;
        private static readonly EncryptionConfig Config = new();
        private static readonly string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                AttachConsole(ATTACH_PARENT_PROCESS);
                RunCli(args);
                Console.WriteLine();
                FreeConsole();
            }
            else
            {
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm(LoadSettings()));
            }
        }

        static void RunCli(string[] args)
        {
            if (args.Length < 2 || (args[0] != "lock" && args[0] != "unlock"))
            {
                Console.WriteLine("Usage: FFLocker lock|unlock <path>");
                return;
            }

            string cmd = args[0];
            string path = args[1];

            if (!File.Exists(path) && !Directory.Exists(path))
            {
                Console.WriteLine("Error: File or folder not found.");
                return;
            }

            Console.Write("Password: ");
            using var password = SecurePasswordReader.ReadPassword();

            var progress = new Progress<int>(p => Console.Write($"\rProgress: {p}%"));
            var logger = new Progress<string>(m => Console.WriteLine(m));

            try
            {
                if (cmd == "lock")
                {
                    if (DualMetadataManager.IsLocked(path))
                    {
                        ((IProgress<string>)logger).Report("The file or folder is already locked.");
                        return;
                    }
                    Lock(path, password, progress, logger);
                }
                else
                {
                    if (!DualMetadataManager.IsLocked(path))
                    {
                        ((IProgress<string>)logger).Report("The file or folder is not locked.");
                        return;
                    }
                    Unlock(path, password, progress, logger);
                }
                ((IProgress<string>)logger).Report("Operation completed successfully.");
            }
            catch (Exception ex)
            {
                ((IProgress<string>)logger).Report($"An error occurred: {ex.Message}");
            }
        }

        public static AppSettings LoadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            return new AppSettings();
        }

        public static void SaveSettings(AppSettings settings)
        {
            var json = JsonSerializer.Serialize(settings);
            File.WriteAllText(SettingsFilePath, json);
        }

        public static void Lock(string path, SecureBuffer password, IProgress<int> progress, IProgress<string> logger)
        {
            if (File.Exists(path))
            {
                LockFile(path, password, progress, logger);
            }
            else if (Directory.Exists(path))
            {
                LockFolder(path, password, progress, logger);
            }
        }

        public static void Unlock(string path, SecureBuffer password, IProgress<int> progress, IProgress<string> logger)
        {
            if (File.Exists(path))
            {
                UnlockFile(path, password, progress, logger);
            }
            else if (Directory.Exists(path))
            {
                UnlockFolder(path, password, progress, logger);
            }
        }

        public static void LockFolder(string root, SecureBuffer password, IProgress<int> progress, IProgress<string> logger)
        {
            var sw = Stopwatch.StartNew();
            
            using var crypto = new SecureCrypto();
            using var masterKeyBuffer = new SecureBuffer(32);
            using var globalSaltBuffer = new SecureBuffer(SALT_SIZE);
            using var keyDerivation = new KeyDerivation();
            
            crypto.GetBytes(globalSaltBuffer.Buffer);
            
            var derivedKey = keyDerivation.DeriveKey(password.Buffer, globalSaltBuffer.Buffer, Config.KeyDerivationIterations, 32);
            Array.Copy(derivedKey, masterKeyBuffer.Buffer, 32);
            CryptographicOperations.ZeroMemory(derivedKey);

            var fileInfos = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Select(f => new { Path = f, RelPath = Path.GetRelativePath(root, f), Size = new FileInfo(f).Length })
                .Where(f => f.Size > 0).ToList();

            var totalBytes = fileInfos.Sum(f => f.Size);
            logger.Report($"Encrypting {fileInfos.Count} files ({FormatSize(totalBytes)})");

            var fileMappings = new ConcurrentBag<FileMapping>();
            var processedFiles = 0;
            var processedBytes = 0L;

            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Config.MaxParallelism };

            Parallel.ForEach(fileInfos, parallelOptions, fileInfo =>
            {
                var mapping = EncryptFile(root, fileInfo.RelPath, masterKeyBuffer.Buffer, crypto, logger);
                fileMappings.Add(mapping);
                
                var processed = Interlocked.Increment(ref processedFiles);
                Interlocked.Add(ref processedBytes, fileInfo.Size);
                
                var percentage = (int)((double)processed / fileInfos.Count * 100);
                progress.Report(percentage);
            });

            var dirMappings = new List<FileMapping>();
            var dirRelPaths = Directory.GetDirectories(root, "*", SearchOption.AllDirectories)
                .Select(d => Path.GetRelativePath(root, d)).OrderByDescending(r => r.Length).ToList();

            foreach (var rel in dirRelPaths)
            {
                string original = Path.Combine(root, rel);
                string obfName = crypto.GenerateSecureFilename(Config.SecureRandomLength);
                string target = Path.Combine(Path.GetDirectoryName(original)!, obfName);

                if (Directory.Exists(original))
                {
                    Directory.Move(original, target);
                    dirMappings.Add(new FileMapping { Real = rel, Obf = obfName, FileSalt = crypto.GenerateFileSalt(), Hash = ComputeHash(rel, obfName) });
                }
            }

            var metadata = new MetadataContainer
            {
                GlobalSalt = globalSaltBuffer.Buffer.ToArray(),
                Files = fileMappings.ToList(),
                Directories = dirMappings,
                CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Checksum = ComputeMetadataChecksum(fileMappings.ToList(), dirMappings)
            };

            DualMetadataManager.SaveMetadata(root, metadata, masterKeyBuffer.Buffer, crypto, logger);

            sw.Stop();
            var encryptionRate = totalBytes / (1024 * 1024.0) / sw.Elapsed.TotalSeconds;
            logger.Report($"Folder secured in {sw.Elapsed.TotalSeconds:F2}s - Rate: {encryptionRate:F1} MB/s");
        }

        public static void UnlockFolder(string root, SecureBuffer password, IProgress<int> progress, IProgress<string> logger)
        {
            var sw = Stopwatch.StartNew();

            var metadata = DualMetadataManager.LoadMetadata(root, password.Buffer, logger);
            if (metadata == null)
            {
                throw new Exception("Unable to decrypt metadata. Invalid password or corrupted containers.");
            }

            using var masterKeyBuffer = new SecureBuffer(32);
            using var keyDerivation = new KeyDerivation();
            
            var derivedKey = keyDerivation.DeriveKey(password.Buffer, metadata.GlobalSalt, Config.KeyDerivationIterations, 32);
            Array.Copy(derivedKey, masterKeyBuffer.Buffer, 32);
            CryptographicOperations.ZeroMemory(derivedKey);

            logger.Report($"Loaded metadata: {metadata.Files.Count} files, {metadata.Directories.Count} directories");
            
            logger.Report("Restoring directory structure...");
            foreach (var dirEntry in metadata.Directories.OrderBy(e => e.Real.Length))
            {
                string obfFull = Path.Combine(root, dirEntry.Obf);
                string realFull = Path.Combine(root, dirEntry.Real);
                if (Directory.Exists(obfFull))
                    Directory.Move(obfFull, realFull);
            }

            logger.Report($"Decrypting {metadata.Files.Count} files...");
            var processedFiles = 0;

            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Config.MaxParallelism };

            Parallel.ForEach(metadata.Files, parallelOptions, fileEntry =>
            {
                DecryptFile(root, fileEntry, masterKeyBuffer.Buffer, logger);
                
                var processed = Interlocked.Increment(ref processedFiles);
                var percentage = (int)((double)processed / metadata.Files.Count * 100);
                progress.Report(percentage);
            });

            DualMetadataManager.CleanupContainers(root, logger);

            sw.Stop();
            logger.Report($"Folder unlocked in {sw.Elapsed.TotalSeconds:F2}s");
        }

        public static void LockFile(string filePath, SecureBuffer password, IProgress<int> progress, IProgress<string> logger)
        {
            string root = Path.GetDirectoryName(filePath)!;
            string relPath = Path.GetFileName(filePath);

            var sw = Stopwatch.StartNew();
            
            using var crypto = new SecureCrypto();
            using var masterKeyBuffer = new SecureBuffer(32);
            using var globalSaltBuffer = new SecureBuffer(SALT_SIZE);
            using var keyDerivation = new KeyDerivation();
            
            crypto.GetBytes(globalSaltBuffer.Buffer);
            
            var derivedKey = keyDerivation.DeriveKey(password.Buffer, globalSaltBuffer.Buffer, Config.KeyDerivationIterations, 32);
            Array.Copy(derivedKey, masterKeyBuffer.Buffer, 32);
            CryptographicOperations.ZeroMemory(derivedKey);

            logger.Report($"Encrypting file...");
            var mapping = EncryptFile(root, relPath, masterKeyBuffer.Buffer, crypto, logger);
            progress.Report(100);

            var metadata = new MetadataContainer
            {
                GlobalSalt = globalSaltBuffer.Buffer.ToArray(),
                Files = new List<FileMapping> { mapping },
                CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Checksum = ComputeMetadataChecksum(new List<FileMapping> { mapping }, new List<FileMapping>())
            };

            DualMetadataManager.SaveMetadata(root, metadata, masterKeyBuffer.Buffer, crypto, logger);

            sw.Stop();
            logger.Report($"File secured in {sw.Elapsed.TotalSeconds:F2}s");
        }

        public static void UnlockFile(string filePath, SecureBuffer password, IProgress<int> progress, IProgress<string> logger)
        {
            string root = Path.GetDirectoryName(filePath)!;
            
            var sw = Stopwatch.StartNew();

            var metadata = DualMetadataManager.LoadMetadata(root, password.Buffer, logger);
            if (metadata == null)
            {
                throw new Exception("Unable to decrypt metadata. Invalid password or corrupted containers.");
            }

            using var masterKeyBuffer = new SecureBuffer(32);
            using var keyDerivation = new KeyDerivation();
            
            var derivedKey = keyDerivation.DeriveKey(password.Buffer, metadata.GlobalSalt, Config.KeyDerivationIterations, 32);
            Array.Copy(derivedKey, masterKeyBuffer.Buffer, 32);
            CryptographicOperations.ZeroMemory(derivedKey);

            var fileEntry = metadata.Files.FirstOrDefault(f => Path.Combine(root, f.Obf) == filePath);
            if (fileEntry == null)
            {
                throw new Exception("File not found in metadata.");
            }

            logger.Report($"Decrypting file...");
            DecryptFile(root, fileEntry, masterKeyBuffer.Buffer, logger);
            progress.Report(100);

            DualMetadataManager.CleanupContainers(root, logger);

            sw.Stop();
            logger.Report($"File unlocked in {sw.Elapsed.TotalSeconds:F2}s");
        }

        static FileMapping EncryptFile(string root, string relPath, byte[] masterKey, SecureCrypto crypto, IProgress<string> logger)
        {
            string inputPath = Path.Combine(root, relPath);
            string obfName = crypto.GenerateSecureFilename(Config.SecureRandomLength) + ".ffl";
            string outputPath = Path.Combine(root, obfName);
            string tempPath = outputPath + ".tmp";

            var fileSalt = crypto.GenerateFileSalt();
            
            using var fileKeyBuffer = new SecureBuffer(32);
            
            try
            {
                DeriveFileKey(masterKey, relPath, fileSalt, fileKeyBuffer.Buffer);
                EncryptFileStream(inputPath, tempPath, fileKeyBuffer.Buffer, crypto);
                File.Move(tempPath, outputPath);
                SecureDeleteFile(inputPath);
                
                return new FileMapping { Real = relPath, Obf = obfName, FileSalt = fileSalt, Hash = ComputeHash(relPath, obfName) };
            }
            catch
            {
                try { File.Delete(tempPath); } catch { }
                try { File.Delete(outputPath); } catch { }
                throw;
            }
        }

        static void DecryptFile(string root, FileMapping entry, byte[] masterKey, IProgress<string> logger)
        {
            string encPath = Path.Combine(root, entry.Obf);
            string realPath = Path.Combine(root, entry.Real);
            string tempPath = realPath + ".tmp";

            string? dir = Path.GetDirectoryName(realPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using var fileKeyBuffer = new SecureBuffer(32);
            
            try
            {
                DeriveFileKey(masterKey, entry.Real, entry.FileSalt, fileKeyBuffer.Buffer);
                
                var expectedHash = ComputeHash(entry.Real, entry.Obf);
                if (expectedHash != entry.Hash)
                {
                    throw new InvalidDataException($"Integrity verification failed for {entry.Real}");
                }
                
                DecryptFileStream(encPath, tempPath, fileKeyBuffer.Buffer);
                File.Move(tempPath, realPath);
                SecureDeleteFile(encPath);
            }
            catch
            {
                try { File.Delete(tempPath); } catch { }
                throw;
            }
        }

        static void EncryptFileStream(string inputPath, string outputPath, byte[] key, SecureCrypto crypto)
        {
            using var inputStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, Config.BufferSize);
            using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, Config.BufferSize);
            using var writer = new BinaryWriter(outputStream);
            
            writer.Write(inputStream.Length);
            
            var buffer = new byte[Config.ChunkSize];
            int bytesRead;
            
            while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                var chunkNonce = new byte[NONCE_SIZE];
                crypto.GetBytes(chunkNonce);
                
                var chunkData = new Span<byte>(buffer, 0, bytesRead);
                var ciphertext = new byte[bytesRead];
                var tag = new byte[TAG_SIZE];
                
                using (var aes = new AesGcm(key))
                {
                    aes.Encrypt(chunkNonce, chunkData, ciphertext, tag);
                }
                
                writer.Write(chunkNonce);
                writer.Write(bytesRead);
                writer.Write(ciphertext);
                writer.Write(tag);
            }
        }

        static void DecryptFileStream(string encPath, string realPath, byte[] key)
        {
            using var inputStream = new FileStream(encPath, FileMode.Open, FileAccess.Read, FileShare.Read, Config.BufferSize);
            using var outputStream = new FileStream(realPath, FileMode.Create, FileAccess.Write, FileShare.None, Config.BufferSize);
            using var reader = new BinaryReader(inputStream);
            
            long originalFileSize = reader.ReadInt64();
            long totalWritten = 0;
            
            while (inputStream.Position < inputStream.Length)
            {
                var chunkNonce = reader.ReadBytes(NONCE_SIZE);
                int chunkSize = reader.ReadInt32();
                var ciphertext = reader.ReadBytes(chunkSize);
                var tag = reader.ReadBytes(TAG_SIZE);
                
                var plaintext = new byte[chunkSize];
                
                using (var aes = new AesGcm(key))
                {
                    aes.Decrypt(chunkNonce, ciphertext, tag, plaintext);
                }
                
                long remainingBytes = originalFileSize - totalWritten;
                int bytesToWrite = (int)Math.Min(chunkSize, remainingBytes);
                outputStream.Write(plaintext, 0, bytesToWrite);
                
                totalWritten += bytesToWrite;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void DeriveFileKey(byte[] masterKey, string filePath, byte[] fileSalt, byte[] output)
        {
            var pathBytes = Encoding.UTF8.GetBytes(filePath);
            var combined = new byte[pathBytes.Length + fileSalt.Length];
            
            Array.Copy(pathBytes, combined, pathBytes.Length);
            Array.Copy(fileSalt, 0, combined, pathBytes.Length, fileSalt.Length);
            
            using var hmac = new HMACSHA256(masterKey);
            var derivedKey = hmac.ComputeHash(combined);
            Array.Copy(derivedKey, output, Math.Min(derivedKey.Length, output.Length));
            
            CryptographicOperations.ZeroMemory(derivedKey);
            CryptographicOperations.ZeroMemory(combined);
        }

        static string ComputeHash(string realPath, string obfPath)
        {
            var combined = Encoding.UTF8.GetBytes(realPath + "|" + obfPath);
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(combined);
            return Convert.ToBase64String(hash);
        }

        static string ComputeMetadataChecksum(List<FileMapping> files, List<FileMapping> directories)
        {
            var allPaths = files.Select(f => f.Real).Concat(directories.Select(d => d.Real)).OrderBy(p => p);
            var combined = string.Join("|", allPaths);
            var bytes = Encoding.UTF8.GetBytes(combined);
            
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        static void SecureDeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    var randomData = new byte[Math.Min(fileInfo.Length, 1024 * 1024)];
                    
                    using (var crypto = new SecureCrypto())
                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Write))
                    {
                        long remaining = fileInfo.Length;
                        while (remaining > 0)
                        {
                            int toWrite = (int)Math.Min(randomData.Length, remaining);
                            crypto.GetBytes(randomData.AsSpan(0, toWrite));
                            fileStream.Write(randomData, 0, toWrite);
                            remaining -= toWrite;
                        }
                    }
                    
                    File.Delete(filePath);
                }
            }
            catch
            {
                try { File.Delete(filePath); } catch { }
            }
        }

        public static string FormatSize(long bytes)
        {
            if (bytes >= 1024 * 1024 * 1024)
                return $"{bytes / (1024 * 1024 * 1024.0):F1}GB";
            if (bytes >= 1024 * 1024)
                return $"{bytes / (1024 * 1024.0):F1}MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F1}KB";
            return $"{bytes}B";
        }
    }
}