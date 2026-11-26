using System.Diagnostics;
using System.Drawing;

namespace Sector;

public sealed class MainForm : Form
{
    private readonly string _root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sector");
    private readonly FlowLayoutPanel _library = new();
    private readonly Label _status = new();

    private static readonly Color Back = Color.FromArgb(27, 29, 32);
    private static readonly Color Panel = Color.FromArgb(39, 42, 47);
    private static readonly Color Orange = Color.FromArgb(240, 138, 36);
    private static readonly Color TextMain = Color.FromArgb(238, 239, 241);
    private static readonly Color TextMuted = Color.FromArgb(160, 164, 170);

    public MainForm()
    {
        Text = "Сектор";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 600);
        Size = new Size(1100, 720);
        BackColor = Back;
        ForeColor = TextMain;
        Font = new Font("Segoe UI", 10F);

        Directory.CreateDirectory(Path.Combine(_root, "Imports"));
        Directory.CreateDirectory(Path.Combine(_root, "Projects"));
        BuildInterface();
        RefreshLibrary();
    }

    private void BuildInterface()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = Back };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 5));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var stripe = new Panel { Dock = DockStyle.Fill, BackColor = Orange };
        root.Controls.Add(stripe, 0, 0);

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Back, Padding = new Padding(24, 12, 24, 8) };
        var brand = new Label { Text = "Сектор", Dock = DockStyle.Left, Width = 220, ForeColor = TextMain, Font = new Font("Georgia", 23F, FontStyle.Regular), TextAlign = ContentAlignment.MiddleLeft };
        var subtitle = new Label { Text = "VIRTUAL ENVIRONMENT  ·  2015 EDITION", Dock = DockStyle.Right, Width = 310, ForeColor = TextMuted, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 8.5F) };
        header.Controls.Add(brand);
        header.Controls.Add(subtitle);
        root.Controls.Add(header, 0, 1);

        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(18, 8, 18, 18), BackColor = Back };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.Controls.Add(body, 0, 2);

        var nav = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Panel, Padding = new Padding(10) };
        body.Controls.Add(nav, 0, 0);
        AddNav(nav, "Главная", true);
        AddNav(nav, "Библиотека", false);
        AddNav(nav, "Импорт файлов", false, ImportFiles);
        AddNav(nav, "Виртуальная среда", false, ShowVirtualSpace);
        AddNav(nav, "История", false, () => SetStatus("История запусков пока пуста."));

        var content = new Panel { Dock = DockStyle.Fill, Padding = new Padding(22, 8, 8, 8), BackColor = Back };
        body.Controls.Add(content, 1, 0);
        var title = new Label { Text = "Библиотека проектов", Dock = DockStyle.Top, Height = 42, ForeColor = TextMain, Font = new Font("Segoe UI Semibold", 17F) };
        content.Controls.Add(title);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, FlowDirection = FlowDirection.LeftToRight };
        var import = MakeButton("Добавить файлы", Orange, Color.Black, ImportFiles);
        var preview = MakeButton("Предпросмотр Blue Screen", Panel, TextMain, ShowBlueScreen);
        actions.Controls.Add(import);
        actions.Controls.Add(preview);
        content.Controls.Add(actions);
        _library.Dock = DockStyle.Fill;
        _library.AutoScroll = true;
        _library.WrapContents = true;
        _library.Padding = new Padding(0, 12, 0, 0);
        content.Controls.Add(_library);
        _library.BringToFront();

        _status.Dock = DockStyle.Bottom;
        _status.Height = 28;
        _status.ForeColor = TextMuted;
        _status.Text = "Готово · Импортированные сценарии хранятся как файлы и не выполняются программой.";
        content.Controls.Add(_status);
    }

    private void AddNav(FlowLayoutPanel nav, string text, bool selected, Action? action = null)
    {
        var button = new Button { Text = text, Width = 166, Height = 42, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0), BackColor = selected ? Color.FromArgb(58, 49, 40) : Panel, ForeColor = selected ? Orange : TextMain, Font = new Font("Segoe UI", 9.5F), Cursor = Cursors.Hand };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) => action?.Invoke();
        nav.Controls.Add(button);
    }

    private Button MakeButton(string text, Color back, Color fore, Action action)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 34, FlatStyle = FlatStyle.Flat, BackColor = back, ForeColor = fore, Padding = new Padding(12, 2, 12, 2), Margin = new Padding(0, 0, 10, 8), Cursor = Cursors.Hand };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) => action();
        return button;
    }

    private void RefreshLibrary()
    {
        _library.Controls.Clear();
        AddProjectCard("Blue Screen", "Синяя экранная симуляция", "Встроенный шаблон", ShowBlueScreen);
        AddProjectCard("Импортированные файлы", "Ваши BAT, PS1, PY, CS и CPP файлы", "Открыть папку", OpenImports);
    }

    private void AddProjectCard(string name, string description, string tag, Action action)
    {
        var card = new Panel { Width = 310, Height = 142, BackColor = Panel, Margin = new Padding(0, 0, 14, 14), Padding = new Padding(14) };
        var marker = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = Orange };
        card.Controls.Add(marker);
        var heading = new Label { Text = name, Left = 16, Top = 12, Width = 260, Height = 28, ForeColor = TextMain, Font = new Font("Segoe UI Semibold", 12F) };
        var desc = new Label { Text = description, Left = 16, Top = 44, Width = 270, Height = 36, ForeColor = TextMuted, Font = new Font("Segoe UI", 8.5F) };
        var button = MakeButton(tag, Color.FromArgb(58, 61, 66), TextMain, action);
        button.Left = 16; button.Top = 91;
        card.Controls.Add(heading); card.Controls.Add(desc); card.Controls.Add(button);
        _library.Controls.Add(card);
    }

    private void ImportFiles()
    {
        using var dialog = new OpenFileDialog { Title = "Импорт файлов в библиотеку Сектора", Filter = "Файлы проектов и ресурсов|*.bat;*.ps1;*.py;*.cs;*.cpp;*.h;*.png;*.jpg;*.jpeg;*.ico;*.wav;*.mp3;*.zip|Все файлы|*.*", Multiselect = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var destination = Path.Combine(_root, "Imports");
        var count = 0;
        foreach (var file in dialog.FileNames)
        {
            var target = Path.Combine(destination, Path.GetFileName(file));
            if (File.Exists(target)) target = Path.Combine(destination, $"{Path.GetFileNameWithoutExtension(file)}_{DateTime.Now:yyyyMMdd_HHmmssfff}{Path.GetExtension(file)}");
            File.Copy(file, target);
            count++;
        }
        SetStatus($"Импортировано файлов: {count}. Сценарии не запускаются автоматически.");
        RefreshLibrary();
    }

    private void OpenImports()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Imports"));
        Process.Start(new ProcessStartInfo("explorer.exe", Path.Combine(_root, "Imports")) { UseShellExecute = true });
    }

    private void ShowVirtualSpace() => MessageBox.Show(this, "Виртуальная область — это внутреннее окно предпросмотра. Для запуска неизвестных файлов используйте отдельную виртуальную машину.", "Виртуальная среда", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void ShowBlueScreen()
    {
        using var preview = new Form { Text = "Blue Screen · Предпросмотр", StartPosition = FormStartPosition.CenterParent, Size = new Size(820, 520), MinimumSize = new Size(640, 420), BackColor = Color.FromArgb(0, 74, 150), ForeColor = Color.White, KeyPreview = true };
        var text = new Label { Dock = DockStyle.Fill, Padding = new Padding(64, 48, 64, 30), ForeColor = Color.White, Font = new Font("Segoe UI", 15F), Text = "\u2639\n\nВозникла проблема (демонстрация).\n\nЭто безопасный макет синего экрана в Секторе. Windows не затронута.\n\nКод демонстрации: SECTOR-2015\n\nНажмите Esc или закройте это окно, чтобы выйти.", TextAlign = ContentAlignment.TopLeft };
        preview.Controls.Add(text);
        preview.KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) preview.Close(); };
        preview.ShowDialog(this);
    }

    private void SetStatus(string message) => _status.Text = message;
}
