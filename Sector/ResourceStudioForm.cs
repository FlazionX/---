using System.Drawing.Text;
using System.Diagnostics;
using System.Text.Json;

namespace Sector;

public sealed record SectorPalette(string Name, string Background, string Foreground, string Accent);
public sealed record SectorPreset(string Name, string Description, string Background, string Foreground, string Accent, string FontFamily, int FontSize);
public sealed record SectorEffectPack(string Name, string Effect, string Color, int Intensity);

public static class ResourceLibrary
{
    public static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Sector");
    public static string Resources => Path.Combine(Root, "Resources");
    public static string Fonts => Path.Combine(Resources, "Fonts");
    public static string Plugins => Path.Combine(Resources, "Plugins");
    public static string Presets => Path.Combine(Resources, "Presets");
    public static string Palettes => Path.Combine(Resources, "Palettes");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static void EnsureFolders()
    {
        foreach (var path in new[] { Resources, Fonts, Plugins, Presets, Palettes })
            Directory.CreateDirectory(path);
    }

    public static void SavePalette(SectorPalette palette)
    {
        ValidateName(palette.Name);
        ValidateColor(palette.Background); ValidateColor(palette.Foreground); ValidateColor(palette.Accent);
        EnsureFolders();
        File.WriteAllText(Path.Combine(Palettes, SafeName(palette.Name) + ".json"), JsonSerializer.Serialize(palette, JsonOptions));
    }

    public static void SavePreset(SectorPreset preset)
    {
        ValidateName(preset.Name);
        ValidateColor(preset.Background); ValidateColor(preset.Foreground); ValidateColor(preset.Accent);
        if (preset.FontSize is < 6 or > 180) throw new InvalidDataException("Размер шрифта должен быть от 6 до 180.");
        EnsureFolders();
        File.WriteAllText(Path.Combine(Presets, SafeName(preset.Name) + ".json"), JsonSerializer.Serialize(preset, JsonOptions));
    }

    public static void SaveEffectPack(SectorEffectPack pack)
    {
        ValidateName(pack.Name); ValidateColor(pack.Color);
        if (pack.Intensity is < 0 or > 100) throw new InvalidDataException("Интенсивность должна быть от 0 до 100.");
        if (pack.Effect is not ("scanlines" or "glitch" or "vignette" or "noise"))
            throw new InvalidDataException("Неизвестный тип эффекта.");
        EnsureFolders();
        File.WriteAllText(Path.Combine(Plugins, SafeName(pack.Name) + ".sector-effect.json"), JsonSerializer.Serialize(pack, JsonOptions));
    }

    public static IReadOnlyList<T> LoadAll<T>(string folder, string pattern)
    {
        EnsureFolders();
        var values = new List<T>();
        foreach (var file in Directory.EnumerateFiles(folder, pattern).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var value = JsonSerializer.Deserialize<T>(File.ReadAllText(file));
                if (value is not null) values.Add(value);
            }
            catch { /* Ignore malformed user resource and keep the manager available. */ }
        }
        return values;
    }

    public static string SafeName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var result = new string(value.Trim().Select(c => invalid.Contains(c) ? '-' : c).ToArray());
        return string.IsNullOrWhiteSpace(result) ? "resource" : result;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 48) throw new InvalidDataException("Название должно содержать от 1 до 48 символов.");
    }

    private static void ValidateColor(string color)
    {
        if (color.Length != 7 || color[0] != '#' || !int.TryParse(color[1..], System.Globalization.NumberStyles.HexNumber, null, out _))
            throw new InvalidDataException("Цвет должен быть в формате #RRGGBB.");
    }
}

