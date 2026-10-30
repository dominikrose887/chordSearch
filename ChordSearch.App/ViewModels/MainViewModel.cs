using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChordSearch.App.Diagnostics;
using ChordSearch.Core.Chords;
using ChordSearch.Core.Music;
using ChordSearch.Core.Results;
using ChordSearch.Core.Services;
using ChordSearch.Core.Settings;
using ChordSearch.Core.Tuning;
using ChordSearch.Core.Voicing;
using ChordSearch.Midi;
using Microsoft.UI.Dispatching;

namespace ChordSearch.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ChordSearchService _searchService;
    private readonly TuningStore _tuningStore = new();
    private readonly AppSettingsStore _settingsStore = new();
    private readonly IMidiPlayer _midi;
    private CancellationTokenSource? _searchCts;
    private int _searchVersion;
    private bool _suppressTuningSearch;
    private bool _suppressSettingsPersist;
    private bool _restoringTuning;
    private DispatcherQueue? _dispatcher;

    public MainViewModel(IMidiPlayer? midi = null)
    {
        AppLog.Info("MainViewModel ctor: start");
        _settingsStore.Load();
        _searchService = new ChordSearchService(new VoicingOptions
        {
            MaxFret = _settingsStore.Current.MaxFrets
        });

        try
        {
            if (midi is not null)
            {
                _midi = midi;
            }
            else
            {
                AppLog.Info("MainViewModel ctor: creating NAudioMidiPlayer");
                _midi = new NAudioMidiPlayer();
                AppLog.Info($"MainViewModel ctor: MIDI available={_midi.IsAvailable}, devices={_midi.Devices.Count}");
            }
        }
        catch (Exception ex)
        {
            AppLog.Error("MIDI init failed — continuing without playback", ex);
            _midi = new NullMidiPlayer();
        }

        Roots =
        [
            new(PitchClass.C, "C"), new(PitchClass.Cs, "C#"), new(PitchClass.D, "D"),
            new(PitchClass.Ds, "D#"), new(PitchClass.E, "E"), new(PitchClass.F, "F"),
            new(PitchClass.Fs, "F#"), new(PitchClass.G, "G"), new(PitchClass.Gs, "G#"),
            new(PitchClass.A, "A"), new(PitchClass.As, "A#"), new(PitchClass.B, "B")
        ];
        Roots[0].IsSelected = true;

        Qualities =
        [
            new(ChordQuality.Major, "Major"),
            new(ChordQuality.Minor, "Minor"),
            new(ChordQuality.Diminished, "Dim"),
            new(ChordQuality.Augmented, "Aug"),
            new(ChordQuality.Sus2, "Sus2"),
            new(ChordQuality.Sus4, "Sus4"),
            new(ChordQuality.Power, "5")
        ];
        Qualities[0].IsSelected = true;

        Extensions =
        [
            new(ChordExtension.Sixth, "6"),
            new(ChordExtension.Seventh, "7"),
            new(ChordExtension.MajorSeventh, "maj7"),
            new(ChordExtension.Ninth, "9"),
            new(ChordExtension.Eleventh, "11"),
            new(ChordExtension.Thirteenth, "13"),
            new(ChordExtension.Add9, "add9"),
            new(ChordExtension.Flat5, "b5"),
            new(ChordExtension.Sharp5, "#5"),
            new(ChordExtension.Flat9, "b9"),
            new(ChordExtension.Sharp9, "#9"),
            new(ChordExtension.Sharp11, "#11"),
            new(ChordExtension.Flat13, "b13")
        ];

        BassOptions = [];
        // Populated after chips exist via RebuildBassOptions()

        PitchClassChoices = Enum.GetValues<PitchClass>().ToList();
        OctaveChoices = Enumerable.Range(0, 7).ToList();

        FretCountChoices = FretCountPresets.All
            .Select(p => new FretCountChoice(p.Frets, p.Label))
            .ToList();

        try
        {
            _suppressSettingsPersist = true;
            MaxFrets = _settingsStore.Current.MaxFrets;
            ScaleLengthMm = _settingsStore.Current.ScaleLengthMm;
            SelectedFretCount = FretCountChoices.FirstOrDefault(c => c.Frets == MaxFrets)
                               ?? FretCountChoices.Last();
            _suppressSettingsPersist = false;

            AppLog.Info($"MainViewModel ctor: settings MaxFrets={MaxFrets}, scale={ScaleLengthMm}mm");
            if (!string.IsNullOrEmpty(_settingsStore.LastLoadWarning))
                AppLog.Warn(_settingsStore.LastLoadWarning);

            AppLog.Info("MainViewModel ctor: loading tunings");
            _tuningStore.Load();
            _suppressTuningSearch = true;
            ActiveTuning = _tuningStore.ActiveTuning;
            _suppressTuningSearch = false;
            AvailableTunings = new ObservableCollection<GuitarTuning>(_tuningStore.AllTunings.ToList());
            if (AvailableTunings.All(t => t.Id != ActiveTuning.Id))
                AvailableTunings.Insert(0, ActiveTuning);

            _suppressSettingsPersist = true;
            Velocity = Math.Clamp(_settingsStore.Current.MidiVelocity, 1, 127);
            _midi.Velocity = Velocity;
            RefreshMidiDevices();
            RestoreMidiDeviceFromSettings();
            _suppressSettingsPersist = false;

            ReloadTuningEditorRows();
            RebuildBassOptions();
            AppLog.Info("MainViewModel ctor: starting initial search");
            _ = RunSearchAsync();
            AppLog.Info("MainViewModel ctor: done");
        }
        catch (Exception ex)
        {
            AppLog.Fatal("MainViewModel ctor failed after MIDI", ex);
            throw;
        }
    }

    public void AttachDispatcher(DispatcherQueue dispatcher) => _dispatcher = dispatcher;

    public ObservableCollection<ChipItem> Roots { get; }
    public ObservableCollection<ChipItem> Qualities { get; }
    public ObservableCollection<ChipItem> Extensions { get; }
    public ObservableCollection<BassOption> BassOptions { get; }
    public ObservableCollection<VoicingResult> Results { get; } = [];
    public ObservableCollection<GuitarTuning> AvailableTunings { get; }
    public ObservableCollection<MidiDeviceInfo> MidiDevices { get; } = [];
    public ObservableCollection<TuningStringRow> TuningStrings { get; } = [];
    public List<PitchClass> PitchClassChoices { get; }
    public List<int> OctaveChoices { get; }
    public List<FretCountChoice> FretCountChoices { get; }

    private List<VoicingResult> _allResults = [];

    [ObservableProperty] public partial string ChordName { get; set; } = "C";
    [ObservableProperty] public partial string TheorySummary { get; set; } = "";
    [ObservableProperty] public partial string PitchClassText { get; set; } = "";
    [ObservableProperty] public partial string IntervalText { get; set; } = "";
    [ObservableProperty] public partial VoicingResult? SelectedResult { get; set; }
    [ObservableProperty] public partial GuitarTuning ActiveTuning { get; set; } = null!;
    [ObservableProperty] public partial bool IsSearching { get; set; }
    [ObservableProperty] public partial string StatusText { get; set; } = "";
    [ObservableProperty] public partial bool MidiAvailable { get; set; }
    [ObservableProperty] public partial string MidiStatusText { get; set; } = "MIDI: not initialized";
    [ObservableProperty] public partial MidiDeviceInfo? SelectedMidiDevice { get; set; }
    [ObservableProperty] public partial int Velocity { get; set; } = 90;
    [ObservableProperty] public partial int StringCount { get; set; } = 6;
    [ObservableProperty] public partial string TuningName { get; set; } = "Standard E";
    [ObservableProperty] public partial int MaxFrets { get; set; } = 24;
    [ObservableProperty] public partial double ScaleLengthMm { get; set; } = 648;
    [ObservableProperty] public partial FretCountChoice? SelectedFretCount { get; set; }

    [ObservableProperty] public partial bool FilterOpenPosition { get; set; }
    [ObservableProperty] public partial bool FilterNoBarre { get; set; }
    [ObservableProperty] public partial bool FilterMaxThreeFingers { get; set; }

    [ObservableProperty] public partial bool ShowAllChordTones { get; set; }
    [ObservableProperty] public partial PitchClass ChordRoot { get; set; } = PitchClass.C;
    [ObservableProperty] public partial IReadOnlyList<PitchClass> ChordTones { get; set; } = [];

    partial void OnSelectedMidiDeviceChanged(MidiDeviceInfo? value)
    {
        try
        {
            if (value is not null)
                _midi.SelectedDeviceIndex = value.Index;
            UpdateMidiStatus();
            if (!_midi.IsAvailable && !string.IsNullOrEmpty(_midi.LastError))
                AppLog.Error("MIDI device open failed: " + _midi.LastError);
            PersistMidiSettings();
        }
        catch (Exception ex)
        {
            AppLog.Error("MIDI device change failed", ex);
            MidiAvailable = false;
            MidiStatusText = "MIDI: error — " + ex.Message;
            StatusText = "MIDI device failed";
        }
    }

    private void UpdateMidiStatus()
    {
        MidiAvailable = _midi.IsAvailable;
        if (MidiAvailable)
        {
            MidiStatusText =
                $"MIDI ready — {_midi.Devices.FirstOrDefault(d => d.Index == _midi.SelectedDeviceIndex)?.Name ?? "device"}";
            return;
        }

        if (!string.IsNullOrEmpty(_midi.LastError))
        {
            AppLog.Warn("MIDI: " + _midi.LastError);
            MidiStatusText = "MIDI failed — " + _midi.LastError;
            StatusText = "MIDI device failed";
        }
        else
        {
            MidiStatusText = "MIDI unavailable (check Settings → Device)";
        }
    }

    partial void OnVelocityChanged(int value)
    {
        var clamped = Math.Clamp(value, 1, 127);
        _midi.Velocity = clamped;
        if (!_suppressSettingsPersist)
            PersistMidiSettings();
    }

    private void RestoreMidiDeviceFromSettings()
    {
        var saved = _settingsStore.Current.MidiDeviceIndex;
        if (saved < 0 || MidiDevices.Count == 0) return;
        var match = MidiDevices.FirstOrDefault(d => d.Index == saved);
        if (match is not null)
            SelectedMidiDevice = match;
    }

    private void PersistMidiSettings()
    {
        if (_suppressSettingsPersist) return;
        try
        {
            _settingsStore.Current.MidiDeviceIndex = SelectedMidiDevice?.Index ?? _midi.SelectedDeviceIndex;
            _settingsStore.Current.MidiVelocity = Math.Clamp(Velocity, 1, 127);
            _settingsStore.Save();
        }
        catch (Exception ex)
        {
            AppLog.Error("MIDI settings save failed", ex);
        }
    }

    partial void OnSelectedFretCountChanged(FretCountChoice? value)
    {
        if (value is null || _suppressSettingsPersist) return;
        MaxFrets = value.Frets;
    }

    partial void OnMaxFretsChanged(int value)
    {
        var clamped = Math.Clamp(value, 12, 24);
        if (clamped != value)
        {
            MaxFrets = clamped;
            return;
        }

        AppLog.Info($"MaxFrets -> {clamped}");
        _searchService.SetMaxFret(clamped);
        if (!_suppressSettingsPersist)
        {
            _settingsStore.Current.MaxFrets = clamped;
            try { _settingsStore.Save(); }
            catch (Exception ex) { AppLog.Error("Settings save failed", ex); }

            _suppressSettingsPersist = true;
            SelectedFretCount = FretCountChoices.FirstOrDefault(c => c.Frets == clamped)
                               ?? FretCountChoices.Last();
            _suppressSettingsPersist = false;

            _ = RunSearchAsync();
        }
    }

    partial void OnScaleLengthMmChanged(double value)
    {
        if (_suppressSettingsPersist) return;
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            ScaleLengthMm = 648;
            return;
        }

        var clamped = value is < 500 or > 700 ? 648.0 : value;
        if (Math.Abs(clamped - value) > 0.01)
        {
            ScaleLengthMm = clamped;
            return;
        }

        _settingsStore.Current.ScaleLengthMm = clamped;
        try { _settingsStore.Save(); }
        catch (Exception ex) { AppLog.Error("Settings save failed", ex); }
    }

    partial void OnActiveTuningChanged(GuitarTuning value)
    {
        // WinUI ComboBox TwoWay clears SelectedItem (null) when the ItemsSource
        // collection replaces an item by reference — never leave ActiveTuning null.
        if (value is null)
        {
            if (_restoringTuning) return;
            RestoreActiveTuningSelection();
            return;
        }

        AppLog.Info($"ActiveTuning changed -> {value.Name} ({value.StringCount} strings)");
        StringCount = value.StringCount;
        TuningName = value.Name;
        ReloadTuningEditorRows();
        _tuningStore.SetActive(value);
        try { _tuningStore.SaveActive(); }
        catch (Exception ex) { AppLog.Error("SaveActive failed", ex); }
        if (!_suppressTuningSearch)
            _ = RunSearchAsync();
    }

    private void RestoreActiveTuningSelection()
    {
        var preferredId = _tuningStore.ActiveTuning?.Id;
        var fallback = AvailableTunings.FirstOrDefault(t => preferredId is not null && t.Id == preferredId)
                       ?? AvailableTunings.FirstOrDefault()
                       ?? _tuningStore.ActiveTuning;
        if (fallback is null) return;

        _restoringTuning = true;
        _suppressTuningSearch = true;
        try
        {
            ActiveTuning = fallback;
            AppLog.Warn($"ActiveTuning was cleared by UI binding — restored '{fallback.Name}'");
        }
        finally
        {
            _suppressTuningSearch = false;
            _restoringTuning = false;
        }
    }

    [RelayCommand]
    private void SelectRoot(ChipItem item)
    {
        foreach (var r in Roots) r.IsSelected = false;
        item.IsSelected = true;
        RebuildBassOptions();
        _ = RunSearchAsync();
    }

    [RelayCommand]
    private void SelectQuality(ChipItem item)
    {
        foreach (var q in Qualities) q.IsSelected = false;
        item.IsSelected = true;
        RebuildBassOptions();
        _ = RunSearchAsync();
    }

    [RelayCommand]
    private void ToggleExtension(ChipItem item)
    {
        item.IsSelected = !item.IsSelected;
        // Re-sync extension chips after conflict resolution (e.g. 7 vs maj7)
        var resolved = BuildSpecCore(includeBass: false)
            .WithExtensions(Extensions.Where(e => e.IsSelected).Select(e => e.As<ChordExtension>()));
        foreach (var ext in Extensions)
            ext.IsSelected = resolved.Extensions.Contains((ChordExtension)ext.Value);
        RebuildBassOptions();
        _ = RunSearchAsync();
    }

    [RelayCommand]
    private void SelectBass(BassOption item)
    {
        foreach (var b in BassOptions) b.IsSelected = false;
        item.IsSelected = true;
        _ = RunSearchAsync();
    }

    [RelayCommand]
    private async Task PlayChordAsync()
    {
        if (SelectedResult is null)
        {
            AppLog.Warn("PlayChord: no selection");
            return;
        }

        if (!_midi.IsAvailable)
        {
            AppLog.Warn("PlayChord: MIDI not available");
            StatusText = "MIDI not available — open Settings";
            return;
        }

        if (ActiveTuning is null)
        {
            RestoreActiveTuningSelection();
            if (ActiveTuning is null)
            {
                StatusText = "No tuning selected";
                return;
            }
        }

        try
        {
            AppLog.Info($"PlayChord: {ChordName} shape={SelectedResult.Voicing.ShapeKey}");
            await _midi.PlayChordAsync(SelectedResult, ActiveTuning);
            StatusText = $"{Results.Count} voicings";
        }
        catch (Exception ex)
        {
            AppLog.Error("PlayChord failed", ex);
            StatusText = "Playback error: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task PlayArpeggioAsync()
    {
        if (SelectedResult is null || !_midi.IsAvailable) return;
        if (ActiveTuning is null)
        {
            RestoreActiveTuningSelection();
            if (ActiveTuning is null) return;
        }

        try
        {
            AppLog.Info($"PlayArpeggio: {ChordName}");
            await _midi.PlayArpeggioAsync(SelectedResult, ActiveTuning);
        }
        catch (Exception ex)
        {
            AppLog.Error("PlayArpeggio failed", ex);
            StatusText = "Playback error: " + ex.Message;
        }
    }

    public async Task PlayPitchAsync(Pitch pitch)
    {
        if (!_midi.IsAvailable)
        {
            AppLog.Warn("PlayPitch: MIDI not available");
            return;
        }

        try
        {
            AppLog.Info($"PlayPitch: {pitch}");
            await _midi.PlayPitchAsync(pitch);
        }
        catch (Exception ex)
        {
            AppLog.Error("PlayPitch failed", ex);
            throw;
        }
    }

    [RelayCommand]
    private void StopMidi() => _midi.Stop();

    [RelayCommand]
    private void RefreshMidiDevices()
    {
        if (_midi is NAudioMidiPlayer na)
            na.RefreshDevices();

        MidiDevices.Clear();
        foreach (var d in _midi.Devices)
            MidiDevices.Add(d);

        SelectedMidiDevice = MidiDevices.FirstOrDefault(d => d.Index == _midi.SelectedDeviceIndex)
                             ?? MidiDevices.FirstOrDefault();
        UpdateMidiStatus();
    }

    [RelayCommand]
    private void ApplyStringCount()
    {
        var count = Math.Clamp(StringCount, 4, 8);
        StringCount = count;
        if (ActiveTuning is null || ActiveTuning.StringCount == count)
            return;

        // Snap to the usual standard tuning for 4 / 6 / 7 strings.
        GuitarTuning? standard = count switch
        {
            4 => TuningPresets.BassStandard,
            6 => TuningPresets.StandardE,
            7 => TuningPresets.SevenStringStandard,
            _ => null
        };

        if (standard is not null)
        {
            var existing = AvailableTunings.FirstOrDefault(t => t.Id == standard.Id);
            if (existing is null)
            {
                existing = standard.Copy();
                AvailableTunings.Insert(0, existing);
            }

            ActiveTuning = existing;
            return;
        }

        // 5 / 8 strings: keep pitch content, pad or trim from the low end.
        var pitches = ActiveTuning.OpenStrings.ToList();
        var fill = pitches.Count > 0 ? pitches[0] : Pitch.FromNote(PitchClass.E, 2);
        while (pitches.Count < count)
            pitches.Insert(0, fill);
        while (pitches.Count > count)
            pitches.RemoveAt(0);
        var updated = new GuitarTuning(ActiveTuning.Name, pitches, ActiveTuning.Id);
        ReplaceTuningInList(updated);
        ActiveTuning = updated;
    }

    [RelayCommand]
    private void SelectStandardStringCount(object? parameter)
    {
        var count = parameter switch
        {
            int i => i,
            string s when int.TryParse(s, out var n) => n,
            _ => 6
        };
        StringCount = Math.Clamp(count, 4, 8);
        ApplyStringCount();
    }

    [RelayCommand]
    private void ApplyTuningFromEditor()
    {
        try
        {
            if (ActiveTuning is null)
            {
                RestoreActiveTuningSelection();
                if (ActiveTuning is null)
                    throw new InvalidOperationException("No active tuning.");
            }

            var name = string.IsNullOrWhiteSpace(TuningName) ? ActiveTuning.Name : TuningName.Trim();
            var pitches = TuningStrings.Select(row => Pitch.FromNote(row.PitchClass, row.Octave)).ToArray();
            if (pitches.Length is < 4 or > 8)
                throw new InvalidOperationException("Tuning must have 4–8 strings.");

            var updated = new GuitarTuning(name, pitches, ActiveTuning.Id);
            // Update list first, then select — avoids ComboBox clearing SelectedItem to null.
            ReplaceTuningInList(updated);
            _suppressTuningSearch = true;
            ActiveTuning = updated;
            _suppressTuningSearch = false;
            _tuningStore.SetActive(updated);
            _tuningStore.SaveActive();
            _ = RunSearchAsync();
        }
        catch (Exception ex)
        {
            AppLog.Error("ApplyTuningFromEditor failed", ex);
            StatusText = "Could not apply tuning: " + ex.Message;
        }
    }

    [RelayCommand]
    private void SaveCurrentTuning()
    {
        try
        {
            ApplyTuningFromEditor();
            if (ActiveTuning is null)
                throw new InvalidOperationException("No active tuning to save.");

            var saved = ActiveTuning.Clone(ActiveTuning.Name);
            _tuningStore.SaveUserTuning(saved);
            if (AvailableTunings.All(t => t.Id != saved.Id))
                AvailableTunings.Add(saved);
            // Select the newly saved copy so ComboBox has a matching reference
            _suppressTuningSearch = true;
            ActiveTuning = saved;
            _suppressTuningSearch = false;
            StatusText = $"Saved tuning '{saved.Name}'.";
        }
        catch (Exception ex)
        {
            AppLog.Error("SaveCurrentTuning failed", ex);
            StatusText = "Could not save tuning: " + ex.Message;
        }
    }

    [RelayCommand]
    private void TransposeTuningUp() => TransposeTuning(1);

    [RelayCommand]
    private void TransposeTuningDown() => TransposeTuning(-1);

    private void TransposeTuning(int semitones)
    {
        try
        {
            // Build a new instance — never mutate ActiveTuning / presets in place
            Pitch[] pitches;
            if (TuningStrings.Count == ActiveTuning.StringCount)
            {
                pitches = TuningStrings
                    .Select(row => Pitch.FromNote(row.PitchClass, row.Octave).Transpose(semitones))
                    .ToArray();
            }
            else
            {
                pitches = ActiveTuning.OpenStrings.Select(p => p.Transpose(semitones)).ToArray();
            }

            var updated = new GuitarTuning(ActiveTuning.Name, pitches, ActiveTuning.Id);
            ReplaceTuningInList(updated);
            ActiveTuning = updated;
            StatusText = semitones > 0 ? "Tuning +1 semitone" : "Tuning −1 semitone";
            AppLog.Info($"TransposeTuning {semitones:+0;-0}");
        }
        catch (Exception ex)
        {
            AppLog.Error("TransposeTuning failed", ex);
            StatusText = "Transpose failed: " + ex.Message;
        }
    }

    private void ReplaceTuningInList(GuitarTuning updated)
    {
        var idx = AvailableTunings.ToList().FindIndex(t => t.Id == updated.Id);
        if (idx >= 0)
            AvailableTunings[idx] = updated;
        else
            AvailableTunings.Insert(0, updated);
    }

    public ChordSpec BuildSpec() => BuildSpecCore(includeBass: true);

    private ChordSpec BuildSpecCore(bool includeBass)
    {
        var root = Roots.First(r => r.IsSelected).As<PitchClass>();
        var quality = Qualities.First(q => q.IsSelected).As<ChordQuality>();
        var exts = Extensions.Where(e => e.IsSelected).Select(e => e.As<ChordExtension>()).ToArray();
        PitchClass? bass = null;
        if (includeBass && BassOptions.Count > 0)
            bass = BassOptions.FirstOrDefault(b => b.IsSelected)?.PitchClass;

        return new ChordSpec
        {
            Root = root,
            Quality = quality,
            Bass = bass
        }.WithExtensions(exts);
    }

    private async Task RunSearchAsync()
    {
        var version = Interlocked.Increment(ref _searchVersion);
        var previousCts = _searchCts;
        _searchCts = new CancellationTokenSource();
        var ct = _searchCts.Token;
        try { previousCts?.Cancel(); } catch (ObjectDisposedException) { /* ignore */ }
        previousCts?.Dispose();

        ChordSpec spec;
        try { spec = BuildSpec(); }
        catch (Exception ex)
        {
            AppLog.Error("BuildSpec failed", ex);
            StatusText = "Builder error";
            return;
        }

        if (ActiveTuning is null)
        {
            AppLog.Warn("Search skipped: no active tuning");
            return;
        }

        AppLog.Info($"Search begin: {spec.DisplayName} v{version} (maxFret={MaxFrets})");
        ChordName = spec.DisplayName;
        IsSearching = true;
        StatusText = "Searching…";

        try
        {
            var tuning = ActiveTuning.Copy();
            var response = await Task.Run(() =>
            {
                AppLog.Info($"Search worker: start {spec.DisplayName}");
                var r = _searchService.Search(spec, tuning, ct);
                AppLog.Info($"Search worker: done, {r.Results.Count} results");
                return r;
            }, ct).ConfigureAwait(false);

            if (version != _searchVersion || ct.IsCancellationRequested)
            {
                AppLog.Info($"Search superseded v{version}");
                return;
            }

            await RunOnUiAsync(() =>
            {
                TheorySummary = response.TheorySummary;
                PitchClassText = string.Join(" · ", response.PitchClasses);
                IntervalText = string.Join(", ", response.Intervals.Select(i => i.ToString()));
                ChordRoot = response.Formula.Root;
                ChordTones = response.Formula.PitchClasses.OrderBy(pc => ((int)pc - (int)response.Formula.Root + 12) % 12).ToArray();

                _allResults = response.Results.ToList();
                ApplyVoicingFilters();
                IsSearching = false;
                AppLog.Info($"Search UI updated: {StatusText}");
            });
        }
        catch (OperationCanceledException)
        {
            AppLog.Info($"Search canceled v{version}");
            if (version == _searchVersion)
            {
                await RunOnUiAsync(() =>
                {
                    IsSearching = false;
                    if (StatusText.StartsWith("Searching", StringComparison.Ordinal))
                        StatusText = Results.Count == 0 ? "Search canceled." : $"{Results.Count} voicings";
                });
            }
        }
        catch (Exception ex)
        {
            AppLog.Error("Search failed", ex);
            await RunOnUiAsync(() =>
            {
                StatusText = "Search error: " + ex.Message;
                IsSearching = false;
            });
        }
    }

    private Task RunOnUiAsync(Action action)
    {
        var dq = _dispatcher ?? DispatcherQueue.GetForCurrentThread();
        if (dq is null || dq.HasThreadAccess)
        {
            action();
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!dq.TryEnqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }))
        {
            // Never mutate ObservableCollections off the UI thread.
            AppLog.Warn("DispatcherQueue.TryEnqueue failed — skipping UI update");
            tcs.TrySetException(new InvalidOperationException("UI dispatcher unavailable"));
        }

        return tcs.Task;
    }

    partial void OnFilterOpenPositionChanged(bool value) => ApplyVoicingFilters();
    partial void OnFilterNoBarreChanged(bool value) => ApplyVoicingFilters();
    partial void OnFilterMaxThreeFingersChanged(bool value) => ApplyVoicingFilters();

    private void ApplyVoicingFilters()
    {
        IEnumerable<VoicingResult> q = _allResults;

        if (FilterOpenPosition)
            q = q.Where(r => r.IsOpenPosition);
        if (FilterNoBarre)
            q = q.Where(r => !r.HasBarre);
        if (FilterMaxThreeFingers)
            q = q.Where(r => r.FingerCount <= 3);

        // Rank order (easy → harder) already applied by search
        var filtered = q.ToList();
        var keep = SelectedResult;

        Results.Clear();
        foreach (var r in filtered)
            Results.Add(r);

        SelectedResult = keep is not null && Results.Contains(keep)
            ? keep
            : Results.FirstOrDefault();

        StatusText = _allResults.Count == 0
            ? "No playable voicings found."
            : filtered.Count == _allResults.Count
                ? $"{filtered.Count} voicings"
                : $"{filtered.Count} of {_allResults.Count} voicings";
    }

    /// <summary>
    /// Bass choices: Any bass (no hard filter) + chord tones as required low note.
    /// </summary>
    private void RebuildBassOptions()
    {
        var previous = BassOptions.FirstOrDefault(b => b.IsSelected)?.PitchClass;
        var spec = BuildSpecCore(includeBass: false);
        var tones = ChordBassOptions.ValidBassPitchClasses(spec);

        BassOptions.Clear();
        var anyOpt = new BassOption(null, "Any bass");
        BassOptions.Add(anyOpt);

        foreach (var pc in tones)
        {
            var label = pc == spec.Root
                ? "Root low"
                : PitchClassNames.ToDisplayName(pc, spec.AccidentalPreference);
            BassOptions.Add(new BassOption(pc, label));
        }

        if (previous is not null && tones.Contains(previous.Value))
        {
            BassOptions.First(b => b.PitchClass == previous).IsSelected = true;
        }
        else
        {
            if (previous is not null)
                AppLog.Info($"Bass {previous} dropped — not a tone of {spec.DisplayName}");
            anyOpt.IsSelected = true;
        }
    }

    private void ReloadTuningEditorRows()
    {
        TuningStrings.Clear();
        for (var i = 0; i < ActiveTuning.StringCount; i++)
        {
            var p = ActiveTuning.OpenPitch(i);
            TuningStrings.Add(new TuningStringRow
            {
                StringNumber = i + 1,
                PitchClass = p.PitchClass,
                Octave = p.Octave,
                Display = p.ToDisplayName()
            });
        }
    }

    public void DisposeMidi() => _midi.Dispose();
}

