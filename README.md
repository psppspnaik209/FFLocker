It works but
USE AT OWN RISK
I AM NOT RESPONSIBLE FOR ANY DATA LOSS
Still in development


# FFLocker

**FFLocker** (File Folder Locker) is a military-grade .NET console application that provides cryptographic "locking" of directories by encrypting files and obfuscating both file and folder names. It features advanced streaming encryption for unlimited file sizes, triple-redundant metadata storage, and parallel processing for optimal performance and reliability.

## Table of Contents

* [Features](#features)
* [Performance](#performance)
* [Prerequisites](#prerequisites)
* [Setup & Build](#setup--build)
* [Usage](#usage)
* [How It Works](#how-it-works)
* [Technical Specifications](#technical-specifications)
* [Security Considerations](#security-considerations)
* [Limitations](#limitations)
* [License](#license)

## Features

### 🔒 **Security**
* **AES-256-GCM encryption**: Industry-standard authenticated encryption providing both confidentiality and integrity
* **Enhanced key derivation**: 600,000 PBKDF2 iterations with SHA-256 for maximum GPU attack resistance
* **Per-file unique salts**: Each file uses a cryptographically unique salt for perfect forward secrecy
* **Per-file keys**: Each file uses a unique key derived from the master key via HMAC-SHA256
* **Cryptographic nonces**: Unique 96-bit nonces per chunk prevent replay attacks
* **Tamper detection**: Any modification to encrypted files is automatically detected and rejected

### 🛡️ **Reliability**
* **Triple-redundant metadata**: Primary, backup, and recovery containers ensure no single point of failure
* **Atomic operations**: All file operations are atomic to prevent corruption during interruption
* **Automatic fallback**: If primary metadata fails, automatically tries backup and recovery containers
* **Integrity verification**: Multi-layer hash verification for all metadata and file mappings

### ⚡ **Performance**
* **Streaming encryption**: Handles files of any size with constant 1MB memory usage
* **Parallel processing**: Multi-threaded encryption/decryption utilizing all CPU cores
* **Optimized I/O**: Large buffers and efficient disk operations for maximum throughput
* **Real-time progress**: Live speed reporting and progress tracking
* **Adaptive parallelism**: Automatically adjusts threading for optimal performance

### 🛡️ **Obfuscation**
* **File name randomization**: All files get cryptographically secure random `.ffl` names
* **Directory name obfuscation**: Folders renamed with secure random identifiers
* **Structure hiding**: Complete filesystem layout obfuscation
* **No fingerprinting**: No identifying headers or metadata signatures

## Performance

### Benchmark Results (2.3GB dataset, 147 Files, 21 Folders on SATA SSD):

| Operation | Time | Speed | Details |
|-----------|------|-------|---------|
| **Lock** | 4.09s | 586.6 MB/s | Full AES-256-GCM encryption |
| **Unlock** | 8.37s | 287.0 MB/s | Full decryption with verification |

**Performance characteristics:**
- **586+ MB/s encryption speed** with full security
- **Triple-redundant metadata** with minimal performance impact
- **Automatic fallback recovery** ensures reliability
- **Constant memory usage** regardless of file sizes

## Prerequisites

* **Windows** operating system (uses AesGcm via Windows CNG)
* **.NET 6.0 SDK** or later: [Download here](https://dotnet.microsoft.com/download)

Verify installation:
```bash
dotnet --version
```

## Setup & Build

1. **Clone/download** the project and place `Program.cs` in your project folder
2. **Create project file** (`FFLocker.csproj`) or use the provided one
3. **Build the application**:
   ```bash
   dotnet build -c Release
   ```

The executable will be in `bin/Release/net6.0-windows/`.

## Usage

### Basic Operations

```bash
# Encrypt and lock a folder
dotnet run -- lock "C:\Path\To\Folder"

# Decrypt and unlock the folder
dotnet run -- unlock "C:\Path\To\Folder"
```

### Example Session

**Locking a folder:**
```bash
dotnet run -- lock "K:\Downloads_3\folder"
Password: ****
Encrypting 147 files (2.3GB) with dual-redundant metadata...
Progress: 17.0% (25/147) - 18.7 MB/s
Progress: 34.0% (50/147) - 84.0 MB/s
Progress: 51.0% (75/147) - 114.6 MB/s
Progress: 68.0% (100/147) - 117.6 MB/s
Progress: 85.0% (125/147) - 158.1 MB/s
Progress: 100.0% (147/147) - 590.9 MB/s
Saved primary metadata container
Saved backup metadata container
Saved recovery metadata container
Folder secured with triple-redundant metadata in 4.09s - Rate: 586.6 MB/s
Three metadata containers created for maximum reliability
```

**Unlocking a folder:**
```bash
dotnet run -- unlock "K:\Downloads_3\folder"
Password: ****
Attempting to load primary metadata container...
Successfully loaded primary metadata container
Loaded metadata: 147 files, 21 directories
Restoring directory structure...
Decrypting 147 files...
Progress: 17.0% (25/147)
Progress: 34.0% (50/147)
Progress: 51.0% (75/147)
Progress: 68.0% (100/147)
Progress: 85.0% (125/147)
Progress: 100.0% (147/147)
Folder unlocked in 8.37s
```

### What Happens During Operations

**During lock:**
- Enter your password when prompted
- All files are encrypted using streaming AES-256-GCM with unique salts
- Files get cryptographically secure random `.ffl` names
- Directories are obfuscated with secure random names
- Original files/folders are securely deleted with random overwriting
- Three metadata containers are created for maximum reliability

**During unlock:**
- Enter the same password used for locking
- System attempts to load primary metadata container, with automatic fallback to backup/recovery
- Directory structure is restored first (shallow to deep)
- Files are decrypted back to original locations using streaming
- All obfuscated files and metadata containers are cleaned up

## How It Works

### Streaming Architecture

FFLocker uses a **streaming encryption model** that provides several key advantages:

1. **Constant Memory Usage**: Only 1MB RAM used regardless of file size
2. **Unlimited File Sizes**: Can handle TB-scale files without issues
3. **Chunk-Based Processing**: Files processed in 1MB chunks with unique nonces
4. **Parallel Safety**: Multiple large files can be processed simultaneously

```
File → 1MB Chunks → Per-Chunk Encryption → Secure Random .ffl Output
[Chunk 1: Nonce₁ + AES-GCM₁ + Tag₁]
[Chunk 2: Nonce₂ + AES-GCM₂ + Tag₂]
[Chunk N: NonceN + AES-GCMN + TagN]
```

### Triple-Redundant Metadata System

FFLocker uses a proven dual-redundant approach similar to VeraCrypt's header backup system:

```
.fflmeta    (Primary metadata container)
.fflbkup    (Backup metadata container)  
.fflrcvr    (Recovery metadata container)
```

**Benefits:**
- **No single point of failure**: If one container is corrupted, others provide recovery
- **Automatic fallback**: System tries primary → backup → recovery automatically
- **Proven reliability**: Based on established encryption tool architectures
- **Integrity verification**: Each container includes verification hashes

### Security Model

1. **Master Key Derivation**: `PBKDF2(password + global_salt, 600,000 iterations) → 256-bit key`
2. **Per-File Salts**: Each file gets a unique 256-bit cryptographic salt
3. **Per-File Keys**: `HMAC-SHA256(master_key, file_path + file_salt) → unique 256-bit key`
4. **Chunk Nonces**: Each chunk gets a cryptographically secure random 96-bit nonce
5. **Authentication**: Each chunk authenticated with 128-bit GCM tag

## Technical Specifications

### File Format

**Encrypted File Structure:**
```
Header: [FileSize]
Chunks: [Nonce][ChunkSize][Ciphertext][Tag] × N
```

### Metadata Container Format
```
.fflmeta/.fflbkup/.fflrcvr:
# FFLocker Dual-Redundant Metadata Container
# Container Type: Primary/Backup/Recovery
# Created: [timestamp] UTC
version:2.0
container_id:[0,1,2]
global_salt:[base64]
iv:[base64]
tag:[base64]
data:[base64]
```

### Cryptographic Parameters
- **Encryption**: AES-256-GCM (authenticated encryption)
- **Key Derivation**: PBKDF2-SHA256, 600,000 iterations (GPU-resistant)
- **Nonce Size**: 96 bits (cryptographically secure random per chunk)
- **Authentication Tag**: 128 bits per chunk
- **Global Salt**: 256 bits (random per operation)
- **Per-File Salt**: 256 bits (unique per file)

### Performance Characteristics
- **Memory Usage**: Constant 1MB per file operation
- **Parallelism**: Auto-adjusted based on file sizes and system resources
- **Chunk Size**: 1MB for optimal I/O performance
- **Encryption Ratio**: 100% - all data is fully encrypted

## Security Considerations

### Password Security
- **Use strong passwords**: High entropy recommended (16+ characters)
- **No password recovery**: Lost passwords mean permanent data loss
- **Enhanced brute force protection**: 600,000 PBKDF2 iterations significantly increases attack cost

### Cryptographic Strength
- **NIST-approved algorithms**: AES-256-GCM, PBKDF2-SHA256
- **Perfect forward secrecy**: Unique keys and salts per file
- **Authenticated encryption**: Prevents tampering and forgery
- **Cryptographic randomness**: Hardware-based random number generation
- **GPU attack resistance**: Enhanced iteration count protects against specialized hardware

### Threat Model
- ✅ **Protects against**: File system access, data theft, forensic analysis
- ✅ **Detects tampering**: Any file modification breaks authentication
- ✅ **Prevents single points of failure**: Triple-redundant metadata recovery
- ✅ **Resists GPU attacks**: Enhanced key derivation parameters
- ⚠️ **Does not protect against**: Memory analysis while unlocked, keyloggers, coercion

### Security Features
- **Triple-redundant recovery**: Multiple metadata containers prevent data loss
- **Atomic operations**: Prevents corruption during interruption
- **Secure deletion**: Original files overwritten with random data before deletion
- **No metadata leakage**: No identifying signatures or plaintext headers

## Limitations

### Current Limitations
- **Windows only**: Uses Windows-specific AesGcm implementation
- **Password dependent**: No password recovery mechanism available
- **File system locks**: Cannot encrypt files currently in use by other applications
- **Sequential directory operations**: Directory moves not parallelized for filesystem safety

### Technical Constraints
- **Platform dependency**: Requires Windows CNG for AES-GCM
- **Antivirus interference**: May be flagged due to encryption behavior
- **Network drive performance**: Reduced speeds on network storage
- **Large dataset time**: Very large datasets still require significant processing time

## ⚠️ Important Warnings

1. **Test with non-critical data first** - Always verify functionality before use
2. **Maintain secure backups** - Lost passwords result in permanent data loss
3. **Antivirus exclusions** - May need to exclude FFLocker from real-time scanning
4. **Avoid interruption** - Do not interrupt locking/unlocking operations
5. **Verify integrity** - Always verify unlocked data before deleting encrypted copies
6. **Strong passwords** - Use high-entropy passwords with mix of characters

## License

MIT License - see LICENSE file for details.

## Performance Notes

FFLocker is optimized for:
- **SSD storage**: Best performance on solid-state drives
- **Modern CPUs**: Utilizes AES-NI instructions when available
- **Adequate RAM**: Minimal memory requirements (1MB per file operation)
- **Fast storage**: Network drives and slow storage will reduce throughput

For optimal performance:
- Use SSDs for both source and temporary storage
- Ensure adequate free disk space (2x dataset size during encryption)
- Close unnecessary applications to free up CPU cores
- Ensure antivirus exclusions are configured properly

**Remember: FFLocker provides military-grade encryption for sensitive data. Use responsibly and always maintain secure backups of important files.
In the end your password determines the strength of this program**
