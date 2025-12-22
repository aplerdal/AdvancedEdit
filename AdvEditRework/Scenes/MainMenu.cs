using System.Numerics;
using AdvancedLib.Project;
using AdvEditRework.UI;
using AdvEditRework.UI.Editors;
using NativeFileDialogs.Net;
using Raylib_cs;

namespace AdvEditRework.Scenes;

public class MainMenu : Scene
{
    public static readonly Dictionary<string, string> ProjectFilter = new() { { "Advanced Project", "amkp" }, { "All files", "*" } };

    public override void Init(ref Project? project)
    {
        Gui.SetFontMksc();
    }

    public override void Update(ref Project? project)
    {
        Raylib.ClearBackground(Gui.Style.BgColor);
        Gui.SetCursorPos(new Vector2(8));
        Gui.TitleBox("Start", new(256.0f, 512.0f));
        if (Gui.LabelButton((char)MapEditIcon.FileOpen + "Open Project"))
        {
            var status = Nfd.OpenDialog(out var path, ProjectFilter);
            if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path))
            {
                project = Project.Unpack(path);
                Program.SetScene(new TrackEditorScene());
            }
        }

        if (Gui.LabelButton((char)MapEditIcon.FileNew + "New Project"))
        {
            Program.SetScene(new CreateProject());
        }

        if (Gui.LabelButton((char)MapEditIcon.Settings + "Settings"))
        {
            Program.SetScene(new SettingsMenu());
        }

        Gui.LabelButton((char)MapEditIcon.Help + "Help");
        Gui.LabelLink((char)MapEditIcon.Discord + "Discord", "https://discord.gg/tDNDgfC5sD");
    }

    public override void Dispose()
    {
        // 
    }
}