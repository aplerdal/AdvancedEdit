using AdvancedLib.Project;
using AdvEditRework.DearImGui;
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
        ImHelper.BeginEmptyWindow("Settings", new Rectangle(viewport.Pos, viewport.Size));
        {
            ImGui.Text("Settings");
            ImGui.Separator();
            
            // General settings
            ImGui.InputInt("UI Scale", ref Settings.Shared.UIScale);
            
            // Keybinds
            if (ImGui.BeginTable("Keybinds", 2, ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerV))
            {
                ImGui.TableSetupColumn("Description");
                ImGui.TableSetupColumn("Keybind");
                ImGui.TableHeadersRow();
                var settings = Settings.Shared;
                KeybindRow("Pen Tool Hotkey", ref settings.DrawBind);
                KeybindRow("Eyedropper Tool Hotkey", ref settings.EyedropperBind);
                KeybindRow("Rectangle Tool Hotkey", ref settings.RectangleBind);
                KeybindRow("Selection Tool Hotkey", ref settings.SelectBind);
                KeybindRow("Bucket Tool Hotkey", ref settings.BucketBind);
                ImGui.EndTable();
            }
            
            if (ImGui.Button("Exit"))
            {
                Settings.Save();
                Program.SetScene(new MainMenu());
            }
        }
        ImGui.End();
    }

    private void KeybindRow(string text, ref KeyboardKey key)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.Text(text);
        ImGui.TableSetColumnIndex(1);
        ImHelper.Keybind(text, ref key);
    }
    
    public override void Dispose()
    {
        //
    }
}