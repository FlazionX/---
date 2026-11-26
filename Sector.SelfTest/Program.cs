using Sector;

var root = Path.Combine(Path.GetTempPath(), "SectorSelfTest-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);

try
{
    var sourceA = Path.Combine(root, "source-a");
    var sourceB = Path.Combine(root, "source-b");
    var imports = Path.Combine(root, "imports");
    var folderSource = Path.Combine(root, "folder-source");
    var folderImports = Path.Combine(root, "folder-imports");
    var exports = Path.Combine(root, "exports");

    Directory.CreateDirectory(sourceA);
    Directory.CreateDirectory(sourceB);
    Directory.CreateDirectory(Path.Combine(folderSource, "nested"));
    File.WriteAllText(Path.Combine(sourceA, "shared.txt"), "one");
    File.WriteAllText(Path.Combine(sourceB, "shared.txt"), "two");
    File.WriteAllText(Path.Combine(folderSource, "nested", "asset.txt"), "folder asset");

    var fileResult = ImportService.ImportFiles(
        [Path.Combine(sourceA, "shared.txt"), Path.Combine(sourceB, "shared.txt")], imports);
    Check(fileResult.ImportedCount == 2, "ImportFiles should copy both files.");
    Check(File.Exists(Path.Combine(imports, "shared.txt")), "First imported file missing.");
    Check(File.Exists(Path.Combine(imports, "shared (2).txt")), "Duplicate name should be preserved with a suffix.");

    var folderResult = ImportService.ImportDirectory(folderSource, folderImports);
    Check(folderResult.ImportedCount == 1, "ImportDirectory should import one nested file.");
    Check(File.Exists(Path.Combine(folderImports, "nested", "asset.txt")), "Nested folder structure was not preserved.");

    var selfImportRejected = false;
    try { ImportService.ImportDirectory(folderSource, folderSource); }
    catch (InvalidOperationException) { selfImportRejected = true; }
    Check(selfImportRejected, "Importing a folder into itself must be rejected.");

    var projectPath = Path.Combine(root, "roundtrip.sector.json");
    var original = new SimulationTemplate("custom-test", "Мой проект", "Проверка сохранения проекта",
        "TEST", Color.FromArgb(22, 145, 201), SimulationStyle.Messenger, "МОЙ ПРОЕКТ", "2.1");
    ProjectFileService.Save(original, projectPath);
    var restored = ProjectFileService.Load(projectPath);
    Check(restored.Name == original.Name, "Project name was not restored.");
    Check(restored.Description == original.Description, "Project description was not restored.");
    Check(restored.Style == original.Style, "Project style was not restored.");
    Check(restored.Accent.ToArgb() == original.Accent.ToArgb(), "Project accent color was not restored.");
    Check(restored.Mark == original.Mark && restored.Version == original.Version, "Project metadata was not restored.");

    var invalidRejected = false;
    try { ProjectFileService.Validate(original with { Name = new string('x', 41) }); }
    catch (InvalidDataException) { invalidRejected = true; }
    Check(invalidRejected, "Overlong project names must be rejected.");

    foreach (var template in SimulationCatalog.All)
    {
        var output = DemoExporter.Export(template, exports);
        var html = Path.Combine(output, template.Id + ".html");
        var bat = Path.Combine(output, "Запустить демо.bat");
        Check(File.Exists(html), $"HTML export missing for {template.Id}.");
        Check(File.Exists(bat), $"BAT launcher missing for {template.Id}.");
        var launcher = File.ReadAllText(bat);
        Check(launcher.Contains("start \"\" \"%~dp0", StringComparison.Ordinal),
            $"BAT launcher does not open the local HTML file for {template.Id}.");
        Check(!launcher.Contains("taskkill", StringComparison.OrdinalIgnoreCase) &&
              !launcher.Contains("cipher", StringComparison.OrdinalIgnoreCase) &&
              !launcher.Contains("reg add", StringComparison.OrdinalIgnoreCase),
            $"Unsafe command found in launcher for {template.Id}.");
        Check(File.ReadAllText(html).Contains("НЕ БЛОКИРУЕТ WINDOWS", StringComparison.Ordinal),
            $"Safety notice missing from HTML export for {template.Id}.");
    }

    Console.WriteLine("PASS: project JSON round-trip, validation, import, nested folders, duplicate names, self-import guard and all five HTML/BAT exports.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine("FAIL: " + ex);
    return 1;
}
finally
{
    try { Directory.Delete(root, recursive: true); }
    catch { }
}

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
