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

namespace FFLocker
{
    class Mapping
    {
        public string Real { get; set; } = string.Empty;
        public string Obf { get; set; } = string.Empty;
        public long Size { get; set; }
        public bool IsPartial { get; set; }
    }

    class EncryptionConfig
    {
        public bool UseIntermittentEncryption { get; set; } = false;
        public int ChunkSize { get; set; } = 1024 * 1024; // 1MB chunks for streaming
        public int FastModeChunkSize { get; set; } = 1024 * 1024; // 1MB chunks for fast mode
        public int FastModeEncryptSize { get; set; } = 64 * 1024; // Encrypt 64KB per chunk in fast mode
        public long PartialThreshold { get; set; } = 50 * 1024 * 1024; // 50MB - files larger than this use partial encryption
        public int MaxParallelism { get; set; } = Environment.ProcessorCount;
        public int BufferSize { get; set; } = 1024 * 1024; // 1MB buffer
    }

    class Program
    {
        private const int PBKDF2_ITERATIONS = 100_000;
        private const string ContainerName = ".fflcontainer";
        private static readonly EncryptionConfig Config = new();

        static void Main(string[] args)
        {
            if (args.Length < 2 || (args[0] != "lock" && args[0] != "unlock"))
            {
                Console.WriteLine("Usage: FFLocker lock|unlock <folderPath> [--fast] [--partial-threshold=50MB]");
                Console.WriteLine("  --fast: Enable chunk-level intermittent encryption for faster processing");
                Console.WriteLine("  --partial-threshold=SIZE: Files larger than SIZE use partial encryption");
                return;
            }

            string cmd = args[0];
            string root = args[1];
            string cPath = Path.Combine(root, ContainerName);

            // Parse additional arguments
            ParseArguments(args.Skip(2));

            if (!Directory.Exists(root))
            {
                Console.WriteLine("Error: Folder not found.");
                return;
            }

            Console.Write("Password: ");
            string password = ReadPassword();

            if (cmd == "lock")
                LockFolder(root, password, cPath);
            else
                UnlockFolder(root, password, cPath);
        }

        static void ParseArguments(IEnumerable<string> args)
        {
            foreach (var arg in args)
            {
                if (arg == "--fast")
                {
                    Config.UseIntermittentEncryption = true;
                    double ratio = (double)Config.FastModeEncryptSize / Config.FastModeChunkSize * 100;
                    Console.WriteLine($"Fast mode enabled: Encrypting {Config.FastModeEncryptSize / 1024}KB per {Config.FastModeChunkSize / 1024}KB chunk ({ratio:F1}% encryption ratio)");
                }
                else if (arg.StartsWith("--partial-threshold="))
                {
                    string sizeStr = arg.Substring("--partial-threshold=".Length);
                    if (TryParseSize(sizeStr, out long size))
                    {
                        Config.PartialThreshold = size;
                        Console.WriteLine($"Partial encryption threshold set to: {FormatSize(size)}");
                    }
                }
            }
        }

