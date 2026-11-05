# Aukronome

A high-precision, sample-accurate metronome for Windows 10 & 11, engineered in C# (.NET 8) and styled in the stark brutalist **RawBlock Design System**.

Designed for solo musicians, music teachers, and rhythm practice, Aukronome mixes clicks directly into a continuous audio stream at exact sample offsets, guaranteeing zero timing jitter regardless of CPU load, system buffer size, or background processes.

---

## Features

- **Sample-Accurate Timing:** Timing is calculated from hardware audio sample rates, completely eliminating OS timer jitter and GC stalls.
- **RawBlock Brutalist UI:** High-contrast monochrome interface featuring 0px border radius, heavy structural borders, and bold typography (`Archivo Black`, `Work Sans`, `Space Mono`).
- **Flexible Time Signatures:** Full support for simple, compound, and complex odd meters (`4/4`, `3/4`, `6/8`, `7/8`, `8/8`, `12/8`, etc.) with quick-select chips and direct text entry.
- **3-State Interactive Accent Editor:** Click individual beat blocks to toggle between **Accent (`ACC`)**, **Normal (`NRM`)**, and **Mute (`MUTE`)** for rhythm gap training.
- **Subdivisions with Independent Volume:** Practice 8ths, Triplets, 16ths, Quintuplets, and Sextuplets with a dedicated volume fader.
- **100% Algorithmic Sound Synthesis:** Zero external WAV dependencies. Sounds (Click, Beep, Woodblock, Cowbell, Hi-Hat) are generated in code algorithms.
- **Adaptive Tap Tempo:** 4-tap rolling average with smart pause detection that automatically adapts to slow tempos.
- **Session & Preset Persistence:** Save song tempos, time signatures, and accent patterns to `%AppData%\Aukronome\presets.json`. Restores your exact window size and settings on launch.
- **Latency-Compensated Visual Sync:** Beat blocks invert to solid black precisely when sound leaves the physical speaker cones.

---

## Quick Start & Installation

### Option 1: Windows Graphical Setup Installer (Recommended — No CMD Needed)
For everyday musicians and users who want to install and use Aukronome without opening a terminal:

