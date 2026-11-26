using System.Drawing;

namespace Sector;

public enum SimulationStyle
{
    BlueProtocol,
    SnowCat,
    RedTerminal,
    Messenger,
    AmberPanel
}

public sealed record SimulationTemplate(
    string Id,
    string Name,
    string Description,
    string Mark,
    Color Accent,
    SimulationStyle Style,
    string Mood,
    string Version);

public static class SimulationCatalog
{
    public static IReadOnlyList<SimulationTemplate> All { get; } =
    [
        new("blue-protocol", "Синий протокол", "Чистая синяя композиция, крупная типографика и диагностические строки.", ":( ", Color.FromArgb(25, 105, 190), SimulationStyle.BlueProtocol, "СИНИЙ ПРОТОКОЛ", "01"),
        new("snow-cat", "Снежный кот", "Тёмная зимняя оболочка, бирюзовые акценты и нарисованный талисман.", "CAT", Color.FromArgb(38, 123, 141), SimulationStyle.SnowCat, "ЗИМНИЙ АРХИВ", "02"),
        new("red-terminal", "Красный терминал", "Бордовый терминал, ASCII-иллюстрация и моноширинная графика.", ">_", Color.FromArgb(157, 35, 39), SimulationStyle.RedTerminal, "КРАСНЫЙ ТЕРМИНАЛ", "03"),
        new("quiet-chat", "Тихий чат", "Макет старого мессенджера с карточкой файла и короткими сообщениями.", "•••", Color.FromArgb(43, 105, 125), SimulationStyle.Messenger, "ТИХИЙ ЧАТ", "04"),
        new("amber-panel", "Янтарная панель", "Классическое окно середины 2010-х с янтарным индикатором.", "///", Color.FromArgb(174, 111, 38), SimulationStyle.AmberPanel, "ЯНТАРНАЯ ПАНЕЛЬ", "05")
    ];
}
