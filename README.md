# Aukronome // RawBlock Edition

A sample-accurate Windows desktop metronome built with C# / .NET 8, WPF, and NAudio (WASAPI shared mode), styled strictly in the **RawBlock Design System**.

## RawBlock Design System
- **Brutalist Anti-Design:** Stark black-on-white (`#000000` on `#FFFFFF`), no rounded corners (`0px` radius on all elements), no drop shadows or blur effects.
- **Visual Hierarchy through Border Weight:** Structural 3px borders (`border-thick`) and 5px borders (`border-heavy`).
- **High-Contrast Inversions:** Full black-to-white / white-to-black inversions on hover, active states, and latency-compensated beat flashes.
- **Typography:** `Archivo Black` headers and hero BPM, `Space Mono` inputs and numeric metrics, `Work Sans` structural labels.

## Time Signature Controls (4/4, 6/8, 8/8 & Custom Meters)
- **One-Click Quick Select Chips:** Instant selection for `2/4`, `3/4`, `4/4`, `5/4`, `6/8`, `7/8`, `8/8`, `9/8`, and `12/8`.
- **Combined Direct Input:** Type any time signature directly (e.g. `4/4`, `6/8`, `8/8`, `7/8`) in the unified input box and press Enter.
- **Musical Denominator Stepping:** Denominator buttons `−` and `+` step through standard musical note values (`1, 2, 4, 8, 16, 32`), doubling or halving the beat unit note.
- **Musical Default Accents:**
  - `6/8` automatically groups into 3+3 compound pulse (accent on beats 1 and 4).
  - `8/8` automatically groups into 3+3+2 (accent on beats 1, 4, and 7).
  - `7/8` automatically groups into 2+2+3 (accent on beats 1, 3, and 5).
  - `4/4` / `3/4` / `2/4` accent beat 1.
  - Click any beat block to customize: `Accent` → `Normal` → `Mute`.

## Additional Features
- **Sample-Accurate Audio Engine (`MetronomeSampleProvider`):** Renders synthesized clicks at exact sample frame offsets.
- **5 Built-in Synthesized Sound Families:** `Click`, `Beep`, `Woodblock`, `Cowbell`, and `Hi-Hat` (with distinct `Accent`, `Normal`, and `Subdivision` voices synthesized in code).
- **1–400 BPM Range:** Slider, `−10`/`−1`/`+1`/`+10` buttons, exact numeric input, and adaptive Tap Tempo.
- **Subdivisions:** `None (1×)`, `8ths (2×)`, `Triplets (3×)`, `16ths (4×)`, `Quintuplets (5×)`, and `Sextuplets (6×)` with an independent subdivision volume slider.
- **Latency-Compensated Visual Sync:** Polls WASAPI hardware position so beat blocks invert in real-time when clicks are heard.
- **Presets & Session Persistence:** Named presets (`presets.json`) and session restore (`settings.json`) under `%AppData%\Aukronome\`.

## Keyboard Shortcuts (Window Focused)
| Shortcut | Action |
|---|---|
| `Space` | Start / Stop |
| `↑` / `↓` | ±1 BPM |
| `Shift + ↑` / `↓` | ±10 BPM |
| `T` | Tap Tempo |
| `Ctrl + S` | Save Preset |

## Building, Running & Publishing

```powershell
cd C:\Users\ACER\.gemini\antigravity\scratch\Aukronome

# Run from source
dotnet run --project src/Aukronome.App

# Run xUnit tests
dotnet test

# Publish self-contained single-file executable
dotnet publish src/Aukronome.App -p:PublishProfile=SingleFile
```
