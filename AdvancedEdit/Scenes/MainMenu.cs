using System.Numerics;
using AdvancedLib.Project;
using AdvEditRework.DearImGui;
using AdvEditRework.UI;
using AdvEditRework.UI.Editors;
using Hexa.NET.ImGui;
using NativeFileDialogs.Net;
using Raylib_cs;

namespace AdvEditRework.Scenes;

public class MainMenu : Scene
{
    private Texture2D _iconTexture;
    private ExceptionPopup? _exceptionPopup;

    public override void Init(ref Project? project)
    {
        _iconTexture = Program.TextureManager.GetTexture("font.png");
    }

    private bool QuickOption(MapEditIcon icon, string tooltip)
    {
        var size = new Vector2(128);
        var iconIdx = (int)icon;
        var src = new Rectangle(iconIdx * 16, 8 * 16, 16, 16);
        var dest = new Rectangle(ImGui.GetCursorScreenPos(), size);
        var hovered = Raylib.CheckCollisionPointRec(ImGui.GetMousePos(), dest);
        if (hovered) Raylib.DrawRectangleRec(dest, new Color(225, 225, 225));
        Raylib.DrawRectangleLinesEx(dest, 4, new Color(200, 200, 200));
        Raylib.DrawTexturePro(_iconTexture, src, dest, Vector2.Zero, 0.0f, Color.White);
        ImGui.Dummy(dest.Size);
        ImGui.SetItemTooltip(tooltip);
        return hovered && Raylib.IsMouseButtonPressed(MouseButton.Left);
    }

    private void DrawQuickOptions(ref Project? project)
    {
        const float height = 128f;
        const int totalOptions = 4;

        var center = Raylib.GetScreenWidth() / 2;
        var drawStart = center - (totalOptions * (height + 4) - 4) / 2;
        ImGui.SetCursorPos(new Vector2(drawStart, ImGui.GetCursorPos().Y));
        if (QuickOption(MapEditIcon.FileOpen, "Open project"))
        {
            var status = Nfd.OpenDialog(out var path, TrackEditorScene.ProjectFilter);
            if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path))
            {
                try
                {
                    Settings.Shared.UpdateProjectList(path);
                    project = Project.Unpack(path);
                    Program.SetScene(new TrackEditorScene());
                }
                catch (Exception e)
                {
                    _exceptionPopup = new ExceptionPopup("Error loading project", e);
                }
            }
        }

        ImGui.SameLine();
        if (QuickOption(MapEditIcon.FileNew, "Create new project"))
        {
            Program.SetScene(new CreateProject());
        }

        ImGui.SameLine();
        if (QuickOption(MapEditIcon.Settings, "Settings"))
        {
            Program.SetScene(new SettingsMenu());
        }

        ImGui.SameLine();
        if (QuickOption(MapEditIcon.Discord, "Discord"))
        {
            Raylib.OpenURL("https://discord.gg/tDNDgfC5sD");
        }
    }

    public override void Update(ref Project? project)
    {
        Raylib.ClearBackground(Color.White);

        _exceptionPopup?.Update();

        ImHelper.BeginEmptyWindow("MainMenuWindow", new Rectangle(Vector2.Zero, Raylib.GetScreenWidth(), Raylib.GetScreenHeight()));
        ImGui.NewLine();
        DrawQuickOptions(ref project);
        ImGui.NewLine();
        ImGui.Separator();
        if (ImGui.BeginTable("optionsTable", 3, ImGuiTableFlags.BordersInnerV, ImGui.GetContentRegionAvail()))
        {
            ImGui.TableSetupColumn("Quick Start");
            ImGui.TableSetupColumn("Recent Projects");
            ImGui.TableSetupColumn("Patch Notes");
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            {
                ImGui.Text("Quick Start");
                ImGui.Separator();
                if (ImGui.TextLink("New Project"))
                    Program.SetScene(new CreateProject());
                if (ImGui.TextLink("Open Project"))
                {
                    var status = Nfd.OpenDialog(out var path, TrackEditorScene.ProjectFilter);
                    if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path))
                    {
                        try
                        {
                            Settings.Shared.UpdateProjectList(path);
                            project = Project.Unpack(path);
                            Program.SetScene(new TrackEditorScene());
                        }
                        catch (Exception e)
                        {
                            _exceptionPopup = new ExceptionPopup("Error loading project", e);
                        }
                    }
                }

                if (ImGui.TextLink("Settings"))
                    Program.SetScene(new SettingsMenu());
                if (ImGui.TextLink("Discord"))
                    Raylib.OpenURL("https://discord.gg/tDNDgfC5sD");
            }
            ImGui.TableSetColumnIndex(1);
            {
                ImGui.Text("Recent Projects");
                ImGui.Separator();
                var recents = Settings.Shared.RecentProjectFiles;
                if (recents.Count == 0)
                {
                    ImGui.BeginDisabled();
                    ImGui.Text("No recent projects...");
                    ImGui.EndDisabled();
                }
                else
                {
                    foreach (var recentPath in recents.ToList())
                    {
                        if (!ImGui.Selectable(recentPath)) continue;

                        if (File.Exists(recentPath))
                        {
                            Settings.Shared.UpdateProjectList(recentPath);
                            try
                            {
                                project = Project.Unpack(recentPath);
                                Program.SetScene(new TrackEditorScene());
                            }
                            catch (Exception e)
                            {
                                _exceptionPopup = new ExceptionPopup("Error loading project", e);
                            }
                        }
                        else
                        {
                            recents.Remove(recentPath);
                        }
                    }
                }
            }
            ImGui.TableSetColumnIndex(2);
            {
                ImGui.Text("Patch Notes");
                ImGui.Separator();
                PatchNotes();
            }

            ImGui.EndTable();
        }

        ImHelper.EndEmptyWindow();
    }

    public override void Dispose()
    {
        // 
    }

    private static void PatchNotes()
    {
        // This is a dumb system, but it will do.
        ImGui.SeparatorText("1.0.0 Release Candidate 5");
        ImGui.TextWrapped("- Fixed battle track cup loading"u8);
        ImGui.TextWrapped("- Added windows installer option. "u8);
        ImGui.NewLine();
        ImGui.SeparatorText("1.0.0 Release Candidate 4");
        ImGui.TextWrapped("- Added setting for default base rom"u8);
        ImGui.TextWrapped("- Added optional track overlay when editing minimap"u8);
        ImGui.TextWrapped("- Fixed various issues with .smkc file imports"u8);
        ImGui.TextWrapped("- Fixed battle tracks crashing upon viewing"u8);
        ImGui.TextWrapped("- Fixed adding item boxes"u8);
        ImGui.TextWrapped("- Fixed issue where item boxes in first or last zone could cause a crash"u8);
        ImGui.TextWrapped("- Moved track imports and exports to \"Track\" menu"u8);
        ImGui.NewLine();
        ImGui.SeparatorText("1.0.0 Release Candidate 3");
        ImGui.TextWrapped("- Fixed themes for imported SMKC files"u8);
        ImGui.TextWrapped("- Fixed crash when deleting objects"u8);
        ImGui.TextWrapped("- Added error checking when loading projects and other files"u8);
        ImGui.NewLine();
        ImGui.SeparatorText("1.0.0 Release Candidate 2");
        ImGui.TextWrapped("- Fixed DPI scaling issues"u8);
        ImGui.NewLine();
        ImGui.SeparatorText("1.0.0 Release Candidate 1");
        ImGui.TextWrapped("Wow, a real release! If you are reading this, thank you for helping test the editor! I look forward to seeing what everyone is able to make.\nHappy racing!\n - Antimattur"u8);
    }
}