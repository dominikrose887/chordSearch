# Chord Search — Documentation

Personal desktop tool for building guitar chords, finding playable voicings across the neck, inspecting fingerings on a fretboard, and previewing them over MIDI.

**Version:** 1.0.5  
**Platform:** Windows 10/11 (x64), unpackaged WinUI 3 app  
**Audience:** solo / personal use (not an enterprise-deployed product)

---

## Features

### Chord builder
- Compose chords as **Root → Quality → Extensions → Bass** (no giant chord-name dropdown).
- Qualities: major, minor, dim, aug, sus2, sus4, power (5).
- Extensions: 6, 7, maj7, 9, 11, 13, add9, b5/#5, b9/#9, #11, b13 — with conflict resolution (e.g. 7 vs maj7).
- Slash / alternate bass limited to **chord tones** (invalid bass rejected before search).

### Voicing search
- Sliding fret-window search across the neck (default max fret 12–24).
- Scores span, open strings, coverage, barre opportunity, root vs non-root bass.
- Filters: open position, no barre, max three fingers.
- Results ranked easiest → harder; cooperative **cancellation** when the chord changes quickly.

### Fingering
- Searches for a legal assignment of fingers 1–4 (open/muted strings use none).
- Hard rules: one finger one fret; multi-string same finger must clear the barre span; stretch limits grow with neck position.
- Does not reject a shape only because a lower fret sits between higher frets — it tries rearrangements (e.g. Em7 pocket grips). If no assignment works, the shape is dropped.
- Playable shapes get a difficulty score (span, finger count, barre width, stretch).

### Fretboard & diagrams
- Interactive fretboard: inlays, X/O, finger numbers, barre bars, note names.
- **Voicing only** mode: selected shape with finger-colored dots, barres, mute marks.
- **All chord tones** (map) mode: every chord tone across the neck, colored by degree (1 / b3 / 5 / b7 …); legend lists degrees instead of fingers.
- Grid of voicing diagrams for the current result set.
- Click a fretted note to hear that pitch (when MIDI is available).

### Tuning
- Top-bar preset picker plus quick **4 / 6 / 7** buttons for standard tunings (EADG, EADGBE, BEADGBE).
- 4–8 strings; per-string pitch class + octave in Settings.
- Built-in presets (Standard E, Standard C, Drop D, Open G/D, half-step down, bass, 7-string).
- Applying string count 4 / 6 / 7 selects that standard; 5 / 8 pad or trim the current pitches.
- User tunings saved as JSON; active tuning persisted across launches.
- Transpose ±1 semitone without mutating built-in presets (working copies only).

### MIDI
- Chord strum and arpeggio preview via NAudio `MidiOut`.
- Device picker, velocity, GM guitar program.
- Graceful fallback to a no-op player if MIDI init fails; errors surface in UI status + log.

### Resilience (personal-quality hardening)
- Local crash / exception logging under `%LocalAppData%\ChordSearch\logs` (rotated, keeps last 20 files).
- Settings recover from corrupt JSON and rewrite defaults.
- Atomic writes for settings and tunings (temp file + replace).

---

## Stack

| Layer | Choice | Notes |
|-------|--------|--------|
| UI | **WinUI 3** + Windows App SDK | Modern Windows desktop UI |
| Runtime | **.NET 10** (`net10.0-windows10.0.26100.0`) | Unpackaged (`WindowsPackageType=None`) |
| MVVM | **CommunityToolkit.Mvvm** | Source generators, relay commands |
| Domain | **ChordSearch.Core** (pure .NET) | No UI references — fully unit-testable |
| MIDI | **NAudio** 2.2.1 | `MidiOut` only |
| Tests | **xUnit** (`ChordSearch.Core.Tests`) | Domain coverage (~150 tests) |
| Packaging | Self-contained **win-x64** publish | No MSIX / Store requirement for v1 |

### Solution layout

```
ChordSearch.slnx
├── ChordSearch.Core          # Theory, voicing, fingering, settings, tuning I/O
├── ChordSearch.Midi          # IMidiPlayer + NAudio / Null implementations
├── ChordSearch.App           # WinUI shell, ViewModels, custom controls
└── ChordSearch.Core.Tests    # Unit tests for Core
```

---

## Code map

### Core (`ChordSearch.Core`)

| Area | Types | Role |
|------|-------|------|
| Music | `Pitch`, `PitchClass` | MIDI note + pitch-class helpers |
| Chords | `ChordSpec`, `ChordFormula`, `ChordNamer`, `ChordQuality`, bass helpers | Spec → intervals / tones / display name |
| Voicing | `VoicingEngine`, `VoicingOptions`, `StringFret` | Windowed enumeration + scoring |
| Fingering | `FingeringEngine` | Finger assignment / rejection |
| Services | `ChordSearchService` | Facade: options snapshot, search, ranking |
| Tuning | `GuitarTuning`, `TuningPresets`, `TuningStore` | Presets + user JSON store |
| Settings | `AppSettings`, `AppSettingsStore` | Max frets, scale length, MIDI prefs |
| I/O | `AtomicFile` | Crash-safe text writes |

**Search path:** `ChordSpec` → `ChordFormula` → `VoicingEngine.FindVoicings` → `FingeringEngine.Assign` → ranked `VoicingResult` list.  
`CancellationToken` is checked between fret windows and during fingering so UI cancels are cooperative.

### App (`ChordSearch.App`)

