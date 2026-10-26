using ChordSearch.Core.Music;
using ChordSearch.Core.Results;
using ChordSearch.Core.Tuning;

namespace ChordSearch.Midi;

/// <summary>No-op MIDI player used when device init fails.</summary>
public sealed class NullMidiPlayer : IMidiPlayer
{
    public bool IsAvailable => false;
    public string? LastError => null;
    public IReadOnlyList<MidiDeviceInfo> Devices { get; } = [];
    public int SelectedDeviceIndex { get; set; } = -1;
    public int Velocity { get; set; } = 90;
    public int GuitarProgram { get; set; } = 25;

    public Task PlayChordAsync(VoicingResult voicing, GuitarTuning tuning, int strumDelayMs = 25, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task PlayArpeggioAsync(VoicingResult voicing, GuitarTuning tuning, int noteMs = 180, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task PlayPitchAsync(Pitch pitch, int durationMs = 400, CancellationToken ct = default)
        => Task.CompletedTask;

    public void Stop() { }
    public void Dispose() { }
}
