using System.Drawing;
using System.Text.Json;

namespace Sector;

public sealed class EditorForm : Form
{
    private static readonly Color Bg = Color.FromArgb(20, 23, 28);
    private static readonly Color Surface = Color.FromArgb(29, 34, 41);
    private static readonly Color Surface2 = Color.FromArgb(37, 43, 51);
    private static readonly Color Orange = Color.FromArgb(235, 133, 43);
    private static readonly Color MainText = Color.FromArgb(235, 238, 242);
    private static readonly Color Muted = Color.FromArgb(151, 161, 174);

    private readonly TextBox _name = new();
    private readonly TextBox _description = new();
    private readonly TextBox _mark = new();
    private readonly TextBox _version = new();
    private readonly ComboBox _style = new();
    private readonly Panel _colorSwatch = new();
    private readonly Label _colorValue = new();
    private readonly Label _status = new();
    private Color _accent = Color.FromArgb(25, 105, 190);
    private string? _currentPath;

    public EditorForm()
    {
        Text = "Сектор — редактор проекта";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(900, 650);
        Size = new Size(1080, 740);
        BackColor = Bg;
        ForeColor = MainText;
        Font = new Font("Segoe UI", 9.5f);
        BuildUi();
        LoadTemplate(SimulationCatalog.All[0]);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Padding = new Padding(18), BackColor = Bg };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        Controls.Add(root);

