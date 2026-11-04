# RawBlock Design System Specification

## Overview

**RawBlock** is an unapologetic, anti-design system that strips user interfaces down to their structural bones. Originally formulated for avant-garde art platforms and technical tools, it embraces thick borders, high-contrast monochrome geometry, and systemic minimalism.

There are no rounded corners, no soft drop shadows, and no decorative animations. Every element looks as though it was assembled directly from raw layout primitives.

In **Aukronome**, RawBlock ensures maximum readability, instant visual feedback from across the room, and absolute clarity during intense music practice.

---

## 1. Palette & Color Tokens

RawBlock strictly enforces a high-contrast palette. Accent colors are reserved solely for state indication and functional feedback:

| Token | Hex | Role / Semantic Meaning |
|---|---|---|
| **Black** | `#000000` | Text, primary structural borders, active button fills, inverted state fills |
| **White** | `#FFFFFF` | Background, inverted text, card surface base |
| **Blue** | `#0000FF` | Interactive links and secondary highlights only |
| **Success** | `#008000` | Confirmations, success states, active practice indicators |
| **Warning** | `#FFA500` | High-importance accents and prompt indicators |
| **Error / Stop** | `#FF0000` | Active playback state (STOP button) and validation errors |

---

## 2. Typography

Typography in RawBlock is divided into three distinct typefaces serving specific structural roles:

- **Headline Font (`Archivo Black`):** Ultra-heavy geometric display type for high visual impact numbers and headers.
- **Body Font (`Work Sans`):** Clean, legible grotesque sans-serif for labels, body copy, and UI descriptions.
- **Mono Font (`Space Mono`):** Monospaced technical font for numeric readouts, time signatures, sample offsets, and tooltips.

### Type Scale

| Style | Typeface | Size | Weight | Line Height | Usage |
|---|---|---|---|---|---|
| **h1** | Archivo Black | 64px | Regular | 1.0 | Primary tempo readout (BPM numbers) |
| **h2** | Archivo Black | 48px | Regular | 1.05 | Major section titles |
| **h3** | Archivo Black | 32px | Regular | 1.1 | Card headers, primary action buttons |
| **h4** | Work Sans | 22px | Semibold | 1.2 | Subsection headers |
| **body** | Work Sans | 16px | Regular | 1.6 | General descriptions, options |
| **small** | Work Sans | 14px | Regular | 1.5 | Secondary annotations |
| **tiny** | Work Sans | 12px | Regular | 1.4 | Status bar text, micro labels |
| **mono** | Space Mono | 15px | Regular | 1.5 | Numeric inputs, time signatures |

---

## 3. Structural Rules & Geometry

### Zero Border Radius (`radius: 0px`)
- **No rounded corners under any circumstances.**
- Every button, card, textbox, slider thumb, and popup dialog uses a strict `CornerRadius="0"`.
- Sharp, architectural edges represent structural honesty.

### Heavy Borders
- Borders replace drop shadows and elevation entirely.
- **Standard Border:** `3px solid #000000`
- **Emphasized / Focused Border:** `5px solid #000000`
- **Muted / Inactive Border:** `2px dashed #CCCCCC`

### Spacing Scale
The base unit is 8px. In RawBlock, intentional spacing irregularity is utilized to create visual tension and hierarchy:
- `sp-1`: 4px
- `sp-2`: 8px
- `sp-3`: 16px
- `sp-4`: 24px
- `sp-5`: 40px
- `sp-6`: 64px

---

## 4. Component Implementations in Aukronome

### Buttons
- **Default State:** Transparent or white background, 3px solid black border, black uppercase text.
- **Hover State:** Instant inversion — black background, crisp white text.
- **Active / Pressed State:** 5px solid black border with inverted content.
- **Stop Button:** Inverts to solid Red (`#FF0000`) with bold white text to signal active playback.

### Beat Blocks (Accent Editor)
- Square blocks with high visual prominence:
  - **`ACC` (Accent):** 5px solid black border with heavy uppercase label.
  - **`NRM` (Normal):** 3px solid black border with standard uppercase label.
  - **`MUTE` (Muted):** 2px dashed border with muted gray text.
- **Flash State:** Inverts instantly to solid black on downbeats, providing unmistakable visual pulse even with system audio muted.

### Controls & Inputs
- **Numeric Text Boxes:** White background, 3px black border, monospaced text, zero rounding.
- **Sliders:** High-contrast solid black track with square, non-rounded thumb slider.
- **Dropdowns / Combos:** Rectangular boxes with heavy black arrow indicators.

---

## 5. Do's and Don'ts

1. **Do** use heavy borders (`3px`–`5px`) as the primary organizer rather than drop shadows.
2. **Don't** round any corners—ever. Sharp edges are non-negotiable.
3. **Do** use full color inversions (black to white, white to black) for interactive hover states.
4. **Don't** use opacity fades for disabled states; use lighter border colors (`#CCCCCC`) and solid fills instead.
5. **Do** use `Archivo Black` at large sizes (`48px`–`64px`) for core numerical information.
6. **Don't** add decorative illustrations or skeumorphic gradients; content and typography carry the UI.
