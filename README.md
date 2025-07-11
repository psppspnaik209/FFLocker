# FFLocker (File & Folder Locker)

FFLocker is a modern Windows application built with WinUI 3 that provides strong, password-based encryption for your files and folders. It is designed with a focus on security, reliability, and a clean, easy-to-use interface.

## Features

*   **Strong Encryption:** Uses **AES-256-GCM** for authenticated encryption, ensuring both the confidentiality and integrity of your data.
*   **Robust Key Derivation:** Implements **Argon2id**, the winner of the Password Hashing Competition, to derive a strong encryption key from your password, providing excellent resistance against modern cracking hardware.
*   **Self-Contained Files:** Each encrypted file (`.ffl`) is a portable, self-contained vault. All the necessary metadata is embedded within the file's header, so you can move a single encrypted file to another machine and decrypt it with just the password.
*   **Fail-Safe Operations:** FFLocker uses a transactional approach for file operations. It encrypts to temporary files first and only commits the changes (deleting originals) after a successful run. This prevents data loss or corruption if the process is interrupted.
*   **Privacy-Focused Folder Encryption:** When a folder is locked, the original directory structure is obscured. All files are encrypted and stored in the root of the locked folder, preventing attackers from inferring information from the folder hierarchy.
*   **Modern UI:** A clean and intuitive user interface built with WinUI 3, featuring Light and Dark mode support and a detailed log view.
*   **Windows Integration:** Optionally integrate FFLocker into the Windows context menu for quick lock/unlock operations.

## Getting Started

### Prerequisites

*   **Windows** operating system.
*   **.NET 8 SDK** or later.
*   **Visual Studio 2022** with the **.NET Multi-platform App UI development** workload installed.

### Installation & Building

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/psppspnaik209/FFLocker.git
    ```
2.  **Open the solution** (`FFLocker.sln`) in Visual Studio 2022.
3.  **Build the application:**
    *   From the menu, select `Build > Build Solution`.
    *   Alternatively, press `Ctrl+Shift+B`.

### Running the Application

*   In the Visual Studio toolbar, ensure `FFLocker (Unpackaged)` is selected as the startup project.
*   Press the green "play" button or `F5` to run the application.

## How to Use

### Main Application

*   **Selecting a File or Folder:**
    1.  Choose whether you want to select a "File" or "Folder" using the radio buttons.
    2.  Click the "Browse..." button to select the item you want to lock or unlock.
*   **Locking and Unlocking:**
    1.  Click the "Lock" or "Unlock" button.
    2.  A dialog will appear prompting you for a password. Enter your password and click "Confirm".
    3.  When locking a folder, its name will be changed to `FolderName_USE_FOR_FOLDER_UNLOCK_DO_NOT_DELETE`. To unlock it, simply select this renamed folder.
*   **Options:**
    *   **Dark/Light Mode:** Use the toggle switch to change the application theme.
    *   **Log:** See detailed logs of the application's operations.
    *   **Context Menu:** Enable or disable the Windows context menu integration.

### Context Menu

For convenience, you can integrate FFLocker directly into the Windows right-click context menu.

*   **Enabling the Context Menu:**
    1.  **Run `FFLocker.exe` as an administrator.**
    2.  Click the "Context Menu" checkbox in the application.
*   **Using the Context Menu:**
    1.  Right-click on any file or folder.
    2.  Go to the "FFLocker" sub-menu.
    3.  Click "Lock" or "Unlock."
    4.  A dialog will open, prompting you for your password.

## Security Model & User Advice

### Your Password is Your Only Key

The security of your locked files depends entirely on the strength of your password. 
*   **There is absolutely no password recovery.** If you forget your password, your data will be permanently inaccessible.
*   Use a long, complex, and unique password that you will not forget.

### Threat Model

*   **FFLocker Protects Against:** Unauthorized access to your files on a stolen or compromised computer (offline attacks). If someone steals your laptop or hard drive, they will not be able to access the contents of your `.ffl` files without the password.
*   **FFLocker Does NOT Protect Against:** Active malware on a running system. If your computer is infected with a keylogger or screen recorder, it could capture your password as you type it. Always ensure your system is secure before handling sensitive data.

### Best Practices

*   **Do Not Delete `.ffl` Files:** These are your encrypted files. Deleting them is equivalent to deleting your original data.
*   **Files In Use:** FFLocker cannot encrypt files that are currently open or in use by another program. Ensure files are closed before locking.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.