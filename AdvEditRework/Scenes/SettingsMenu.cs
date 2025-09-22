using System.Numerics;
using AdvancedLib.Project;
using AdvEditRework.DearImGui;
using AdvEditRework.UI;
using Hexa.NET.ImGui;
using Raylib_cs;

namespace AdvEditRework.Scenes;

public class SettingsMenu : Scene
{
    public override void Init(ref Project? project)
    {
        
    }

    public override void Update(ref Project? project)
    {
        var viewport = ImGui.GetMainViewport();
        
        Raylib.ClearBackground(Color.RayWhite);
        ImHelper.BeginEmptyWindow("Settings",new Rectangle(viewport.Pos, viewport.Size));
        {
            ImGui.Text("Settings");
            ImGui.Separator();
            ImGui.InputInt("UI Scale", ref Settings.Shared.UIScale);
            if (ImGui.Button("Exit"))
            {
                Program.SetScene(new MainMenu());
            }
        }
        ImGui.End();
    }

    public override void Dispose()
    {
        //
    }
}