        static bool TryParseSize(string sizeStr, out long bytes)
        {
            bytes = 0;
            if (string.IsNullOrEmpty(sizeStr)) return false;

            string numStr = sizeStr.ToUpper();
            long multiplier = 1;

            if (numStr.EndsWith("KB"))
            {
                multiplier = 1024;
                numStr = numStr.Substring(0, numStr.Length - 2);
            }
            else if (numStr.EndsWith("MB"))
            {
                multiplier = 1024 * 1024;
                numStr = numStr.Substring(0, numStr.Length - 2);
            }
            else if (numStr.EndsWith("GB"))
            {
                multiplier = 1024 * 1024 * 1024;
                numStr = numStr.Substring(0, numStr.Length - 2);
            }

            if (long.TryParse(numStr, out long num))
            {
                bytes = num * multiplier;
                return true;
            }
            return false;
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

        static void LockFolder(string root, string password, string cPath)
        {
            if (File.Exists(cPath))
            {
                Console.WriteLine("Error: Folder is already locked.");
                return;
            }

            var sw = Stopwatch.StartNew();
            
            // 1) Salt & key derivation
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            var kdf = new Rfc2898DeriveBytes(password, salt, PBKDF2_ITERATIONS, HashAlgorithmName.SHA256);
            byte[] masterKey = kdf.GetBytes(32);

            // 2) Collect all files with size info
            var fileInfos = Directory
                .EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Select(f => new { 
                    Path = f, 
                    RelPath = Path.GetRelativePath(root, f),
                    Size = new FileInfo(f).Length
                })
                .Where(f => f.Size > 0) // Skip empty files
                .ToList();

            Console.WriteLine($"Processing {fileInfos.Count} files ({FormatSize(fileInfos.Sum(f => f.Size))})...");

            var entries = new ConcurrentBag<Mapping>();
            var processedFiles = 0;
            var totalFiles = fileInfos.Count;
            var processedBytes = 0L;
            var totalBytes = fileInfos.Sum(f => f.Size);

            // 3) Adjust parallelism based on file sizes to avoid memory exhaustion
            var avgFileSize = totalBytes / Math.Max(totalFiles, 1);
            var maxParallel = avgFileSize > 100 * 1024 * 1024 ? 
                Math.Max(1, Environment.ProcessorCount / 2) : Config.MaxParallelism;

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxParallel
            };

            Parallel.ForEach(fileInfos, parallelOptions, fileInfo =>
            {
                try
                {
                    var mapping = EncryptFile(root, fileInfo.RelPath, fileInfo.Size, masterKey);
                    entries.Add(mapping);
                    
                    var processed = Interlocked.Increment(ref processedFiles);
                    var processedSize = Interlocked.Add(ref processedBytes, fileInfo.Size);
                    
                    if (processed % 50 == 0 || processed == totalFiles)
                    {
                        var progress = (double)processed / totalFiles * 100;
                        var speedMBps = processedSize / (1024 * 1024.0) / sw.Elapsed.TotalSeconds;
                        Console.WriteLine($"Progress: {progress:F1}% ({processed}/{totalFiles}) - {speedMBps:F1} MB/s");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error encrypting {fileInfo.RelPath}: {ex.Message}");
                }
            });

            // 4) Directory obfuscation (sequential for safety)
            var dirRelPaths = Directory
                .GetDirectories(root, "*", SearchOption.AllDirectories)
                .Select(d => Path.GetRelativePath(root, d))
                .OrderByDescending(r => r.Length)
                .ToList();

            foreach (var rel in dirRelPaths)
            {
                string original = Path.Combine(root, rel);
                string obfName = Path.GetRandomFileName();
                string target = Path.Combine(root, obfName);

                Directory.Move(original, target);
                entries.Add(new Mapping { Real = rel, Obf = obfName, Size = 0, IsPartial = false });
            }

            // 5) Save metadata
            SaveMetadata(cPath, entries.ToList(), salt, masterKey);

            sw.Stop();
            var encryptionRate = totalBytes / (1024 * 1024.0) / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"Folder locked in {sw.Elapsed.TotalSeconds:F2}s - Rate: {encryptionRate:F1} MB/s");
        }

        static Mapping EncryptFile(string root, string relPath, long fileSize, byte[] masterKey)
        {
            string inputPath = Path.Combine(root, relPath);
            string obfName = Path.GetRandomFileName() + ".ffl";
            string outPath = Path.Combine(root, obfName);

            // Determine if we should use partial encryption
            bool usePartial = Config.UseIntermittentEncryption && fileSize > Config.PartialThreshold;

            // Derive per-file key
            byte[] fileKey = DeriveFileKey(masterKey, relPath);

            if (usePartial)
            {
                EncryptFileStreamFast(inputPath, outPath, fileKey);
            }
            else
            {
                EncryptFileStreamFull(inputPath, outPath, fileKey);
            }

            File.Delete(inputPath);
            return new Mapping { Real = relPath, Obf = obfName, Size = fileSize, IsPartial = usePartial };
        }

