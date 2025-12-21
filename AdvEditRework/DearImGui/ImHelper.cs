using Hexa.NET.ImGui;
using Raylib_cs;

namespace AdvEditRework.DearImGui;

public static class ImHelper
{
    public static bool BeginEmptyWindow(string name, Rectangle bounds)
    {
        ImGui.SetNextWindowPos(bounds.Position);
        ImGui.SetNextWindowSize(bounds.Size);
        const ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoBackground |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoTitleBar;
        return ImGui.Begin(name, flags);
    }

    public static void EndEmptyWindow()
    {
        ImGui.End();
    }

    public static Color Color(ImGuiCol themeColor)
    {
        var imColor = ImGui.GetStyle().Colors[(int)themeColor];
        return new Color(imColor.X, imColor.Y, imColor.Z, imColor.W);
    }
}