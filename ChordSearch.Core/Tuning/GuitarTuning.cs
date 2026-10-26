using System.Text.Json.Serialization;
using ChordSearch.Core.Music;

namespace ChordSearch.Core.Tuning;

/// <summary>Open-string pitches from lowest (index 0) to highest.</summary>
public sealed class GuitarTuning
{
    public GuitarTuning(string name, IEnumerable<Pitch> openStrings, string? id = null)
    {
        Name = name;
        Id = id ?? Guid.NewGuid().ToString("N");
        OpenStrings = openStrings.ToArray();
        if (OpenStrings.Count is < 4 or > 8)
            throw new ArgumentOutOfRangeException(nameof(openStrings), "String count must be 4–8.");
    }

    public string Id { get; }

    public string Name { get; set; }

    public IReadOnlyList<Pitch> OpenStrings { get; private set; }

    [JsonIgnore]
    public int StringCount => OpenStrings.Count;

    public Pitch OpenPitch(int stringIndex) => OpenStrings[stringIndex];

    public Pitch PitchAt(int stringIndex, int fret)
    {
        if (fret < 0) throw new ArgumentOutOfRangeException(nameof(fret));
        return OpenStrings[stringIndex].Transpose(fret);
    }

    public void SetStringCount(int count, Pitch? fillWith = null)
    {
        if (count is < 4 or > 8)
            throw new ArgumentOutOfRangeException(nameof(count));

        var list = OpenStrings.ToList();
        var fill = fillWith ?? Pitch.FromNote(PitchClass.E, 2);
        while (list.Count < count)
            list.Insert(0, fill);
        while (list.Count > count)
            list.RemoveAt(0);
        OpenStrings = list;
    }

    public void SetOpenString(int stringIndex, Pitch pitch)
    {
        var arr = OpenStrings.ToArray();
        arr[stringIndex] = pitch;
        OpenStrings = arr;
    }

    /// <summary>Transpose every open string by the same interval (e.g. ±1 semitone).</summary>
    public void TransposeAll(int semitones)
    {
        OpenStrings = OpenStrings.Select(p => p.Transpose(semitones)).ToArray();
    }

    /// <summary>Independent copy with a new Id (e.g. Save As).</summary>
    public GuitarTuning Clone(string? newName = null)
        => new(newName ?? Name, OpenStrings, Guid.NewGuid().ToString("N"));

    /// <summary>Independent copy keeping the same Id — safe working copy of a preset or saved tuning.</summary>
    public GuitarTuning Copy() => new(Name, OpenStrings, Id);

    public TuningDto ToDto() => new()
    {
        Id = Id,
        Name = Name,
        OpenMidiNotes = OpenStrings.Select(p => p.MidiNote).ToArray()
    };

    public static GuitarTuning FromDto(TuningDto dto)
        => new(dto.Name, dto.OpenMidiNotes.Select(m => new Pitch(m)), dto.Id);
}

public sealed class TuningDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int[] OpenMidiNotes { get; set; } = [];
}
