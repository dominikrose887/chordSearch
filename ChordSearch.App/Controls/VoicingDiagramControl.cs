using ChordSearch.Core.Results;
using ChordSearch.Core.Voicing;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;

namespace ChordSearch.App.Controls;

/// <summary>Compact vertical chord-box diagram for list rows (low E on the left).</summary>
public sealed class VoicingDiagramControl : UserControl
{
    public static readonly DependencyProperty ResultProperty =
        DependencyProperty.Register(nameof(Result), typeof(VoicingResult), typeof(VoicingDiagramControl),
            new PropertyMetadata(null, OnChanged));

    private readonly Canvas _canvas = new();

    private const int FretsShown = 4;
    private const double PadL = 14;
    private const double PadR = 4;
    private const double PadT = 14;
    private const double PadB = 4;

    public VoicingDiagramControl()
    {
        _canvas.Background = new SolidColorBrush(Color.FromArgb(255, 250, 248, 245));
        Content = _canvas;
        SizeChanged += (_, _) => Redraw();
        MinWidth = 52;
        MinHeight = 68;
    }

    public VoicingResult? Result
    {
        get => (VoicingResult?)GetValue(ResultProperty);
        set => SetValue(ResultProperty, value);
    }

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((VoicingDiagramControl)d).Redraw();

    private void Redraw()
    {
        _canvas.Children.Clear();
        var result = Result;
        if (result is null || result.Voicing.StringCount == 0)
            return;

        var w = ActualWidth > 1 ? ActualWidth : Width;
        var h = ActualHeight > 1 ? ActualHeight : Height;
        if (double.IsNaN(w) || w < 8) w = MinWidth;
        if (double.IsNaN(h) || h < 8) h = MinHeight;

        var voicing = result.Voicing;
        var stringCount = voicing.StringCount;
        var startFret = DiagramStartFret(voicing);
        var showNut = startFret <= 1;

        var gridW = Math.Max(8, w - PadL - PadR);
        var gridH = Math.Max(8, h - PadT - PadB);
        var cellW = gridW / Math.Max(1, stringCount - 1);
        var cellH = gridH / FretsShown;

        // Fret number when not at nut
        if (!showNut)
        {
            _canvas.Children.Add(new TextBlock
            {
                Text = startFret.ToString(),
                FontSize = 9,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 80, 80, 80)),
                Margin = new Thickness(1, PadT + cellH * 0.15, 0, 0)
            });
        }

        // Horizontal fret lines
        for (var f = 0; f <= FretsShown; f++)
        {
            var y = PadT + f * cellH;
            var thick = f == 0 && showNut ? 3.0 : 1.0;
            _canvas.Children.Add(new Line
            {
                X1 = PadL,
                Y1 = y,
                X2 = PadL + gridW,
                Y2 = y,
                Stroke = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40)),
                StrokeThickness = thick
            });
        }

        // Vertical strings
        for (var s = 0; s < stringCount; s++)
        {
            var x = PadL + s * cellW;
            _canvas.Children.Add(new Line
            {
                X1 = x,
                Y1 = PadT,
                X2 = x,
                Y2 = PadT + gridH,
                Stroke = new SolidColorBrush(Color.FromArgb(255, 55, 55, 55)),
                StrokeThickness = s == 0 ? 1.6 : 1.0
            });
        }

        // Marks above nut / first fret line
        for (var s = 0; s < stringCount; s++)
        {
            var sf = voicing.Strings[s];
            var x = PadL + s * cellW;
            var markY = PadT - 8;

            if (sf.IsMuted)
            {
                AddText(x, markY, "×", Color.FromArgb(255, 180, 40, 40), 11);
            }
            else if (sf.IsOpen)
            {
                AddText(x, markY, "○", Color.FromArgb(255, 30, 120, 60), 10);
            }
            else if (sf.Fret >= startFret && sf.Fret < startFret + FretsShown)
            {
                var fretIndex = sf.Fret - startFret; // 0 = first cell below nut/start
                var cy = PadT + (fretIndex + 0.5) * cellH;
                var r = Math.Min(cellW, cellH) * 0.32;
                _canvas.Children.Add(new Ellipse
                {
                    Width = r * 2,
                    Height = r * 2,
                    Fill = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)),
                    Margin = new Thickness(x - r, cy - r, 0, 0)
                });
            }
            else if (sf.IsFretted)
            {
                // Outside visible window — still mark as muted-ish hint
                AddText(x, markY, "·", Color.FromArgb(255, 120, 120, 120), 10);
            }
        }
    }

    private void AddText(double centerX, double topY, string text, Color color, double size)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = size,
            Foreground = new SolidColorBrush(color),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        tb.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var tw = tb.DesiredSize.Width;
        tb.Margin = new Thickness(centerX - tw / 2, topY - 2, 0, 0);
        _canvas.Children.Add(tb);
    }

    /// <summary>
    /// Window start: nut (1) for open-area shapes; otherwise the shape's min fret.
    /// Diagram cells map to frets startFret .. startFret+FretsShown-1.
    /// </summary>
    internal static int DiagramStartFret(ChordVoicing voicing)
    {
        var min = voicing.MinFret;
        var max = voicing.MaxFret;
        if (min is null || max is null)
            return 1;

        // Keep nut visible when the shape fits in frets 1–4
        if (max <= FretsShown)
            return 1;

        // Prefer window that includes all fretted notes when possible
        if (max - min + 1 <= FretsShown)
            return min.Value;

        return Math.Max(1, max.Value - FretsShown + 1);
    }
}