| Piece | Role |
|-------|------|
| `MainViewModel` | Builder state, search orchestration, tuning editor, MIDI commands |
| `FretboardControl` / `VoicingDiagramControl` | Canvas-drawn diagrams; fretboard supports voicing + degree map modes |
| `AppLog` | Session file logger + prune |
| `App` / `MainWindow` | Lifecycle, unhandled-exception hooks, MIDI dispose on close |

### Midi (`ChordSearch.Midi`)

- `IMidiPlayer` — `PlayChordAsync`, `PlayArpeggioAsync`, `PlayPitchAsync`, `LastError`.
- `NAudioMidiPlayer` — real devices; `NullMidiPlayer` — safe fallback.

---

## Design decisions

These are intentional product/engineering choices for a **personal** Windows tool — not enterprise packaging requirements.

1. **Unpackaged WinUI**  
   Runs without Developer Mode / sideload certificates. Publish is a self-contained folder + zip “installer”, not MSIX/Store.

2. **Chord builder instead of a name dictionary**  
   Composing from root/quality/extensions covers altered chords without maintaining a huge name table. Naming is derived (`ChordNamer`).

3. **Playability over completeness**  
   `FingeringEngine` is the authority for playability: it searches finger assignments and drops shapes with none. Crowded wide stretches without a low barre may still be rejected in voicing; sparse 2–3 note reaches defer to fingering. Non-contiguous mutes are rejected. Some awkward shapes may be omitted — preferred over listing unplayable junk.  
   **Barre span rule:** one finger on several strings at the same fret presses every string in between. Intermediate strings may not be open, muted, or fretted closer to the nut than the barre fret (notes in front of the barre are OK).  
   **Pocket / twister:** a lower fret between higher frets on both sides is allowed when a clean pocket fingering exists (index in the valley, outer fingers on the peaks). Extra intermediate frets that block every assignment still reject (true twister).

4. **Windowed search**  
   Sliding windows + per-window caps keep search latency acceptable on a full neck while still finding open and high positions.

5. **Core without UI / MIDI dependencies**  
   Domain logic stays testable and free of WinRT. MIDI and WinUI sit at the edges.

6. **Preset immutability via copies**  
   Built-in tunings are static definitions; UI always works on `Copy()` instances so transpose/edit cannot corrupt the catalog for the process lifetime.

7. **Local JSON persistence**  
   No cloud sync. Settings and tunings live under `%LocalAppData%\ChordSearch\`. Atomic writes reduce corruption if the process dies mid-save.

8. **Fail closed in Release for UI crashes**  
   Unhandled UI exceptions are logged and not swallowed in Release (Debug keeps the process alive for inspection).

9. **No DI container**  
   Construction is straightforward in `MainViewModel` / window code. Acceptable complexity for a single-window personal app.

10. **Trimming off for publish**  
    WinUI + NAudio are not trim-friendly; Release publish keeps `PublishTrimmed=false` and uses ReadyToRun.

---

## Data locations

| Data | Path |
|------|------|
| Settings | `%LocalAppData%\ChordSearch\settings.json` |
| Tunings | `%LocalAppData%\ChordSearch\tunings\*.json` (+ `_active.json`) |
| Logs | `%LocalAppData%\ChordSearch\logs\` (Debug may also write to repo `logs\`) |

---

## Build, test, run

```powershell
# Tests
dotnet test ChordSearch.Core.Tests

# Debug run (x64)
dotnet build ChordSearch.App/ChordSearch.App.csproj -c Debug -p:Platform=x64
dotnet run --project ChordSearch.App/ChordSearch.App.csproj -c Debug -p:Platform=x64
```

### Requirements (dev)
- Windows 10 1809+ / Windows 11  
- .NET 10 SDK  
- Windows App SDK workload / packages restored via NuGet  

### Requirements (end user, released zip)
- Windows 10/11 x64  
- Self-contained build embeds the runtime — no separate .NET install required  
- A MIDI output device (optional; app runs without sound if none)

---

## Release package

Build installer + portable zip (requires [Inno Setup 6](https://jrsoftware.org/isinfo.php)):

```powershell
powershell -ExecutionPolicy Bypass -File packaging\build-release.ps1 -Version 1.0.5
```

Artifacts under `artifacts\`:

| File | Purpose |
|------|---------|
| `ChordSearch-Setup-1.0.5.exe` | Wizard installer (choose folder, Start Menu, Add/Remove Programs uninstall) |
| `ChordSearch-1.0.5-win-x64-portable.zip` | No-install portable; includes `portable.txt` so data lives in `.\Data` |

### Installed vs portable data

| Mode | Settings / tunings / logs |
|------|---------------------------|
| Installer | `%LocalAppData%\ChordSearch\` |
| Portable (`portable.txt` present) | `{app}\Data\` |

### Uninstall (installer build)

Windows **Settings → Apps**, or the Start Menu **Uninstall Chord Search** entry.  
User data under LocalAppData is left in place (delete manually if desired).

---

## Known limitations

- English UI only; no accessibility automation pass on custom canvases.  
- No auto-update channel — install a newer release zip manually.  
- MIDI quality depends on the system / device synth (Microsoft GS Wavetable, etc.).  
- Fingering heuristics are opinionated; not every textbook shape will appear.  
- App / Midi layers are not covered by automated UI tests (Core is).

---

## License / third-party

- Application code: see repository (add a `LICENSE` if you publish publicly under a specific license).  
- **NAudio** — open-source (see NuGet / project license).  
- **CommunityToolkit.Mvvm**, **Windows App SDK** — per their respective licenses.
