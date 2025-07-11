using Konscious.Security.Cryptography;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FFLocker.Logic
{
    public class FileHeader
    {
        // FFLOCKER (8 bytes)
        public static readonly byte[] MagicBytes = { 0x46, 0x46, 0x4C, 0x4F, 0x43, 0x4B, 0x45, 0x52 };
        public const byte Version = 1;
        public byte[] GlobalSalt { get; set; } = new byte[32];
        public byte[] FileSalt { get; set; } = new byte[32];
        public byte[] PathNonce { get; set; } = new byte[12];
        public ushort PathLength { get; set; }
        public byte[] EncryptedPath { get; set; } = [];
        public byte[] PathEncryptionTag { get; set; } = new byte[16];
        public long OriginalFileLength { get; set; }
        public bool IsHelloUsed { get; set; }
        public ushort HelloEncryptedKeyLength { get; set; }
        public byte[] HelloEncryptedKey { get; set; } = [];

        public int GetHeaderSize()
        {
            return MagicBytes.Length + 1 + GlobalSalt.Length + FileSalt.Length + PathNonce.Length + sizeof(ushort) + EncryptedPath.Length + PathEncryptionTag.Length + sizeof(long);
        }

        public void WriteTo(Stream stream)
        {
            stream.Write(MagicBytes, 0, MagicBytes.Length);
            stream.WriteByte(Version);
            stream.Write(GlobalSalt, 0, GlobalSalt.Length);
            stream.Write(FileSalt, 0, FileSalt.Length);
            stream.Write(PathNonce, 0, PathNonce.Length);
            
            var pathLengthBytes = BitConverter.GetBytes(PathLength);
            stream.Write(pathLengthBytes, 0, pathLengthBytes.Length);
            stream.Write(EncryptedPath, 0, EncryptedPath.Length);
            stream.Write(PathEncryptionTag, 0, PathEncryptionTag.Length);

            var fileLengthBytes = BitConverter.GetBytes(OriginalFileLength);
            stream.Write(fileLengthBytes, 0, fileLengthBytes.Length);

            stream.WriteByte(IsHelloUsed ? (byte)1 : (byte)0);
            if (IsHelloUsed)
            {
                var helloKeyLengthBytes = BitConverter.GetBytes(HelloEncryptedKeyLength);
                stream.Write(helloKeyLengthBytes, 0, helloKeyLengthBytes.Length);
                stream.Write(HelloEncryptedKey, 0, HelloEncryptedKey.Length);
            }
        }

        public static FileHeader ReadFrom(Stream stream)
        {
            var header = new FileHeader();

            byte[] buffer = new byte[8];
            stream.Read(buffer, 0, 8);
            if (!buffer.SequenceEqual(MagicBytes))
                throw new InvalidDataException("Not a valid FFLocker file.");

            int version = stream.ReadByte();
            if (version != Version)
                throw new InvalidDataException($"Unsupported version: {version}");

            stream.Read(header.GlobalSalt, 0, 32);
            stream.Read(header.FileSalt, 0, 32);
            stream.Read(header.PathNonce, 0, 12);

            buffer = new byte[2];
            stream.Read(buffer, 0, 2);
            header.PathLength = BitConverter.ToUInt16(buffer, 0);

            header.EncryptedPath = new byte[header.PathLength];
            stream.Read(header.EncryptedPath, 0, header.PathLength);

            header.PathEncryptionTag = new byte[16];
            stream.Read(header.PathEncryptionTag, 0, 16);

            buffer = new byte[8];
            stream.Read(buffer, 0, 8);
            header.OriginalFileLength = BitConverter.ToInt64(buffer, 0);

            header.IsHelloUsed = stream.ReadByte() == 1;
            if (header.IsHelloUsed)
            {
                buffer = new byte[2];
                stream.Read(buffer, 0, 2);
                header.HelloEncryptedKeyLength = BitConverter.ToUInt16(buffer, 0);

                header.HelloEncryptedKey = new byte[header.HelloEncryptedKeyLength];
                stream.Read(header.HelloEncryptedKey, 0, header.HelloEncryptedKey.Length);
            }

            return header;
        }
    }

    public partial class EncryptionConfig
    {
        public int ChunkSize { get; set; } = 4 * 1024 * 1024;
        public int MaxParallelism { get; set; } = Environment.ProcessorCount;
        public int BufferSize { get; set; } = 4 * 1024 * 1024;
        public int SecureRandomLength { get; set; } = 32;
        public int Argon2Iterations { get; set; } = 4;
        public int Argon2MemorySize { get; set; } = 65536; // 64 MB
        public int Argon2DegreeOfParallelism { get; set; } = Environment.ProcessorCount;
    }

    public partial class SecureBuffer : IDisposable
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

        public byte[] Buffer
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return _buffer;
            }
        }
        public int Length
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return _buffer.Length;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                CryptographicOperations.ZeroMemory(_buffer);
                if (_handle.IsAllocated)
                    _handle.Free();
                _buffer = null!;
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }

    public partial class SecureCrypto : IDisposable
    {
        private readonly RandomNumberGenerator _rng;
        private bool _disposed;

        public SecureCrypto()
        {
            _rng = RandomNumberGenerator.Create();
        }

        public void GetBytes(byte[] buffer)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _rng.GetBytes(buffer);
        }

        public void GetBytes(Span<byte> buffer)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _rng.GetBytes(buffer);
        }

        public string GenerateSecureFilename(int length)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var randomBytes = new byte[length];
            _rng.GetBytes(randomBytes);

            return Convert.ToBase64String(randomBytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=')[..length];
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
            GC.SuppressFinalize(this);
        }
    }

    public partial class KeyDerivation : IDisposable
    {
        private bool _disposed;
        private readonly EncryptionConfig _config = new();

        public byte[] DeriveKey(byte[] password, byte[] salt, int keyLength)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            using var argon2 = new Argon2id(password)
            {
                Salt = salt,
                DegreeOfParallelism = _config.Argon2DegreeOfParallelism,
                Iterations = _config.Argon2Iterations,
                MemorySize = _config.Argon2MemorySize
            };

            return argon2.GetBytes(keyLength);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }

    public static class EncryptionManager
    {
        private const int NONCE_SIZE = 12;
        private const int TAG_SIZE = 16;
        private static readonly byte[] MagicFooter = { 0x45, 0x4E, 0x44, 0x46, 0x46, 0x4C, 0x4F, 0x43, 0x4B }; // ENDFFLOCK
        private static readonly EncryptionConfig Config = new();

        public static bool IsLocked(string path)
        {
            if (File.Exists(path))
            {
                return path.EndsWith(".ffl", StringComparison.OrdinalIgnoreCase);
            }
            if (Directory.Exists(path))
            {
                return Directory.EnumerateFiles(path, "*.ffl").Any();
            }
            return false;
        }

        public static bool IsHelloUsed(string path)
        {
            if (!File.Exists(path)) return false;
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                var header = FileHeader.ReadFrom(fs);
                return header.IsHelloUsed;
            }
            catch
            {
                return false;
            }
        }

        public static FileHeader? GetFileHeader(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                return FileHeader.ReadFrom(fs);
            }
            catch
            {
                return null;
            }
        }

        public static void UnlockWithMasterKey(string path, SecureBuffer masterKey, IProgress<int> progress, IProgress<string> logger)
        {
            if (path.EndsWith(".ffl", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
            {
                var sw = Stopwatch.StartNew();
                var header = GetFileHeader(path);
                if (header == null) throw new Exception("Could not read file header.");
                
                var originalPath = DecryptPathFromHeader(header, masterKey.Buffer);
                var realPath = Path.Combine(Path.GetDirectoryName(path)!, originalPath);
                
                logger.Report($"Decrypting file: {originalPath}");
                var (tempPath, finalPath) = DecryptFile(path, realPath, masterKey.Buffer, logger);
                
                File.Move(tempPath, finalPath, true);
                SecureDeleteFile(path);
                
                progress.Report(100);
                LockedItemsDatabase.Remove(realPath);

                sw.Stop();
                logger.Report($"File unlocked in {sw.Elapsed.TotalSeconds:F2}s");
            }
            else if (Directory.Exists(path))
            {
                // For folders, the existing UnlockFolder method already uses the master key correctly.
                UnlockFolder(path, masterKey, progress, logger);
            }
        }

        public static string Lock(string path, SecureBuffer password, IProgress<int> progress, IProgress<string> logger, byte[]? helloEncryptedKey = null)
        {
            string lockedPath;
            bool isFolder = false;
            if (File.Exists(path))
            {
                lockedPath = LockFile(path, password, progress, logger, helloEncryptedKey);
            }
            else if (Directory.Exists(path))
            {
                lockedPath = LockFolder(path, password, progress, logger, helloEncryptedKey);
                isFolder = true;
            }
            else
            {
                throw new FileNotFoundException("The specified file or folder does not exist.", path);
            }
            LockedItemsDatabase.Add(new LockedItemInfo { OriginalPath = path, LockedPath = lockedPath, IsFolder = isFolder });
            return lockedPath;
        }

        public static void Unlock(string path, SecureBuffer password, IProgress<int> progress, IProgress<string> logger)
        {
            if (path.EndsWith(".ffl", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
            {
                UnlockFile(path, password, progress, logger);
            }
            else if (Directory.Exists(path))
            {
                UnlockFolder(path, password, progress, logger);
            }
        }

        public static string LockFolder(string root, SecureBuffer password, IProgress<int> progress, IProgress<string> logger, byte[]? helloEncryptedKey)
        {
            var sw = Stopwatch.StartNew();

            using var crypto = new SecureCrypto();
            using var masterKeyBuffer = new SecureBuffer(32);
            using var globalSaltBuffer = new SecureBuffer(32);
            using var keyDerivation = new KeyDerivation();

            crypto.GetBytes(globalSaltBuffer.Buffer);

            var derivedKey = keyDerivation.DeriveKey(password.Buffer, globalSaltBuffer.Buffer, 32);
            Array.Copy(derivedKey, masterKeyBuffer.Buffer, 32);
            CryptographicOperations.ZeroMemory(derivedKey);

            var fileInfos = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Select(f => new { Path = f, RelPath = Path.GetRelativePath(root, f), Size = new FileInfo(f).Length })
                .Where(f => f.Size > 0).ToList();

            var totalBytes = fileInfos.Sum(f => f.Size);
            logger.Report($"Found {fileInfos.Count} files to encrypt, total size: {FormatSize(totalBytes)}.");

            var processedFiles = 0;
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Config.MaxParallelism };
            var encryptedFilePaths = new ConcurrentBag<(string, string, string)>();
            var exceptionOccurred = false;

            logger.Report($"Starting encryption with up to {Config.MaxParallelism} parallel tasks...");

            try
            {
                Parallel.ForEach(fileInfos, parallelOptions, (fileInfo, loopState) =>
                {
                    var paths = EncryptFile(root, fileInfo.RelPath, masterKeyBuffer.Buffer, globalSaltBuffer.Buffer, crypto, logger, helloEncryptedKey != null, helloEncryptedKey);
                    encryptedFilePaths.Add(paths);
                    var processed = Interlocked.Increment(ref processedFiles);
                    var percentage = (int)((double)processed / fileInfos.Count * 100);
                    progress.Report(percentage);
                });
            }
            catch (Exception ex)
            {
                logger.Report($"[ERROR] An exception occurred during encryption: {ex.Message}");
                exceptionOccurred = true;
            }

            if (exceptionOccurred)
            {
                logger.Report("An error occurred. Rolling back changes...");
                foreach(var (_, tempPath, _) in encryptedFilePaths)
                {
                    try { File.Delete(tempPath); } catch { }
                }
                throw new Exception("Encryption failed and was rolled back.");
            }

            logger.Report("All files encrypted. Committing changes...");
            foreach(var (originalPath, tempPath, finalPath) in encryptedFilePaths)
            {
                File.Move(tempPath, finalPath, true);
                SecureDeleteFile(originalPath);
            }

            logger.Report("Renaming directories...");
            var dirRelPaths = Directory.GetDirectories(root, "*", SearchOption.AllDirectories)
                .Select(d => Path.GetRelativePath(root, d)).OrderByDescending(r => r.Length).ToList();

            foreach (var rel in dirRelPaths)
            {
                string original = Path.Combine(root, rel);
                if (Directory.Exists(original) && !Directory.EnumerateFileSystemEntries(original).Any())
                {
                    Directory.Delete(original);
                }
            }

            sw.Stop();
            var encryptionRate = totalBytes / (1024 * 1024.0) / sw.Elapsed.TotalSeconds;
            logger.Report($"Folder secured in {sw.Elapsed.TotalSeconds:F2}s - Rate: {encryptionRate:F1} MB/s");

            try
            {
                string newRoot = root + "_USE_FOR_FOLDER_UNLOCK_DO_NOT_DELETE";
                Directory.Move(root, newRoot);
                logger.Report($"Renamed folder to: {Path.GetFileName(newRoot)}");
                return newRoot;
            }
            catch (Exception ex)
            {
                logger.Report($"Warning: Could not rename the locked folder. Please rename it manually to include '_USE_FOR_FOLDER_UNLOCK_DO_NOT_DELETE' for clarity. Error: {ex.Message}");
                return root;
            }
        }

        public static void UnlockFolder(string root, SecureBuffer password, IProgress<int> progress, IProgress<string> logger)
        {
            var sw = Stopwatch.StartNew();
            var encryptedFiles = Directory.GetFiles(root, "*.ffl");
            if (encryptedFiles.Length == 0)
            {
                logger.Report("No encrypted files found to unlock.");
                return;
            }

            byte[]? globalSalt = null;
            var fileMappings = new ConcurrentDictionary<string, string>();

            // First pass: Read headers to get global salt and paths
            using (var fs = new FileStream(encryptedFiles[0], FileMode.Open, FileAccess.Read))
            {
                var header = FileHeader.ReadFrom(fs);
                globalSalt = header.GlobalSalt;
            }

            using var masterKeyBuffer = new SecureBuffer(32);
            using var keyDerivation = new KeyDerivation();
            var derivedKey = keyDerivation.DeriveKey(password.Buffer, globalSalt, 32);
            Array.Copy(derivedKey, masterKeyBuffer.Buffer, 32);
            CryptographicOperations.ZeroMemory(derivedKey);

            logger.Report("Reading file headers...");
            foreach (var file in encryptedFiles)
            {
                using var fs = new FileStream(file, FileMode.Open, FileAccess.Read);
                var header = FileHeader.ReadFrom(fs);
                var originalPath = DecryptPathFromHeader(header, masterKeyBuffer.Buffer);
                fileMappings[file] = Path.Combine(root, originalPath);
            }

            logger.Report($"Found {fileMappings.Count} files. Restoring directory structure...");
            foreach (var path in fileMappings.Values.Select(p => Path.GetDirectoryName(p)).Distinct())
            {
                if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }

            logger.Report($"Decrypting {fileMappings.Count} files with up to {Config.MaxParallelism} parallel tasks...");
            var processedFiles = 0;
            var decryptedFilePaths = new ConcurrentBag<(string, string)>();
            var exceptionOccurred = false;

            try
            {
                Parallel.ForEach(fileMappings, new ParallelOptions { MaxDegreeOfParallelism = Config.MaxParallelism }, mapping =>
                {
                    var paths = DecryptFile(mapping.Key, mapping.Value, masterKeyBuffer.Buffer, logger);
                    decryptedFilePaths.Add(paths);

                    var processed = Interlocked.Increment(ref processedFiles);
                    var percentage = (int)((double)processed / fileMappings.Count * 100);
                    progress.Report(percentage);
                });
            }
            catch(Exception ex)
            {
                logger.Report($"[ERROR] An exception occurred during decryption: {ex.Message}");
                exceptionOccurred = true;
            }

            if(exceptionOccurred)
            {
                logger.Report("An error occurred. Rolling back changes...");
                foreach(var (tempPath, _) in decryptedFilePaths)
                {
                    try { File.Delete(tempPath); } catch { }
                }
                throw new Exception("Decryption failed and was rolled back.");
            }

            logger.Report("All files decrypted. Committing changes...");
            foreach(var (tempPath, finalPath) in decryptedFilePaths)
            {
                File.Move(tempPath, finalPath, true);
            }
            foreach(var encFile in encryptedFiles)
            {
                SecureDeleteFile(encFile);
            }


            logger.Report("Cleaning up...");
            LockedItemsDatabase.Remove(root);

            sw.Stop();
            logger.Report($"Folder unlocked in {sw.Elapsed.TotalSeconds:F2}s");

            if (root.EndsWith("_USE_FOR_FOLDER_UNLOCK_DO_NOT_DELETE"))
            {
                try
                {
                    string originalRoot = root.Replace("_USE_FOR_FOLDER_UNLOCK_DO_NOT_DELETE", "");
                    Directory.Move(root, originalRoot);
                    logger.Report($"Renamed folder back to: {Path.GetFileName(originalRoot)}");
                }
                catch (Exception ex)
                {
                    logger.Report($"Warning: Could not rename the unlocked folder back to its original name. Error: {ex.Message}");
                }
            }
        }

        public static string LockFile(string filePath, SecureBuffer password, IProgress<int> progress, IProgress<string> logger, byte[]? helloEncryptedKey = null)
        {
            string root = Path.GetDirectoryName(filePath)!;
            string relPath = Path.GetFileName(filePath);

            var sw = Stopwatch.StartNew();

            using var crypto = new SecureCrypto();
            using var masterKeyBuffer = new SecureBuffer(32);
            using var globalSaltBuffer = new SecureBuffer(32);
            using var keyDerivation = new KeyDerivation();

            crypto.GetBytes(globalSaltBuffer.Buffer);

            var derivedKey = keyDerivation.DeriveKey(password.Buffer, globalSaltBuffer.Buffer, 32);
            Array.Copy(derivedKey, masterKeyBuffer.Buffer, 32);
            CryptographicOperations.ZeroMemory(derivedKey);

            logger.Report($"Encrypting file: {relPath} ({FormatSize(new FileInfo(filePath).Length)})");
            
            var (originalPath, tempPath, finalPath) = EncryptFile(root, relPath, masterKeyBuffer.Buffer, globalSaltBuffer.Buffer, crypto, logger, helloEncryptedKey != null, helloEncryptedKey);
            
            File.Move(tempPath, finalPath, true);
            SecureDeleteFile(originalPath);

            progress.Report(100);

            sw.Stop();
            var encryptionRate = new FileInfo(finalPath).Length / (1024 * 1024.0) / sw.Elapsed.TotalSeconds;
            logger.Report($"File secured in {sw.Elapsed.TotalSeconds:F2}s - Rate: {encryptionRate:F1} MB/s");

            return finalPath;
        }

        public static void UnlockFile(string filePath, SecureBuffer password, IProgress<int> progress, IProgress<string> logger)
        {
            var sw = Stopwatch.StartNew();

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var header = FileHeader.ReadFrom(fs);
            fs.Close();

            using var masterKeyBuffer = new SecureBuffer(32);
            using var keyDerivation = new KeyDerivation();
            var derivedKey = keyDerivation.DeriveKey(password.Buffer, header.GlobalSalt, 32);
            Array.Copy(derivedKey, masterKeyBuffer.Buffer, 32);
            CryptographicOperations.ZeroMemory(derivedKey);

            var originalPath = DecryptPathFromHeader(header, masterKeyBuffer.Buffer);
            var realPath = Path.Combine(Path.GetDirectoryName(filePath)!, originalPath);

            logger.Report($"Decrypting file: {originalPath}");
            var (tempPath, finalPath) = DecryptFile(filePath, realPath, masterKeyBuffer.Buffer, logger);
            
            File.Move(tempPath, finalPath, true);
            SecureDeleteFile(filePath);
            
            progress.Report(100);

            LockedItemsDatabase.Remove(realPath);

            sw.Stop();
            logger.Report($"File unlocked in {sw.Elapsed.TotalSeconds:F2}s");
        }

        static (string, string, string) EncryptFile(string root, string relPath, byte[] masterKey, byte[] globalSalt, SecureCrypto crypto, IProgress<string> logger, bool useHello, byte[]? helloKey)
        {
            string inputPath = Path.Combine(root, relPath);
            string obfName = crypto.GenerateSecureFilename(Config.SecureRandomLength) + ".ffl";
            string outputPath = Path.Combine(root, obfName);
            string tempPath = outputPath + ".tmp";

            var fileSalt = crypto.GenerateFileSalt();
            var pathBytes = Encoding.UTF8.GetBytes(relPath);
            var encryptedPath = new byte[pathBytes.Length];
            var pathTag = new byte[TAG_SIZE];
            var pathNonce = new byte[NONCE_SIZE];
            crypto.GetBytes(pathNonce);

            using (var aes = new AesGcm(masterKey, TAG_SIZE))
            {
                aes.Encrypt(pathNonce, pathBytes, encryptedPath, pathTag);
            }

            byte[]? finalHelloProtectedKey = null;
            if (useHello && helloKey != null)
            {
                finalHelloProtectedKey = new byte[32];
                for (int i = 0; i < 32; i++)
                {
                    finalHelloProtectedKey[i] = (byte)(masterKey[i] ^ helloKey[i]);
                }
            }

            var header = new FileHeader
            {
                GlobalSalt = globalSalt,
                FileSalt = fileSalt,
                PathNonce = pathNonce,
                PathLength = (ushort)encryptedPath.Length,
                EncryptedPath = encryptedPath,
                PathEncryptionTag = pathTag,
                OriginalFileLength = new FileInfo(inputPath).Length,
                IsHelloUsed = useHello,
                HelloEncryptedKey = finalHelloProtectedKey ?? [],
                HelloEncryptedKeyLength = (ushort)(finalHelloProtectedKey?.Length ?? 0)
            };

            try
            {
                using var fileKeyBuffer = new SecureBuffer(32);
                DeriveFileKey(masterKey, relPath, fileSalt, fileKeyBuffer.Buffer);

                EncryptFileStream(inputPath, tempPath, fileKeyBuffer.Buffer, crypto, header);

                return (inputPath, tempPath, outputPath);
            }
            catch
            {
                try { File.Delete(tempPath); } catch { }
                throw;
            }
        }

        static (string, string) DecryptFile(string encPath, string realPath, byte[] masterKey, IProgress<string> logger)
        {
            string tempPath = realPath + ".tmp";

            try
            {
                using var fs = new FileStream(encPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var header = FileHeader.ReadFrom(fs);

                var originalRelPath = DecryptPathFromHeader(header, masterKey);

                using var fileKeyBuffer = new SecureBuffer(32);
                DeriveFileKey(masterKey, originalRelPath, header.FileSalt, fileKeyBuffer.Buffer);

                DecryptFileStream(fs, tempPath, fileKeyBuffer.Buffer, header.OriginalFileLength);
                fs.Close();

                return (tempPath, realPath);
            }
            catch
            {
                try { File.Delete(tempPath); } catch { }
                throw;
            }
        }

        static void EncryptFileStream(string inputPath, string outputPath, byte[] key, SecureCrypto crypto, FileHeader header)
        {
            using var inputStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, Config.BufferSize);
            using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, Config.BufferSize);
            
            header.WriteTo(outputStream);

            var buffer = new byte[Config.ChunkSize];
            int bytesRead;

            while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                var chunkNonce = new byte[NONCE_SIZE];
                crypto.GetBytes(chunkNonce);

                var chunkData = new Span<byte>(buffer, 0, bytesRead);
                var ciphertext = new byte[bytesRead];
                var tag = new byte[TAG_SIZE];

                using (var aes = new AesGcm(key, TAG_SIZE))
                {
                    aes.Encrypt(chunkNonce, chunkData, ciphertext, tag);
                }

                outputStream.Write(chunkNonce, 0, chunkNonce.Length);
                outputStream.Write(BitConverter.GetBytes(bytesRead), 0, sizeof(int));
                outputStream.Write(ciphertext, 0, ciphertext.Length);
                outputStream.Write(tag, 0, tag.Length);
            }

            outputStream.Write(MagicFooter, 0, MagicFooter.Length);
        }

        static void DecryptFileStream(Stream inputStream, string realPath, byte[] key, long originalFileSize)
        {
            using var outputStream = new FileStream(realPath, FileMode.Create, FileAccess.Write, FileShare.None, Config.BufferSize);
            long totalWritten = 0;

            while (totalWritten < originalFileSize)
            {
                var chunkNonce = new byte[NONCE_SIZE];
                inputStream.Read(chunkNonce, 0, NONCE_SIZE);

                var sizeBuffer = new byte[sizeof(int)];
                inputStream.Read(sizeBuffer, 0, sizeof(int));
                int chunkSize = BitConverter.ToInt32(sizeBuffer, 0);

                var ciphertext = new byte[chunkSize];
                inputStream.Read(ciphertext, 0, chunkSize);

                var tag = new byte[TAG_SIZE];
                inputStream.Read(tag, 0, TAG_SIZE);

                var plaintext = new byte[chunkSize];

                using (var aes = new AesGcm(key, TAG_SIZE))
                {
                    aes.Decrypt(chunkNonce, ciphertext, tag, plaintext);
                }

                long remainingBytes = originalFileSize - totalWritten;
                int bytesToWrite = (int)Math.Min(chunkSize, remainingBytes);
                outputStream.Write(plaintext, 0, bytesToWrite);

                totalWritten += bytesToWrite;
            }

            var footerBuffer = new byte[MagicFooter.Length];
            inputStream.Read(footerBuffer, 0, footerBuffer.Length);
            if (!footerBuffer.SequenceEqual(MagicFooter))
            {
                throw new InvalidDataException("File is corrupt or truncated. Footer is invalid.");
            }
        }

        static string DecryptPathFromHeader(FileHeader header, byte[] masterKey)
        {
            var decryptedPathBytes = new byte[header.EncryptedPath.Length];
            using (var aes = new AesGcm(masterKey, TAG_SIZE))
            {
                aes.Decrypt(header.PathNonce, header.EncryptedPath, header.PathEncryptionTag, decryptedPathBytes);
            }
            return Encoding.UTF8.GetString(decryptedPathBytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void DeriveFileKey(byte[] masterKey, string filePath, byte[] fileSalt, byte[] output)
        {
            var pathBytes = Encoding.UTF8.GetBytes(filePath);
            var combined = new byte[pathBytes.Length + fileSalt.Length];

            Buffer.BlockCopy(pathBytes, 0, combined, 0, pathBytes.Length);
            Buffer.BlockCopy(fileSalt, 0, combined, pathBytes.Length, fileSalt.Length);

            using var hmac = new HMACSHA256(masterKey);
            var derivedKey = hmac.ComputeHash(combined);
            Array.Copy(derivedKey, output, Math.Min(derivedKey.Length, output.Length));

            CryptographicOperations.ZeroMemory(derivedKey);
            CryptographicOperations.ZeroMemory(combined);
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