1. Download **`Aukronome-Setup-v1.0.0.exe`** (from [GitHub Releases](https://github.com/aukabanana/Aukronome/releases) or build it below).
2. Double-click the `.exe` to open the setup wizard.
3. Click **Next > Install**:
   - **No administrator privileges or UAC prompts required** (installs to `%LocalAppData%\Programs\Aukronome`).
   - Automatically places a **Desktop shortcut** with the brutalist Aukronome logo.
   - Automatically registers in the **Windows Start Menu** (searchable by pressing the Windows key and typing `Aukronome`).
   - Adds a clean uninstaller in Windows Settings (*Apps > Installed apps*).

> **To build the installer locally without typing commands:**
> Simply double-click **`Build-Installer.bat`** in the repository root. It compiles the single-file binary, packages the installer using Inno Setup, and outputs `installer\output\Aukronome-Setup-v1.0.0.exe`.

---

### Option 2: Standalone Portable Executable (No Install Needed)
Run Aukronome on any Windows 10/11 machine directly from a USB drive without installing .NET or running setup:

```powershell
# Publish the single-file executable:
dotnet publish src/Aukronome.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist/portable
```
Copy `dist\portable\Aukronome.exe` anywhere and double-click to run.

---

### Option 3: Run from Source (.NET 8 SDK)
If you are developing or testing from source:

```powershell
git clone https://github.com/aukabanana/Aukronome.git
cd Aukronome
dotnet run --project src/Aukronome.App
```

---

### Option 4: Create Shortcuts from Cloned Source
If you cloned the source code and want Desktop & Start Menu shortcuts pointing to your local build:

```powershell
# In PowerShell:
.\Create-Shortcuts.ps1
```

---

## User Guide: Practicing with Aukronome

### 1. Starting & Stopping
- Press the **Spacebar** or click the large **START [SPACE]** button at the bottom.
- During playback, the button inverts to solid Red (**STOP [SPACE]**).

### 2. Setting Tempo (1 – 400 BPM)
Aukronome offers 4 input modes:
1. **Quick Steppers:** Click `−10`, `−1`, `+1`, or `+10` in the top card.
2. **Tempo Slider:** Drag the full-width slider between 1 and 400 BPM.
3. **Exact Numeric Input:** Type your desired BPM into the `EXACT` box and press **Enter** (or click outside).
4. **Tap Tempo:** Tap the **TAP TEMPO (T)** button or press the **`T`** key repeatedly in time. Aukronome calculates a rolling average across your last 4 taps with adaptive pause detection.

### 3. Time Signatures & Odd Meters
- **One-Click Quick Select:** Click any preset chip:
  - `4/4`: 4 beats with downbeat accent.
  - `6/8`: 6 beats with compound 3+3 pulse (beats 1 and 4 accented).
  - `8/8`: 8 beats with 3+3+2 asymmetric pulse (beats 1, 4, and 7 accented).
  - `7/8`: 7 beats with 2+2+3 Balkan/Progressive pulse (beats 1, 3, and 5 accented).
  - Also available: `2/4`, `3/4`, `5/4`, `9/8`, and `12/8`.
- **Direct Input:** Type any custom meter (e.g. `11/8`, `15/16`, `5/8`) in the `DIRECT INPUT` box and press **Enter**.
- **Musical Steppers:** Use `−` and `+` on the numerator (`1–32`) and denominator (`1, 2, 4, 8, 16, 32`), doubling or halving the note value.

### 4. Interactive Beat Blocks & Accent Editor
Every beat in the measure is rendered as a brutalist square block in the **BEATS & ACCENTS** row:
- **Click any beat block** to cycle through 3 levels:
  - **`ACC` (Accent):** Strong emphasized click (5px thick black border).
  - **`NRM` (Normal):** Standard beat click (3px thick black border).
  - **`MUTE` (Muted):** Completely silent beat (gray dashed block).
- **Rhythm Gap Training:** Mute specific beats (e.g. mute beat 3 in 4/4, or alternate muted measures) to test and develop your internal clock.
- **Visual Sync:** Each block inverts to solid Black the exact instant the sound wave exits your speakers.

### 5. Subdivisions
Select your desired subdivision from the dropdown:
- `None (1×)`
- `8ths (2×)`
- `Triplets (3×)`
- `16ths (4×)`
- `Quintuplets (5×)`
- `Sextuplets (6×)`

Use the **SUBDIVISION VOLUME** slider to balance subdivision clicks relative to the primary beats.

### 6. Algorithmic Synthesized Click Sounds
All audio is synthesized mathematically in C# code with zero external WAV files:
- **Click:** Crisp, punchy transient click.
- **Beep:** Classic electronic metronome tone.
- **Woodblock:** Organic acoustic hardwood percussion strike.
- **Cowbell:** 808-style dual-oscillator resonant strike.
- **Hi-Hat:** Metallic sizzle cymbal.

Each sound has dedicated **Accent**, **Normal**, and **Subdivision** voices tuned by pitch and envelope.

### 7. Presets & Persistence
Save your favorite tempo, meter, accents, and sound setups for specific songs or exercises:
- **SAVE:** Overwrite the current preset (**Ctrl+S**).
- **SAVE AS:** Name and save a new preset.
- **RENAME:** Update the active preset's name.
- **DELETE:** Remove the active preset.
- Presets are stored in `%AppData%\Aukronome\presets.json` and are never overwritten by git updates.

---

## Keyboard Shortcuts

| Key | Action |
|---|---|
| `Space` | Start / Stop metronome |
| `↑` | Increase BPM by 1 |
| `↓` | Decrease BPM by 1 |
| `Shift + ↑` | Increase BPM by 10 |
| `Shift + ↓` | Decrease BPM by 10 |
| `T` | Tap tempo |
| `Ctrl + S` | Save current preset |
| `Enter` | Commit text entry in BPM or Time Signature boxes |
| `Esc` | Cancel text entry / dismiss focus |

---

## Architecture & Design System Documentation

- **[Audio Architecture Deep Dive](docs/ARCHITECTURE.md):** Detailed guide to sample-accurate audio streaming, WASAPI shared mode, lock-free parameter exchanges, and latency compensation algorithms.
- **[RawBlock Design System Specification](docs/DESIGN_SYSTEM.md):** Complete specification of color tokens, typography scales, 0px border radius principles, and brutalist UI components.

---

## Repository Structure

```
Aukronome/
├── Aukronome.sln
├── Build-Installer.bat          # 1-click double-executable installer builder
├── Create-Shortcuts.ps1         # Desktop & Start Menu shortcut creator
├── README.md                    # Main project documentation
├── docs/
│   ├── ARCHITECTURE.md          # Technical audio engine & timing documentation
│   └── DESIGN_SYSTEM.md         # RawBlock brutalist UI design system specification
├── installer/
│   ├── Aukronome.iss            # Inno Setup 6 compiler script
│   └── output/                  # Compiled Aukronome-Setup-v1.0.0.exe
├── src/
│   ├── Aukronome.Core/          # Audio engine, sound synthesis, models, persistence
│   │   ├── Audio/               # MetronomeAudioEngine, MetronomeSampleProvider, SoundSynthesizer
│   │   ├── Models/              # MusicalTypes, AccentLevel, SubdivisionType, ClickSoundType
│   │   ├── Persistence/         # StorageService, AppSettings, MetronomePreset
│   │   └── Services/            # TapTempoCalculator
│   └── Aukronome.App/           # WPF MVVM application
│       ├── Assets/              # Aukronome.ico, Aukronome.png
│       ├── ViewModels/          # MainViewModel, BeatDotViewModel, OptionItem
│       ├── Views/               # MainWindow.xaml, MainWindow.xaml.cs
│       └── App.xaml             # RawBlock brutalist styles and color dictionary
└── tests/
    └── Aukronome.Core.Tests/    # 35 xUnit tests (timing, tap tempo, storage, meter math)
```

---

## Development & Verification

### Build & Run
```powershell
# Build entire solution
dotnet build

# Run WPF application
dotnet run --project src/Aukronome.App
```

### Run Unit Tests
```powershell
# Run all 35 xUnit tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

### Packaging & Installer
```powershell
# Run the 1-click automated build:
.\Build-Installer.bat --no-pause
```

---

## License

This project is licensed under the terms of the [MIT License](LICENSE).
