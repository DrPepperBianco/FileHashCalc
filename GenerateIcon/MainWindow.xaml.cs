using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GenerateIcon;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Headless mode: "GenerateIcon --export" writes app.ico and exits.
        if (Environment.GetCommandLineArgs().Contains("--export"))
        {
            Export(showMessage: false);
            Application.Current.Shutdown();
        }
    }

    private void Export_Click(object sender, RoutedEventArgs e) => Export(showMessage: true);

    private void Export(bool showMessage)
    {
        var fileShape = (Geometry)FindResource("FileShape");
        var foldCover = (Geometry)FindResource("FoldCover");
        var foldLine = (Geometry)FindResource("FoldLine");
        var hashSymbol = (Geometry)FindResource("HashSymbol");
        var outlineColor = (Color)FindResource("OutlineColor");
        var hashColor = (Color)FindResource("HashColor");

        var outputPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FileHashCalc", "app.ico"));

        int[] sizes = [16, 24, 32, 48, 256];

        using var stream = File.Create(outputPath);
        using var writer = new BinaryWriter(stream);

        writer.Write((short)0);
        writer.Write((short)1);
        writer.Write((short)sizes.Length);

        var pngList = new List<byte[]>();
        foreach (var size in sizes)
            pngList.Add(RenderPng(size, outlineColor, hashColor, fileShape, foldCover, foldLine, hashSymbol));

        int offset = 6 + sizes.Length * 16;
        for (int i = 0; i < sizes.Length; i++)
        {
            var s = sizes[i];
            writer.Write((byte)(s < 256 ? s : 0));
            writer.Write((byte)(s < 256 ? s : 0));
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((short)1);
            writer.Write((short)32);
            writer.Write(pngList[i].Length);
            writer.Write(offset);
            offset += pngList[i].Length;
        }
        foreach (var data in pngList)
            writer.Write(data);

        if (showMessage)
            MessageBox.Show($"Icon exportiert:\n{outputPath}", "Fertig",
                MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static byte[] RenderPng(int size, Color outlineCol, Color hashCol,
        Geometry fileShape, Geometry foldCover, Geometry foldLine, Geometry hash)
    {
        var visual = new DrawingVisual();
        RenderOptions.SetEdgeMode(visual, EdgeMode.Aliased);

        using (var ctx = visual.RenderOpen())
        {
            var margin = size <= 24 ? 0.0 : size <= 48 ? size * 0.04 : size * 0.08;
            var scale = (size - 2 * margin) / 24.0;
            var pixelOffset = size <= 32 ? 0.5 : 0.0;

            ctx.PushTransform(new TranslateTransform(margin + pixelOffset, margin + pixelOffset));
            ctx.PushTransform(new ScaleTransform(scale, scale));

            var outlineBrush = new SolidColorBrush(outlineCol);
            var strokeWidth = size <= 16 ? 2.0 : size <= 32 ? 1.8 : 1.4;
            var strokePen = new Pen(outlineBrush, strokeWidth)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round,
            };

            ctx.DrawGeometry(Brushes.White, strokePen, fileShape);
            ctx.DrawGeometry(Brushes.White, null, foldCover);
            ctx.DrawGeometry(null, strokePen, foldLine);
            ctx.DrawGeometry(new SolidColorBrush(hashCol), null, hash);

            ctx.Pop();
            ctx.Pop();
        }

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var ms = new MemoryStream();
        encoder.Save(ms);
        return ms.ToArray();
    }
}
