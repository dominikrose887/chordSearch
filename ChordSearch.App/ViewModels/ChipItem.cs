using CommunityToolkit.Mvvm.ComponentModel;

namespace ChordSearch.App.ViewModels;

public partial class ChipItem : ObservableObject
{
    public ChipItem(object value, string label)
    {
        Value = value;
        Label = label;
    }

    public object Value { get; }

    [ObservableProperty]
    public partial string Label { get; set; } = "";

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public T As<T>() => (T)Value;
}