public sealed class ResourceStudioForm : Form
{
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
    private ListBox? _fontList;
    private readonly ListView _resources = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true };
    private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 30, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Color _bg = Color.FromArgb(20,23,28), _surface = Color.FromArgb(29,34,41), _text = Color.FromArgb(235,238,242), _orange = Color.FromArgb(235,133,43);

    public ResourceStudioForm()
    {
        ResourceLibrary.EnsureFolders();
        Text = "Сектор — Менеджер ресурсов";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(980, 680); MinimumSize = new Size(760, 520);
        BackColor = _bg; ForeColor = _text; Font = new Font("Segoe UI", 9f);
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(14), BackColor = _bg };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        Controls.Add(root); root.Controls.Add(_tabs, 0, 0); root.Controls.Add(_status, 0, 1);
        BuildResourcesTab(); BuildPresetsTab(); BuildPalettesTab(); BuildFontsTab(); BuildPluginsTab();
        RefreshResources();
    }

    private TabPage NewTab(string name)
    {
        var tab = new TabPage(name) { BackColor = _bg, ForeColor = _text, Padding = new Padding(12) };
        _tabs.TabPages.Add(tab); return tab;
    }

    private FlowLayoutPanel Toolbar(TabPage tab)
    {
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 46, WrapContents = false };
        tab.Controls.Add(bar); return bar;
    }

    private Button AddButton(Control parent, string title, Action action, bool primary = false)
    {
        var button = new Button { Text = title, AutoSize = true, Height = 32, FlatStyle = FlatStyle.Flat,
            BackColor = primary ? _orange : Color.FromArgb(42,49,58), ForeColor = primary ? Color.Black : _text,
            Padding = new Padding(10,2,10,2), Margin = new Padding(0,0,8,0), Cursor = Cursors.Hand };
        button.FlatAppearance.BorderSize = 0; button.Click += (_,_) => action(); parent.Controls.Add(button); return button;
    }

    private void BuildResourcesTab()
    {
        var tab = NewTab("Ресурсы");
        var bar = Toolbar(tab);
        AddButton(bar, "Импортировать файлы", ImportResources, true);
        AddButton(bar, "Открыть папку", () => Open(ResourceLibrary.Resources));
        _resources.Columns.Add("Название", 390); _resources.Columns.Add("Тип", 130); _resources.Columns.Add("Размер", 110);
        _resources.Dock = DockStyle.Fill; _resources.BackColor = _surface; _resources.ForeColor = _text; _resources.BorderStyle = BorderStyle.None;
        tab.Controls.Add(_resources);
        tab.Controls.SetChildIndex(_resources, 0);
    }

    private void BuildPresetsTab()
    {
        var tab = NewTab("Пресеты");
        var bar = Toolbar(tab);
        AddButton(bar, "Создать пресет", () =>
        {
            using var d = new PresetDialog();
            if (d.ShowDialog(this) != DialogResult.OK || d.Result is null) return;
            SetStatus("Пресет сохранён.");
        }, true);
        AddButton(bar, "Открыть папку", () => Open(ResourceLibrary.Presets));
        var list = new ListBox { Dock = DockStyle.Fill, BackColor = _surface, ForeColor = _text, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 11f) };
        list.Items.AddRange(ResourceLibrary.LoadAll<SectorPreset>(ResourceLibrary.Presets, "*.json").Select(p => $"{p.Name} — {p.Description}").ToArray());
        tab.Controls.Add(list); tab.Controls.SetChildIndex(list, 0);
    }

    private void BuildPalettesTab()
    {
        var tab = NewTab("Палитры");
        var bar = Toolbar(tab);
        AddButton(bar, "Сохранить палитру", () =>
        {
            using var d = new ColorDialog { FullOpen = true, Color = Color.FromArgb(235,133,43) };
            if (d.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                ResourceLibrary.SavePalette(new SectorPalette("Palette-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"), "#101820", "#EDF3F6", ColorTranslator.ToHtml(d.Color)));
                SetStatus("Палитра сохранена.");
                RefreshResources();
            }
            catch (Exception ex) { Error("Палитра", ex); }
        }, true);
        AddButton(bar, "Открыть папку", () => Open(ResourceLibrary.Palettes));
        var list = new ListBox { Dock = DockStyle.Fill, BackColor = _surface, ForeColor = _text, BorderStyle = BorderStyle.None };
        foreach (var p in ResourceLibrary.LoadAll<SectorPalette>(ResourceLibrary.Palettes, "*.json"))
            list.Items.Add($"{p.Name}     BG {p.Background}   TEXT {p.Foreground}   ACCENT {p.Accent}");
        tab.Controls.Add(list); tab.Controls.SetChildIndex(list, 0);
    }

    private void BuildFontsTab()
    {
        var tab = NewTab("Шрифты");
        var bar = Toolbar(tab);
        AddButton(bar, "Импортировать TTF / OTF", ImportFonts, true);
        AddButton(bar, "Открыть папку", () => Open(ResourceLibrary.Fonts));
        var list = _fontList = new ListBox { Dock = DockStyle.Fill, BackColor = _surface, ForeColor = _text, BorderStyle = BorderStyle.None };
        foreach (var f in Directory.EnumerateFiles(ResourceLibrary.Fonts).Where(x => Path.GetExtension(x).Equals(".ttf", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(x).Equals(".otf", StringComparison.OrdinalIgnoreCase)))
            list.Items.Add(Path.GetFileName(f));
        tab.Controls.Add(list); tab.Controls.SetChildIndex(list, 0);
        var hint = new Label { Dock = DockStyle.Bottom, Height = 46, ForeColor = Color.Silver, Text = "Шрифты хранятся внутри каталога Сектора и не устанавливаются в Windows автоматически." };
        tab.Controls.Add(hint);
    }

    private void BuildPluginsTab()
    {
        var tab = NewTab("Эффекты");
        var bar = Toolbar(tab);
        AddButton(bar, "Создать пакет эффектов", () =>
        {
            using var d = new EffectDialog();
            if (d.ShowDialog(this) != DialogResult.OK || d.Result is null) return;
            try { ResourceLibrary.SaveEffectPack(d.Result); SetStatus("Пакет эффектов сохранён."); RefreshResources(); }
            catch (Exception ex) { Error("Эффект", ex); }
        }, true);
        AddButton(bar, "Открыть папку", () => Open(ResourceLibrary.Plugins));
        var list = new ListBox { Dock = DockStyle.Fill, BackColor = _surface, ForeColor = _text, BorderStyle = BorderStyle.None };
        foreach (var p in ResourceLibrary.LoadAll<SectorEffectPack>(ResourceLibrary.Plugins, "*.sector-effect.json"))
            list.Items.Add($"{p.Name} · {p.Effect} · {p.Color} · {p.Intensity}%");
        tab.Controls.Add(list); tab.Controls.SetChildIndex(list, 0);
        var hint = new Label { Dock = DockStyle.Bottom, Height = 54, ForeColor = Color.Silver, Text = "Пакеты эффектов — безопасные JSON-описания (scanlines, glitch, vignette, noise). Они не исполняют DLL или BAT-код." };
        tab.Controls.Add(hint);
    }

    private void ImportResources()
    {
        using var d = new OpenFileDialog { Title = "Импортировать ресурсы", Filter = "Поддерживаемые ресурсы|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.wav;*.mp3;*.ogg;*.ttf;*.otf;*.json|Все файлы|*.*", Multiselect = true };
        if (d.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var imported = ImportService.ImportFiles(d.FileNames, ResourceLibrary.Resources);
            SetStatus($"Импортировано {imported.ImportedCount}, пропущено {imported.SkippedCount}.");
            RefreshResources();
        }
        catch (Exception ex) { Error("Импорт ресурсов", ex); }
    }

    private void ImportFonts()
    {
        using var d = new OpenFileDialog { Title = "Выберите шрифты", Filter = "Шрифты|*.ttf;*.otf", Multiselect = true };
        if (d.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var result = ImportService.ImportFiles(d.FileNames, ResourceLibrary.Fonts);
            SetStatus($"Импортировано шрифтов: {result.ImportedCount}. Они доступны в папке Fonts.");
            RefreshFontList();
        }
        catch (Exception ex) { Error("Импорт шрифтов", ex); }
    }

    private void RefreshFontList()
    {
        if (_fontList is null) return;
        _fontList.Items.Clear();
        foreach (var f in Directory.EnumerateFiles(ResourceLibrary.Fonts).Where(x => Path.GetExtension(x).Equals(".ttf", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(x).Equals(".otf", StringComparison.OrdinalIgnoreCase)))
            _fontList.Items.Add(Path.GetFileName(f));
    }

    private void RefreshResources()
    {
        _resources.Items.Clear();
        foreach (var file in Directory.EnumerateFiles(ResourceLibrary.Resources, "*", SearchOption.AllDirectories))
        {
            var info = new FileInfo(file); var item = new ListViewItem(Path.GetRelativePath(ResourceLibrary.Resources, file));
            item.SubItems.Add(info.Extension.ToUpperInvariant()); item.SubItems.Add(info.Length.ToString("N0") + " Б"); _resources.Items.Add(item);
        }
    }

    private void SetStatus(string value) => _status.Text = value;
    private void Error(string title, Exception ex) => MessageBox.Show(this, ex.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    private static void Open(string path) { Directory.CreateDirectory(path); Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true }); }
}

