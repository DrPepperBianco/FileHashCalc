using System.Windows.Media;

namespace FileHashCalc;

public static class HashColorService
{
    public static Dictionary<string, Brush?> BuildColorMap(IEnumerable<HashResult> results)
    {
        var groups = results
            .GroupBy(r => r.Hash, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var colorMap = new Dictionary<string, Brush?>(StringComparer.OrdinalIgnoreCase);
        int colorIndex = 0;

        foreach (var (hash, count) in groups)
        {
            if (count > 1)
            {
                colorMap[hash] = CreatePastelBrush(colorIndex);
                colorIndex++;
            }
            else
            {
                colorMap[hash] = null;
            }
        }

        return colorMap;
    }

    private static SolidColorBrush CreatePastelBrush(int index)
    {
        // Golden angle distribution for visually distinct hues
        var hue = (index * 137.508) % 360;

        var (saturation, lightness) = ThemeManager.IsDark
            ? (0.35, 0.25)   // dark mode: darker, more saturated
            : (0.25, 0.92);  // light mode: light pastels

        var color = HslToColor(hue, saturation, lightness);
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static Color HslToColor(double h, double s, double l)
    {
        double c = (1 - Math.Abs(2 * l - 1)) * s;
        double x = c * (1 - Math.Abs(h / 60 % 2 - 1));
        double m = l - c / 2;

        var (r, g, b) = h switch
        {
            < 60 => (c, x, 0.0),
            < 120 => (x, c, 0.0),
            < 180 => (0.0, c, x),
            < 240 => (0.0, x, c),
            < 300 => (x, 0.0, c),
            _ => (c, 0.0, x),
        };

        return Color.FromRgb(
            (byte)((r + m) * 255),
            (byte)((g + m) * 255),
            (byte)((b + m) * 255));
    }
}
