# FFLocker (File & Folder Locker)

FFLocker is a .NET application for Windows that provides strong, password-based encryption for your files and folders. It is designed with a focus on security, reliability, and flexibility, offering multiple ways to interact with your encrypted data.


## Features

*   **Strong Encryption:** Uses AES-256-GCM for authenticated encryption, ensuring both confidentiality and integrity of your data.
*   **Secure Key Derivation:** Implements PBKDF2 with 600,000 iterations (HMAC-SHA256) to protect against brute-force attacks on your password.
*   **Dual-Mode Interface:**
    *   **GUI Mode:** An intuitive graphical interface for easy operation.
    *   **CLI Mode:** A command-line interface for scripting and automation.
*   **Windows Integration:**
    *   **Context Menu:** Optionally integrate FFLocker into the Windows context menu for quick lock/unlock operations on any file or folder.
*   **User-Friendly GUI:**
    *   **Dark & Light Modes:** Choose the theme that best suits your preference. Your choice is saved and remembered.
    *   **Window Position Saving:** The application remembers where you last placed it on your screen.
*   **Reliability:**
    *   **Triple-Redundant Metadata:** Creates three copies of the encryption metadata (`.fflmeta`, `.fflbkup`, `.fflrcvr`) to protect against data loss if one of the containers is corrupted.
    *   **Atomic Operations:** Designed to prevent data corruption if the application is interrupted during an operation.
*   **Performance:**
    *   **Streaming Encryption:** Encrypts and decrypts files of any size with minimal memory usage (constant 1MB per file).
    *   **Parallel Processing:** Utilizes multiple CPU cores to speed up operations on folders with many files.

## Getting Started

### Prerequisites

*   **Windows** operating system.
*   **.NET 6.0 SDK** or later. You can download it from the official [.NET website](https://dotnet.microsoft.com/download).

### Installation & Building

1.  **Clone or download** the project to your local machine.
2.  **Open a terminal** (like PowerShell or Command Prompt) in the project's root directory.
3.  **Build the application** by running the following command:
    ```bash
    dotnet build -c Release
    ```
The compiled application, `FFLocker.exe`, will be located in the `bin/Release/net6.0-windows/` directory.

## How to Use

FFLocker can be used in three ways: through the GUI, from the command line, or via the Windows context menu.

### 1. GUI Mode

To use the graphical interface, simply double-click `FFLocker.exe`.

*   **Selecting a File or Folder:**
    1.  Choose whether you want to select a "File" or "Folder" using the radio buttons.
    2.  Click the "Browse..." button to select the item you want to lock or unlock.
*   **Locking and Unlocking:**
    1.  Click the "Lock" or "Unlock" button.
    2.  An inline password field will appear. Enter your password and click "Confirm" or press Enter.
*   **Options:**
    *   **Dark Mode:** Toggle the "Dark Mode" checkbox to switch between light and dark themes. Your preference is saved automatically.
    *   **Show more info:** See detailed logs of the application's operations.
    *   **Context Menu:** Enable or disable the Windows context menu integration.

### 2. CLI Mode

Open a terminal and run `FFLocker.exe` with the following commands. This is ideal for scripting or for users who prefer the command line.

*   **To Lock a File or Folder:**
    ```bash
    FFLocker.exe lock "C:\path\to\your\file_or_folder"
    ```
*   **To Unlock a File or Folder:**
    ```bash
    FFLocker.exe unlock "C:\path\to\your\file_or_folder"
    ```
After running the command, you will be prompted to enter your password directly in the terminal.

### 3. Context Menu

For maximum convenience, you can integrate FFLocker directly into the Windows right-click context menu.

*   **Enabling the Context Menu:**
    1.  **Run `FFLocker.exe` as an administrator.**
    2.  Click the "Context Menu" checkbox in the GUI.
    3.  A dialog will ask if you want to restart Windows Explorer to apply the changes. Click "Yes."
*   **Using the Context Menu:**
    1.  Right-click on any file or folder.
    2.  Go to the "FFLocker" sub-menu.
    3.  Click "Lock" or "Unlock."
    4.  The FFLocker CLI will open, and you will be prompted for your password.

## How It Works

### Security Model

1.  **Master Key Derivation:** A 256-bit master key is derived from your password and a global salt using PBKDF2 (600,000 iterations of HMAC-SHA256).
2.  **Per-File Keys:** Each file is encrypted with its own unique key, which is derived from the master key and a unique salt for that file. This ensures that even if one file's key were compromised, it would not affect any other file.
3.  **Authenticated Encryption:** The use of AES-256-GCM means that every chunk of encrypted data is authenticated. This prevents tampering and allows the application to detect if an encrypted file has been modified or corrupted.

### Triple-Redundant Metadata

To ensure you can always recover your data, FFLocker creates three metadata containers:
*   `.fflmeta` (Primary)
*   `.fflbkup` (Backup)
*   `.fflrcvr` (Recovery)

If the primary container is damaged, the application will automatically try to use the backup, and then the recovery container, significantly reducing the risk of data loss due to file corruption.

## Security Considerations

*   **Password Strength is Critical:** The security of your locked files and folders depends entirely on the strength of your password. Use a long, complex, and unique password.
*   **No Password Recovery:** There is **no way** to recover a lost password. If you forget your password, your data will be permanently inaccessible.
*   **Threat Model:**
    *   **Protects against:** Unauthorized access to your files on a stolen or compromised computer.
    *   **Does not protect against:** Malware on a running system, such as keyloggers or screen recorders, that could capture your password as you type it.

## Limitations

*   **Windows Only:** This application is designed for and tested on Windows.
*   **Files in Use:** FFLocker cannot encrypt files that are currently open or in use by another program.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.