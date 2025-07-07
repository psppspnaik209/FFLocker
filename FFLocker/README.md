# FFLocker (File & Folder Locker)

FFLocker is a lightweight, open-source Windows application for securing your files and folders with strong, password-based encryption. It is built with a modern WinUI 3 interface and integrates directly into the Windows shell for ease of use.

## Features

*   **Strong Encryption:** Uses AES-256-GCM for authenticated encryption, which protects both the confidentiality and integrity of your data.
*   **Robust Password Protection:** Implements PBKDF2 (HMAC-SHA256) with 600,000 iterations to derive a strong encryption key from your password, making brute-force attacks difficult.
*   **Modern & Efficient UI:**
    *   A clean user interface built with WinUI 3.
    *   Support for Light, Dark, and system-default themes.
    *   A two-column layout that allows you to view locked items and primary controls side-by-side.
    *   A detailed log view for troubleshooting and observing operations.
*   **Windows Explorer Integration:**
    *   Optionally add "Lock" and "Unlock" commands to the Windows context menu for any file or folder.
    *   Requires running the application as an administrator once to enable/disable.
*   **Locked Item Management:**
    *   View a list of all items you have locked.
    *   Toggle between viewing the items' original names and their encrypted (fake) names.

## Getting Started

### Prerequisites

*   Windows 10 or later.
*   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later.
*   **Visual Studio 2022** with the **.NET Multi-platform App UI development** workload installed.

### Building from Source

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/psppspnaik209/FFLocker.git
    ```
2.  **Open the solution:**
    *   Open `FFLocker.sln` in Visual Studio 2022.
3.  **Build the application:**
    *   From the menu, select `Build > Build Solution` (or press `Ctrl+Shift+B`).
    *   Ensure `FFLocker (Unpackaged)` is selected as the startup project in the toolbar.
    *   Press `F5` to run.

## How to Use

### Main Application

1.  **Select a File or Folder:**
    *   Use the **File** or **Folder** radio buttons to choose what you want to select.
    *   Click **Browse...** to open the file/folder dialog and make your selection.
2.  **Locking and Unlocking:**
    *   Click the **Lock** or **Unlock** button.
    *   Enter your password when prompted.
3.  **Viewing Locked Items:**
    *   Click the **Show Locked Items** button to display the list of all encrypted items.
    *   Use the **Display Names** dropdown above the list to toggle between seeing the original file/folder names and the encrypted names on disk.
4.  **Log:**
    *   Check the **Log** box to see a detailed view of application operations.

### Context Menu

To lock and unlock files directly from Windows Explorer:

1.  **Run FFLocker as an administrator** one time.
2.  Check the **Context Menu** box in the app.
3.  A dialog will ask if you want to restart Windows Explorer. This is necessary for the changes to appear.
4.  You can now right-click on any file or folder and use the "FFLocker" submenu.

## Security Summary: Pros & Cons

### ✔️ Pros (What FFLocker Does Well)
*   **Strong, Modern Encryption:** Uses the industry-standard AES-256-GCM algorithm to protect your data.
*   **Offline Attack Protection:** An excellent choice for protecting files on a laptop, external hard drive, or USB stick that might be lost or stolen.
*   **Ease of Use:** A simple, no-frills interface for quickly locking and unlocking files.

### ❌ Cons (What FFLocker Cannot Do)
*   **No Protection on a Compromised System:** If your computer has a virus or keylogger, your password can be stolen, and this tool cannot protect you. Always ensure your system is secure *before* using FFLocker.
*   **No Plausible Deniability:** It is obvious that the encrypted `.ffl` files are locked by this tool.
*   **No Password Recovery:** This is a feature, not a bug. If you forget your password, your data is gone forever.

### **Your Password is Your Key**
The security of this entire system rests on the strength of your password. Please use a **long, complex, and unique password** that you will not forget.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
