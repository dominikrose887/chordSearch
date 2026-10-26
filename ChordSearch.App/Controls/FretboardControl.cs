using ChordSearch.App.Diagnostics;
using ChordSearch.Core.Music;
using ChordSearch.Core.Results;
using ChordSearch.Core.Tuning;
using ChordSearch.Core.Voicing;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI;

namespace ChordSearch.App.Controls;

public sealed class FretboardControl : UserControl
{
    public static readonly DependencyProperty ResultProperty =
        DependencyProperty.Register(nameof(Result), typeof(VoicingResult), typeof(FretboardControl),
            new PropertyMetadata(null, OnVisualPropertyChanged));

    public static readonly DependencyProperty TuningProperty =
        DependencyProperty.Register(nameof(Tuning), typeof(GuitarTuning), typeof(FretboardControl),
            new PropertyMetadata(null, OnVisualPropertyChanged));

    public static readonly DependencyProperty MaxFretProperty =
        DependencyProperty.Register(nameof(MaxFret), typeof(int), typeof(FretboardControl),
            new PropertyMetadata(24, OnVisualPropertyChanged));

    public static readonly DependencyProperty ScaleLengthMmProperty =
        DependencyProperty.Register(nameof(ScaleLengthMm), typeof(double), typeof(FretboardControl),
            new PropertyMetadata(FretGeometry.StandardScaleLengthMm, OnVisualPropertyChanged));

    private const double PadLeft = 48;
    private const double PadRight = 10;
    private const double PadTop = 18;
    private const double PadBottom = 22;
    private const double NeckEdgeMm = 3.5;
    private const double NutWidthSixStringMm = 43.0;

    private readonly Canvas _canvas = new();
    private readonly StackPanel _legend = new()
    {
        Orientation = Orientation.Horizontal,
        Spacing = 14,
        HorizontalAlignment = HorizontalAlignment.Center,
        Margin = new Thickness(0, 8, 0, 0)
    };

    private double _pxPerMm;
    private double _neckLeft;
    private double _neckTop;
    private double _neckBottom;

    public event EventHandler<Pitch>? NoteClicked;

    public FretboardControl()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        _canvas.Background = new SolidColorBrush(Color.FromArgb(255, 36, 28, 22));
        Grid.SetRow(_canvas, 0);
        Grid.SetRow(_legend, 1);
        root.Children.Add(_canvas);
        root.Children.Add(_legend);
        Content = root;