        static void EncryptFileStreamFull(string inputPath, string outputPath, byte[] key)
        {
            using var inputStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, Config.BufferSize);
            using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, Config.BufferSize);
            using var writer = new BinaryWriter(outputStream);
            
            // Write file header for full encryption
            byte[] masterNonce = RandomNumberGenerator.GetBytes(12);
            long fileSize = inputStream.Length;
            writer.Write((byte)0); // Mode: 0 = full encryption
            writer.Write(masterNonce);
            writer.Write(fileSize);
            
            byte[] buffer = new byte[Config.ChunkSize];
            int chunkIndex = 0;
            int bytesRead;
            
            while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                // Create unique nonce for each chunk
                byte[] chunkNonce = CreateChunkNonce(masterNonce, chunkIndex);
                
                // Prepare data for this chunk
                byte[] chunkData = new byte[bytesRead];
                Array.Copy(buffer, chunkData, bytesRead);
                
                // Encrypt entire chunk
                byte[] ciphertext = new byte[bytesRead];
                byte[] tag = new byte[16];
                
                using (var aes = new AesGcm(key))
                {
                    aes.Encrypt(chunkNonce, chunkData, ciphertext, tag);
                }
                
                // Write chunk: [chunkSize][ciphertext][tag]
                writer.Write(bytesRead);
                writer.Write(ciphertext);
                writer.Write(tag);
                
                chunkIndex++;
                
