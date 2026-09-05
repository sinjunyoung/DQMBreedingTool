using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DQMBreedingTool.Data;

public static class IconLoader
{
    static readonly Regex IdPattern = new(@"ic_0*(\d+)\.png", RegexOptions.IgnoreCase);

    public static Dictionary<int, ImageSource> LoadIcons(string resourceNameSuffix)
    {
        var result = new Dictionary<int, ImageSource>();
        var asm = Assembly.GetExecutingAssembly();
        string? fullName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(resourceNameSuffix, StringComparison.OrdinalIgnoreCase));

        if (fullName == null)
            return result;

        using var resStream = asm.GetManifestResourceStream(fullName);

        if (resStream == null)
            return result;

        using var memStream = new MemoryStream();

        resStream.CopyTo(memStream);
        memStream.Position = 0;

        using var archive = new ZipArchive(memStream, ZipArchiveMode.Read);

        foreach (var entry in archive.Entries)
        {
            var match = IdPattern.Match(entry.Name);

            if (!match.Success) 
                continue;

            if (!int.TryParse(match.Groups[1].Value, out int id)) 
                continue;

            using var entryStream = entry.Open();
            using var entryMem = new MemoryStream();

            entryStream.CopyTo(entryMem);
            entryMem.Position = 0;

            var bmp = new BitmapImage();

            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = entryMem;
            bmp.EndInit();
            bmp.Freeze();

            result[id] = bmp;
        }

        return result;
    }
}