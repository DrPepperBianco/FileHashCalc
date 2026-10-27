using System.IO;
using System.Text.Json.Serialization;

namespace FileHashCalc;

public class HashResult
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public string Algorithm { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public long FileSize { get; set; }

    [JsonIgnore]
    public string Directory => Path.GetDirectoryName(FilePath) ?? string.Empty;
}