        var heading = new Panel { Dock = DockStyle.Fill };
        var title = new Label { Text = "Редактор проекта", Dock = DockStyle.Top, Height = 34, Font = new Font("Segoe UI Semibold", 19f), ForeColor = MainText };
        var subtitle = new Label { Text = "Настройте внешний вид. Изменения видны в предпросмотре справа.", Dock = DockStyle.Fill, ForeColor = Muted };
        heading.Controls.Add(subtitle);
        heading.Controls.Add(title);
        root.Controls.Add(heading, 0, 0);

        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Bg };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        root.Controls.Add(body, 0, 1);

        var fields = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 12, Padding = new Padding(14), BackColor = Surface, AutoScroll = true };
        for (var i = 0; i < 6; i++) fields.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 2 ? 92 : 64));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.Controls.Add(fields, 0, 0);

        AddField(fields, "Название проекта", _name, 0, 40);
        AddField(fields, "Короткое описание", _description, 1, 120, multiline: true);
        AddField(fields, "Стиль основы", _style, 2);
        _style.DropDownStyle = ComboBoxStyle.DropDownList;
        _style.Items.AddRange(["Синий протокол", "Снежный кот", "Красный терминал", "Тихий чат", "Янтарная панель"]);
        _style.SelectedIndexChanged += (_, _) => UpdateStatus("Стиль изменён. Нажмите «Предпросмотр», чтобы посмотреть результат.");

        AddField(fields, "Значок / короткая метка", _mark, 3, 8);
        AddField(fields, "Версия проекта", _version, 4, 12);

        var colorPanel = new Panel { Dock = DockStyle.Fill, BackColor = Surface };
        var colorLabel = new Label { Text = "Акцентный цвет", Left = 0, Top = 2, Width = 150, ForeColor = Muted };
        _colorSwatch.SetBounds(0, 26, 48, 25);
        _colorSwatch.BackColor = _accent;
        _colorSwatch.BorderStyle = BorderStyle.FixedSingle;
        _colorValue.SetBounds(58, 28, 100, 22);
        _colorValue.ForeColor = MainText;
        _colorValue.Text = ColorTranslator.ToHtml(_accent);
        var chooseColor = MakeButton("Выбрать цвет", ChooseColor);
        chooseColor.SetBounds(165, 23, 125, 32);
        colorPanel.Controls.AddRange([colorLabel, _colorSwatch, _colorValue, chooseColor]);
        fields.Controls.Add(colorPanel, 0, 5);

        var tips = new Label
        {
            Dock = DockStyle.Fill, ForeColor = Muted,
            Text = "Подсказка\n• Название — до 40 символов.\n• Описание — до 120 символов.\n• Значок — короткий текст или символ.\n• Импортированные BAT и другие скрипты здесь не запускаются."
        };
        fields.Controls.Add(tips, 0, 6);

        var previewPanel = new Panel { Dock = DockStyle.Fill, BackColor = Surface, Padding = new Padding(12) };
        var previewTitle = new Label { Text = "ПРЕДПРОСМОТР", Dock = DockStyle.Top, Height = 30, ForeColor = Muted, Font = new Font("Segoe UI Semibold", 8.5f) };
        var preview = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(10, 14, 19) };
        preview.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(_accent);
            e.Graphics.FillRectangle(brush, 0, 0, preview.Width, 5);
            using var titleFont = new Font("Segoe UI Semibold", 19f);
            using var textBrush = new SolidBrush(Color.White);
            e.Graphics.DrawString(string.IsNullOrWhiteSpace(_name.Text) ? "Новый проект" : _name.Text, titleFont, textBrush, 24, 42);
            using var subFont = new Font("Segoe UI", 9.5f);
            using var mutedBrush = new SolidBrush(Color.FromArgb(174, 190, 201));
            e.Graphics.DrawString(_description.Text, subFont, mutedBrush, new RectangleF(24, 88, Math.Max(100, preview.Width - 48), 70));
            using var markFont = new Font("Consolas", 32f, FontStyle.Bold);
            e.Graphics.DrawString(string.IsNullOrWhiteSpace(_mark.Text) ? "SECTOR" : _mark.Text, markFont, brush, 24, 190);
            using var smallFont = new Font("Consolas", 9f);
            e.Graphics.DrawString("LOCAL PROJECT  /  " + _version.Text, smallFont, mutedBrush, 24, 255);
            using var tagFont = new Font("Segoe UI", 8f);
            e.Graphics.DrawString(_style.SelectedItem?.ToString() ?? "Синий протокол", tagFont, mutedBrush, 24, 285);
        };
        foreach (var input in new Control[] { _name, _description, _mark, _version })
            input.TextChanged += (_, _) => preview.Invalidate();
        _style.SelectedIndexChanged += (_, _) => preview.Invalidate();
        body.Controls.Add(previewPanel, 1, 0);
        previewPanel.Controls.Add(preview);
        previewPanel.Controls.Add(previewTitle);

        var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 12, 0, 0), BackColor = Bg };
        footer.Controls.Add(MakeButton("Новый", NewProject));
        footer.Controls.Add(MakeButton("Открыть", OpenProject));
        footer.Controls.Add(MakeButton("Сохранить", SaveProject, true));
        footer.Controls.Add(MakeButton("Предпросмотр", ShowPreview));
        footer.Controls.Add(MakeButton("Экспорт HTML + BAT", ExportProject));
        root.Controls.Add(footer, 0, 2);

        _status.Dock = DockStyle.Fill;
        _status.ForeColor = Muted;
        _status.Text = "Новый проект готов.";
        root.Controls.Add(_status, 0, 3);
    }

    private static void AddField(TableLayoutPanel parent, string label, TextBox box, int row, int maxLength, bool multiline = false)
    {
        box.MaxLength = maxLength;
        box.Dock = DockStyle.Bottom;
        box.BackColor = Surface2;
        box.ForeColor = MainText;
        box.BorderStyle = BorderStyle.FixedSingle;
        box.Font = new Font("Segoe UI", 10f);
        if (multiline) { box.Multiline = true; box.Height = 48; box.ScrollBars = ScrollBars.Vertical; }
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Surface };
        panel.Controls.Add(box);
        panel.Controls.Add(new Label { Text = label, Dock = DockStyle.Top, Height = 22, ForeColor = Muted });
        parent.Controls.Add(panel, 0, row);
    }

    private Button MakeButton(string text, Action action, bool primary = false)
    {
        var button = new Button
        {
            Text = text, AutoSize = true, Height = 34, MinimumSize = new Size(82, 32),
            FlatStyle = FlatStyle.Flat, BackColor = primary ? Orange : Surface2,
            ForeColor = primary ? Color.FromArgb(25, 25, 25) : MainText,
            Padding = new Padding(10, 2, 10, 2), Margin = new Padding(0, 0, 8, 0),
            Cursor = Cursors.Hand, Font = new Font("Segoe UI Semibold", 8.5f)
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) => action();
        return button;
    }

    private void ChooseColor()
    {
        using var dialog = new ColorDialog { Color = _accent, FullOpen = true, AnyColor = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _accent = dialog.Color;
        _colorSwatch.BackColor = _accent;
        _colorValue.Text = ColorTranslator.ToHtml(_accent);
        Invalidate(true);
    }

    private void NewProject()
    {
        if (!ConfirmDiscard()) return;
        _currentPath = null;
        LoadTemplate(SimulationCatalog.All[0]);
        UpdateStatus("Создан новый проект.");
    }

    private bool ConfirmDiscard()
    {
        if (MessageBox.Show(this, "Создать новый проект? Несохранённые изменения будут потеряны.",
            "Сектор", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return false;
        return true;
    }

    private void LoadTemplate(SimulationTemplate template)
    {
        _name.Text = template.Name;
        _description.Text = template.Description;
        _mark.Text = template.Mark.Trim();
        _version.Text = template.Version;
        _accent = template.Accent;
        _colorSwatch.BackColor = _accent;
        _colorValue.Text = ColorTranslator.ToHtml(_accent);
        _style.SelectedIndex = (int)template.Style;
    }

    private bool TryBuildTemplate(out SimulationTemplate template)
    {
        var name = _name.Text.Trim();
        var description = _description.Text.Trim();
        if (name.Length == 0 || description.Length == 0)
        {
            MessageBox.Show(this, "Заполните название и описание проекта.", "Не хватает данных", MessageBoxButtons.OK, MessageBoxIcon.Information);
            template = SimulationCatalog.All[0];
            return false;
        }
        if (_style.SelectedIndex < 0)
        {
            MessageBox.Show(this, "Выберите стиль основы.", "Не выбран стиль", MessageBoxButtons.OK, MessageBoxIcon.Information);
            template = SimulationCatalog.All[0];
            return false;
        }
        var style = (SimulationStyle)_style.SelectedIndex;
        var id = _currentPath is null ? "custom-" + Guid.NewGuid().ToString("N")[..8] : Path.GetFileNameWithoutExtension(_currentPath);
        template = new SimulationTemplate(id, name, description, _mark.Text.Trim(), _accent, style,
            name.ToUpperInvariant(), string.IsNullOrWhiteSpace(_version.Text) ? "1.0" : _version.Text.Trim());
        return true;
    }

    private void SaveProject()
    {
        if (!TryBuildTemplate(out var template)) return;
        if (!string.IsNullOrWhiteSpace(_currentPath))
        {
            try
            {
                ProjectFileService.Save(template, _currentPath);
                UpdateStatus("Проект сохранён: " + Path.GetFileName(_currentPath));
            }
            catch (Exception ex) { ShowError("Сохранение проекта", ex); }
            return;
        }
        using var dialog = new SaveFileDialog
        {
            Title = "Сохранить проект Сектора",
            Filter = "Проект Сектора (*.sector.json)|*.sector.json",
            FileName = MakeFileName(template.Name) + ".sector.json",
            AddExtension = false,
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            ProjectFileService.Save(template, dialog.FileName);
            _currentPath = dialog.FileName;
            UpdateStatus("Проект сохранён: " + Path.GetFileName(_currentPath));
        }
        catch (Exception ex) { ShowError("Сохранение проекта", ex); }
    }

    private void OpenProject()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Открыть проект Сектора",
            Filter = "Проекты Сектора (*.sector.json)|*.sector.json|Все файлы|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (!ConfirmDiscard()) return;
        try
        {
            var template = ProjectFileService.Load(dialog.FileName);
            _currentPath = dialog.FileName;
            LoadTemplate(template);
            UpdateStatus("Проект открыт: " + Path.GetFileName(_currentPath));
        }
        catch (Exception ex) { ShowError("Открытие проекта", ex); }
    }

    private void ShowPreview()
    {
        if (!TryBuildTemplate(out var template)) return;
        using var preview = new PreviewForm(template);
        preview.ShowDialog(this);
    }

    private void ExportProject()
    {
        if (!TryBuildTemplate(out var template)) return;
        using var dialog = new FolderBrowserDialog { Description = "Выберите папку для HTML-демонстрации" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var output = DemoExporter.Export(template, dialog.SelectedPath);
            MessageBox.Show(this, "Демо сохранено в папку:\n" + output + "\n\nBAT-файл только открывает HTML в браузере.",
                "Экспорт завершён", MessageBoxButtons.OK, MessageBoxIcon.Information);
            UpdateStatus("Экспортировано: " + output);
        }
        catch (Exception ex) { ShowError("Экспорт", ex); }
    }

    private void UpdateStatus(string message) => _status.Text = message;

    private void ShowError(string title, Exception ex)
    {
        UpdateStatus(title + ": " + ex.Message);
        MessageBox.Show(this, ex.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private static string MakeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(value.Select(c => invalid.Contains(c) ? '-' : c).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(safe) ? "Новый проект" : safe;
    }
}
