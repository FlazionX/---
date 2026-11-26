using System.Diagnostics;
using System.Drawing;
using System.Text.Json;

namespace Sector;

public sealed class MainForm : Form
{
    private readonly string _dataRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sector");
    private readonly string _importsRoot;
    private readonly string _historyPath;
    private readonly Panel _content = new();
    private readonly Label _status = new();
    private readonly List<HistoryEntry> _history = new();

    private static readonly Color Bg = Color.FromArgb(20, 23, 28);
    private static readonly Color Surface = Color.FromArgb(29, 34, 41);
    private static readonly Color Surface2 = Color.FromArgb(37, 43, 51);
    private static readonly Color Orange = Color.FromArgb(235, 133, 43);
    private static readonly Color MainText = Color.FromArgb(235, 238, 242);
    private static readonly Color Muted = Color.FromArgb(151, 161, 174);

    public MainForm()
    {
        _importsRoot = Path.Combine(_dataRoot, "Imports");
        _historyPath = Path.Combine(_dataRoot, "history.json");
        Directory.CreateDirectory(_importsRoot);
        Directory.CreateDirectory(Path.Combine(_dataRoot, "Exports"));
        LoadHistory();

        Text = "Сектор";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(980, 660);
        Size = new Size(1180, 780);
        BackColor = Bg;
        ForeColor = MainText;
        Font = new Font("Segoe UI", 9.5f);
        AllowDrop = true;
        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;

        BuildShell();
        ShowLibrary();
    }