public sealed class PresetDialog : Form
{
    public SectorPreset? Result { get; private set; }
    private readonly TextBox _name = new() { Text = "Retro Blue" }, _description = new() { Text = "Синяя ретро-тема" }, _bg = new() { Text = "#101820" }, _fg = new() { Text = "#EDF3F6" }, _accent = new() { Text = "#EB852B" }, _font = new() { Text = "Consolas" };
    private readonly NumericUpDown _size = new() { Minimum = 6, Maximum = 180, Value = 24 };
    public PresetDialog()
    {
        Text = "Новый пресет"; Size = new Size(440,430); StartPosition = FormStartPosition.CenterParent; BackColor = Color.FromArgb(20,23,28); ForeColor = Color.White;
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 2, RowCount = 8 };
        Controls.Add(panel);
        Add("Название", _name); Add("Описание", _description); Add("Фон (#RRGGBB)", _bg); Add("Текст (#RRGGBB)", _fg); Add("Акцент (#RRGGBB)", _accent); Add("Шрифт", _font); Add("Размер", _size);
        var save = new Button { Text = "Сохранить", Dock = DockStyle.Fill, BackColor = Color.FromArgb(235,133,43) };
        save.Click += (_,_) => { Result = new SectorPreset(_name.Text, _description.Text, _bg.Text, _fg.Text, _accent.Text, _font.Text, (int)_size.Value); try { ResourceLibrary.SavePreset(Result); DialogResult = DialogResult.OK; Close(); } catch (Exception ex) { MessageBox.Show(this, ex.Message); } };
        panel.Controls.Add(save, 1, 7);
    }
    private void Add(string label, Control control)
    {
        var row = panelRow++; var l = new Label { Text = label, Dock = DockStyle.Fill, ForeColor = Color.Silver, TextAlign = ContentAlignment.MiddleLeft };
        control.Dock = DockStyle.Fill; panel.Controls.Add(l,0,row); panel.Controls.Add(control,1,row);
    }
    private int panelRow;
    private TableLayoutPanel panel => (TableLayoutPanel)Controls[0];
}

