using System.Drawing;

namespace Sector;

public enum SimulationStyle
{
    BlueProtocol,
    SnowCat,
    RedTerminal,
    Messenger,
    AmberPanel,
    GreenMatrix,
    VioletGlitch,
    GreyDialog,
    PixelAlert,
    DarkArchive
}

public sealed record SimulationTemplate(
    string Id,
    string Name,
    string Description,
    string Mark,
    Color Accent,
    SimulationStyle Style,
    string Mood,
    string Version,
    string Narration = "Это безопасная визуальная демонстрация. Операционная система и ваши файлы не затрагиваются.");

public static class SimulationCatalog
{
    public static IReadOnlyList<SimulationTemplate> All { get; } =
    [
        new("blue-protocol", "Синий протокол", "Чистая синяя композиция, крупная типографика и диагностические строки.", ":( ", Color.FromArgb(25, 105, 190), SimulationStyle.BlueProtocol, "СИНИЙ ПРОТОКОЛ", "01"),
        new("snow-cat", "Снежный кот", "Тёмная зимняя оболочка, бирюзовые акценты и нарисованный талисман.", "CAT", Color.FromArgb(38, 123, 141), SimulationStyle.SnowCat, "ЗИМНИЙ АРХИВ", "02"),
        new("red-terminal", "Красный терминал", "Бордовый терминал, ASCII-иллюстрация и моноширинная графика.", ">_", Color.FromArgb(157, 35, 39), SimulationStyle.RedTerminal, "КРАСНЫЙ ТЕРМИНАЛ", "03"),
        new("quiet-chat", "Тихий чат", "Макет старого мессенджера с карточкой файла и короткими сообщениями.", "•••", Color.FromArgb(43, 105, 125), SimulationStyle.Messenger, "ТИХИЙ ЧАТ", "04"),
        new("amber-panel", "Янтарная панель", "Классическое окно середины 2010-х с янтарным индикатором.", "///", Color.FromArgb(174, 111, 38), SimulationStyle.AmberPanel, "ЯНТАРНАЯ ПАНЕЛЬ", "05"),
        new("green-matrix", "Зелёный протокол", "Тёмный терминал с зелёными строками, курсором и ретро-диагностикой.", "0101", Color.FromArgb(42, 164, 91), SimulationStyle.GreenMatrix, "GREEN PROTOCOL", "06", "Зелёный протокол. Локальная демонстрация запущена. Все системные функции остаются доступными."),
        new("violet-glitch", "Фиолетовый сбой", "Фиолетовая геометрия, мягкие помехи и цифровая типографика.", "GL!", Color.FromArgb(135, 86, 190), SimulationStyle.VioletGlitch, "VIOLET GLITCH", "07", "Фиолетовый сбой. Это только визуальная сцена. Никаких изменений в Windows не производится."),
        new("grey-dialog", "Старый диалог", "Серое окно в духе классических приложений Windows середины десятых.", "OK", Color.FromArgb(115, 126, 139), SimulationStyle.GreyDialog, "CLASSIC DIALOG", "08", "Старый диалог. Макет классического окна. Вы можете закрыть предпросмотр клавишей Escape."),
        new("pixel-alert", "Pixel Alert", "Пиксельный экран предупреждения с крупными блоками и ретро-шрифтом.", "8BIT", Color.FromArgb(197, 79, 69), SimulationStyle.PixelAlert, "PIXEL ALERT", "09", "Pixel Alert. Пиксельная визуальная демонстрация. Компьютер продолжает работать нормально."),
        new("dark-archive", "Тёмный архив", "Чёрно-синяя панель, архивные метки и спокойная анимация загрузки.", "ARCH", Color.FromArgb(54, 111, 145), SimulationStyle.DarkArchive, "DARK ARCHIVE", "10", "Тёмный архив. Демонстрационные материалы загружены локально. Ваши данные не меняются.")
    ];
}
