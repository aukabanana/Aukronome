# Aukronome

A high-precision, sample-accurate metronome for Windows, engineered in C# (.NET 8) and styled in the stark brutalist **RawBlock Design System**.

Designed for solo musicians, music teachers, and rhythm practice, Aukronome mixes clicks directly into a continuous audio stream at exact sample offsets, guaranteeing zero timing jitter regardless of buffer size or CPU spikes.

---

## Quick Start Guide

### Option 1: Standalone Portable App (No Install Needed)
You can produce a single self-contained `.exe` file that runs on any Windows 10/11 computer without installing the .NET runtime:

```powershell
# In the project root, publish the single-file executable:
dotnet publish src/Aukronome.App -p:PublishProfile=SingleFile
```
The resulting executable will be in:
`src\Aukronome.App\bin\Release\net8.0-windows\win-x64\publish\Aukronome.exe`

Just double-click `Aukronome.exe` to run. You can copy this single `.exe` to a USB drive or anywhere on your PC.

### Option 2: Run from Source
If you have the .NET 8 SDK installed:
```powershell
git clone https://github.com/aukabanana/Aukronome.git
cd Aukronome
dotnet run --project src/Aukronome.App
```

### Option 3: Add to Desktop & Windows Start Menu (Make Searchable)
To make Aukronome searchable in Windows Search (so you can just press the **Windows key**, type **`Aukronome`**, and hit Enter):
```powershell
# In PowerShell:
.\Create-Shortcuts.ps1
```
This script automatically detects the project path, builds the executable if needed, and installs shortcuts on your **Desktop** and inside your **Windows Start Menu Programs folder** with the official logo.

---

## User Guide: How to Practice with Aukronome

### 1. Starting & Stopping
- Press the **Spacebar** or click the large **START [SPACE]** button at the bottom.
- When playing, the button turns solid Red (**STOP [SPACE]**).

### 2. Setting Tempo (1 – 400 BPM)
There are 4 ways to set your tempo:
1. **Quick Buttons:** Click `−10`, `−1`, `+1`, or `+10` in the top card.
2. **Tempo Slider:** Drag the slider between 1 and 400 BPM.
3. **Exact Numeric Input:** Type your desired BPM into the `EXACT` box and press **Enter** (or click outside).
4. **Tap Tempo:** Tap the **TAP TEMPO (T)** button or press the **`T`** key repeatedly in time. Aukronome averages the last 4 taps with adaptive pause detection.

### 3. Time Signatures (4/4, 6/8, 8/8, Odd Meters)
- **One-Click Quick Select:** Click any preset chip:
  - `4/4`: 4 beats with downbeat accent.
  - `6/8`: 6 beats with compound 3+3 pulse (beats 1 and 4 accented).
  - `8/8`: 8 beats with 3+3+2 pulse (beats 1, 4, and 7 accented).
  - `7/8`: 7 beats with 2+2+3 pulse (beats 1, 3, and 5 accented).
  - Also available: `2/4`, `3/4`, `5/4`, `9/8`, and `12/8`.
- **Direct Input:** Type any time signature (e.g. `6/8`, `8/8`, `7/8`) in the `DIRECT INPUT` box and press **Enter**.
- **Musical Steppers:** Use `−` and `+` on the numerator (`1–32`) and denominator (`1, 2, 4, 8, 16, 32`), doubling or halving the beat unit note.

### 4. Interactive Beat Blocks & Accent Editor
Every beat is displayed as a brutalist square block in the **BEATS & ACCENTS** row:
- **Click any beat block** to cycle through 3 states:
  - **`ACC` (Accent):** Strong emphasized click (5px thick border).
  - **`NRM` (Normal):** Standard click (3px thick border).
  - **`MUTE` (Muted):** Completely silent beat (gray dashed block).
- **Pro Practice Tip (Gap Training):** Mute random beats (e.g. mute beat 3 in 4/4, or alternate muted bars) to test and develop your internal timing.
- **Visual Sync:** Each block inverts to solid Black when the click is heard through your speakers, calibrated for hardware audio latency.

### 5. Subdivisions
Select subdivisions from the dropdown:
- `None (1×)`
- `8ths (2×)`
- `Triplets (3×)`
- `16ths (4×)`
- `Quintuplets (5×)`
- `Sextuplets (6×)`

Adjust the **SUBDIVISION VOLUME** slider independently from the **MASTER VOLUME** to set the subdivision clicks to your preferred level.

### 6. Built-in Synthesized Click Sounds
All sounds are synthesized directly in code algorithms with zero external WAV files:
- **Click:** Crisp, punchy transient click.
- **Beep:** Classic electronic metronome tone.
- **Woodblock:** Organic acoustic percussion strike.
- **Cowbell:** 808-style dual-oscillator cowbell.
- **Hi-Hat:** Metallic sizzle cymbal.

Each sound has dedicated **Accent**, **Normal**, and **Subdivision** voices tuned by pitch and envelope.

### 7. Managing Presets
Save your favorite tempo, time signature, accents, and sound setups for specific songs or exercises:
- **SAVE:** Overwrite the currently selected preset (**Ctrl+S**).
- **SAVE AS:** Name and save a new preset.
- **RENAME:** Change the name of the active preset.
- **DELETE:** Remove the active preset.
- Presets are automatically saved to `%AppData%\Aukronome\presets.json`.

---

## Keyboard Shortcuts (Hands-Free Control)

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
| `Esc` | Cancel text entry / close popups |

---

## Technical Specifications & Audio Architecture

- **Platform:** Windows 10 & 11 (64-bit).
- **Audio Output:** NAudio WASAPI Shared Mode (`~35 ms` latency, 48 kHz float).
  - *Why Shared Mode?* You can play Aukronome alongside YouTube music lessons, Spotify, DAW backing tracks, or video calls simultaneously without exclusive audio driver locks.
- **Timing Engine:** Clicks are mixed in sample offsets calculated from sample rate, not system timers or UI dispatchers.
- **Session Persistence:** Window bounds and settings automatically restore from `%AppData%\Aukronome\settings.json`.

---

## Development & Testing

```powershell
# Build entire solution
dotnet build

# Run all 35 xUnit unit tests
dotnet test

# Run tests with detailed log output
dotnet test --logger "console;verbosity=detailed"
```

## License
MIT License. Free to use, modify, and distribute for personal and educational practice.
