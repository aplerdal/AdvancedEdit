using System.Numerics;
using AdvancedLib.Project;
using AdvEditRework.UI;
using Hexa.NET.ImGui;
using Raylib_cs;

namespace AdvEditRework.Scenes;

public class Settings : Scene
{
    public override void Init(ref Project? project)
    {
        
    }

    public override void Update(ref Project? project)
    {
        var viewport = ImGui.GetMainViewport();
        
        ImGui.SetNextWindowSize(viewport.WorkSize);
        ImGui.SetNextWindowPos(viewport.WorkPos);
        ImGui.SetNextWindowViewport(viewport.ID);
        ImGui.Begin("Settings",
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoMove
        );
        {
            ImGui.Text("Settings");
        }
        ImGui.End();
    }

    public override void Dispose()
    {
        throw new NotImplementedException();
    }
}