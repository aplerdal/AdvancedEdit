using System.Diagnostics;
using System.Formats.Tar;
using System.Numerics;
using AdvancedLib.Game;
using AdvancedLib.Project;
using AdvEditRework.UI;
using AdvEditRework.UI.Editors;
using AdvEditRework.UI.Editors.AI;
using AdvEditRework.UI.Editors.Object;
using Hexa.NET.ImGui;
using NativeFileDialogs.Net;

namespace AdvEditRework.Scenes;

public class TrackEditorScene : Scene
{
    public static readonly Dictionary<string, string> MAKEFilter = new() { { "MAKE track", "smkc" }, { "All files", "*" } };
    public static readonly Dictionary<string, string> TrackFilter = new() { { "Advanced Edit track", "amkt" }, { "All files", "*" } };

    private ProjectTrack? _projectTrack;
    private TrackView? _view;
    private Editor? _editor;
    private Track? _currentTrack;

    public override void Init(ref Project? project)
    {
    }

    private void SetView(TrackView view)
    {
        _view?.Dispose();
        _view = view;
        SetEditor(new MapEditor(view));
    }

    private void SetEditor(Editor editor)
    {
        _editor?.Dispose();
        _editor = editor;
    }

    private bool MainMenuBar(ref Project? project)
    {
        var isActive = false;
        if (project is null) return false;

        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                isActive = true;
                if (ImGui.MenuItem("Save Project"))
                {
                    var name = new string(project.Name.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray());
                    if (string.IsNullOrWhiteSpace(name)) name = "mksc";
                    var status = Nfd.SaveDialog(out var path, MainMenu.ProjectFilter, name + ".amkp");
                    if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path))
                    {
                        if (_currentTrack is not null) _projectTrack?.SaveTrackDataAsync(_currentTrack).Wait();
                        project.Save(path);
                    }
                }

                if (ImGui.MenuItem("Open Project"))
                {
                    var status = Nfd.OpenDialog(out var path, MainMenu.ProjectFilter);
                    if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path)) project = Project.Unpack(path);
                }

                if (ImGui.MenuItem("New Project")) Program.SetScene(new CreateProject());

                if (ImGui.MenuItem("Close Project"))
                {
                    // TODO: Implement popup to confirm
                    project = null;
                    Program.SetScene(new MainMenu());
                }

                ImGui.Separator();
                
                ImGui.BeginDisabled(_currentTrack is null || _projectTrack is null);
                if (ImGui.BeginMenu("Import"))
                {
                    if (ImGui.MenuItem("SMK Track (.smkc)"))
                    {
                        var status = Nfd.OpenDialog(out var path, MAKEFilter);
                        if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path))
                        {
                            var track = MakeTrack.ModifyFromStream(File.OpenRead(path), project!);
                            _projectTrack!.SaveTrackDataAsync(track).Wait();
                            _currentTrack = _projectTrack.LoadTrackData();
                            SetView(new TrackView(_currentTrack));
                        }
                    }

                    ImGui.BeginDisabled(_projectTrack is null || _currentTrack is null);
                    if (ImGui.MenuItem("MKSC Track (.amkt)"))
                    {
                        Debug.Assert(_projectTrack is not null && _currentTrack is not null);
                        var status = Nfd.OpenDialog(out var path, TrackFilter);
                        if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path))
                        {
                            TarFile.ExtractToDirectory(path, _projectTrack.Folder, true);
                            _currentTrack = _projectTrack.LoadTrackData();
                            SetView(new TrackView(_currentTrack));
                        }
                    }

                    ImGui.EndDisabled();
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Export"))
                {
                    ImGui.BeginDisabled(_projectTrack is null || _currentTrack is null);
                    if (ImGui.MenuItem("MKSC Track (.amkt)"))
                    {
                        Debug.Assert(_projectTrack is not null && _currentTrack is not null);
                        _projectTrack.SaveTrackDataAsync(_currentTrack).Wait();
                        var trackFolder = _projectTrack.Folder;
                        var status = Nfd.SaveDialog(out var path, TrackFilter,$"{_projectTrack.Name}.amkt");
                        if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path))
                        {
                            TarFile.CreateFromDirectory(trackFolder, path, false);
                        }
                    }
                    ImGui.EndDisabled();

                    if (ImGui.MenuItem("Rom (.gba)"))
                    {
                        Debug.Assert(project is not null);
                        // Save active track
                        if (_currentTrack is not null) _projectTrack?.SaveTrackDataAsync(_currentTrack).Wait();

                        var openStatus = Nfd.OpenDialog(out var romPath, CreateProject.RomFilter);
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
                    ImGui.EndMenu();
                }
                
                ImGui.EndDisabled();
                
                ImGui.Separator();

                if (ImGui.MenuItem("Quit")) Program.ShouldClose = true;

                ImGui.EndMenu();
            }

            isActive |= TrackSelectorMenu(project);

            isActive |= ModeSelector(project);

            ImGui.EndMainMenuBar();
        }

        return isActive;
    }

    private EditMode _mode;

    private bool ModeSelector(Project project)
    {
        var windowPos = ImGui.GetWindowPos();
        var windowSize = ImGui.GetWindowSize();
        var windowCenter = windowPos + windowSize / 2;

        float ButtonSize(string text)
        {
            return ImGui.CalcTextSize(text).X + ImGui.GetStyle().FramePadding.X * 2.0f;
        }

        var barSize = ButtonSize("Layout") + 16 + ButtonSize("AI Map") + 16 + ButtonSize("Graphics") + 16 + ButtonSize("Objects");
        var buttonPos = new Vector2(windowCenter.X - barSize / 2f, 0);
        ImGui.BeginDisabled(_view is null);
        ImGui.SetCursorScreenPos(buttonPos);
        if (ImGui.MenuItem("Layout", "", _mode == EditMode.Map))
        {
            Debug.Assert(_view != null && _projectTrack != null);
            _view.RegenTextureBuffers();
            SetEditor(new MapEditor(_view));
            _mode = EditMode.Map;
        }

        buttonPos += new Vector2(ButtonSize("Layout") + 16, 0);
        ImGui.SetCursorScreenPos(buttonPos);
        if (ImGui.MenuItem("AI Map", "", _mode == EditMode.Ai))
        {
            Debug.Assert(_view != null);
            _view.RegenTextureBuffers();
            SetEditor(new AiEditor(_view));
            _mode = EditMode.Ai;
        }

        buttonPos += new Vector2(ButtonSize("AI Map") + 16, 0);
        ImGui.SetCursorScreenPos(buttonPos);
        if (ImGui.MenuItem("Graphics", "", _mode == EditMode.Graphics))
        {
            Debug.Assert(_view != null);
            SetEditor(new TrackGfxEditor(_view.Track));
            _mode = EditMode.Graphics;
        }
        ImGui.BeginDisabled(_view?.Track.ObstacleGfx is null);
        buttonPos += new Vector2(ButtonSize("Graphics") + 16, 0);
        ImGui.SetCursorScreenPos(buttonPos);
        if (ImGui.MenuItem("Objects", "", _mode == EditMode.Objects))
        {
            Debug.Assert(_view != null);
            SetEditor(new ObjectEditor(_view, project.Config.ObstacleOam));
            _mode = EditMode.Objects;
        }
        ImGui.EndDisabled();

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

        var isActive = false;
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
                                if (_currentTrack is not null) _projectTrack?.SaveTrackDataAsync(_currentTrack).Wait();
                                _projectTrack = track;
                                track.ResolveFolder(Path.Combine(project.Folder, cup.Name));
                                _currentTrack = track.LoadTrackData();
                                _mode = EditMode.Map;
                                SetView(new TrackView(_currentTrack));
                            }
                        }

                        ImGui.EndMenu();
                    }
                }

                ImGui.EndMenu();
            }

            ImGui.BeginDisabled(_editor is not MapEditor && _editor is not AiEditor);
            // if (ImGui.MenuItem("Screenshot"))
            // {
            //     var openStatus = Nfd.SaveDialog(out var imgPath, ImageFilter, $"{_projectTrack?.Name}.bmp");
            //     if (openStatus == NfdStatus.Ok && !string.IsNullOrEmpty(imgPath))
            //         switch (_editor)
            //         {
            //             case MapEditor mapEditor:
            //                 mapEditor.View.TrackScreenshot(imgPath);
            //                 break;
            //             case AiEditor aiEditor:
            //                 aiEditor.View.TrackScreenshot(imgPath);
            //                 break;
            //         }
            // }

            ImGui.EndDisabled();
            ImGui.EndMenu();
        }

        return isActive;
    }

    public override void Update(ref Project? project)
    {
        var hasFocus = MainMenuBar(ref project);
        _editor?.Update(!hasFocus);
    }

    public override void Dispose()
    {
        _view?.Dispose();
        _editor?.Dispose();
    }
}