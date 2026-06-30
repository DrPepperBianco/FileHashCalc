using System.IO;
using System.Text.Json;

namespace FileHashCalc;

public static class ResultStore
{
    private static readonly string StorePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FileHashCalc", "results.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public static List<HashResult> Load()
    {
        if (!File.Exists(StorePath))
            return [];

        try
        {
            var json = File.ReadAllText(StorePath);
            return JsonSerializer.Deserialize<List<HashResult>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static void Save(List<HashResult> results)
    {
        var dir = Path.GetDirectoryName(StorePath)!;
        Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(results, JsonOptions);
        File.WriteAllText(StorePath, json);
    }
}