        BuildLegend();
        SizeChanged += (_, _) => Redraw();
        _canvas.PointerPressed += OnPointerPressed;
    }

    public VoicingResult? Result
    {
        get => (VoicingResult?)GetValue(ResultProperty);
        set => SetValue(ResultProperty, value);
    }

    public GuitarTuning? Tuning
    {
        get => (GuitarTuning?)GetValue(TuningProperty);
        set => SetValue(TuningProperty, value);
    }

    public int MaxFret
    {
        get => (int)GetValue(MaxFretProperty);
        set => SetValue(MaxFretProperty, value);
    }

    public double ScaleLengthMm
    {
        get => (double)GetValue(ScaleLengthMmProperty);
        set => SetValue(ScaleLengthMmProperty, value);
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((FretboardControl)d).Redraw();

    private static Color FingerColor(int finger) => finger switch
    {
        1 => Color.FromArgb(255, 37, 99, 235),    // index — deep blue
        2 => Color.FromArgb(255, 22, 163, 74),    // middle — deep green
        3 => Color.FromArgb(255, 217, 119, 6),    // ring — deep amber
        4 => Color.FromArgb(255, 147, 51, 234),   // pinky — deep violet
        _ => Color.FromArgb(255, 75, 85, 99)
    };

    private static readonly SolidColorBrush LegendTextBrush =
        new(Color.FromArgb(255, 40, 40, 40));

    private static string FingerLabel(int finger) => finger switch
    {
        1 => "1 — index",
        2 => "2 — middle",
        3 => "3 — ring",
        4 => "4 — pinky",
        _ => "?"
    };

    private void BuildLegend()
    {
        _legend.Children.Clear();
        for (var f = 1; f <= 4; f++)
            _legend.Children.Add(LegendItem(FingerColor(f), FingerLabel(f)));

        _legend.Children.Add(LegendOpenSample());
        _legend.Children.Add(LegendGlyph("×", Colors.Salmon, "mute"));
    }

    private static UIElement LegendOpenSample()
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        row.Children.Add(new Ellipse
        {
            Width = 14,
            Height = 14,
            Fill = new SolidColorBrush(Color.FromArgb(255, 28, 28, 28)),
            Stroke = new SolidColorBrush(Color.FromArgb(255, 110, 220, 130)),
            StrokeThickness = 2,
            VerticalAlignment = VerticalAlignment.Center
        });
        row.Children.Add(new TextBlock
        {
            Text = "open",
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = LegendTextBrush
        });
        return row;
    }

    private static UIElement LegendItem(Color color, string text)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        row.Children.Add(new Ellipse
        {
            Width = 12,
            Height = 12,
            Fill = new SolidColorBrush(color),
            VerticalAlignment = VerticalAlignment.Center
        });
        row.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = LegendTextBrush
        });
        return row;
    }

    private static UIElement LegendGlyph(string glyph, Color color, string text)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        row.Children.Add(new TextBlock
        {
            Text = glyph,
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(color),
            VerticalAlignment = VerticalAlignment.Center
        });
        row.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = LegendTextBrush
        });
        return row;
    }

    private double FretWireX(int fret, double scale)
        => _neckLeft + FretGeometry.DistanceFromNutMm(fret, scale) * _pxPerMm;

    private double FretCellCenterX(int fret, double scale)
        => _neckLeft + FretGeometry.FretCellCenterMm(fret, scale) * _pxPerMm;

    private double StringY(int stringIndex, int stringCount)
    {
        var edge = NeckEdgeMm * _pxPerMm;
        var spanTop = _neckTop + edge;
        var spanBottom = _neckBottom - edge;
        var gap = stringCount <= 1 ? 0 : (spanBottom - spanTop) / (stringCount - 1);
        return spanTop + (stringCount - 1 - stringIndex) * gap;
    }

    private static double NutWidthMm(int stringCount)
        => NutWidthSixStringMm * (stringCount / 6.0);

    public void Redraw()
    {
        try { RedrawCore(); }
        catch (Exception ex) { AppLog.Error("FretboardControl.Redraw failed", ex); }
    }

    private void RedrawCore()
    {
        _canvas.Children.Clear();
        if (_canvas.ActualWidth <= 1 || _canvas.ActualHeight <= 1)
            return;

        var tuning = Tuning;
        if (tuning is null || tuning.StringCount < 1)
            return;

        var maxFret = Math.Clamp(MaxFret, 12, 24);
        var scale = ScaleLengthMm > 0 ? ScaleLengthMm : FretGeometry.StandardScaleLengthMm;
        var stringCount = tuning.StringCount;

        var visibleMm = FretGeometry.DistanceFromNutMm(maxFret, scale);
        var nutMm = NutWidthMm(stringCount);
        var aspect = nutMm / Math.Max(1e-6, visibleMm);

        var availW = Math.Max(80, _canvas.ActualWidth - PadLeft - PadRight);
        var availH = Math.Max(40, _canvas.ActualHeight - PadTop - PadBottom);

        double neckWidth, neckHeight;
        if (availW * aspect <= availH)
        {
            neckWidth = availW;
            neckHeight = availW * aspect;
        }
        else
        {
            neckHeight = availH;
            neckWidth = availH / aspect;
        }

        _pxPerMm = neckWidth / Math.Max(1e-6, visibleMm);
        _neckLeft = PadLeft + (availW - neckWidth) / 2;
        _neckTop = PadTop + (availH - neckHeight) / 2;
        _neckBottom = _neckTop + neckHeight;

        Place(new Rectangle
        {
            Width = neckWidth,
            Height = neckHeight,
            Fill = new SolidColorBrush(Color.FromArgb(255, 110, 78, 48)),
            RadiusX = 3,
            RadiusY = 3
        }, _neckLeft, _neckTop);

        int[] inlays = [3, 5, 7, 9, 12, 15, 17, 19, 21, 24];
        var midY = (_neckTop + _neckBottom) / 2;
        var inlaySpread = Math.Max(6, (_neckBottom - _neckTop - 2 * NeckEdgeMm * _pxPerMm) * 0.28);
        foreach (var f in inlays)
        {
            if (f < 1 || f > maxFret) continue;
            var cx = FretCellCenterX(f, scale);
            var inlayR = Math.Clamp(2.2 * _pxPerMm, 2.5, 5.5);
            if (f is 12 or 24)
            {
                AddInlay(cx, midY - inlaySpread, inlayR);
                AddInlay(cx, midY + inlaySpread, inlayR);
            }
            else AddInlay(cx, midY, inlayR);
        }

        for (var fret = 0; fret <= maxFret; fret++)
        {
            var x = FretWireX(fret, scale);
            var isNut = fret == 0;
            _canvas.Children.Add(new Line
            {
                X1 = x,
                Y1 = _neckTop,
                X2 = x,
                Y2 = _neckBottom,
                Stroke = new SolidColorBrush(isNut ? Colors.FloralWhite : Color.FromArgb(255, 200, 200, 200)),
                StrokeThickness = isNut ? 4 : 1.5
            });
        }

        for (var fret = 1; fret <= maxFret; fret++)
        {
            if (fret > 12 && fret % 2 != 0 && fret is not (15 or 17 or 19 or 21 or 24))
                continue;

            var label = new TextBlock
            {
                Text = fret.ToString(),
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromArgb(200, 230, 230, 230))
            };
            CenterText(label, FretCellCenterX(fret, scale), _neckBottom + 4);
        }

        Place(new TextBlock
        {
            Text = $"{scale:0} mm · {maxFret}fr",
            FontSize = 11,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 170, 60))
        }, _neckLeft, Math.Max(2, _neckTop - 16));

        for (var s = 0; s < stringCount; s++)
        {
            var y = StringY(s, stringCount);
            var thickness = 1.1 + (stringCount - 1 - s) * 0.3;
            _canvas.Children.Add(new Line
            {
                X1 = _neckLeft,
                Y1 = y,
                X2 = _neckLeft + neckWidth,
                Y2 = y,
                Stroke = new SolidColorBrush(Color.FromArgb(255, 230, 220, 190)),
                StrokeThickness = thickness
            });

            var open = new TextBlock
            {
                Text = tuning.OpenPitch(s).ToDisplayName(),
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromArgb(220, 220, 220, 220))
            };
            CenterText(open, _neckLeft / 2, y - 8);
        }

        var result = Result;
        if (result is null)
            return;

        if (result.Voicing.StringCount != stringCount)
        {
            AppLog.Warn("Fretboard: voicing/tuning string mismatch");
            return;
        }

        foreach (var barre in result.Fingering.Barres)
        {
            if (barre.Fret < 1 || barre.Fret > maxFret) continue;
            if (barre.FromString < 0 || barre.EndString >= stringCount) continue;

            var cellW = FretGeometry.FretCellWidthMm(barre.Fret, scale) * _pxPerMm;
            var barW = Math.Clamp(cellW * 0.42, 7, 16);
            var cx = FretCellCenterX(barre.Fret, scale);
            var y1 = StringY(barre.FromString, stringCount);
            var y2 = StringY(barre.EndString, stringCount);
            var finger = barre.Finger is >= 1 and <= 4 ? barre.Finger : 1;
            var color = FingerColor(finger);

            var bar = new Rectangle
            {
                Width = barW,
                Height = Math.Abs(y2 - y1) + 16,
                RadiusX = barW / 2,
                RadiusY = barW / 2,
                Fill = new SolidColorBrush(Color.FromArgb(200, color.R, color.G, color.B)),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 1
            };
            Place(bar, cx - barW / 2, Math.Min(y1, y2) - 8);
        }

        for (var s = 0; s < stringCount; s++)
        {
            var sf = result.Voicing.Strings[s];
            var y = StringY(s, stringCount);

            if (sf.IsMuted)
            {
                DrawGlyph(_neckLeft - 14, y, "×", Colors.Salmon, 14);
                continue;
            }

            if (sf.IsOpen)
            {
                // Open string: same note circle at the nut (not a finger color)
                var openR = Math.Clamp(FretGeometry.FretCellWidthMm(1, scale) * _pxPerMm * 0.34, 9, 14);
                var openNote = PitchClassNames.ToDisplayName(tuning.OpenPitch(s).PitchClass);
                var openCx = _neckLeft - openR - 2; // just left of the nut
                DrawOpenNoteDot(openCx, y, openR, openNote);
                continue;
            }

            var fret = sf.Fret;
            if (fret < 1 || fret > maxFret) continue;

            var cx = FretCellCenterX(fret, scale);
            var cellW = FretGeometry.FretCellWidthMm(fret, scale) * _pxPerMm;
            var r = Math.Clamp(cellW * 0.42, 11, 18);
            var finger = s < result.Fingering.Fingers.Count ? result.Fingering.Fingers[s].Value : 0;

            string note;
            try
            {
                note = PitchClassNames.ToDisplayName(tuning.PitchAt(s, fret).PitchClass);
            }
            catch
            {
                note = "?";
            }

            DrawFingerNoteDot(cx, y, r, finger, note);
        }
    }

    private void DrawOpenNoteDot(double cx, double cy, double r, string noteName)
    {
        // Hollow-style: dark fill + light green ring so it's distinct from fretted finger colors
        Place(new Ellipse
        {
            Width = r * 2,
            Height = r * 2,
            Fill = new SolidColorBrush(Color.FromArgb(255, 20, 20, 20)),
            Stroke = new SolidColorBrush(Color.FromArgb(255, 110, 220, 130)),
            StrokeThickness = 2.2
        }, cx - r, cy - r);

        DrawCenteredNoteLabel(cx, cy, r, noteName, Colors.White);
    }

    private void DrawFingerNoteDot(double cx, double cy, double r, int finger, string noteName)
    {
        var fill = FingerColor(finger);
        Place(new Ellipse
        {
            Width = r * 2,
            Height = r * 2,
            Fill = new SolidColorBrush(fill),
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 1.6
        }, cx - r, cy - r);

        // White on deep fills — high contrast for all finger colors
        DrawCenteredNoteLabel(cx, cy, r, noteName, Colors.White);
    }

    private void DrawCenteredNoteLabel(double cx, double cy, double r, string noteName, Color textColor)
    {
        var fontSize = noteName.Length <= 1
            ? Math.Clamp(r * 1.2, 11, 16)
            : Math.Clamp(r * 0.95, 10, 13);

        // Soft dark halo so letters stay readable on any finger color
        var halo = new TextBlock
        {
            Text = noteName,
            FontSize = fontSize,
            FontWeight = Microsoft.UI.Text.FontWeights.ExtraBold,
            Foreground = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0))
        };
        halo.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var top = cy - halo.DesiredSize.Height / 2;
        CenterText(halo, cx + 0.6, top + 0.6);

        var label = new TextBlock
        {
            Text = noteName,
            FontSize = fontSize,
            FontWeight = Microsoft.UI.Text.FontWeights.ExtraBold,
            Foreground = new SolidColorBrush(textColor)
        };
        label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        CenterText(label, cx, cy - label.DesiredSize.Height / 2);
    }

    private void DrawGlyph(double centerX, double centerY, string text, Color color, double fontSize)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = new SolidColorBrush(color)
        };
        CenterText(tb, centerX, centerY - fontSize * 0.55);
    }

    private void AddInlay(double cx, double cy, double r)
    {
        Place(new Ellipse
        {
            Width = r * 2,
            Height = r * 2,
            Fill = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255))
        }, cx - r, cy - r);
    }

    private void CenterText(TextBlock tb, double centerX, double topY)
    {
        tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Place(tb, centerX - tb.DesiredSize.Width / 2, topY);
    }

    private void Place(UIElement el, double x, double y)
    {
        Canvas.SetLeft(el, x);
        Canvas.SetTop(el, y);
        _canvas.Children.Add(el);
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            if (Tuning is null) return;
            var pt = e.GetCurrentPoint(_canvas).Position;
            if (!TryHitPitch(pt.X, pt.Y, out var pitch))
                return;
            NoteClicked?.Invoke(this, pitch);
        }
        catch (Exception ex)
        {
            AppLog.Error("Fretboard pointer press failed", ex);
        }
    }

    /// <summary>Resolve the pitch under a canvas point (open nut or fretted cell).</summary>
    private bool TryHitPitch(double x, double y, out Pitch pitch)
    {
        pitch = default;
        var tuning = Tuning;
        if (tuning is null || _pxPerMm <= 0) return false;

        var stringCount = tuning.StringCount;
        var maxFret = Math.Clamp(MaxFret, 12, 24);
        var scale = ScaleLengthMm > 0 ? ScaleLengthMm : FretGeometry.StandardScaleLengthMm;

        // Nearest string by Y
        var bestString = -1;
        var bestDy = double.MaxValue;
        for (var s = 0; s < stringCount; s++)
        {
            var dy = Math.Abs(y - StringY(s, stringCount));
            if (dy < bestDy)
            {
                bestDy = dy;
                bestString = s;
            }
        }

        var stringTolerance = Math.Max(10, (_neckBottom - _neckTop) / (stringCount * 1.6));
        if (bestString < 0 || bestDy > stringTolerance)
            return false;

        // Open / mute zone left of nut
        if (x < _neckLeft)
        {
            pitch = tuning.OpenPitch(bestString);
            return true;
        }

        // Fretted cell under X
        for (var fret = 1; fret <= maxFret; fret++)
        {
            var left = FretWireX(fret - 1, scale);
            var right = FretWireX(fret, scale);
            if (x >= left && x <= right)
            {
                pitch = tuning.PitchAt(bestString, fret);
                return true;
            }
        }

        return false;
    }
}