    private void BuildShell()
    {
        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = Bg };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 5));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        Controls.Add(shell);

        shell.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = Orange }, 0, 0);

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Bg, Padding = new Padding(24, 8, 24, 8) };
        var brand = new Label
        {
            Text = "Сектор", Dock = DockStyle.Left, Width = 260,
            ForeColor = MainText, Font = new Font("Georgia", 24f, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft
        };
        var edition = new Label
        {
            Text = "VIRTUAL STUDIO     /     2015–2016", Dock = DockStyle.Right, Width = 310,
            ForeColor = Muted, Font = new Font("Segoe UI", 8.5f),
            TextAlign = ContentAlignment.MiddleRight
        };
        header.Controls.Add(brand);
        header.Controls.Add(edition);
        shell.Controls.Add(header, 0, 1);

        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(18, 4, 18, 10), BackColor = Bg };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.Controls.Add(body, 0, 2);

        var sidebar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            BackColor = Surface, Padding = new Padding(10), AutoScroll = true
        };
        body.Controls.Add(sidebar, 0, 0);
        AddNav(sidebar, "Библиотека", ShowLibrary, true);
        AddNav(sidebar, "Мои файлы", ShowFiles);
        AddNav(sidebar, "Виртуальная среда", ShowEnvironment);
        AddNav(sidebar, "История", ShowHistory);
        AddNav(sidebar, "Настройки", ShowSettings);

        _content.Dock = DockStyle.Fill;
        _content.BackColor = Bg;
        _content.Padding = new Padding(22, 4, 4, 4);
        body.Controls.Add(_content, 1, 0);

        var footer = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(16, 19, 23), Padding = new Padding(20, 4, 20, 4) };
        _status.Dock = DockStyle.Fill;
        _status.ForeColor = Muted;
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _status.Text = "Готово  ·  Перетащите сюда файлы для безопасного импорта";
        footer.Controls.Add(_status);
        shell.Controls.Add(footer, 0, 3);
    }

    private void AddNav(FlowLayoutPanel nav, string label, Action action, bool selected = false)
    {
        var button = new Button
        {
            Text = label, Width = 176, Height = 42, Margin = new Padding(0, 0, 0, 7),
            FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(13, 0, 0, 0), BackColor = selected ? Color.FromArgb(61, 49, 38) : Surface,
            ForeColor = selected ? Orange : MainText, Font = new Font("Segoe UI", 9.5f),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Surface2;
        button.Click += (_, _) => action();
        nav.Controls.Add(button);
    }

    private void SetPage(Control page)
    {
        _content.SuspendLayout();
        _content.Controls.Clear();
        page.Dock = DockStyle.Fill;
        _content.Controls.Add(page);
        _content.ResumeLayout();
    }

    private static TableLayoutPanel CreatePageLayout()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Bg, Margin = Padding.Empty };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        return layout;
    }

    private Panel CreatePageHeader(string title, string description)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Bg };
        var titleLabel = new Label
        {
            Text = title, Dock = DockStyle.Top, Height = 38, ForeColor = MainText,
            Font = new Font("Segoe UI Semibold", 18f)
        };
        var subtitle = new Label
        {
            Text = description, Dock = DockStyle.Fill, ForeColor = Muted,
            Font = new Font("Segoe UI", 9f), TextAlign = ContentAlignment.TopLeft
        };
        panel.Controls.Add(subtitle);
        panel.Controls.Add(titleLabel);
        return panel;
    }

    private FlowLayoutPanel CreateActionBar() => new()
    {
        Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
        WrapContents = false, BackColor = Bg, Padding = new Padding(0, 5, 0, 4)
    };

    private Button MakeButton(string text, Action action, bool primary = false)
    {
        var button = new Button
        {
            Text = text, AutoSize = true, Height = 34, MinimumSize = new Size(100, 32),
            FlatStyle = FlatStyle.Flat, BackColor = primary ? Orange : Surface2,
            ForeColor = primary ? Color.FromArgb(25, 25, 25) : MainText,
            Padding = new Padding(12, 2, 12, 2), Margin = new Padding(0, 0, 9, 0),
            Cursor = Cursors.Hand, Font = new Font("Segoe UI Semibold", 8.8f)
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) => action();
        return button;
    }

    private void ShowLibrary()
    {
        var page = CreatePageLayout();
        page.Controls.Add(CreatePageHeader("Библиотека симуляций", "Пять самостоятельных визуальных тем. Общие эффекты — разные характеры и оформление."), 0, 0);
        var actions = CreateActionBar();
        actions.Controls.Add(MakeButton("Импортировать файлы", ImportFiles, true));
        actions.Controls.Add(MakeButton("Импортировать папку", ImportFolder));
        actions.Controls.Add(MakeButton("Открыть каталог", OpenImports));
        page.Controls.Add(actions, 0, 1);

        var cards = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight, BackColor = Bg, Padding = new Padding(0, 8, 8, 8)
        };
        foreach (var template in SimulationCatalog.All)
            cards.Controls.Add(CreateTemplateCard(template));
        page.Controls.Add(cards, 0, 2);
        SetPage(page);
    }

    private Control CreateTemplateCard(SimulationTemplate template)
    {
        var card = new Panel
        {
            Width = 275, Height = 224, BackColor = Surface,
            Margin = new Padding(0, 0, 14, 14), Padding = new Padding(13)
        };
        var preview = new Panel { Dock = DockStyle.Top, Height = 82, BackColor = template.Accent };
        preview.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(Color.FromArgb(35, Color.White));
            e.Graphics.FillEllipse(brush, preview.Width - 74, -30, 120, 120);
            using var font = new Font("Segoe UI Semibold", 22f);
            using var textBrush = new SolidBrush(Color.White);
            e.Graphics.DrawString(template.Mark, font, textBrush, 16, 20);
        };
        var title = new Label
        {
            Text = template.Name, Left = 0, Top = 94, Width = 245, Height = 27,
            ForeColor = MainText, Font = new Font("Segoe UI Semibold", 12f)
        };
        var description = new Label
        {
            Text = template.Description, Left = 0, Top = 122, Width = 245, Height = 42,
            ForeColor = Muted, Font = new Font("Segoe UI", 8.5f)
        };
        var open = MakeButton("Открыть предпросмотр", () => OpenTemplate(template), true);
        open.Left = 0;
        open.Top = 174;
        card.Controls.Add(open);
        card.Controls.Add(description);
        card.Controls.Add(title);
        card.Controls.Add(preview);
        return card;
    }

    private void ShowFiles()
    {
        var page = CreatePageLayout();
        page.Controls.Add(CreatePageHeader("Мои файлы", "Импортированные сценарии и ресурсы хранятся локально. Сектор не запускает их."), 0, 0);
        var actions = CreateActionBar();
        actions.Controls.Add(MakeButton("Добавить файлы", ImportFiles, true));
        actions.Controls.Add(MakeButton("Добавить папку", ImportFolder));
        actions.Controls.Add(MakeButton("Открыть папку", OpenImports));
        page.Controls.Add(actions, 0, 1);

        var list = new ListView
        {
            Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true,
            BackColor = Surface, ForeColor = MainText, BorderStyle = BorderStyle.None
        };
        list.Columns.Add("Имя файла", 330);
        list.Columns.Add("Тип", 110);
        list.Columns.Add("Размер", 110);
        list.Columns.Add("Изменён", 170);
        try
        {
            foreach (var file in Directory.EnumerateFiles(_importsRoot).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
            {
                var info = new FileInfo(file);
                var item = new ListViewItem(info.Name);
                item.SubItems.Add(info.Extension.ToUpperInvariant());
                item.SubItems.Add(FormatSize(info.Length));
                item.SubItems.Add(info.LastWriteTime.ToString("dd.MM.yyyy HH:mm"));
                list.Items.Add(item);
            }
        }
        catch (Exception ex) { SetStatus("Не удалось прочитать каталог: " + ex.Message); }
        page.Controls.Add(list, 0, 2);
        SetPage(page);
    }

    private void ShowEnvironment()
    {
        var page = CreatePageLayout();
        page.Controls.Add(CreatePageHeader("Виртуальная среда", "Изолированная область предпросмотра визуальных макетов — не виртуальная машина Windows."), 0, 0);
        var actions = CreateActionBar();
        actions.Controls.Add(MakeButton("Открыть Blue Screen", () => OpenTemplate(SimulationCatalog.All[0]), true));
        actions.Controls.Add(MakeButton("Открыть Cat / Snow", () => OpenTemplate(SimulationCatalog.All[1])));
        page.Controls.Add(actions, 0, 1);

        var info = new Panel { Dock = DockStyle.Fill, BackColor = Surface, Padding = new Padding(22) };
        var heading = new Label
        {
            Text = "Безопасная демонстрационная область", Dock = DockStyle.Top, Height = 36,
            Font = new Font("Segoe UI Semibold", 14f), ForeColor = MainText
        };
        var body = new Label
        {
            Text = "• Все эффекты рисуются только внутри отдельного окна предпросмотра.\n\n" +
                   "• Окно можно закрыть кнопкой «Выход», клавишей Esc или обычным крестиком.\n\n" +
                   "• Импортированные BAT / PS1 / Python / C# / C++ файлы не исполняются.\n\n" +
                   "• Для проверки неизвестных программ используйте настоящую виртуальную машину с контрольной точкой.\n\n" +
                   "• Здесь нет шифрования, автозапуска, отключения диспетчера задач или блокировки рабочего стола.",
            Dock = DockStyle.Fill, ForeColor = Muted, Font = new Font("Segoe UI", 10.5f)
        };
        info.Controls.Add(body);
        info.Controls.Add(heading);
        page.Controls.Add(info, 0, 2);
        SetPage(page);
    }

    private void ShowHistory()
    {
        var page = CreatePageLayout();
        page.Controls.Add(CreatePageHeader("История", "Последние открытия шаблонов и экспортированные демонстрации."), 0, 0);
        page.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = Bg }, 0, 1);
        var list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, BackColor = Surface, ForeColor = MainText, BorderStyle = BorderStyle.None };
        list.Columns.Add("Дата", 180);
        list.Columns.Add("Действие", 180);
        list.Columns.Add("Проект", 280);
        foreach (var entry in _history.OrderByDescending(x => x.Timestamp).Take(100))
        {
            var item = new ListViewItem(entry.Timestamp.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss"));
            item.SubItems.Add(entry.Action);
            item.SubItems.Add(entry.Template);
            list.Items.Add(item);
        }
        page.Controls.Add(list, 0, 2);
        SetPage(page);
    }

    private void ShowSettings()
    {
        var page = CreatePageLayout();
        page.Controls.Add(CreatePageHeader("Настройки", "Локальные каталоги и сведения о сборке."), 0, 0);
        var actions = CreateActionBar();
        actions.Controls.Add(MakeButton("Открыть данные Сектора", OpenDataFolder, true));
        actions.Controls.Add(MakeButton("Очистить историю", ClearHistory));
        page.Controls.Add(actions, 0, 1);
        var info = new Label
        {
            Dock = DockStyle.Fill, ForeColor = Muted, Font = new Font("Segoe UI", 10f),
            Text = "Версия: 0.2.0\nПлатформа: Windows / .NET 8 WinForms\n\nКаталог данных:\n" + _dataRoot +
                   "\n\nИмпортируемые скрипты хранятся как обычные файлы и не запускаются приложением."
        };
        page.Controls.Add(info, 0, 2);
        SetPage(page);
    }

    private void OpenTemplate(SimulationTemplate template)
    {
        using var preview = new PreviewForm(template);
        preview.ShowDialog(this);
        AddHistory(template.Name, "Предпросмотр");
    }

    private void ImportFiles()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Импортировать файлы в Сектор",
            Filter = "Все файлы|*.*",
            Multiselect = true,
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        ImportPaths(dialog.FileNames);
    }

    private void ImportFolder()
    {
        using var dialog = new FolderBrowserDialog { Description = "Выберите папку с проектом или ресурсами" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var result = ImportService.ImportDirectory(dialog.SelectedPath, _importsRoot);
            SetStatus($"Импортировано: {result.ImportedCount} файлов · Пропущено: {result.SkippedCount}");
            ShowFiles();
        }
        catch (Exception ex) { ShowError("Импорт папки", ex); }
    }

    private void ImportPaths(IEnumerable<string> paths)
    {
        try
        {
            var result = ImportService.ImportFiles(paths, _importsRoot);
            SetStatus($"Импортировано: {result.ImportedCount} файлов · Пропущено: {result.SkippedCount}");
            ShowFiles();
        }
        catch (Exception ex) { ShowError("Импорт файлов", ex); }
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true) e.Effect = DragDropEffects.Copy;
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths) ImportPaths(paths);
    }

    private void OpenImports() => OpenFolder(_importsRoot);
    private void OpenDataFolder() => OpenFolder(_dataRoot);

    private static void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
    }

    private void AddHistory(string name, string action)
    {
        _history.Add(new HistoryEntry(DateTime.UtcNow, name, action));
        try { File.WriteAllText(_historyPath, JsonSerializer.Serialize(_history, new JsonSerializerOptions { WriteIndented = true })); }
        catch (Exception ex) { SetStatus("Не удалось сохранить историю: " + ex.Message); }
    }

    private void LoadHistory()
    {
        try
        {
            if (File.Exists(_historyPath))
                _history.AddRange(JsonSerializer.Deserialize<List<HistoryEntry>>(File.ReadAllText(_historyPath)) ?? []);
        }
        catch { _history.Clear(); }
    }

    private void ClearHistory()
    {
        _history.Clear();
        try { if (File.Exists(_historyPath)) File.Delete(_historyPath); }
        catch (Exception ex) { ShowError("Очистка истории", ex); }
        ShowHistory();
        SetStatus("История очищена.");
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} Б",
        < 1024 * 1024 => $"{bytes / 1024d:0.0} КБ",
        _ => $"{bytes / 1024d / 1024d:0.0} МБ"
    };

    private void ShowError(string title, Exception ex)
    {
        SetStatus(title + ": " + ex.Message);
        MessageBox.Show(this, ex.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}

public sealed record HistoryEntry(DateTime Timestamp, string Template, string Action);
