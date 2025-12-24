using System.Diagnostics;
using System.Numerics;
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

    // This method is needed because ImGuiRenderer clears the queue of key presses every frame and
    // ImGui doesn't have a similar method that I know of
    public static KeyboardKey GetKeyPressed()
    {
        foreach (var key in Enum.GetValues(typeof(KeyboardKey)).Cast<KeyboardKey>())
        {
            if (Raylib.IsKeyPressed(key))
                return key;
        }

        return KeyboardKey.Null;
    }
    private static string _activeId = string.Empty;
    public static void Keybind(string id, ref KeyboardKey key)
    {
        Debug.Assert(id != string.Empty);
        ImGui.PushID(id);
        bool listening = id == _activeId;
        
        var label = listening ? "Press a key..." : Enum.GetName(key);
        if (ImGui.Selectable(label, ref listening, Vector2.Zero)) _activeId = id;
        
        if (listening)
        {
            var pressedKey = GetKeyPressed();
            if (pressedKey == KeyboardKey.Escape)
            {
                _activeId = string.Empty;
            } else if (pressedKey != KeyboardKey.Null) {
                key = pressedKey;
                _activeId = string.Empty;
            }
        }
        ImGui.PopID(); 
    }

    public static Color Color(ImGuiCol themeColor)
    {
        var imColor = ImGui.GetStyle().Colors[(int)themeColor];
        return new Color(imColor.X, imColor.Y, imColor.Z, imColor.W);
    }
}