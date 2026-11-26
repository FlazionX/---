namespace Sector;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, args) =>
        {
            try
            {
                var log = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Sector", "crash.log");
                Directory.CreateDirectory(Path.GetDirectoryName(log)!);
                File.AppendAllText(log, $"[{DateTime.Now:O}] {args.Exception}\r\n\r\n");
            }
            catch { }
            MessageBox.Show("Произошла ошибка. Подробности записаны в LocalAppData\\Sector\\crash.log.",
                "Сектор", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        Application.Run(new MainForm());
    }
}
