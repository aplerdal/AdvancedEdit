
using System.Diagnostics;
using AdvancedLib.Project;
using AdvEditRework;
using Hexa.NET.ImGui;
using NativeFileDialogs.Net;
using Raylib_cs;

namespace AdvancedEdit.UI;

public static class MenuBar
{
    private static readonly Dictionary<string, string> RomFilter = new() {{"MKSC Rom","gba"},{"All files","*"}};

    private static readonly Dictionary<string, string> ProjectFilter = new() { { "Advanced Project", "amkp" }, { "All files", "*" } };
    
    /// <summary>
    /// Render the menu bar
    /// </summary>
    public static void Update(ref Project? project)
    {
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("Open ROM", "ctrl+o"))
                {
                    var status = Nfd.OpenDialog(out var path, RomFilter, null);
                    if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path))
                    {
                        using var romStream = File.OpenRead(path);
                        project = Project.FromRom(romStream, Path.GetFileNameWithoutExtension(path));
                    }
                }

                ImGui.BeginDisabled(project is null);
                if (ImGui.MenuItem("Save ROM", "ctrl+s"))
                {
                    var status = Nfd.SaveDialog(out var path, RomFilter, $"{project.Name}.gba");
                    if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path))
                    {
                        
                    }
                }
                ImGui.EndDisabled();
                
                ImGui.Separator();
                if (ImGui.MenuItem("Open Project", "ctrl+shift+o"))
                {
                    var status = Nfd.OpenDialog(out var path, ProjectFilter, null);
                    if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path))
                    {
                        project = Project.Unpack(path);
                    }
                    
                }

                ImGui.BeginDisabled(project is null);
                if (ImGui.MenuItem("Save Project", "ctrl+shift+s"))
                {
                    var status = Nfd.SaveDialog(out var path, ProjectFilter, $"{project.Name}.amkp");
                    if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path))
                    {
                        project.Save(path);
                    }
                }
                ImGui.EndDisabled();
                
                ImGui.Separator();
                if (ImGui.MenuItem("Exit", "alt+f4")) ; // TODO: Handle exiting

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Edit"))
            {
                ImGui.MenuItem("Undo", "ctrl+z"); // TODO: Add undo and redo callbacks to the menu bar. Windows should handle registering them.
                ImGui.MenuItem("Redo", "ctrl+y");
                ImGui.Separator();
                ImGui.MenuItem("Copy", "ctrl+c");
                ImGui.MenuItem("Paste", "ctrl+v");
                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }
    }
}