# ⚡ SmartZip

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10-purple.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-blue.svg)](https://www.microsoft.com/windows)
[![Language](https://img.shields.io/badge/C%23-13-512BD4.svg)](https://learn.microsoft.com/dotnet/csharp/)

> **Zip any folder, skip what you don't need — node_modules, .git, junk — safely.**

SmartZip is a **free, lightweight, single-file Windows desktop app** for creating ZIP archives with visual tree-based exclusion — a full folder tree view lets you check/uncheck anything you don't want in the archive before hitting "Create ZIP". Built from scratch in C# WinForms with a sleek dark theme UI.

---

## 👀 Why SmartZip Exists

When sharing or backing up a project folder — especially web dev projects — you don't want `node_modules` (often 100+ MB), `.git` history, build outputs (`bin/obj`), or other junk bloating your ZIP and making it hard to read. SmartZip shows you **everything** that would go into the ZIP, lets you **uncheck** what to exclude with a visual tree, shows you **live file counts and size estimates**, then zips **only what's checked**.

It's the control of manual selection with the visual clarity of a proper file tree — right in a clean Windows app.

---

## ✨ Features

### 📦 Zip Folder
- **Full folder tree** — every subfolder and file loaded in a navigable tree with checkboxes
- **Visual exclusion** — uncheck anything you don't want in the ZIP (folders, files, anything)
- **Quick Exclude Presets** — one click to auto-uncheck `node_modules`, `.git`, `.vs`, `bin`, `obj`, and more
- **Live file count & size estimates** — shows included / excluded file counts and sizes as you toggle
- **Async zipping with progress** — non-blocking UI with real-time progress bar and status updates
- **Relative path preservation** — ZIP entries are stored with correct relative paths, so extraction restores the folder structure exactly

### 📂 Unzip File
- **Preview ZIP contents** — see the full file tree inside any ZIP before extracting
- **Folder extraction with progress** — async extraction with real-time file count updates
- **Overwrite detection** — warns if destination isn't empty, so you don't accidentally wipe files
- **Auto-complete path suggestion** — when you pick a ZIP, destination folder auto-suggests an output path

### 🎨 UI & Design
- **Custom dark theme** — hand-tuned indigo/purple accent dark mode UI, not system theme
- **Custom folder/file icons** — built-in programmatically generated icons for the tree
- **Custom styled button** — hover effects, proper disabled states, accent coloring
- **Responsive layout** — TableLayoutPanel based, resizes correctly
- **About tab** — full usage instructions built into the app

### 🔒 Safe
- **Original files never modified** — ZIP is always created as a separate new file
- **Nothing deleted** — exclusions only control what goes into the ZIP, never touch source
- **Temporary file + atomic rename** — writes to `.tmp` first, only overwrites final path on success

---

## 📋 System Requirements

| Requirement | Minimum |
|---|---|
| **OS** | Windows 10 / Windows 11 (64-bit) |
| **Runtime** | .NET 10 Desktop Runtime or .NET 10 SDK |
| **RAM** | 2 GB |
| **Disk Space** | Enough to hold both source folder + output ZIP |

> ⚡ **No internet required.** SmartZip has zero external dependencies — every pixel of the UI is drawn in code. No NuGet packages, no web APIs, no telemetry.

---

## 🚀 Getting Started

### Option 1: Download the Release

1. Go to [Releases](https://github.com/YOUR_USERNAME/SmartZip/releases) (update with your username)
2. Download the latest `SmartZip.zip`
3. Extract and run `SmartZip.exe` — no installer needed

### Option 2: Build from Source

1. Clone this repository:
   ```bash
   git clone https://github.com/HarryChhote7/SmartZip.git
   cd SmartZip
   ```

2. Make sure you have .NET 10 SDK installed:
   ```bash
   dotnet --version
   ```
   Install from [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)

3. Build:
   ```bash
   dotnet build SmartZip.csproj
   ```

4. Run:
   ```bash
   dotnet bin/Debug/net10.0-windows/SmartZip.dll
   ```
   Or find the executable at `bin/Debug/net10.0-windows/SmartZip.exe` (self-contained build produces one `.exe`)

### Option 3: Build Script

Use the included `build.bat`:
```batch
build.bat
```
This will build in Release configuration and tell you the output path.

---

## 📖 How to Use

### Zip a Folder

1. **Open SmartZip** → You'll see the "📦 Zip Folder" tab by default
2. **Click "Browse"** on the Source Folder → Select the folder you want to ZIP
3. **Wait for the tree to load** → Every file and subfolder appears in a checkbox tree
4. **Uncheck what you want to exclude:**
   - Click a checkbox to uncheck a folder or file (dashes mean children are excluded)
   - Click a checkbox to check a folder or file (checks all children too)
5. **Use Quick Exclude Presets (optional):**
   - On the right panel, check the presets you want → Click "Apply Exclusions"
   - This auto-unchecks matching folders throughout the tree
   - Common presets: `node_modules`, `.git`, `bin`, `obj`, `__pycache__`, etc.
6. **Check the estimate** → The bottom-right panel shows included file count/size and excluded count/size
7. **Choose output path** → Click Browse or let it auto-complete (e.g., `folder_backup.zip`)
8. **Click "Create ZIP"** → Watch the progress bar, get a completion dialog with an option to open the folder

### Unzip a File

1. **Go to the "📂 Unzip File" tab**
2. **Click "Browse ZIP"** → Select your `.zip` file
3. **Preview contents** → The ZIP file tree shows what's inside
4. **Choose destination folder** → Click "Browse" for extraction location
5. **Click "Extract Files"** → Watch progress, get option to open when done

---

## 🏗️ Architecture

SmartZip is intentionally designed as a **single `.cs` file** (~1280 lines) so it's readable, auditable, and easy to modify without an IDE. This is a deliberate choice — a self-contained WinForms app that anyone can open in any text editor and understand.

```
SmartZip/
├── SmartZip.cs            # Main application (all UI + logic in ~1280 lines)
├── SmartZip.csproj        # .NET 10 project file
├── build.bat              # Windows build script
└── README.md              # This file
```

### Key Classes

| Class | File:Lines | Role |
|---|---|---|
| `Program` | SmartZip.cs:22 | Entry point (calls `MainForm`) |
| `MainForm` | SmartZip.cs:36 | The main WinForms window — all UI and logic |
| `DarkButton` | SmartZip.cs:1237 | Custom painted Button subclass (dark theme) |

### How It Works

```
Browse folder → LoadTree() → populates TreeView with checkbox per file/folder
Check/uncheck toggled → TreeNode_AfterCheck() → propogates up/down
Apply presets → ApplyPresetsToNodes() → unmatches matching folders
Click Create ZIP → CollectCheckedFiles() → reads checked tree nodes → creates ZIP at output path
```

The ZIP is created using `System.IO.Compression.ZipFile` — entries are stored with relative paths from the source folder, so extraction restores the exact directory structure (minus excluded items) without the source folder prefix leaking into paths.

---

## 🛠️ Development

### Prerequisites

- [Visual Studio 2022](https://visualstudio.microsoft.com/) (Community is free) with ".NET desktop development" workload, OR
- [.NET 10 SDK](https://dotnet.microsoft.com/download) with any text editor

### Build Commands

```bash
# Debug build (with debugging symbols)
dotnet build SmartZip.csproj

# Release build (optimized, no debug symbols)
dotnet build SmartZip.csproj -c Release

# Build and run in one command
dotnet run --project SmartZip.csproj
```

---

## 🔧 Troubleshooting

| Problem | Solution |
|---|---|
| Build fails with C# 5 errors | You're using the old .NET Framework compiler. Install .NET 10 SDK from [microsoft.com/dotnet](https://dotnet.microsoft.com/download) |
| App doesn't show dark theme | SmartZip uses a custom dark theme built in code. It should always look dark. If it looks wrong, make sure .NET 10 runtime is installed correctly |
| Very large folders take a while to load | SmartZip scans every file to show sizes. Large projects (100K+ files) will take a few seconds — this is normal and the UI remains responsive |
| ZIP already exists warning appears | SmartZip warns before overwriting. Choose Yes to overwrite or No to pick a different filename |
| Can't open ZIP preview | Make sure the file is a valid `.zip` file. RAR, 7z, and other formats are not supported by the built-in ZIP reader |

---

## 🤝 Contributing

Contributions are welcome! SmartZip is open source and built for the community. Here's how:

- **Found a bug?** Open an [Issue](https://github.com/YOUR_USERNAME/SmartZip/issues) with steps to reproduce
- **Have a feature idea?** Open a [Discussion](https://github.com/YOUR_USERNAME/SmartZip/discussions) or an [Issue](https://github.com/YOUR_USERNAME/SmartZip/issues)
- **Want to submit code?** Fork this repo, make your changes, and open a [Pull Request](https://github.com/YOUR_USERNAME/SmartZip/pulls)

---

## 📄 License

This project is licensed under the **MIT License**. See the [LICENSE](LICENSE) file for details.

In short: You are free to use, modify, distribute, and sell anything based on SmartZip, as long as you include the original license notice.

---

## 🎬 Credits

- **Built by** HarryChhote

---

## ⭐ Star This Repo!

If SmartZip saved you from zipping up a 500 MB `node_modules` folder — give this repo a star. 🙏
