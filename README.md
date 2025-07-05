# FFLocker (File & Folder Locker)

FFLocker is a .NET application for Windows that provides strong, password-based encryption for your files and folders. It is designed with a focus on security and ease of use, offering a graphical user interface and deep integration with the Windows shell.

## How to Use

### GUI Mode

To use the graphical interface, simply double-click `FFLocker.exe`.

*   **Selecting a File or Folder:**
    1.  Choose whether you want to select a "File" or "Folder" using the radio buttons.
    2.  Click the "Browse..." button to select the item you want to lock or unlock.
*   **Locking and Unlocking:**
    1.  Click the "Lock" or "Unlock" button.
    2.  An inline password field will appear. Enter your password and click "Confirm" or press Enter.
*   **Viewing Locked Items:**
    *   Click the "Show Locked Items" button to see a list of all files and folders you have locked.
    *   The list will show the original path by default. You can use the dropdown menu to see the generated (obfuscated) names.
    *   The list will automatically refresh to show items locked via the context menu while the app is open.
*   **Options:**
    *   **Dark Mode:** Toggle the "Dark Mode" checkbox to switch between light and dark themes. Your preference is saved automatically.
    *   **Show more info:** See detailed logs of the application's operations, including performance metrics.
    *   **Context Menu:** Enable or disable the Windows context menu integration.

### Context Menu

For maximum convenience, you can integrate FFLocker directly into the Windows right-click context menu.

*   **Enabling the Context Menu:**
    1.  **Run `FFLocker.exe` as an administrator.**
    2.  Click the "Context Menu" checkbox in the GUI.
    3.  A dialog will ask if you want to restart Windows Explorer to apply the changes. Click "Yes."
*   **Using the Context Menu:**
    1.  Right-click on any file or folder.
    2.  Go to the "FFLocker" sub-menu.
    3.  Click "Lock" or "Unlock."
    4.  A console window will open, and you will be prompted for your password.

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

## Features

*   **Encryption:** Uses AES-256-GCM for authenticated encryption, ensuring both confidentiality and integrity of your data.
*   **Key Derivation:** Implements PBKDF2 with 600,000 iterations (HMAC-SHA256) to derive a strong key from your password.
*   **User-Friendly GUI:**
    *   An intuitive graphical interface for easy operation.
    *   View all locked items, with the ability to toggle between original and obfuscated names.
    *   Dark & Light Modes, with your preference saved automatically.
    *   The application remembers its last position on your screen.
*   **Windows Integration:**
    *   Optionally integrate FFLocker into the Windows context menu for quick lock/unlock operations.
*   **Reliability:**
    *   Creates three copies of the encryption metadata (`.fflmeta`, `.fflbkup`, `.fflrcvr`) to protect against data loss.
    *   Designed to prevent data corruption if the application is interrupted during an operation.
*   **Performance:**
    *   Encrypts and decrypts files of any size with minimal memory usage.
    *   Utilizes multiple CPU cores to speed up operations on folders.

## Security Considerations

*   **Password Strength is Critical:** The security of your locked files and folders depends entirely on the strength of your password. Use a long, complex, and unique password.
*   **No Password Recovery:** There is **no way** to recover a lost password. If you forget your password, your data will be permanently inaccessible.
*   **Threat Model:**
    *   **Protects against:** Unauthorized access to your files on a stolen or compromised computer.
    *   **Does not protect against:** Malware on a running system, such as keyloggers or screen recorders, that could capture your password as you type it.

## Limitations

*   **Windows Only:** This application is designed for and tested on Windows.
*   **Files in Use:** FFLocker cannot encrypt files that are currently open or in use by another program.
*   **CLI Support:** The command-line interface (CLI) is deprecated and no longer officially supported. While it may still function, the GUI and context menu are the recommended ways to use the application.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
