using System.IO;
using System.Security.Cryptography;

namespace FileHashCalc;

public static class HashService
{
    public static readonly string[] SupportedAlgorithms =
        ["MD5", "SHA-1", "SHA-256", "SHA-384", "SHA-512"];

    public static async Task<HashResult> ComputeHashAsync(
        string filePath,
        string algorithm,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        using var hash = CreateAlgorithm(algorithm);
        var fileInfo = new FileInfo(filePath);

        await using var stream = new FileStream(
            filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 81920, useAsync: true);

        var buffer = new byte[81920];
        long totalRead = 0;
        long fileSize = stream.Length;

        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            hash.TransformBlock(buffer, 0, bytesRead, null, 0);
            totalRead += bytesRead;
            progress?.Report(fileSize > 0 ? (double)totalRead / fileSize : 1.0);
        }

        hash.TransformFinalBlock([], 0, 0);

        return new HashResult
        {
            FilePath = filePath,
            FileName = fileInfo.Name,
            Hash = Convert.ToHexString(hash.Hash!).ToLowerInvariant(),
            Algorithm = algorithm,
            Timestamp = DateTime.Now,
            FileSize = fileSize,
        };
    }

    private static HashAlgorithm CreateAlgorithm(string name) => name switch
    {
        "MD5" => MD5.Create(),
        "SHA-1" => SHA1.Create(),
        "SHA-256" => SHA256.Create(),
        "SHA-384" => SHA384.Create(),
        "SHA-512" => SHA512.Create(),
        _ => throw new ArgumentException($"Unbekannter Algorithmus: {name}"),
    };
}
