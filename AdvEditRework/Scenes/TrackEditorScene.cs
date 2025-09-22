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
        _editor = editor;
        _editor.Init();
    }
    private void MainMenuBar(ref Project? project)
    {
        if (project is null) return;
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
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

                if (ImGui.MenuItem("Export Rom"))
                {
                    var name = new string(project.Name.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray());
                    if (string.IsNullOrWhiteSpace(name)) name = "mksc_hack";
                    var status = Nfd.SaveDialog(out var path, MainMenu.ProjectFilter, name + ".gba");
                    if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path))
                    {
                        if (_currentTrack != null) _projectTrack?.SaveTrackData(_currentTrack);
                        project.Save(path);
                    }
                }

                if (ImGui.MenuItem("Quit"))
                {
                    Program.ShouldClose = true;
                }
                
                ImGui.EndMenu();
            }

            TrackSelectorMenu(project);

            ModeSelector();
            
            ImGui.EndMainMenuBar();
        }
    }

    private EditMode _mode;
    void ModeSelector()
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

    }
    private void TrackSelectorMenu(Project? project)
    {
        if (project is null)
        {
            ImGui.BeginDisabled();
            if (ImGui.BeginMenu("Track")) ImGui.EndMenu();
            ImGui.EndDisabled();
            return;
        }
        
        if (ImGui.BeginMenu("Track"))
        {
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
    }
    public override void Update(ref Project? project)
    {
        MainMenuBar(ref project);
        if (_editor is not null) _editor.Update();
    }

    public override void Dispose()
    {
        if (_view is not null) _view.Dispose();
    }
}