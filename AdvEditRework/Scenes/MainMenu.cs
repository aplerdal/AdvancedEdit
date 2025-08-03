using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using AdvancedLib.Project;
using AdvEditRework.UI;
using NativeFileDialogs.Net;
using Raylib_cs;

namespace AdvEditRework.Scenes;

public class MainMenu : Scene
{
    private static readonly Dictionary<string, string> RomFilter = new() {{"MKSC Rom","gba"},{"All files","*"}};

    private static readonly Dictionary<string, string> ProjectFilter = new() { { "Advanced Project", "amkp" }, { "All files", "*" } };

    public override void Init(ref Project? project)
    {
        //
    }
    
    public override void Update(ref Project? project)
    {
        Raylib.ClearBackground(Gui.Style.BgColor);
        Gui.SetCursorPos(new Vector2(8));
        Gui.TitleBox("Start", new (256.0f, 512.0f));
        if (Gui.LabelButton((char)Icon.FileOpen + "Open Project"))
        {
            var status = Nfd.OpenDialog(out var path, ProjectFilter, null);
            if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path))
            {
                project = Project.Unpack(path);
            }
        }
        if (Gui.LabelButton((char)Icon.FileNew + "New Project"))
        {
            var status = Nfd.OpenDialog(out var path, RomFilter, null);
            if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path))
            {
                using var romStream = File.OpenRead(path);
                project = Project.FromRom(romStream, Path.GetFileNameWithoutExtension(path));
            }
        }

        if (Gui.LabelButton((char)Icon.Settings + "Settings"))
        {
            Program.SetScene(new Settings());
        }
        Gui.LabelButton((char)Icon.Help + "Help");
        Gui.LabelLink((char)Icon.Discord + "Discord", "https://discord.gg/tDNDgfC5sD");
    }
    
    public override void Dispose()
    {
        throw new NotImplementedException();
    }
}