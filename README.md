# Chord Search

WinUI 3 (.NET 10) desktop app for building guitar chords, finding playable voicings across the neck, viewing detailed fretboard fingerings, and previewing them over MIDI.

**Docs:** [DOCUMENTATION.md](./DOCUMENTATION.md) — features, stack, architecture, design decisions, install/release notes.

## Download

Get the latest **win-x64** zip from [GitHub Releases](https://github.com/dominikrose887/chordSearch/releases). Unzip and run `Install-ChordSearch.ps1`, or launch `ChordSearch.App.exe` directly.

## Solution

| Project | Role |
|---------|------|
| `ChordSearch.Core` | Pitch/tuning/chord theory, voicing + fingering engines |
| `ChordSearch.Midi` | NAudio MIDI playback |
| `ChordSearch.App` | WinUI 3 MVVM UI |
| `ChordSearch.Core.Tests` | Unit tests |

## Run (dev)

```powershell
dotnet test ChordSearch.Core.Tests
dotnet build ChordSearch.App/ChordSearch.App.csproj -c Debug -p:Platform=x64
dotnet run --project ChordSearch.App/ChordSearch.App.csproj -c Debug -p:Platform=x64
```

## Features

- **Chord builder**: Root → Quality → Extensions → Bass (no giant chord-name dropdown)
- **Voicing search**: full-neck windowed search with span/bass/coverage scoring
- **Fingering**: barre detection, stretch limits, adjacent-finger rules
- **Fretboard**: inlays, X/O, finger numbers, barre bars, note names
- **Tuning**: 4–8 strings, per-string pitch/octave, presets, JSON save under `%LocalAppData%\ChordSearch\tunings`
- **MIDI**: chord strum, arpeggio, device picker, velocity

## Publish (release zip)

```powershell
dotnet publish ChordSearch.App/ChordSearch.App.csproj -c Release -p:Platform=x64 -p:RuntimeIdentifier=win-x64 -p:SelfContained=true -p:Version=1.0.1
```

> **Note:** Unpackaged WinUI needs `EnableMsixTooling=true` plus `.pri`/`.xbf` in the publish folder. Without those, the exe exits immediately (`Cannot locate resource ms-appx:///MainWindow.xaml`).