public sealed class EffectDialog : Form
{
    public SectorEffectPack? Result { get; private set; }
    public EffectDialog()
    {
        Text = "Новый пакет эффектов"; Size = new Size(420,280); StartPosition = FormStartPosition.CenterParent; BackColor = Color.FromArgb(20,23,28); ForeColor = Color.White;
        var name = new TextBox { Text = "My Effect" }; var effect = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        effect.Items.AddRange(["scanlines","glitch","vignette","noise"]); effect.SelectedIndex = 0;
        var color = new TextBox { Text = "#EB852B" }; var intensity = new NumericUpDown { Minimum = 0, Maximum = 100, Value = 50 };
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 2, RowCount = 5 }; Controls.Add(grid);
        void Add(string label, Control control, int row) { grid.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, ForeColor = Color.Silver, TextAlign = ContentAlignment.MiddleLeft },0,row); control.Dock = DockStyle.Fill; grid.Controls.Add(control,1,row); }
        Add("Название",name,0); Add("Эффект",effect,1); Add("Цвет",color,2); Add("Интенсивность",intensity,3);
        var save = new Button { Text = "Сохранить", Dock = DockStyle.Fill, BackColor = Color.FromArgb(235,133,43) };
        save.Click += (_,_) => { Result = new SectorEffectPack(name.Text,effect.Text,color.Text,(int)intensity.Value); try { ResourceLibrary.SaveEffectPack(Result); DialogResult=DialogResult.OK; Close(); } catch(Exception ex){MessageBox.Show(this,ex.Message);} };
        grid.Controls.Add(save,1,4);
    }
}

public static class FontLibrary
{
    private static readonly PrivateFontCollection Fonts = new();
    private static readonly HashSet<string> Loaded = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object Gate = new();

    public static Font CreateFont(string family, float size, FontStyle style = FontStyle.Regular)
    {
        LoadImportedFonts();
        lock (Gate)
        {
            var imported = Fonts.Families.FirstOrDefault(x => x.Name.Equals(family, StringComparison.OrdinalIgnoreCase));
            if (imported is not null) return new Font(imported, size, style, GraphicsUnit.Pixel);
        }
        try { return new Font(family, size, style, GraphicsUnit.Pixel); }
        catch { return new Font(FontFamily.GenericSansSerif, size, style, GraphicsUnit.Pixel); }
    }

    public static IReadOnlyList<string> GetFamilyNames()
    {
        LoadImportedFonts();
        lock (Gate) return Fonts.Families.Select(x => x.Name).ToArray();
    }

    private static void LoadImportedFonts()
    {
        ResourceLibrary.EnsureFolders();
        lock (Gate)
        {
            foreach (var file in Directory.EnumerateFiles(ResourceLibrary.Fonts).Where(x => x.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".otf", StringComparison.OrdinalIgnoreCase)))
            {
                if (!Loaded.Add(file)) continue;
                try { Fonts.AddFontFile(file); } catch { Loaded.Remove(file); }
            }
        }
    }
}
