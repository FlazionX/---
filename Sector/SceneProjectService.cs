using System.Drawing;
using System.Text.Json;

namespace Sector;

public static class SceneProjectService
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static void Save(SceneProject project, string path)
    {
        Validate(project);
        var full = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        var temp = full + ".tmp";
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(project, Options), new System.Text.UTF8Encoding(false));
            File.Move(temp, full, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    public static SceneProject Load(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists) throw new FileNotFoundException("Файл проекта не найден.", path);
        if (info.Length > 64 * 1024 * 1024) throw new InvalidDataException("Проект больше 64 МБ.");
        var project = JsonSerializer.Deserialize<SceneProject>(File.ReadAllText(path))
            ?? throw new InvalidDataException("Файл проекта повреждён.");
        Validate(project);
        return project;
    }

    public static void Validate(SceneProject project)
    {
        if (project is null) throw new InvalidDataException("Пустой проект.");
        if (project.SchemaVersion != 1) throw new InvalidDataException("Неподдерживаемая версия проекта.");
        if (string.IsNullOrWhiteSpace(project.Name) || project.Name.Length > 80)
            throw new InvalidDataException("Название проекта должно содержать 1–80 символов.");
        if (project.CanvasWidth is < 320 or > 3840 || project.CanvasHeight is < 240 or > 2160)
            throw new InvalidDataException("Недопустимое разрешение сцены.");
        if (!IsHex(project.BackgroundHex)) throw new InvalidDataException("Некорректный цвет фона.");
        if (project.Elements is null || project.Elements.Count > 500)
            throw new InvalidDataException("В проекте допускается не более 500 объектов.");
        foreach (var e in project.Elements)
        {
            if (string.IsNullOrWhiteSpace(e.Id) || e.Id.Length > 80 || string.IsNullOrWhiteSpace(e.Name) || e.Name.Length > 100)
                throw new InvalidDataException("У объекта отсутствует имя или идентификатор.");
            if (!Enum.IsDefined(e.Type) || !float.IsFinite(e.X) || !float.IsFinite(e.Y) ||
                !float.IsFinite(e.Width) || !float.IsFinite(e.Height) ||
                e.Width is < 8 or > 3840 || e.Height is < 8 or > 2160 ||
                e.X < 0 || e.Y < 0 || e.X + e.Width > project.CanvasWidth || e.Y + e.Height > project.CanvasHeight)
                throw new InvalidDataException("Обнаружены некорректные координаты или размеры объекта.");
            if (e.Text is null || e.Text.Length > 4000 || e.FontFamily is null || e.FontFamily.Length > 100 ||
                !float.IsFinite(e.FontSize) || e.FontSize is < 6 or > 180 || !IsHex(e.ColorHex))
                throw new InvalidDataException("Некорректные свойства объекта.");
            if (e.Type == SceneElementType.Image)
            {
                if (string.IsNullOrWhiteSpace(e.ImageBase64) || e.ImageBase64.Length > 32 * 1024 * 1024)
                    throw new InvalidDataException("Изображение отсутствует или слишком большое.");
                try
                {
                    var bytes = Convert.FromBase64String(e.ImageBase64);
                    using var stream = new MemoryStream(bytes);
                    using var image = Image.FromStream(stream);
                    if (image.Width > 8192 || image.Height > 8192) throw new InvalidDataException("Изображение слишком большое.");
                }
                catch (FormatException) { throw new InvalidDataException("Повреждённые данные изображения."); }
                catch (ArgumentException) { throw new InvalidDataException("Неподдерживаемый файл изображения."); }
            }
        }
    }

    private static bool IsHex(string? value) =>
        value is { Length: 7 } && value[0] == '#' && value[1..].All(Uri.IsHexDigit);
}