public partial class BassOption : ObservableObject
{
    public BassOption(PitchClass? pitchClass, string label)
    {
        PitchClass = pitchClass;
        Label = label;
    }

    public PitchClass? PitchClass { get; }

    [ObservableProperty] public partial string Label { get; set; } = "";
    [ObservableProperty] public partial bool IsSelected { get; set; }
}

public sealed class FretCountChoice(int frets, string label)
{
    public int Frets { get; } = frets;
    public string Label { get; } = label;
    public override string ToString() => Label;
}

public partial class TuningStringRow : ObservableObject
{
    public IReadOnlyList<PitchClass> PitchClassOptions { get; } = Enum.GetValues<PitchClass>();

    [ObservableProperty] public partial int StringNumber { get; set; }
    [ObservableProperty] public partial PitchClass PitchClass { get; set; }
    [ObservableProperty] public partial int Octave { get; set; }
    [ObservableProperty] public partial string Display { get; set; } = "";

    public double OctaveValue
    {
        get => Octave;
        set => Octave = (int)Math.Round(value);
    }

    partial void OnPitchClassChanged(PitchClass value) => RefreshDisplay();
    partial void OnOctaveChanged(int value)
    {
        OnPropertyChanged(nameof(OctaveValue));
        RefreshDisplay();
    }

    private void RefreshDisplay()
        => Display = Pitch.FromNote(PitchClass, Octave).ToDisplayName();
}
