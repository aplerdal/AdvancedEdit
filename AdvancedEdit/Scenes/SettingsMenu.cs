using AdvancedLib.Project;
using AdvEditRework.DearImGui;
using Hexa.NET.ImGui;
using NativeFileDialogs.Net;
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

        Raylib.ClearBackground(Color.White);
        ImHelper.BeginEmptyWindow("Settings", new Rectangle(viewport.Pos, viewport.Size));
        {
            ImGui.Text("Settings");
            ImGui.Separator();

            // Keybinds
            var settings = Settings.Shared;
            if (ImGui.BeginTable("Keybinds", 2, ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerV))
            {
                ImGui.TableSetupColumn("Description");
                ImGui.TableSetupColumn("Keybind");
                ImGui.TableHeadersRow();
                KeybindRow("Pen Tool Hotkey", ref settings.DrawBind);
                KeybindRow("Eyedropper Tool Hotkey", ref settings.EyedropperBind);
                KeybindRow("Rectangle Tool Hotkey", ref settings.RectangleBind);
                KeybindRow("Selection Tool Hotkey", ref settings.SelectBind);
                KeybindRow("Bucket Tool Hotkey", ref settings.BucketBind);
                ImGui.EndTable();
            }

            string pathString = settings.BaseRomPath ?? string.Empty;
            ImGui.InputText("Base rom path", ref pathString, (uint)pathString.Length + 32);
            ImGui.SameLine();
            if (ImGui.Button("Open"))
            {
                var status = Nfd.OpenDialog(out var path, CreateProject.RomFilter);
                if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path)) settings.BaseRomPath = path;
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