                // Clear sensitive data
                Array.Clear(chunkData, 0, chunkData.Length);
                Array.Clear(ciphertext, 0, ciphertext.Length);
            }
            
            Array.Clear(buffer, 0, buffer.Length);
        }

        static void EncryptFileStreamFast(string inputPath, string outputPath, byte[] key)
        {
            using var inputStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, Config.BufferSize);
            using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, Config.BufferSize);
            using var writer = new BinaryWriter(outputStream);
            
            // Write file header for fast mode
            byte[] masterNonce = RandomNumberGenerator.GetBytes(12);
            long fileSize = inputStream.Length;
            writer.Write((byte)1); // Mode: 1 = fast/intermittent encryption
            writer.Write(masterNonce);
            writer.Write(fileSize);
            writer.Write(Config.FastModeChunkSize); // Store chunk size
            writer.Write(Config.FastModeEncryptSize); // Store encrypt size per chunk
            
            byte[] buffer = new byte[Config.FastModeChunkSize];
            int chunkIndex = 0;
            int bytesRead;
            
            while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                // Create unique nonce for this chunk
                byte[] chunkNonce = CreateChunkNonce(masterNonce, chunkIndex);
                
                // Determine how much to encrypt in this chunk
                int toEncrypt = Math.Min(Config.FastModeEncryptSize, bytesRead);
                
                if (toEncrypt > 0)
                {
                    // Extract the portion to encrypt
                    byte[] plaintextPortion = new byte[toEncrypt];
                    Array.Copy(buffer, 0, plaintextPortion, 0, toEncrypt);
                    
                    // Encrypt only the first portion
                    byte[] ciphertextPortion = new byte[toEncrypt];
                    byte[] tag = new byte[16];
                    
                    using (var aes = new AesGcm(key))
                    {
                        aes.Encrypt(chunkNonce, plaintextPortion, ciphertextPortion, tag);
                    }
                    
                    // Replace the encrypted portion in the buffer
                    Array.Copy(ciphertextPortion, 0, buffer, 0, toEncrypt);
                    
                    // Write chunk: [chunkSize][toEncrypt][modifiedChunkData][tag]
                    writer.Write(bytesRead);
                    writer.Write(toEncrypt);
                    writer.Write(buffer, 0, bytesRead);
                    writer.Write(tag);
                    
                    // Clear sensitive data
                    Array.Clear(plaintextPortion, 0, plaintextPortion.Length);
                    Array.Clear(ciphertextPortion, 0, ciphertextPortion.Length);
                }
                else
                {
                    // No encryption needed for this chunk (shouldn't happen with current logic)
                    writer.Write(bytesRead);
                    writer.Write(0); // No bytes encrypted
                    writer.Write(buffer, 0, bytesRead);
                    // No tag needed
                }
                
                chunkIndex++;
            }
            
            Array.Clear(buffer, 0, buffer.Length);
        }

        static byte[] CreateChunkNonce(byte[] masterNonce, int chunkIndex)
        {
            byte[] chunkNonce = new byte[12];
            Array.Copy(masterNonce, chunkNonce, 12);
            
            // XOR the chunk index into the last 4 bytes for unique nonces
            byte[] indexBytes = BitConverter.GetBytes(chunkIndex);
            for (int i = 0; i < 4; i++)
            {
                chunkNonce[8 + i] ^= indexBytes[i];
            }
            
            return chunkNonce;
        }

        static void UnlockFolder(string root, string password, string cPath)
        {
            if (!File.Exists(cPath))
            {
                Console.WriteLine("Error: Folder is not locked.");
                return;
            }

            var sw = Stopwatch.StartNew();

            // 1) Read and decrypt metadata
            var (entries, masterKey) = LoadMetadata(cPath, password);
            if (entries == null)
            {
                Console.WriteLine("Wrong password or corrupted data.");
                return;
            }

            var fileEntries = entries.Where(e => e.Obf.EndsWith(".ffl")).ToList();
            var dirEntries = entries.Where(e => !e.Obf.EndsWith(".ffl")).ToList();

            // 2) Restore directories FIRST (shallow to deep order)
            Console.WriteLine("Restoring directory structure...");
            foreach (var entry in dirEntries.OrderBy(e => e.Real.Length))
            {
                string obfFull = Path.Combine(root, entry.Obf);
                string realFull = Path.Combine(root, entry.Real);
                string realDir = Path.GetDirectoryName(realFull) ?? string.Empty;

                if (!string.IsNullOrEmpty(realDir) && !Directory.Exists(realDir))
                    Directory.CreateDirectory(realDir);

                if (Directory.Exists(obfFull))
                {
                    Directory.Move(obfFull, realFull);
                    Console.WriteLine($"Restored directory: {entry.Real}");
                }
            }

            // 3) Adjust parallelism for large files
            var totalBytes = fileEntries.Sum(e => e.Size);
            var avgFileSize = totalBytes / Math.Max(fileEntries.Count, 1);
            var maxParallel = avgFileSize > 100 * 1024 * 1024 ? 
                Math.Max(1, Environment.ProcessorCount / 2) : Config.MaxParallelism;

            // 4) Now decrypt files in parallel
            Console.WriteLine($"Decrypting {fileEntries.Count} files...");
            var processedFiles = 0;
            var totalFiles = fileEntries.Count;
            var processedBytes = 0L;

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxParallel
            };

            Parallel.ForEach(fileEntries, parallelOptions, entry =>
            {
                try
                {
                    DecryptFile(root, entry, masterKey);
                    
                    var processed = Interlocked.Increment(ref processedFiles);
                    var processedSize = Interlocked.Add(ref processedBytes, entry.Size);
                    
                    if (processed % 50 == 0 || processed == totalFiles)
                    {
                        var progress = (double)processed / totalFiles * 100;
                        var speedMBps = processedSize / (1024 * 1024.0) / sw.Elapsed.TotalSeconds;
                        Console.WriteLine($"Progress: {progress:F1}% ({processed}/{totalFiles}) - {speedMBps:F1} MB/s");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error decrypting {entry.Real}: {ex.Message}");
                }
            });

            // 5) Cleanup
            File.Delete(cPath);

            sw.Stop();
            var decryptionRate = totalBytes / (1024 * 1024.0) / sw.Elapsed.TotalSeconds;
            Console.WriteLine($"Folder unlocked in {sw.Elapsed.TotalSeconds:F2}s - Rate: {decryptionRate:F1} MB/s");
        }

        static void DecryptFile(string root, Mapping entry, byte[] masterKey)
        {
            string encPath = Path.Combine(root, entry.Obf);
            string realPath = Path.Combine(root, entry.Real);

            // Ensure directory exists
            string? dir = Path.GetDirectoryName(realPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            byte[] fileKey = DeriveFileKey(masterKey, entry.Real);

            if (entry.IsPartial)
            {
                DecryptFileStreamFast(encPath, realPath, fileKey);
            }
            else
            {
                DecryptFileStreamFull(encPath, realPath, fileKey);
            }

            File.Delete(encPath);
        }

        static void DecryptFileStreamFull(string encPath, string realPath, byte[] key)
        {
            using var inputStream = new FileStream(encPath, FileMode.Open, FileAccess.Read, FileShare.Read, Config.BufferSize);
            using var outputStream = new FileStream(realPath, FileMode.Create, FileAccess.Write, FileShare.None, Config.BufferSize);
            using var reader = new BinaryReader(inputStream);
            
            // Read file header
            byte mode = reader.ReadByte();
            if (mode != 0) throw new InvalidDataException("Expected full encryption mode");
            
            byte[] masterNonce = reader.ReadBytes(12);
            long originalFileSize = reader.ReadInt64();
            
            int chunkIndex = 0;
            long totalWritten = 0;
            
            while (inputStream.Position < inputStream.Length && totalWritten < originalFileSize)
            {
                // Read chunk metadata
                int chunkSize = reader.ReadInt32();
                byte[] ciphertext = reader.ReadBytes(chunkSize);
                byte[] tag = reader.ReadBytes(16);
                
                // Recreate chunk nonce
                byte[] chunkNonce = CreateChunkNonce(masterNonce, chunkIndex);
                
                // Decrypt chunk
                byte[] plaintext = new byte[chunkSize];
                using (var aes = new AesGcm(key))
                {
                    aes.Decrypt(chunkNonce, ciphertext, tag, plaintext);
                }
                
                // Write decrypted chunk
                long remainingBytes = originalFileSize - totalWritten;
                int bytesToWrite = (int)Math.Min(chunkSize, remainingBytes);
                outputStream.Write(plaintext, 0, bytesToWrite);
                
                totalWritten += bytesToWrite;
                chunkIndex++;
                
                // Clear sensitive data
                Array.Clear(plaintext, 0, plaintext.Length);
                Array.Clear(ciphertext, 0, ciphertext.Length);
            }
        }

        static void DecryptFileStreamFast(string encPath, string realPath, byte[] key)
        {
            using var inputStream = new FileStream(encPath, FileMode.Open, FileAccess.Read, FileShare.Read, Config.BufferSize);
            using var outputStream = new FileStream(realPath, FileMode.Create, FileAccess.Write, FileShare.None, Config.BufferSize);
            using var reader = new BinaryReader(inputStream);
            
            // Read file header
            byte mode = reader.ReadByte();
            if (mode != 1) throw new InvalidDataException("Expected fast encryption mode");
            
            byte[] masterNonce = reader.ReadBytes(12);
            long originalFileSize = reader.ReadInt64();
            int chunkSize = reader.ReadInt32();
            int encryptSize = reader.ReadInt32();
            
            int chunkIndex = 0;
            long totalWritten = 0;
            
            while (inputStream.Position < inputStream.Length && totalWritten < originalFileSize)
            {
                // Read chunk metadata
                int currentChunkSize = reader.ReadInt32();
                int currentEncryptSize = reader.ReadInt32();
                byte[] chunkData = reader.ReadBytes(currentChunkSize);
                
                if (currentEncryptSize > 0)
                {
                    // Read the authentication tag
                    byte[] tag = reader.ReadBytes(16);
                    
                    // Recreate chunk nonce
                    byte[] chunkNonce = CreateChunkNonce(masterNonce, chunkIndex);
                    
                    // Extract encrypted portion
                    byte[] encryptedPortion = new byte[currentEncryptSize];
                    Array.Copy(chunkData, 0, encryptedPortion, 0, currentEncryptSize);
                    
                    // Decrypt the encrypted portion
                    byte[] decryptedPortion = new byte[currentEncryptSize];
                    using (var aes = new AesGcm(key))
                    {
                        aes.Decrypt(chunkNonce, encryptedPortion, tag, decryptedPortion);
                    }
                    
                    // Replace the encrypted portion with decrypted data
                    Array.Copy(decryptedPortion, 0, chunkData, 0, currentEncryptSize);
                    
                    // Clear sensitive data
                    Array.Clear(encryptedPortion, 0, encryptedPortion.Length);
                    Array.Clear(decryptedPortion, 0, decryptedPortion.Length);
                }
                
                // Write the restored chunk
                long remainingBytes = originalFileSize - totalWritten;
                int bytesToWrite = (int)Math.Min(currentChunkSize, remainingBytes);
                outputStream.Write(chunkData, 0, bytesToWrite);
                
                totalWritten += bytesToWrite;
                chunkIndex++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte[] DeriveFileKey(byte[] masterKey, string filePath)
        {
            using var hmac = new HMACSHA256(masterKey);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(filePath));
        }

        static void SaveMetadata(string cPath, List<Mapping> entries, byte[] salt, byte[] masterKey)
        {
            byte[] json = JsonSerializer.SerializeToUtf8Bytes(entries);
            byte[] iv = RandomNumberGenerator.GetBytes(12);
            byte[] metaCipher = new byte[json.Length];
            byte[] metaTag = new byte[16];

            using (var aes = new AesGcm(masterKey))
            {
                aes.Encrypt(iv, json, metaCipher, metaTag);
            }

            var sb = new StringBuilder();
            sb.AppendLine("DO NOT DELETE THIS FILE");
            sb.AppendLine("It contains the key to decrypt your files.");
            sb.AppendLine($"salt:{Convert.ToBase64String(salt)}");
            sb.AppendLine($"iv:{Convert.ToBase64String(iv)}");
            sb.AppendLine($"tag:{Convert.ToBase64String(metaTag)}");
            sb.AppendLine($"cipher:{Convert.ToBase64String(metaCipher)}");

            File.WriteAllText(cPath, sb.ToString());
        }

        static (List<Mapping>?, byte[]) LoadMetadata(string cPath, string password)
        {
            var lines = File.ReadAllLines(cPath);
            string saltB64 = lines.First(l => l.StartsWith("salt:")).Substring(5);
            string ivB64 = lines.First(l => l.StartsWith("iv:")).Substring(3);
            string tagB64 = lines.First(l => l.StartsWith("tag:")).Substring(4);
            string cipherB64 = lines.First(l => l.StartsWith("cipher:")).Substring(7);

            byte[] salt = Convert.FromBase64String(saltB64);
            byte[] iv = Convert.FromBase64String(ivB64);
            byte[] metaTag = Convert.FromBase64String(tagB64);
            byte[] metaCipher = Convert.FromBase64String(cipherB64);

            var kdf = new Rfc2898DeriveBytes(password, salt, PBKDF2_ITERATIONS, HashAlgorithmName.SHA256);
            byte[] masterKey = kdf.GetBytes(32);

            byte[] metaJson = new byte[metaCipher.Length];
            try
            {
                using var aes = new AesGcm(masterKey);
                aes.Decrypt(iv, metaCipher, metaTag, metaJson);
            }
            catch (CryptographicException)
            {
                return (null, Array.Empty<byte>());
            }

            var entries = JsonSerializer.Deserialize<List<Mapping>>(metaJson) ?? new List<Mapping>();
            return (entries, masterKey);
        }

        static string ReadPassword()
        {
            var sb = new StringBuilder();
            ConsoleKeyInfo key;
            while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
            {
                if (key.Key == ConsoleKey.Backspace && sb.Length > 0)
                {
                    sb.Length--;
                    Console.Write("\b \b");
                }
                else if (key.KeyChar != '\0' && key.KeyChar != '\b')
                {
                    sb.Append(key.KeyChar);
                    Console.Write("*");
                }
            }
            Console.WriteLine();
            return sb.ToString();
        }
    }
}
