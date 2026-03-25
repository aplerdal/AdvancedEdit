using System.Runtime.InteropServices;
using AdvancedLib.Project;
using Hexa.NET.ImGui;
using NativeFileDialogs.Net;
using Raylib_cs;

namespace AdvEditRework.Scenes;

public class CreateProject : Scene
{
    public static readonly Dictionary<string, string> RomFilter = new() { { "MKSC Rom", "gba" }, { "All files", "*" } };
    private string _path = string.Empty;
    private string _name = string.Empty;
    
    private Task<Project>? _loadTask;
    private float _progress;
    private string _progressLabel = string.Empty;

    public override void Init(ref Project? project)
    {
        //
    }

    public override void Update(ref Project? project)
    {
        if (_loadTask is { IsCompleted: true })
        {
            // Rethrow any exception on the main thread rather than silently swallowing it
            project = _loadTask.GetAwaiter().GetResult();
            _loadTask = null;
            Program.SetScene(new TrackEditorScene());
            return;
        }
        
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
                ImGui.InputTextWithHint("ROM Path", "C:/path/to/rom.gba", ref _path, 512);
            else
                ImGui.InputTextWithHint("ROM Path", "/path/to/rom.gba", ref _path, 512);

            ImGui.SameLine();
            if (ImGui.Button("Open"))
            {
                var status = Nfd.OpenDialog(out var path, RomFilter);
                if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path)) _path = path;
            }

            if (ImGui.Button("Create") && File.Exists(_path) && _loadTask == null)
            {
                if (_name == string.Empty) _name = "mksc";
                _progress = 0f;
                _progressLabel = "Extracting...";
                var romPath = _path;
                var projectName = _name;
                _loadTask = Task.Run(async () =>
                {
                    await using var romStream = File.OpenRead(romPath);
                    return await Project.FromRomAsync(romStream, projectName);
                });
            }

            ImGui.SameLine();
            if (ImGui.Button("Cancel")) Program.SetScene(new MainMenu());
        }
        ImGui.End();

        // Loading modal — rendered on top while task is in flight
        if (_loadTask is { IsCompleted: false })
        {
            var center = viewport.Size / 2;
            ImGui.SetNextWindowPos(center, ImGuiCond.Always, new(0.5f, 0.5f));
            ImGui.SetNextWindowSize(new (360, 100));
            ImGui.OpenPopup("##loading");
            if (ImGui.BeginPopupModal("##loading",
                    ImGuiWindowFlags.NoTitleBar |
                    ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.NoMove))
            {
                ImGui.Text("Loading ROM...");
                ImGui.ProgressBar(-(float)(Raylib.GetTime()/2), new (-1, 0), _progressLabel);
                ImGui.EndPopup();
            }
        }
    }

    public override void Dispose()
    {
    }
}