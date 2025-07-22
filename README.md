# FFLocker (File & Folder Locker)

FFLocker is a modern Windows application for encrypting your files and folders. It is designed with a focus on security, reliability, and a clean, easy-to-use interface. It can be run as a graphical application, a command-line tool, or from the Windows context menu.

## Features

*   **Strong Encryption:** Uses AES-256-GCM for authenticated encryption and Argon2id for robust key derivation from your password.
*   **Windows Hello Integration:** Optionally protect your data with Windows Hello (fingerprint, face, PIN) for convenient and secure unlocking.
*   **Multiple Operation Modes:**
    *   **GUI:** An intuitive graphical interface for easy operation.
    *   **CLI:** A command-line interface for scripting and automation.
    *   **Context Menu:** Quick access to lock/unlock from the Windows right-click menu.
*   **Self-Contained Files:** Each encrypted file (`.ffl`) is a portable, self-contained vault. All necessary metadata is embedded, so you can move it to another machine and decrypt it with just the password.
*   **Privacy-Focused Folder Encryption:** When a folder is locked, the original directory structure is obscured, preventing attackers from inferring information from the folder hierarchy.

## Getting Started

### Prerequisites

*   **Windows 10/11**
*   **.NET 9 SDK** or later.
*   **Visual Studio 2022** with the **.NET Multi-platform App UI development** workload installed.

### Building the Application

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/psppspnaik209/FFLocker.git
    ```
2.  **Open the solution** (`FFLocker.sln`) in Visual Studio 2022.
3.  **Build the solution** by selecting `Build > Build Solution` or pressing `Ctrl+Shift+B`.

The executable will be located at `FFLocker\bin\x64\Debug\net9.0-windows10.0.26100.0\win-x64\FFLocker.exe`.

## How to Use

FFLocker can be used in three different modes:

### 1. GUI Mode

Run `FFLocker.exe` without any command-line arguments to launch the graphical interface.

*   **Locking and Unlocking:**
    1.  Select "File" or "Folder" and click "Browse..." to choose your item.
    2.  Click "Lock" or "Unlock".
    3.  Enter your password when prompted.
    4.  When locking, you can check "Use Windows Hello" to link the encryption to your device for faster unlocking.
*   **Canceling:** During an operation, a "Cancel" button will appear. Clicking it will prompt you to confirm and then unsafely stop the process.
*   **Locked Items:** The "Show Locked Items" button displays a list of all files and folders you have locked. You can select an item from this list and click "Use This" to quickly load it for unlocking.

### 2. Command-Line (CLI) Mode

You can perform operations directly from your terminal, which is useful for scripting. A console window will appear to handle the operation.

*   **Lock an item:**
    ```bash
    ./FFLocker.exe lock "C:\path\to\your\file_or_folder"
    ```
*   **Unlock an item:**
    ```bash
    ./FFLocker.exe unlock "C:\path\to\your\locked_item"
    ```

You will be prompted to enter your password and choose whether to use Windows Hello.

### 3. Context Menu Mode

For convenience, you can integrate FFLocker into the Windows right-click context menu.

*   **Enabling:**
    1.  Run `FFLocker.exe` **as an administrator**.
    2.  Check the "Context Menu" box in the application's main window.
*   **Using:**
    1.  Right-click on any file or folder.
    2.  Navigate to the "FFLocker" sub-menu and click "Lock" or "Unlock."

## Security Model & User Advice

### Your Password is Your Only Key

The security of your locked files depends entirely on the strength of your password.
*   **There is absolutely no password recovery.** If you forget your password, your data will be permanently inaccessible.
*   If you use Windows Hello, the password still serves as the ultimate backup. If you move the files to a new computer or your Windows Hello configuration is lost, you will need the password to decrypt your data.
*   Use a long, complex, and unique password that you will not forget.

### Threat Model

*   **FFLocker Protects Against:** Unauthorized access to your files on a stolen or compromised computer (offline attacks). If someone steals your laptop or hard drive, they will not be able to access the contents of your `.ffl` files without the password.
*   **FFLocker Does NOT Protect Against:** Active malware on a running system. If your computer is infected with a keylogger or screen recorder, it could capture your password as you type it. Always ensure your system is secure before handling sensitive data.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.