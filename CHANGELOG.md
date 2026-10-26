# Changelog

## 1.0.4

### Changed
- Replaced the rigid “sandwich gate” + scoring hacks with a real fingering playability search: assign fingers 1–4 by backtracking, hard-reject only when no legal assignment exists, then score difficulty.
- Position-dependent stretch: tighter near the nut (~3 frets adjacent), larger toward fret 12+ (~6).
- Barre clarity stays a hard constraint (no open/mute/lower fret under a multi-string finger).
- True twisters (e.g. illegal barre over a lower intermediate with no alternate fingering) still reject; playable “pocket” grips like Em7 (`x-7-5-7-0-x`) assign and rank.
- VoicingEngine wide-span barre requirement now applies only to crowded shapes (4+ fretted notes); sparse reaches defer to fingering.

### Tests
- Em7 pocket / wide-reach assignability and presence in search; illegal barre reject; open C / Em / F barre; position-stretch unit cases.

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
