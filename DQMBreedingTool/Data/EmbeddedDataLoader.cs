using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace DQMBreedingTool.Data;

public static class EmbeddedDataLoader
{
    public static Dictionary<string, string[]> LoadCsvSet(string resourceNameSuffix)
    {
        var result = new Dictionary<string, string[]>();
        var asm = Assembly.GetExecutingAssembly();
        string fullName = asm.GetManifestResourceNames()
            .First(n => n.EndsWith(resourceNameSuffix, StringComparison.OrdinalIgnoreCase));
        using var resStream = asm.GetManifestResourceStream(fullName) ?? throw new InvalidOperationException($"리소스를 찾을 수 없음: {fullName}");
        using var memStream = new MemoryStream();

        resStream.CopyTo(memStream);
        memStream.Position = 0;

        using var archive = new ZipArchive(memStream, ZipArchiveMode.Read);

        foreach (var entry in archive.Entries)
        {
            using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream, Encoding.UTF8);
            var lines = new List<string>();
            string? line;

            while ((line = reader.ReadLine()) != null)
                lines.Add(line);

            result[entry.Name] = [.. lines];
        }

        return result;
    }
}