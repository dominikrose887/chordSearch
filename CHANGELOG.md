# Changelog

## 1.0.3

### Fixed
- Reject impossible multi-string finger / barre spans: if one finger frets several strings at the same fret, intermediate strings cannot sit closer to the nut (or be open/muted).
- Reject “sandwiched” frets: a note cannot sit behind higher frets on both the bass and treble sides (Twister / trapped finger). Fixes the reported C#dim6 voicing.

### Tests
- Added fingering engine cases for illegal barre spans, muted intermediates, and legal mini-barres / F-shape front-of-barre notes.

## 1.0.2

### Fixed
- Blank fretboard and “Playback error” after tuning changes (ComboBox cleared `ActiveTuning` to null).
- Added built-in Standard C preset.

### Added
- Inno Setup installer (`ChordSearch-Setup-*.exe`) with folder picker and uninstall.
- Portable zip with `portable.txt` (data under `.\Data`).

## 1.0.1

### Fixed
- Silent startup crash: publish package now includes `.pri` / `.xbf` XAML resources.

## 1.0.0

- Initial public release.
