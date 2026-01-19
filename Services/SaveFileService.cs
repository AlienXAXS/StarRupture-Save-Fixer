using System.IO;
using System.IO.Compression;
using System.Text;
using StarRuptureSaveFixer.Models;

namespace StarRuptureSaveFixer.Services;

/// <summary>
/// Service for loading and saving Star Rupture save files
/// </summary>
public class SaveFileService
{
    /// <summary>
    /// Loads a .sav file and extracts the JSON content to memory
    /// </summary>
    /// <param name="filePath">Path to the .sav file</param>
    /// <returns>SaveFile object with decompressed JSON content</returns>
    public SaveFile LoadSaveFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Save file not found: {filePath}");
        }

        byte[] fileData = File.ReadAllBytes(filePath);

        if (fileData.Length < 4)
        {
            throw new InvalidDataException("Save file is too small. Expected at least 4 bytes for JSON size header.");
        }

        // Read the first 4 bytes as the JSON size (little-endian)
        int jsonSize = BitConverter.ToInt32(fileData, 0);

        // Extract the compressed data (everything after the first 4 bytes)
        byte[] compressedData = new byte[fileData.Length - 4];
        Array.Copy(fileData, 4, compressedData, 0, compressedData.Length);

        // Decompress the zlib data (raw deflate, no header)
        string jsonContent = DecompressZlibRaw(compressedData);

        return new SaveFile
        {
            FilePath = filePath,
            JsonContent = jsonContent
        };
    }

    /// <summary>
    /// Saves a SaveFile object back to disk as a .sav file
    /// </summary>
    /// <param name="saveFile">The save file to write</param>
    /// <param name="outputPath">Optional output path. If null, overwrites the original file.</param>
    public void SaveSaveFile(SaveFile saveFile, string? outputPath = null)
    {
        string targetPath = outputPath ?? saveFile.FilePath;

        // Compress the JSON content using zlib raw deflate
        byte[] compressedData = CompressZlibRaw(saveFile.JsonContent);

        // Get the JSON size in bytes
        byte[] jsonSizeBytes = BitConverter.GetBytes(Encoding.UTF8.GetByteCount(saveFile.JsonContent));

        // Combine size header + compressed data
        byte[] finalData = new byte[4 + compressedData.Length];
        Array.Copy(jsonSizeBytes, 0, finalData, 0, 4);
        Array.Copy(compressedData, 0, finalData, 4, compressedData.Length);

        // Write to file
        File.WriteAllBytes(targetPath, finalData);
    }

    /// <summary>
    /// Decompresses zlib raw data (deflate without header)
    /// </summary>
    private string DecompressZlibRaw(byte[] compressedData)
    {
        // Check if this is zlib-wrapped data (has 2-byte header)
        // Zlib header typically starts with 0x78 (120 decimal)
        byte[] deflateData = compressedData;

        if (compressedData.Length >= 2 && compressedData[0] == 0x78)
        {
            // This is zlib-wrapped data, strip the 2-byte header and 4-byte checksum trailer
            deflateData = new byte[compressedData.Length - 6];
            Array.Copy(compressedData, 2, deflateData, 0, deflateData.Length);
        }

        using var compressedStream = new MemoryStream(deflateData);
        using var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
        using var decompressedStream = new MemoryStream();

        deflateStream.CopyTo(decompressedStream);
        byte[] decompressedBytes = decompressedStream.ToArray();

        return Encoding.UTF8.GetString(decompressedBytes);
    }

    /// <summary>
    /// Compresses data using zlib format (with header and checksum)
    /// </summary>
    private byte[] CompressZlibRaw(string jsonContent)
    {
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonContent);

        // Compress using raw deflate
        using var deflateStream = new MemoryStream();
        using (var compressor = new DeflateStream(deflateStream, CompressionLevel.Optimal))
        {
            compressor.Write(jsonBytes, 0, jsonBytes.Length);
        }
        byte[] deflateData = deflateStream.ToArray();

        // Add zlib wrapper (2-byte header + deflate data + 4-byte Adler32 checksum)
        using var zlibStream = new MemoryStream();

        // Zlib header (2 bytes): 0x78 0x9C for default compression
        zlibStream.WriteByte(0x78);
        zlibStream.WriteByte(0x9C);

        // Write deflate data
        zlibStream.Write(deflateData, 0, deflateData.Length);

        // Calculate and write Adler32 checksum (4 bytes, big-endian)
        uint adler32 = CalculateAdler32(jsonBytes);
        zlibStream.WriteByte((byte)(adler32 >> 24));
        zlibStream.WriteByte((byte)(adler32 >> 16));
        zlibStream.WriteByte((byte)(adler32 >> 8));
        zlibStream.WriteByte((byte)adler32);

        return zlibStream.ToArray();
    }

    /// <summary>
    /// Calculates Adler32 checksum for zlib
    /// </summary>
    private uint CalculateAdler32(byte[] data)
    {
        const uint MOD_ADLER = 65521;
        uint a = 1, b = 0;

        foreach (byte byteVal in data)
        {
            a = (a + byteVal) % MOD_ADLER;
            b = (b + a) % MOD_ADLER;
        }

        return (b << 16) | a;
    }
}
