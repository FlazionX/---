using System.Drawing;
using System.Net;

namespace Sector;

public static class DemoExporter
{
    public static string Export(SimulationTemplate template, string destination)
    {
        Directory.CreateDirectory(destination);
        var folderName = MakeSafeName(template.Id);
        var output = Path.Combine(destination, folderName);
        Directory.CreateDirectory(output);

        var htmlName = folderName + ".html";
        var batName = "Запустить демо.bat";
        var htmlPath = Path.Combine(output, htmlName);
        var batPath = Path.Combine(output, batName);

        File.WriteAllText(htmlPath, BuildHtml(template), new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        var bat = "@echo off\r\nsetlocal\r\n" +
                  "start \"\" \"%~dp0" + htmlName + "\"\r\n" +
                  "exit /b 0\r\n";
        File.WriteAllText(batPath, bat, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        File.WriteAllText(Path.Combine(output, "README.txt"),
            "СЕКТОР — безопасная визуальная демонстрация\r\n\r\n" +
            "Откройте HTML-файл или BAT-запускатель. BAT только передаёт локальный HTML-файл браузеру.\r\n" +
            "Пакет не меняет настройки Windows, не блокирует ввод и не работает с пользовательскими файлами.\r\n",
            new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        return output;
    }

    private static string MakeSafeName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(value.Select(c => invalid.Contains(c) ? '-' : c).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "sector-demo" : safe;
    }

    private static string BuildHtml(SimulationTemplate template)
    {
        var title = WebUtility.HtmlEncode(template.Name);
        var accent = ColorTranslator.ToHtml(template.Accent);
        var narrationJson = System.Text.Json.JsonSerializer.Serialize(template.Narration);
        var content = template.Style switch
        {
            SimulationStyle.BlueProtocol => "<div class='sad'>:(</div><h1>На демонстрационном экране возникла ошибка.</h1><p>Это визуальный макет. Windows продолжает работать нормально.</p><small>SECTOR-2015-01 · PREVIEW · NO SYSTEM CHANGES</small>",
            SimulationStyle.SnowCat => "<div class='tag'>WINTER COLLECTION / LOCAL DEMO</div><h1>Снежный архив</h1><p>Все демонстрационные материалы готовы к просмотру.</p><div class='cat'> /\\_/\\<br>( o.o )<br> &gt; ^ &lt;</div><small>LOCAL PREVIEW · NO SYSTEM CHANGES</small>",
            SimulationStyle.RedTerminal => "<small>TERMINAL SESSION 04 / LOCAL DEMO</small><pre>        /\\_/\\\n       ( o.o )\n        &gt; ^ &lt;\n\n     [ SECTOR ]</pre><h2>Визуальная сцена запущена</h2><small>Файловая система не изменяется · STATUS: SAFE</small>",
            SimulationStyle.Messenger => "<div class='tag'>MESSAGES / 2015</div><h1>Локальный чат</h1><div class='bubble'>можно посмотреть файл?</div><div class='bubble right'>да, это только демо</div><div class='file'>▣ &nbsp; risovat-demo.bat<br><small>Безопасный HTML-запускатель</small></div><small>ФАЙЛ НЕ ВЫПОЛНЯЕТСЯ ВНУТРИ СЕКТОРА</small>",

            SimulationStyle.PirateWindows => "<div class='tag'>WINDOWS ACTIVATION CENTER / 2015</div><h1>Windows не активирована</h1><pre>0 0 1 0 / / / # # #\n## / 0101 / ##\n0x00 # 0 / 1 #</pre><p>Стилизованный макет старого окна активации.</p>",
            SimulationStyle.AndroidRetro => "<div class='android'><div class='status'>16:04 &nbsp; SIGNAL &nbsp; 82%</div><header>Android</header><h2>Системное сообщение</h2><p>Старая мобильная оболочка Android 4.x / 5.x. Локальная демонстрация интерфейса.</p><button class='ok' onclick='this.textContent=&quot;ГОТОВО&quot;'>ПОНЯТНО</button></div>",
            SimulationStyle.AsciiSilhouette => "<div class='tag'>ASCII SHADOW / 2016</div><pre class='sil'>        ####\n      ##0000##\n     #00000000#\n    /0000000000\\\n    #00  00  00#\n     #00000000#\n      /######\\\n    ###/####\\###\n   ##  /####\\  ##\n       / ## \\\n      /  ##  \\\n         ##</pre><small>0 / # / ASCII VISUAL</small>",
            SimulationStyle.VioletGlitch => "<div class='tag'>VIOLET GLITCH / 2015</div><h1>Визуальный сигнал</h1><p>Анимированная ретро-сцена с фиолетовыми акцентами.</p>",
            SimulationStyle.GreyDialog => "<div class='tag'>CLASSIC DIALOG / 2014</div><h1>Системный диалог</h1><p>Макет классического окна. Это не настоящее системное сообщение.</p>",
            SimulationStyle.PixelAlert => "<div class='tag'>PIXEL ALERT / 2014</div><h1>8-BIT VISUAL</h1><pre>+------------------+\n|  DEMO ONLY       |\n+------------------+</pre>",
            SimulationStyle.DarkArchive => "<div class='tag'>DARK ARCHIVE / 2016</div><h1>Архив загружен</h1><p>Локальная демонстрация завершена успешно.</p>",
            _ => "<div class='tag'>CLASSIC WINDOW</div><h1>Демонстрационная панель</h1><p>Сценарий готов к просмотру. Это макет интерфейса, а не системное окно.</p><div class='progress'><i></i></div><small>LOCAL PREVIEW · SAFE</small>"
        };

        return $@"<!doctype html>
<html lang='ru'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'>
<title>{title} — Сектор</title>
<style>
*{{box-sizing:border-box}} body{{margin:0;min-height:100vh;background:#101820;color:#edf3f6;font:16px 'Segoe UI',Arial,sans-serif;display:grid;place-items:center;padding:28px}}
main{{position:relative;width:min(920px,100%);min-height:520px;border-radius:18px;overflow:hidden;background:linear-gradient(135deg,#102b3c,#10171e);padding:68px clamp(25px,7vw,82px);box-shadow:0 26px 80px #0009;border-top:5px solid {accent}}}
.tag,small{{color:#9eb9c4;letter-spacing:.12em;font:11px Consolas,monospace}}h1{{font-size:clamp(26px,4vw,42px);font-weight:500;max-width:680px}}p{{color:#c1d1d8;line-height:1.7;max-width:650px}}.sad{{font-size:88px;color:white;margin-bottom:20px}}pre{{font:26px Consolas;color:#ff8178;line-height:1.35}}.cat{{position:absolute;right:7%;bottom:12%;font:26px Consolas;color:#e9f4f7;line-height:1.3;background:#183240;padding:25px 35px;border-radius:22px}}.bubble,.file{{padding:16px 20px;background:#1d3544;border-radius:12px;margin:18px 0;max-width:420px}}.right{{margin-left:auto;background:#285c78}}.file{{border:1px solid #456575}}.progress{{height:6px;background:#454545;border-radius:8px;margin-top:45px;overflow:hidden}}.progress i{{display:block;width:68%;height:100%;background:{accent};animation:move 2.4s ease-in-out infinite alternate}}@keyframes move{{from{{width:18%}}to{{width:88%}}}}
.android{{max-width:360px;margin:auto;background:#eef5f5;color:#263238;border:12px solid #101518;border-radius:28px;overflow:hidden;padding:0 20px 22px;min-height:430px}}.android .status{{background:#263238;color:white;padding:8px;font:11px Consolas}}.android header{{background:#5b9d5e;color:white;margin:0 -20px;padding:18px;font-size:24px}}.android h2{{font-size:21px;color:#263238}}.android p{{color:#45545a}}.android .ok{{background:#5b9d5e;color:white;border:0;padding:12px 26px;cursor:pointer}}.sil{{font:clamp(14px,2vw,22px) Consolas;color:#d2a095;line-height:1.25}}.notice{{position:absolute;right:22px;top:18px;background:#ffffff15;border:1px solid #ffffff25;padding:7px 11px;border-radius:20px;font:10px Consolas;letter-spacing:.08em}}
</style></head><body><main><div class='notice'>СИМУЛЯЦИЯ · НЕ БЛОКИРУЕТ WINDOWS</div>{content}</main><button id="sound" style="position:fixed;bottom:22px;right:170px;padding:12px 18px;border:0;border-radius:8px;background:#293a47;color:white;cursor:pointer">Звук 2015</button><button id="speak" style="position:fixed;bottom:22px;right:22px;padding:12px 18px;border:0;border-radius:8px;background:{accent};color:#111;cursor:pointer">Озвучить бота</button><script>document.getElementById("speak").onclick=()=>{{if("speechSynthesis" in window){{speechSynthesis.cancel();speechSynthesis.speak(new SpeechSynthesisUtterance({narrationJson}));}}}};document.getElementById("sound").onclick=()=>{{const C=window.AudioContext||window.webkitAudioContext;if(!C)return;const c=new C(),o=c.createOscillator(),g=c.createGain();o.type="square";o.frequency.setValueAtTime(660,c.currentTime);o.frequency.exponentialRampToValueAtTime(330,c.currentTime+.13);g.gain.setValueAtTime(.07,c.currentTime);g.gain.exponentialRampToValueAtTime(.001,c.currentTime+.18);o.connect(g);g.connect(c.destination);o.start();o.stop(c.currentTime+.19);}};</script></body></html>";
    }
}
