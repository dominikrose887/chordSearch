using ChordSearch.Core.Music;
using ChordSearch.Core.Results;
using ChordSearch.Core.Tuning;
using NAudio.Midi;

namespace ChordSearch.Midi;

public sealed class NAudioMidiPlayer : IMidiPlayer
{
    // NAudio MidiMessage APIs use 1–16 for channel (not 0–15).
    private const int MidiChannel = 1;

    private MidiOut? _out;
    private readonly object _gate = new();
    private CancellationTokenSource? _playCts;

    public NAudioMidiPlayer()
    {
        try
        {
            RefreshDevices();
            if (Devices.Count > 0)
                OpenDevice(0);
        }
        catch
        {
            _out?.Dispose();
            _out = null;
            Devices = [];
            _selectedDeviceIndex = -1;
            throw;
        }
    }

    public bool IsAvailable => _out is not null;

    public string? LastError { get; private set; }

    public IReadOnlyList<MidiDeviceInfo> Devices { get; private set; } = [];

    private int _selectedDeviceIndex = -1;
    public int SelectedDeviceIndex
    {
        get => _selectedDeviceIndex;
        set
        {
            if (value == _selectedDeviceIndex) return;
            OpenDevice(value);
        }
    }

    public int Velocity { get; set; } = 90;

    /// <summary>GM program: 24 = Nylon Guitar, 25 = Steel Guitar.</summary>
    public int GuitarProgram { get; set; } = 25;

    public void RefreshDevices()
    {
        var list = new List<MidiDeviceInfo>();
        for (var i = 0; i < MidiOut.NumberOfDevices; i++)
        {
            var caps = MidiOut.DeviceInfo(i);
            list.Add(new MidiDeviceInfo(i, caps.ProductName));
        }
        Devices = list;
    }

    private void OpenDevice(int index)
    {
        lock (_gate)
        {
            _out?.Dispose();
            _out = null;
            _selectedDeviceIndex = -1;
            LastError = null;

            if (index < 0 || index >= MidiOut.NumberOfDevices)
            {
                if (index >= 0)
                    LastError = $"MIDI device index {index} out of range";
                return;
            }

            try
            {
                _out = new MidiOut(index);
                _selectedDeviceIndex = index;
                _out.Send(MidiMessage.ChangePatch(GuitarProgram, MidiChannel).RawData);
            }
            catch (Exception ex)
            {
                LastError = $"OpenDevice({index}) failed: {ex.Message}";
                _out?.Dispose();
                _out = null;
                _selectedDeviceIndex = -1;
            }
        }
    }

    public async Task PlayChordAsync(VoicingResult voicing, GuitarTuning tuning, int strumDelayMs = 25, CancellationToken ct = default)
    {
        var pitches = GetSounding(voicing, tuning);
        if (pitches.Count == 0 || !IsAvailable) return;

        var token = BeginPlayback(ct);
        try
        {
            EnsurePatch();
            foreach (var pitch in pitches)
            {
                token.ThrowIfCancellationRequested();
                NoteOn(pitch.MidiNote);
                if (strumDelayMs > 0)
                    await Task.Delay(strumDelayMs, token);
            }

            await Task.Delay(900, token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            AllNotesOff();
        }
    }

    public async Task PlayArpeggioAsync(VoicingResult voicing, GuitarTuning tuning, int noteMs = 180, CancellationToken ct = default)
    {
        var pitches = GetSounding(voicing, tuning);
        if (pitches.Count == 0 || !IsAvailable) return;

        var token = BeginPlayback(ct);
        try
        {
            EnsurePatch();
            foreach (var pitch in pitches)
            {
                token.ThrowIfCancellationRequested();
                NoteOn(pitch.MidiNote);
                await Task.Delay(noteMs, token);
                NoteOff(pitch.MidiNote);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            AllNotesOff();
        }
    }

    public async Task PlayPitchAsync(Pitch pitch, int durationMs = 400, CancellationToken ct = default)
    {
        if (!IsAvailable) return;

        var token = BeginPlayback(ct);
        try
        {
            EnsurePatch();
            NoteOn(pitch.MidiNote);
            await Task.Delay(durationMs, token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            NoteOff(pitch.MidiNote);
        }
    }

    public void Stop()
    {
        CancelPlayback();
        AllNotesOff();
    }

    /// <summary>
    /// Cancel any in-flight playback and return a fresh token.
    /// Must NOT dispose the active CTS via `using` in callers — that broke subsequent plays.
    /// </summary>
    private CancellationToken BeginPlayback(CancellationToken external)
    {
        CancelPlayback();
        AllNotesOff();
        _playCts = CancellationTokenSource.CreateLinkedTokenSource(external);
        return _playCts.Token;
    }

    private void CancelPlayback()
    {
        var old = Interlocked.Exchange(ref _playCts, null);
        if (old is null) return;
        try { old.Cancel(); } catch (ObjectDisposedException) { }
        old.Dispose();
    }

    private static List<Pitch> GetSounding(VoicingResult voicing, GuitarTuning tuning)
        => voicing.SoundingPitches(tuning).Where(p => p is not null).Select(p => p!.Value).ToList();

    private void EnsurePatch()
    {
        lock (_gate)
        {
            _out?.Send(MidiMessage.ChangePatch(GuitarProgram, MidiChannel).RawData);
        }
    }

    private void NoteOn(int midi)
    {
        lock (_gate)
        {
            var v = Math.Clamp(Velocity, 1, 127);
            _out?.Send(MidiMessage.StartNote(Math.Clamp(midi, 0, 127), v, MidiChannel).RawData);
        }
    }

    private void NoteOff(int midi)
    {
        lock (_gate)
        {
            _out?.Send(MidiMessage.StopNote(Math.Clamp(midi, 0, 127), 0, MidiChannel).RawData);
        }
    }

    private void AllNotesOff()
    {
        lock (_gate)
        {
            if (_out is null) return;
            _out.Send(MidiMessage.ChangeControl(123, 0, MidiChannel).RawData);
            // Also silence via CC 120 (all sound off) for stubborn synths
            _out.Send(MidiMessage.ChangeControl(120, 0, MidiChannel).RawData);
        }
    }

    public void Dispose()
    {
        Stop();
        lock (_gate)
        {
            _out?.Dispose();
            _out = null;
        }
    }
}
