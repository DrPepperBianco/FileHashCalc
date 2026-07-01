using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

// Combined icon: file outline + # (hash) symbol inside
// Based on Material Design Icons (Apache 2.0):
//   file-chart-check-outline (file shape)
//   folder-pound-outline (hash symbol, repositioned)

// File inner area (white fill) - the paper area inside the file outline
var fileFill = Geometry.Parse(
    "M6,4 H13 V9 H18 V20 H6 Z");

// File outline with fold
var fileOutline = Geometry.Parse(
    "M14,2 H6 C4.9,2 4,2.9 4,4 V20 C4,21.1 4.9,22 6,22 H18 C19.1,22 20,21.1 20,20 V8 L14,2" +
    "M6,4 H13 V9 H18 V20 H6 V4 Z");

// Hash symbol centered in file body (shifted from folder-pound by dx=-3, dy=+2)
var hashSymbol = Geometry.Parse(
    "M16,14 V13 H14.5 L15,11 H14 L13.5,13 H11.5 L12,11 H11 L10.5,13 H9 V14 H10.25 L9.75,16 H8 V17 H9.5 L9,19 H10 L10.5,17 H12.5 L12,19 H13 L13.5,17 H15 V16 H13.75 L14.25,14 H16" +
    "M12.75,16 H10.75 L11.25,14 H13.25 L12.75,16 Z");

int[] sizes = [16, 24, 32, 48, 256];
var fg = Color.FromRgb(0x00, 0x75, 0xB5);
var outputPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FileHashCalc", "app.ico"));

using var stream = File.Create(outputPath);
using var writer = new BinaryWriter(stream);

writer.Write((short)0);
writer.Write((short)1);
writer.Write((short)sizes.Length);

var pngList = new List<byte[]>();
foreach (var size in sizes)
    pngList.Add(RenderPng(size, fg, fileFill, fileOutline, hashSymbol));

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

Console.WriteLine($"Icon generated: {outputPath}");

static byte[] RenderPng(int size, Color fg, Geometry fill, Geometry outline, Geometry hash)
{
    var visual = new DrawingVisual();
    using (var ctx = visual.RenderOpen())
    {
        var margin = size * 0.08;
        var scale = (size - 2 * margin) / 24.0;
        ctx.PushTransform(new TranslateTransform(margin, margin));
        ctx.PushTransform(new ScaleTransform(scale, scale));

        // 1. White fill for the paper area
        ctx.DrawGeometry(Brushes.White, null, fill);
        // 2. Blue file outline
        ctx.DrawGeometry(new SolidColorBrush(fg), null, outline);
        // 3. Blue hash symbol on top
        ctx.DrawGeometry(new SolidColorBrush(fg), null, hash);

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
