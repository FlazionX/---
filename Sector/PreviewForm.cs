using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Diagnostics;

namespace Sector;

public sealed class PreviewForm : Form
{
    private readonly SimulationTemplate _template;
    private readonly SimulationCanvas _canvas;
    private readonly Label _clock = new();

    public PreviewForm(SimulationTemplate template)
    {
        _template = template;
        Text = $"Сектор · {template.Name} — безопасный предпросмотр";
        StartPosition = FormStartPosition.CenterParent;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(760, 520);
        BackColor = Color.FromArgb(18, 21, 26);
        ForeColor = Color.White;
        KeyPreview = true;
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = BackColor };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        Controls.Add(root);

        var top = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(25, 29, 35), Padding = new Padding(20, 8, 20, 8) };
        var title = new Label
        {
            Text = $"СЕКТОР   /   {_template.Name.ToUpperInvariant()}",
            Dock = DockStyle.Left, Width = 420, ForeColor = Color.FromArgb(237, 239, 243),
            Font = new Font("Segoe UI Semibold", 10.5f), TextAlign = ContentAlignment.MiddleLeft
        };
        _clock.Dock = DockStyle.Right;
        _clock.Width = 300;
        _clock.TextAlign = ContentAlignment.MiddleRight;
        _clock.ForeColor = Color.FromArgb(153, 164, 178);
        _clock.Font = new Font("Segoe UI", 8.5f);
        _clock.Text = "ЛОКАЛЬНАЯ ДЕМОНСТРАЦИЯ  ·  ESC — ВЫХОД";
        top.Controls.Add(_clock);
        top.Controls.Add(title);
        root.Controls.Add(top, 0, 0);

        _canvas = new SimulationCanvas(template) { Dock = DockStyle.Fill };
        root.Controls.Add(_canvas, 0, 1);

        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
            BackColor = Color.FromArgb(25, 29, 35), Padding = new Padding(18, 7, 18, 4)
        };
        bottom.Controls.Add(MakeButton("Повторить анимацию", () => _canvas.ResetAnimation(), false));
        bottom.Controls.Add(MakeButton("Экспорт HTML + BAT", ExportDemo, true));
        bottom.Controls.Add(MakeButton("Выход", Close, false));
        root.Controls.Add(bottom, 0, 2);
    }

    private Button MakeButton(string text, Action action, bool primary)
    {
        var button = new Button
        {
            Text = text, AutoSize = true, Height = 32, FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Color.FromArgb(235, 133, 43) : Color.FromArgb(49, 56, 65),
            ForeColor = primary ? Color.FromArgb(24, 25, 26) : Color.White,
            Padding = new Padding(12, 2, 12, 2), Margin = new Padding(0, 0, 9, 0),
            Font = new Font("Segoe UI Semibold", 8.5f), Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) => action();
        return button;
    }

    private void ExportDemo()
    {
        using var dialog = new FolderBrowserDialog { Description = "Выберите папку для безопасной HTML-демонстрации" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var output = DemoExporter.Export(_template, dialog.SelectedPath);
            MessageBox.Show(this,
                $"Демо-пакет сохранён:\n{output}\n\nЗапуск BAT-файла только откроет HTML-макет в браузере. Никаких системных настроек он не меняет.",
                "Экспорт завершён", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Process.Start(new ProcessStartInfo("explorer.exe", output) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Ошибка экспорта", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}

internal sealed class SimulationCanvas : Panel
{
    private readonly SimulationTemplate _template;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 55 };
    private int _phase;

    public SimulationCanvas(SimulationTemplate template)
    {
        _template = template;
        DoubleBuffered = true;
        ResizeRedraw = true;
        _timer.Tick += (_, _) =>
        {
            _phase = (_phase + 1) % 100;
            Invalidate();
        };
        _timer.Start();
        Disposed += (_, _) => _timer.Dispose();
    }

    public void ResetAnimation()
    {
        _phase = 0;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(Color.FromArgb(10, 14, 19));

        switch (_template.Style)
        {
            case SimulationStyle.BlueProtocol: DrawBlue(g); break;
            case SimulationStyle.SnowCat: DrawSnowCat(g); break;
            case SimulationStyle.RedTerminal: DrawRedTerminal(g); break;
            case SimulationStyle.Messenger: DrawMessenger(g); break;
            case SimulationStyle.AmberPanel: DrawAmber(g); break;
        }
        DrawDemoWatermark(g);
    }

    private void DrawDemoWatermark(Graphics g)
    {
        using var font = new Font("Segoe UI Semibold", 8f);
        using var brush = new SolidBrush(Color.FromArgb(170, 255, 255, 255));
        g.DrawString("СЕКТОР  /  ВИЗУАЛЬНАЯ СИМУЛЯЦИЯ", font, brush, 22, Height - 30);
    }

    private void DrawBlue(Graphics g)
    {
        g.Clear(Color.FromArgb(0, 91, 166));
        using var white = new SolidBrush(Color.White);
        using var soft = new SolidBrush(Color.FromArgb(222, 239, 252));
        using var big = new Font("Segoe UI Light", 68f);
        using var title = new Font("Segoe UI Light", 27f);
        using var body = new Font("Segoe UI", 13f);
        using var small = new Font("Consolas", 10f);
        var x = Math.Max(48, Width / 8);
        var y = Math.Max(52, Height / 9);

        g.DrawString(":(", big, white, x, y);
        g.DrawString("На демонстрационном экране возникла ошибка.", title, white, x, y + 112);
        g.DrawString("Это стилизованный макет. Windows продолжает работать нормально.", body, soft, x, y + 172);
        g.DrawString("КОД МАКЕТА: SECTOR-2015-01\nСОСТОЯНИЕ: PREVIEW\nФАЙЛЫ И НАСТРОЙКИ НЕ ИЗМЕНЯЮТСЯ", small, soft, x, y + 235);
        var barWidth = Math.Min(440, Width - x * 2);
        using var track = new SolidBrush(Color.FromArgb(75, 255, 255, 255));
        using var fill = new SolidBrush(Color.White);
        g.FillRectangle(track, x, y + 330, barWidth, 4);
        g.FillRectangle(fill, x, y + 330, barWidth * _phase / 100f, 4);
        g.DrawString("Нажмите Esc или используйте кнопку «Выход».", body, soft, x, y + 352);
    }

    private void DrawSnowCat(Graphics g)
    {
        using var background = new LinearGradientBrush(ClientRectangle,
            Color.FromArgb(8, 20, 31), Color.FromArgb(13, 65, 78), 18f);
        g.FillRectangle(background, ClientRectangle);

        using var snow = new SolidBrush(Color.FromArgb(40, 220, 243, 248));
        for (var i = 0; i < 38; i++)
        {
            var x = (i * 97 + 29) % Math.Max(1, Width);
            var y = (i * 61 + 17 + _phase * (i % 4 + 1)) % Math.Max(1, Height);
            g.FillEllipse(snow, x, y, i % 3 == 0 ? 3 : 2, i % 3 == 0 ? 3 : 2);
        }

        var left = new Rectangle(Width / 10, Height / 5, Math.Max(330, Width * 43 / 100), Height * 58 / 100);
        FillRounded(g, left, Color.FromArgb(224, 16, 31, 43), 20);
        DrawText(g, "SECTOR  /  WINTER COLLECTION", new Font("Segoe UI Semibold", 9f),
            Color.FromArgb(105, 207, 218), left.X + 32, left.Y + 30);
        DrawText(g, "СНЕЖНЫЙ АРХИВ", new Font("Georgia", 27f),
            Color.FromArgb(238, 246, 249), left.X + 30, left.Y + 82);
        DrawText(g, "Все демонстрационные материалы
готовы к просмотру.", new Font("Segoe UI", 13f),
            Color.FromArgb(177, 199, 207), left.X + 32, left.Y + 142);
        DrawText(g, "LOCAL PREVIEW   ·   NO SYSTEM CHANGES", new Font("Consolas", 9f),
            Color.FromArgb(111, 157, 169), left.X + 32, left.Bottom - 74);
        var progress = new Rectangle(left.X + 32, left.Bottom - 42, left.Width - 64, 5);
        FillRounded(g, progress, Color.FromArgb(49, 75, 85), 3);
        FillRounded(g, new Rectangle(progress.X, progress.Y, progress.Width * _phase / 100, progress.Height),
            Color.FromArgb(83, 206, 215), 3);

        DrawCat(g, new Rectangle(Width * 61 / 100, Height / 5, Width / 4, Height * 58 / 100));
    }

    private void DrawCat(Graphics g, Rectangle area)
    {
        var cx = area.Left + area.Width / 2;
        var head = new Rectangle(area.Left + area.Width / 6, area.Top + area.Height / 6, area.Width * 2 / 3, area.Height * 2 / 3);
        using var fur = new SolidBrush(Color.FromArgb(228, 235, 238));
        using var shade = new SolidBrush(Color.FromArgb(161, 180, 190));
        using var dark = new SolidBrush(Color.FromArgb(30, 48, 60));
        using var eye = new SolidBrush(Color.FromArgb(13, 28, 38));
        using var nose = new SolidBrush(Color.FromArgb(210, 139, 151));

        Point[] leftEar = [new(head.Left + 18, head.Top + 42), new(head.Left + 28, head.Top - 10), new(head.Left + 70, head.Top + 25)];
        Point[] rightEar = [new(head.Right - 70, head.Top + 25), new(head.Right - 28, head.Top - 10), new(head.Right - 18, head.Top + 42)];
        g.FillPolygon(fur, leftEar);
        g.FillPolygon(fur, rightEar);
        g.FillEllipse(fur, head);
        g.FillEllipse(shade, head.Left + head.Width / 5, head.Top + head.Height / 2, head.Width / 5, head.Height / 3);
        g.FillEllipse(shade, head.Left + head.Width * 3 / 5, head.Top + head.Height / 2, head.Width / 5, head.Height / 3);
        g.FillEllipse(eye, head.Left + head.Width / 3 - 7, head.Top + head.Height / 2 - 5, 13, 17);
        g.FillEllipse(eye, head.Left + head.Width * 2 / 3 - 6, head.Top + head.Height / 2 - 5, 13, 17);
        g.FillEllipse(nose, cx - 6, head.Top + head.Height * 2 / 3, 12, 8);
        g.DrawArc(new Pen(Color.FromArgb(60, 78, 89), 2f), cx - 13, head.Top + head.Height * 2 / 3 + 5, 13, 14, 0, 90);
        g.DrawArc(new Pen(Color.FromArgb(60, 78, 89), 2f), cx, head.Top + head.Height * 2 / 3 + 5, 13, 14, 90, 90);

        var paw = new Rectangle(cx - head.Width / 5, head.Bottom - 12, head.Width * 2 / 5, head.Height / 3);
        g.FillEllipse(fur, paw);
        for (var i = 0; i < 3; i++)
            g.FillEllipse(shade, paw.Left + 16 + i * 17, paw.Top + 16, 7, 7);
        DrawText(g, "❄", new Font("Segoe UI Symbol", 26f), Color.FromArgb(128, 224, 233), area.Right - 8, area.Top + 5);
    }

    private void DrawRedTerminal(Graphics g)
    {
        g.Clear(Color.FromArgb(27, 5, 8));
        using var red = new SolidBrush(Color.FromArgb(235, 79, 74));
        using var pale = new SolidBrush(Color.FromArgb(237, 211, 206));
        using var mono = new Font("Consolas", 12f);
        using var small = new Font("Consolas", 9f);
        DrawText(g, "TERMINAL SESSION 04     /     LOCAL DEMO", new Font("Consolas", 10f),
            Color.FromArgb(255, 105, 95), 42, 34);
        var art = "        /\_/\\\n       ( o.o )\n        > ^ <\n\n     [ SECTOR ]";
        g.DrawString(art, new Font("Consolas", 24f, FontStyle.Bold), red, 46, 104);
        g.DrawString("ВИЗУАЛЬНАЯ СЦЕНА ЗАПУЩЕНА", mono, pale, 46, Height * 2 / 3);
        g.DrawString("ФАЙЛОВАЯ СИСТЕМА НЕ ИЗМЕНЯЕТСЯ\nСЕАНС: PREVIEW   ·   STATUS: SAFE",
            small, red, 46, Height * 2 / 3 + 38);
        if (_phase < 60)
            g.FillRectangle(red, 46, Height * 2 / 3 + 91, 9, 17);
    }

    private void DrawMessenger(Graphics g)
    {
        g.Clear(Color.FromArgb(12, 22, 31));
        var sidebar = new Rectangle(0, 0, Math.Max(165, Width / 5), Height);
        using var sideBrush = new SolidBrush(Color.FromArgb(18, 33, 45));
        g.FillRectangle(sideBrush, sidebar);
        DrawText(g, "MESSAGES", new Font("Segoe UI Semibold", 10f), Color.FromArgb(135, 174, 189), 24, 28);
        DrawText(g, "Избранное\n\nЛичные\n\nАрхив", new Font("Segoe UI", 10f), Color.FromArgb(162, 183, 194), 24, 82);

        var x = sidebar.Right + 42;
        DrawText(g, "Локальный чат", new Font("Segoe UI Semibold", 19f), Color.FromArgb(228, 237, 241), x, 30);
        DrawText(g, "демонстрационная переписка · 2015", new Font("Segoe UI", 9f), Color.FromArgb(119, 149, 164), x, 63);
        FillRounded(g, new Rectangle(x, 112, Math.Min(380, Width / 2), 58), Color.FromArgb(29, 49, 63), 12);
        DrawText(g, "можно посмотреть файл?", new Font("Segoe UI", 10f), Color.FromArgb(218, 229, 234), x + 16, 130);
        var bubbleX = Math.Max(x + 170, Width - 450);
        FillRounded(g, new Rectangle(bubbleX, 192, Math.Min(340, Width / 2), 58), Color.FromArgb(38, 91, 118), 12);
        DrawText(g, "да, это только демо", new Font("Segoe UI", 10f), Color.White, bubbleX + 16, 210);

        var file = new Rectangle(x, 278, 330, 76);
        FillRounded(g, file, Color.FromArgb(30, 43, 54), 10);
        FillRounded(g, new Rectangle(x + 13, 291, 48, 48), Color.FromArgb(54, 120, 147), 8);
        DrawText(g, "BAT", new Font("Consolas", 11f, FontStyle.Bold), Color.White, x + 18, 307);
        DrawText(g, "risovat-demo.bat", new Font("Segoe UI Semibold", 10f), Color.FromArgb(228, 238, 242), x + 75, 291);
        DrawText(g, "Безопасный HTML-запускатель · 2 КБ", new Font("Segoe UI", 8f), Color.FromArgb(130, 157, 169), x + 75, 316);

        DrawText(g, "ФАЙЛ НЕ ВЫПОЛНЯЕТСЯ ВНУТРИ СЕКТОРА", new Font("Consolas", 9f),
            Color.FromArgb(108, 184, 193), x, 390);
        DrawText(g, "ESC — ВЫХОД ИЗ ПРЕДПРОСМОТРА", new Font("Consolas", 9f),
            Color.FromArgb(108, 184, 193), x, 416);
    }

    private void DrawAmber(Graphics g)
    {
        g.Clear(Color.FromArgb(27, 29, 32));
        var width = Math.Min(620, Width - 80);
        var height = Math.Min(360, Height - 80);
        var box = new Rectangle((Width - width) / 2, (Height - height) / 2, width, height);
        FillRounded(g, box, Color.FromArgb(43, 46, 50), 5);
        g.FillRectangle(new SolidBrush(Color.FromArgb(167, 105, 35)), box.X, box.Y, box.Width, 42);
        DrawText(g, "СЕКТОР / CLASSIC WINDOW", new Font("Segoe UI Semibold", 10f),
            Color.White, box.X + 18, box.Y + 12);
        DrawText(g, "Демонстрационная панель", new Font("Segoe UI Semibold", 19f),
            Color.FromArgb(235, 237, 239), box.X + 28, box.Y + 76);
        DrawText(g, "Сценарий готов к просмотру. Это макет интерфейса, а не системное окно.",
            new Font("Segoe UI", 10f), Color.FromArgb(178, 184, 190), box.X + 30, box.Y + 128);
        for (var i = 0; i < 3; i++)
        {
            var y = box.Y + 194 + i * 25;
            DrawText(g, new[] { "Проверка визуальной темы", "Проверка анимации", "Проверка локального предпросмотра" }[i],
                new Font("Segoe UI", 8.5f), Color.FromArgb(193, 198, 202), box.X + 30, y);
            var track = new Rectangle(box.X + box.Width - 160, y + 2, 105, 7);
            FillRounded(g, track, Color.FromArgb(65, 69, 73), 4);
            FillRounded(g, new Rectangle(track.X, track.Y, track.Width * (( _phase + i * 17) % 100) / 100, track.Height),
                Color.FromArgb(231, 154, 66), 4);
        }
    }

    private static void DrawText(Graphics g, string text, Font font, Color color, float x, float y)
    {
        using var brush = new SolidBrush(color);
        g.DrawString(text, font, brush, x, y);
    }

    private static void FillRounded(Graphics g, Rectangle rectangle, Color color, int radius)
    {
        if (rectangle.Width <= 0 || rectangle.Height <= 0) return;
        using var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(rectangle.X, rectangle.Y, d, d, 180, 90);
        path.AddArc(rectangle.Right - d, rectangle.Y, d, d, 270, 90);
        path.AddArc(rectangle.Right - d, rectangle.Bottom - d, d, d, 0, 90);
        path.AddArc(rectangle.X, rectangle.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        using var brush = new SolidBrush(color);
        g.FillPath(brush, path);
    }
}
