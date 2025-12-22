using System.Runtime.InteropServices;
using AdvancedLib.Project;
using Hexa.NET.ImGui;
using NativeFileDialogs.Net;

namespace AdvEditRework.Scenes;

public class CreateProject : Scene
{
    public static readonly Dictionary<string, string> RomFilter = new() { { "MKSC Rom", "gba" }, { "All files", "*" } };
    private string _path = string.Empty;
    private string _name = string.Empty;

    public override void Init(ref Project? project)
    {
        //
    }

    public override void Update(ref Project? project)
    {
        var viewport = ImGui.GetMainViewport();

        ImGui.SetNextWindowSize(viewport.WorkSize);
        ImGui.SetNextWindowPos(viewport.WorkPos);
        ImGui.SetNextWindowViewport(viewport.ID);
        ImGui.Begin("Create New Project",
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoMove
        );
        {
            ImGui.Text("Create New Project");
            ImGui.Separator();
            ImGui.InputTextWithHint("Name", "Project Name", ref _name, 128);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ImGui.InputTextWithHint("ROM Path", "C:/path/to/rom.gba", ref _path, 512);
            }
            else
            {
                ImGui.InputTextWithHint("ROM Path", "/path/to/rom.gba", ref _path, 512);
            }

            ImGui.SameLine();
            if (ImGui.Button("Open"))
            {
                var status = Nfd.OpenDialog(out var path, RomFilter);
                if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path))
                {
                    _path = path;
                }
            }

            if (ImGui.Button("Create") && File.Exists(_path))
            {
                if (_name == String.Empty) _name = "mksc";
                using var romStream = File.OpenRead(_path);
                project = Project.FromRom(romStream, _name);
                Program.SetScene(new TrackEditorScene());
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                Program.SetScene(new MainMenu());
            }
        }
        ImGui.End();
    }

    public override void Dispose()
    {
    }
}