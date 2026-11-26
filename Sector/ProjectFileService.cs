using System.Drawing;
using System.Text.Json;

namespace Sector;

public static class ProjectFileService
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private sealed record ProjectDocument(
        string Id, string Name, string Description, string Mark,
        string Accent, string Style, string Mood, string Version);

    public static void Save(SimulationTemplate template, string path)
    {
        Validate(template);
        var document = new ProjectDocument(template.Id, template.Name, template.Description,
            template.Mark, ColorTranslator.ToHtml(template.Accent), template.Style.ToString(),
            template.Mood, template.Version);
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directory)) throw new IOException("Не удалось определить папку проекта.");
        Directory.CreateDirectory(directory);
        var temporary = fullPath + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(document, Options), new System.Text.UTF8Encoding(false));
            File.Move(temporary, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public static SimulationTemplate Load(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists) throw new FileNotFoundException("Файл проекта не найден.", path);
        if (info.Length > 256 * 1024) throw new InvalidDataException("Файл проекта слишком большой.");
        var document = JsonSerializer.Deserialize<ProjectDocument>(File.ReadAllText(path))
            ?? throw new InvalidDataException("Файл проекта пустой или повреждённый.");
        if (!Enum.TryParse<SimulationStyle>(document.Style, ignoreCase: false, out var style))
            throw new InvalidDataException("Неизвестный стиль проекта.");
        if (!ColorTranslator.FromHtml(document.Accent).IsKnownColor &&
            !document.Accent.StartsWith("#", StringComparison.Ordinal))
            throw new InvalidDataException("Некорректный цвет проекта.");
        var template = new SimulationTemplate(document.Id, document.Name, document.Description,
            document.Mark, ColorTranslator.FromHtml(document.Accent), style, document.Mood, document.Version);
        Validate(template);
        return template;
    }

    public static void Validate(SimulationTemplate template)
    {
        if (string.IsNullOrWhiteSpace(template.Id) || template.Id.Length > 80)
            throw new InvalidDataException("Некорректный идентификатор проекта.");
        if (string.IsNullOrWhiteSpace(template.Name) || template.Name.Trim().Length > 40)
            throw new InvalidDataException("Название должно содержать от 1 до 40 символов.");
        if (string.IsNullOrWhiteSpace(template.Description) || template.Description.Trim().Length > 120)
            throw new InvalidDataException("Описание должно содержать от 1 до 120 символов.");
        if (template.Mark.Length > 8) throw new InvalidDataException("Значок не должен превышать 8 символов.");
        if (template.Version.Length > 12) throw new InvalidDataException("Версия не должна превышать 12 символов.");
        if (!Enum.IsDefined(template.Style)) throw new InvalidDataException("Неизвестный стиль проекта.");
    }
}
