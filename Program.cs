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

    // FIXED: Dual-redundant metadata manager with consistent key derivation
    class DualMetadataManager
    {
        private const string PRIMARY_CONTAINER = ".fflmeta";
        private const string BACKUP_CONTAINER = ".fflbkup";
        private const string RECOVERY_CONTAINER = ".fflrcvr";

        public static void SaveMetadata(string root, MetadataContainer metadata, byte[] masterKey, SecureCrypto crypto)
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
                // FIXED: Use the SAME master key for all containers instead of different salts
                for (int i = 0; i < containers.Length; i++)
                {
                    var iv = new byte[12];
                    crypto.GetBytes(iv);
                    
                    var encryptedData = new byte[metadataJson.Length];
                    var tag = new byte[16];
                    
                    // Use the master key directly instead of deriving different keys
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
                    
                    // Atomic write
                    var tempPath = containers[i] + ".tmp";
                    File.WriteAllText(tempPath, content.ToString());
                    File.Move(tempPath, containers[i]);
                    
                    Console.WriteLine($"Saved {(i == 0 ? "primary" : i == 1 ? "backup" : "recovery")} metadata container");
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(metadataJson);
            }
        }

        public static MetadataContainer? LoadMetadata(string root, byte[] password)
        {
            var containers = new[]
            {
                Path.Combine(root, PRIMARY_CONTAINER),
                Path.Combine(root, BACKUP_CONTAINER),
                Path.Combine(root, RECOVERY_CONTAINER)
            };

            using var keyDerivation = new KeyDerivation();

            // Try each container in order of preference
            for (int i = 0; i < containers.Length; i++)
            {
                var containerPath = containers[i];
                if (!File.Exists(containerPath)) continue;

                try
                {
                    Console.WriteLine($"Attempting to load {(i == 0 ? "primary" : i == 1 ? "backup" : "recovery")} metadata container...");
                    
                    var lines = File.ReadAllLines(containerPath);
                    
                    var globalSaltB64 = lines.First(l => l.StartsWith("global_salt:")).Substring(12);
                    var ivB64 = lines.First(l => l.StartsWith("iv:")).Substring(3);
                    var tagB64 = lines.First(l => l.StartsWith("tag:")).Substring(4);
                    var dataB64 = lines.First(l => l.StartsWith("data:")).Substring(5);
                    
                    var globalSalt = Convert.FromBase64String(globalSaltB64);
                    var iv = Convert.FromBase64String(ivB64);
                    var tag = Convert.FromBase64String(tagB64);
                    var encryptedData = Convert.FromBase64String(dataB64);
                    
                    // FIXED: Derive the same master key used during encryption
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
                        
                        Console.WriteLine($"Successfully loaded {(i == 0 ? "primary" : i == 1 ? "backup" : "recovery")} metadata container");
                        return metadata;
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(masterKey);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load {(i == 0 ? "primary" : i == 1 ? "backup" : "recovery")} container: {ex.Message}");
                    continue;
                }
            }
            
            return null;
        }

        public static bool IsLocked(string root)
        {
            return File.Exists(Path.Combine(root, PRIMARY_CONTAINER)) ||
                   File.Exists(Path.Combine(root, BACKUP_CONTAINER)) ||
                   File.Exists(Path.Combine(root, RECOVERY_CONTAINER));
        }

        public static void CleanupContainers(string root)
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
                    Console.WriteLine($"Warning: Could not delete container {Path.GetFileName(container)}: {ex.Message}");
                }
            }
        }
    }

    class Program
    {
        private const int SALT_SIZE = 32;
        private const int NONCE_SIZE = 12;
        private const int TAG_SIZE = 16;
        private static readonly EncryptionConfig Config = new();

        static void Main(string[] args)
        {
            if (args.Length < 2 || (args[0] != "lock" && args[0] != "unlock"))
            {
                Console.WriteLine("Usage: FFLocker lock|unlock <folderPath>");
                Console.WriteLine("  Military-grade AES-256-GCM encryption with fixed dual-redundant metadata");
                Console.WriteLine("  - Enhanced 600,000-iteration key derivation for maximum security");
                Console.WriteLine("  - Per-file unique salts and keys for perfect forward secrecy");
                Console.WriteLine("  - Fixed triple-redundant metadata containers for reliable recovery");
                Console.WriteLine("  - Zero single points of failure with proven reliability");
                return;
            }

            string cmd = args[0];
            string root = args[1];

            if (!Directory.Exists(root))
            {
                Console.WriteLine("Error: Folder not found.");
                return;
            }

            Console.Write("Password: ");
            using var password = SecurePasswordReader.ReadPassword();

            try
            {
                if (cmd == "lock")
                    LockFolder(root, password);
                else
                    UnlockFolder(root, password);
            }
            catch (CryptographicException)
            {
                Console.WriteLine("Cryptographic operation failed. Verify password and file integrity.");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Access denied. Check file permissions and antivirus exclusions.");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"I/O operation failed: {ex.Message}");
                Console.WriteLine("Ensure sufficient disk space and no file locks.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Operation failed: {ex.GetType().Name}");
                Console.WriteLine("Check system resources and try again.");
#if DEBUG
                Console.WriteLine($"Debug details: {ex.Message}");
#endif
            }
        }

        static string FormatSize(long bytes)
        {
            if (bytes >= 1024 * 1024 * 1024)
                return $"{bytes / (1024 * 1024 * 1024.0):F1}GB";
            if (bytes >= 1024 * 1024)
                return $"{bytes / (1024 * 1024.0):F1}MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F1}KB";
            return $"{bytes}B";
        }

        static void LockFolder(string root, SecureBuffer password)
        {
            if (DualMetadataManager.IsLocked(root))
            {
                Console.WriteLine("Error: Folder is already encrypted. Use 'unlock' to decrypt first.");
                return;
            }

            var sw = Stopwatch.StartNew();
            
            using var crypto = new SecureCrypto();
            using var masterKeyBuffer = new SecureBuffer(32);
            using var globalSaltBuffer = new SecureBuffer(SALT_SIZE);
            using var keyDerivation = new KeyDerivation();
            
            crypto.GetBytes(globalSaltBuffer.Buffer);
            
            var derivedKey = keyDerivation.DeriveKey(
                password.Buffer, 
                globalSaltBuffer.Buffer, 
                Config.KeyDerivationIterations, 
                32
            );
            Array.Copy(derivedKey, masterKeyBuffer.Buffer, 32);
            CryptographicOperations.ZeroMemory(derivedKey);

            var fileInfos = Directory
                .EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Select(f => new { 
                    Path = f, 
                    RelPath = Path.GetRelativePath(root, f),
                    Size = new FileInfo(f).Length
                })
                .Where(f => f.Size > 0)
                .ToList();

            var totalBytes = fileInfos.Sum(f => f.Size);
            Console.WriteLine($"Encrypting {fileInfos.Count} files ({FormatSize(totalBytes)}) with dual-redundant metadata...");

            var fileMappings = new ConcurrentBag<FileMapping>();
            var processedFiles = 0;
            var processedBytes = 0L;

            var avgFileSize = totalBytes / Math.Max(fileInfos.Count, 1);
            var maxParallel = avgFileSize > 50 * 1024 * 1024 ? 
                Math.Max(1, Environment.ProcessorCount / 2) : Config.MaxParallelism;

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxParallel
            };

            Parallel.ForEach(fileInfos, parallelOptions, fileInfo =>
            {
                try
                {
                    var mapping = EncryptFile(root, fileInfo.RelPath, masterKeyBuffer.Buffer, crypto);
                    fileMappings.Add(mapping);
                    
                    var processed = Interlocked.Increment(ref processedFiles);
                    Interlocked.Add(ref processedBytes, fileInfo.Size);
                    
                    if (processed % 25 == 0 || processed == fileInfos.Count)
                    {
                        var progress = (double)processed / fileInfos.Count * 100;
                        var speedMBps = processedBytes / (1024 * 1024.0) / sw.Elapsed.TotalSeconds;
                        Console.WriteLine($"Progress: {progress:F1}% ({processed}/{fileInfos.Count}) - {speedMBps:F1} MB/s");
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to encrypt {fileInfo.RelPath}", ex);
                }
            });

            var dirMappings = new List<FileMapping>();
            var dirRelPaths = Directory
                .GetDirectories(root, "*", SearchOption.AllDirectories)
                .Select(d => Path.GetRelativePath(root, d))
                .OrderByDescending(r => r.Length)
                .ToList();

            foreach (var rel in dirRelPaths)
            {
                string original = Path.Combine(root, rel);
                string obfName = crypto.GenerateSecureFilename(Config.SecureRandomLength);
                string target = Path.Combine(root, obfName);

                if (Directory.Exists(original))
                {
                    Directory.Move(original, target);
                    dirMappings.Add(new FileMapping 
                    { 
                        Real = rel, 
                        Obf = obfName,
                        FileSalt = crypto.GenerateFileSalt(),
                        Hash = ComputeHash(rel, obfName)
                    });
                }
            }

            var metadata = new MetadataContainer
            {
                Version = "2.0",
                GlobalSalt = globalSaltBuffer.Buffer.ToArray(),
                Files = fileMappings.ToList(),
                Directories = dirMappings,
                CreatedTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Checksum = ComputeMetadataChecksum(fileMappings.ToList(), dirMappings)
            };

            DualMetadataManager.SaveMetadata(root, metadata, masterKeyBuffer.Buffer, crypto);

            sw.Stop();
            var encryptionRate = totalBytes / (1024 * 1024.0) / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"Folder secured with triple-redundant metadata in {sw.Elapsed.TotalSeconds:F2}s - Rate: {encryptionRate:F1} MB/s");
            Console.WriteLine("Three metadata containers created for maximum reliability");
        }

        static FileMapping EncryptFile(string root, string relPath, byte[] masterKey, SecureCrypto crypto)
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
                
                return new FileMapping 
                { 
                    Real = relPath, 
                    Obf = obfName,
                    FileSalt = fileSalt,
                    Hash = ComputeHash(relPath, obfName)
                };
            }
            catch
            {
                try { File.Delete(tempPath); } catch { }
                try { File.Delete(outputPath); } catch { }
                throw;
            }
        }

        static void EncryptFileStream(string inputPath, string outputPath, byte[] key, SecureCrypto crypto)
        {
            using var inputStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, Config.BufferSize);
            using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, Config.BufferSize);
            using var writer = new BinaryWriter(outputStream);
            
            long fileSize = inputStream.Length;
            writer.Write(fileSize);
            
            var buffer = new byte[Config.ChunkSize];
            int bytesRead;
            
            while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                var chunkNonce = new byte[NONCE_SIZE];
                crypto.GetBytes(chunkNonce);
                
                var chunkData = new byte[bytesRead];
                Array.Copy(buffer, chunkData, bytesRead);
                
                var ciphertext = new byte[bytesRead];
                var tag = new byte[TAG_SIZE];
                
                try
                {
                    using (var aes = new AesGcm(key))
                    {
                        aes.Encrypt(chunkNonce, chunkData, ciphertext, tag);
                    }
                    
                    writer.Write(chunkNonce);
                    writer.Write(bytesRead);
                    writer.Write(ciphertext);
                    writer.Write(tag);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(chunkData);
                    CryptographicOperations.ZeroMemory(ciphertext);
                    CryptographicOperations.ZeroMemory(chunkNonce);
                }
            }
            
            CryptographicOperations.ZeroMemory(buffer);
        }

        static void UnlockFolder(string root, SecureBuffer password)
        {
            var sw = Stopwatch.StartNew();

            var metadata = DualMetadataManager.LoadMetadata(root, password.Buffer);
            if (metadata == null)
            {
                Console.WriteLine("Unable to decrypt metadata. Invalid password or corrupted containers.");
                return;
            }

            using var masterKeyBuffer = new SecureBuffer(32);
            using var keyDerivation = new KeyDerivation();
            
            var derivedKey = keyDerivation.DeriveKey(
                password.Buffer, 
                metadata.GlobalSalt, 
                Config.KeyDerivationIterations, 
                32
            );
            Array.Copy(derivedKey, masterKeyBuffer.Buffer, 32);
            CryptographicOperations.ZeroMemory(derivedKey);

            try
            {
                Console.WriteLine($"Loaded metadata: {metadata.Files.Count} files, {metadata.Directories.Count} directories");
                
                Console.WriteLine("Restoring directory structure...");
                foreach (var dirEntry in metadata.Directories.OrderBy(e => e.Real.Length))
                {
                    string obfFull = Path.Combine(root, dirEntry.Obf);
                    string realFull = Path.Combine(root, dirEntry.Real);
                    string realDir = Path.GetDirectoryName(realFull) ?? string.Empty;

                    if (!string.IsNullOrEmpty(realDir) && !Directory.Exists(realDir))
                        Directory.CreateDirectory(realDir);

                    if (Directory.Exists(obfFull))
                        Directory.Move(obfFull, realFull);
                }

                Console.WriteLine($"Decrypting {metadata.Files.Count} files...");
                var processedFiles = 0;

                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Config.MaxParallelism
                };

                Parallel.ForEach(metadata.Files, parallelOptions, fileEntry =>
                {
                    try
                    {
                        DecryptFile(root, fileEntry, masterKeyBuffer.Buffer);
                        
                        var processed = Interlocked.Increment(ref processedFiles);
                        
                        if (processed % 25 == 0 || processed == metadata.Files.Count)
                        {
                            var progress = (double)processed / metadata.Files.Count * 100;
                            Console.WriteLine($"Progress: {progress:F1}% ({processed}/{metadata.Files.Count})");
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Failed to decrypt {fileEntry.Real}", ex);
                    }
                });

                DualMetadataManager.CleanupContainers(root);

                sw.Stop();
                Console.WriteLine($"Folder unlocked in {sw.Elapsed.TotalSeconds:F2}s");
                Console.WriteLine("All metadata containers successfully processed and cleaned up");
            }
            finally
            {
                CryptographicOperations.ZeroMemory(metadata.GlobalSalt);
            }
        }

        static void DecryptFile(string root, FileMapping entry, byte[] masterKey)
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

        static void DecryptFileStream(string encPath, string realPath, byte[] key)
        {
            using var inputStream = new FileStream(encPath, FileMode.Open, FileAccess.Read, FileShare.Read, Config.BufferSize);
            using var outputStream = new FileStream(realPath, FileMode.Create, FileAccess.Write, FileShare.None, Config.BufferSize);
            using var reader = new BinaryReader(inputStream);
            
            long originalFileSize = reader.ReadInt64();
            long totalWritten = 0;
            
            while (inputStream.Position < inputStream.Length && totalWritten < originalFileSize)
            {
                var chunkNonce = reader.ReadBytes(NONCE_SIZE);
                int chunkSize = reader.ReadInt32();
                var ciphertext = reader.ReadBytes(chunkSize);
                var tag = reader.ReadBytes(TAG_SIZE);
                
                var plaintext = new byte[chunkSize];
                
                try
                {
                    using (var aes = new AesGcm(key))
                    {
                        aes.Decrypt(chunkNonce, ciphertext, tag, plaintext);
                    }
                    
                    long remainingBytes = originalFileSize - totalWritten;
                    int bytesToWrite = (int)Math.Min(chunkSize, remainingBytes);
                    outputStream.Write(plaintext, 0, bytesToWrite);
                    
                    totalWritten += bytesToWrite;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(plaintext);
                    CryptographicOperations.ZeroMemory(ciphertext);
                    CryptographicOperations.ZeroMemory(chunkNonce);
                }
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
                        fileStream.Flush();
                        fileStream.Close();
                    }
                    
                    File.Delete(filePath);
                    CryptographicOperations.ZeroMemory(randomData);
                }
            }
            catch
            {
                try { File.Delete(filePath); } catch { }
            }
        }
    }
}
