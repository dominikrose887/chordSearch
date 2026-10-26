using ChordSearch.Core.Music;
using ChordSearch.Core.Results;
using ChordSearch.Core.Tuning;

namespace ChordSearch.Midi;

public interface IMidiPlayer : IDisposable
{
    bool IsAvailable { get; }
    /// <summary>Last open/device error message, if any.</summary>
    string? LastError { get; }
    IReadOnlyList<MidiDeviceInfo> Devices { get; }
    int SelectedDeviceIndex { get; set; }
    int Velocity { get; set; }
    int GuitarProgram { get; set; }

    Task PlayChordAsync(VoicingResult voicing, GuitarTuning tuning, int strumDelayMs = 25, CancellationToken ct = default);
    Task PlayArpeggioAsync(VoicingResult voicing, GuitarTuning tuning, int noteMs = 180, CancellationToken ct = default);
    Task PlayPitchAsync(Pitch pitch, int durationMs = 400, CancellationToken ct = default);
    void Stop();
}

public sealed record MidiDeviceInfo(int Index, string Name);
