using System.Diagnostics;
using System.Numerics;
using AdvancedLib.Game;
using AdvancedLib.Project;
using AdvEditRework.UI;
using AdvEditRework.UI.Editors;
using AdvEditRework.UI.Tools;
using Hexa.NET.ImGui;
using NativeFileDialogs.Net;

namespace AdvEditRework.Scenes;

public class TrackEditorScene : Scene
{
    private bool _quitPopupOpen = true;
    private ProjectTrack? _projectTrack;
    private TrackView? _view;
    private Editor? _editor;
    private Track? _currentTrack;
    public override void Init(ref Project? project)
    {
    }
    void SetView(TrackView view)
    {
        _view?.Dispose();
        _view = view;
        SetEditor(new MapEditor(view));
    }

    void SetEditor(Editor editor)
    {
        _editor?.Dispose();
        _editor = editor;
    }
    private bool MainMenuBar(ref Project? project)
    {
        bool isActive = false;
        if (project is null) return false;
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                isActive |= true;
                if (ImGui.MenuItem("Save Project"))
                {
                    var name = new string(project.Name.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray());
                    if (string.IsNullOrWhiteSpace(name)) name = "mksc";
                    var status = Nfd.SaveDialog(out var path, MainMenu.ProjectFilter, name+".amkp");
                    if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path))
                    {
                        if (_currentTrack != null) _projectTrack?.SaveTrackData(_currentTrack);
                        project.Save(path);
                    }
                }

                if (ImGui.MenuItem("Open Project"))
                {
                    var status = Nfd.OpenDialog(out var path, MainMenu.ProjectFilter, null);
                    if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path))
                    {
                        project = Project.Unpack(path);
                    }
                }

                if (ImGui.MenuItem("New Project"))
                {
                    Program.SetScene(new CreateProject());
                }

                if (ImGui.MenuItem("Close Project"))
                {
                    // TODO: Implement popup to confirm
                    project = null;
                    Program.SetScene(new MainMenu());
                }
                ImGui.Separator();

                if (ImGui.MenuItem("Create Rom"))
                {
                    var openStatus = Nfd.OpenDialog(out var romPath, CreateProject.RomFilter, null);
                    if (openStatus == NfdStatus.Ok && !string.IsNullOrEmpty(romPath))
                    {
                        var name = new string(project.Name.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray());
                        if (string.IsNullOrWhiteSpace(name)) name = "mksc_hack";
                        var status = Nfd.SaveDialog(out var savePath, CreateProject.RomFilter, name + ".gba");
                        if (status == NfdStatus.Ok && !string.IsNullOrEmpty(savePath))
                        {
                            File.Copy(romPath, savePath, true);
                            using var fileStream = File.Open(savePath, FileMode.Open);
                            project.ToRom(fileStream);
                        }
                    }
                }

                if (ImGui.MenuItem("Quit"))
                {
                    Program.ShouldClose = true;
                }
                
                ImGui.EndMenu();
            }

            isActive |= TrackSelectorMenu(project);

            isActive |= ModeSelector();
            
            ImGui.EndMainMenuBar();
        }

        return isActive;
    }

    private EditMode _mode;
    private bool ModeSelector()
    {
        var windowPos = ImGui.GetWindowPos();
        var windowSize = ImGui.GetWindowSize();
        var windowCenter = windowPos + windowSize / 2;
        float ButtonSize(string text) => ImGui.CalcTextSize(text).X + ImGui.GetStyle().FramePadding.X * 2.0f;
        var barSize = ButtonSize("Layout") + 16 + ButtonSize("AI Map") + 16 + ButtonSize("Graphics");
        var buttonPos = new Vector2(windowCenter.X - barSize / 2f, 0);
        ImGui.BeginDisabled(_view is null);
        ImGui.SetCursorScreenPos(buttonPos);
        if (ImGui.MenuItem("Layout", "", _mode == EditMode.Map))
        {
            SetEditor(new MapEditor(_view));
            _mode = EditMode.Map;
        }
        buttonPos += new Vector2(ButtonSize("Layout") + 16, 0);
        ImGui.SetCursorScreenPos(buttonPos);
        if (ImGui.MenuItem("AI Map", "", _mode == EditMode.Ai))
        {
            SetEditor(new AiEditor(_view));
            _mode = EditMode.Ai;
        }
        buttonPos += new Vector2(ButtonSize("AI Map") + 16, 0);
        ImGui.SetCursorScreenPos(buttonPos);
        if (ImGui.MenuItem("Graphics", "", _mode == EditMode.Graphics))
        {
            SetEditor(new TrackGfxEditor(_view.Track));
            _mode = EditMode.Graphics;
        } 
        ImGui.EndDisabled();
        return false;
    }
    private bool TrackSelectorMenu(Project? project)
    {
        if (project is null)
        {
            ImGui.BeginDisabled();
            if (ImGui.BeginMenu("Track")) ImGui.EndMenu();
            ImGui.EndDisabled();
            return false;
        }

        bool isActive = false;
        if (ImGui.BeginMenu("Track"))
        {
            isActive = true;
            if (ImGui.BeginMenu("Load"))
            {
                foreach (var cup in project.Config.Cups)
                {
                    if (string.IsNullOrEmpty(cup.Name)) continue;
                    if (ImGui.BeginMenu(cup.Name))
                    {
                        foreach (var track in cup.Tracks)
                        {
                            if (string.IsNullOrEmpty(track.Name)) continue;
                            if (ImGui.MenuItem(track.Name))
                            {
                                if (_currentTrack != null) _projectTrack?.SaveTrackData(_currentTrack);
                                _projectTrack = track;
                                _currentTrack = track.LoadTrackData();
                                SetView(new TrackView(_currentTrack));
                            }
                        }
                        ImGui.EndMenu();
                    }
                }
                ImGui.EndMenu();
            }
            ImGui.EndMenu();
        }

        return isActive;
    }
    public override void Update(ref Project? project)
    {
        bool hasFocus = MainMenuBar(ref project);
        _editor?.Update(!hasFocus);
    }

    public override void Dispose()
    {
        _view?.Dispose();
        _editor?.Dispose();
    }
}