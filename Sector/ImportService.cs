namespace Sector;

public sealed record ImportResult(int ImportedCount, int SkippedCount, IReadOnlyList<string> ImportedFiles);

public static class ImportService
{
    private const long MaxFileBytes = 100L * 1024 * 1024;
    private const long MaxTotalBytes = 300L * 1024 * 1024;
    private const int MaxFiles = 600;

    public static ImportResult ImportFiles(IEnumerable<string> paths, string destination)
    {
        Directory.CreateDirectory(destination);
        var imported = new List<string>();
        var skipped = 0;
        long totalBytes = 0;

        foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (imported.Count >= MaxFiles) { skipped++; continue; }
            if (!File.Exists(path)) { skipped++; continue; }

            var info = new FileInfo(path);
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0 ||
                info.Length > MaxFileBytes || totalBytes + info.Length > MaxTotalBytes)
            {
                skipped++;
                continue;
            }

            var target = GetUniquePath(destination, Path.GetFileName(path));
            File.Copy(path, target, overwrite: false);
            imported.Add(target);
            totalBytes += info.Length;
        }

        return new ImportResult(imported.Count, skipped, imported);
    }

    public static ImportResult ImportDirectory(string source, string destination)
    {
        if (!Directory.Exists(source)) throw new DirectoryNotFoundException("Выбранная папка не найдена.");
        Directory.CreateDirectory(destination);

        var root = Path.GetFullPath(source);
        var imported = new List<string>();
        var skipped = 0;
        long totalBytes = 0;
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0 && imported.Count < MaxFiles)
        {
            var current = pending.Pop();
            foreach (var directory in Directory.EnumerateDirectories(current))
            {
                if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) == 0)
                    pending.Push(directory);
                else
                    skipped++;
            }

            foreach (var file in Directory.EnumerateFiles(current))
            {
                if (imported.Count >= MaxFiles) { skipped++; continue; }
                if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0) { skipped++; continue; }

                var info = new FileInfo(file);
                if (info.Length > MaxFileBytes || totalBytes + info.Length > MaxTotalBytes)
                {
                    skipped++;
                    continue;
                }

                var relative = Path.GetRelativePath(root, file);
                var safeRelative = relative.Replace('\\', Path.DirectorySeparatorChar)
                                           .Replace('/', Path.DirectorySeparatorChar);
                if (Path.IsPathRooted(safeRelative) || safeRelative.Split(Path.DirectorySeparatorChar).Any(x => x == ".."))
                {
                    skipped++;
                    continue;
                }

                var targetDirectory = Path.Combine(destination, Path.GetDirectoryName(safeRelative) ?? string.Empty);
                Directory.CreateDirectory(targetDirectory);
                var target = GetUniquePath(targetDirectory, Path.GetFileName(file));
                File.Copy(file, target, overwrite: false);
                imported.Add(target);
                totalBytes += info.Length;
            }
        }

        return new ImportResult(imported.Count, skipped, imported);
    }

    private static string GetUniquePath(string directory, string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "imported-file";
        var candidate = Path.Combine(directory, safeName);
        if (!File.Exists(candidate)) return candidate;

        var name = Path.GetFileNameWithoutExtension(safeName);
        var extension = Path.GetExtension(safeName);
        for (var index = 2; index < 10000; index++)
        {
            candidate = Path.Combine(directory, $"{name} ({index}){extension}");
            if (!File.Exists(candidate)) return candidate;
        }
        throw new IOException("Слишком много файлов с одинаковым именем.");
    }
}
