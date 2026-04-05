using System.Diagnostics;
using System.Formats.Tar;
using System.Numerics;
using AdvancedLib.Game;
using AdvancedLib.Project;
using AdvEditRework.DearImGui;
using AdvEditRework.UI;
using AdvEditRework.UI.Editors;
using AdvEditRework.UI.Editors.AI;
using AdvEditRework.UI.Editors.Object;
using Hexa.NET.ImGui;
using NativeFileDialogs.Net;
using Raylib_cs;

namespace AdvEditRework.Scenes;

public class TrackEditorScene : Scene
{
    public static readonly Dictionary<string, string> ProjectFilter = new() { { "Advanced Project", "amkp" }, { "All files", "*" } };
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
        var isActive = Raylib.GetMousePosition().Y < (ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.Y * 2);
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
                    var status = Nfd.SaveDialog(out var path, ProjectFilter, name + ".amkp");
                    if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path))
                    {
                        if (_currentTrack is not null) _projectTrack?.SaveTrackDataAsync(_currentTrack).Wait();
                        project.Save(path);
                        Settings.Shared.UpdateProjectList(path);
                    }
                }

                if (ImGui.BeginMenu("Open Project"))
                {
                    if (ImGui.MenuItem("Open File"))
                    {
                        var status = Nfd.OpenDialog(out var path, ProjectFilter);
                        if (status == NfdStatus.Ok && !string.IsNullOrEmpty(path))
                        {
                            Settings.Shared.UpdateProjectList(path);
                            project = Project.Unpack(path);
                        }
                    }

                    ImGui.Separator();

                    var recents = Settings.Shared.RecentProjectFiles;
                    for (int i = 0; i < 4 && i < recents.Count; i++)
                    {
                        var path = recents[i];
                        var display = path;
                        if (path.Length > 24)
                        {
                            display = path[..10] + "..." + path[^10..];
                        }

                        if (ImGui.MenuItem(display))
                        {
                            Settings.Shared.UpdateProjectList(path);
                            project = Project.Unpack(path);
                        }
                    }

                    ImGui.EndMenu();
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
                        var status = Nfd.SaveDialog(out var path, TrackFilter, $"{_projectTrack.Name}.amkt");
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
        var openSettings = false;
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

            ImGui.BeginDisabled(_currentTrack is null);
            if (ImGui.MenuItem("Settings"))
            {
                openSettings = true;
            }

            ImGui.EndDisabled();
            if (ImGui.MenuItem("Close"))
            {
                _editor?.Dispose();
                _view?.Dispose();
                _editor = null;
                _view = null;
            }

            ImGui.EndMenu();
        }

        if (openSettings)
            ImGui.OpenPopup("Track Settings");
        isActive |= ProjectSettingsPopup(_currentTrack!);

        return isActive;
    }

    private bool ProjectSettingsPopup(Track track)
    {
        bool isActive = false;
        var screenSize = new Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
        var size = new Vector2(screenSize.X / 3f, screenSize.Y * (3 / 4f));
        ImGui.SetNextWindowSize(size, ImGuiCond.Appearing);
        var pos = (screenSize / 2) - (size / 2);
        ImGui.SetNextWindowPos(pos);
        if (ImGui.BeginPopupModal("Track Settings"))
        {
            isActive = true;

            int value;

            ImGui.SeparatorText("Track Config");

            value = (int)track.Config.Laps;
            ImGui.InputInt("Laps", ref value);
            value = Math.Clamp(value, 3, 5);
            track.Config.Laps = (uint)value;

            value = (int)track.Config.Theme;
            ImGui.InputInt("Theme", ref value);
            track.Config.Theme = (uint)value;

            value = (int)track.Config.SongID;
            ImGui.InputInt("Song ID", ref value);
            track.Config.SongID = (uint)value;

            value = (int)track.Config.BackgroundIndex;
            ImGui.InputInt("Background Art", ref value);
            track.Config.BackgroundIndex = (uint)value;

            value = (int)track.Config.BackgroundBehavior;
            ImGui.InputInt("Background Behavior", ref value);
            track.Config.BackgroundBehavior = (uint)value;

            value = (int)track.Config.PaletteBehavior;
            ImGui.InputInt("Palette Animation Type", ref value);
            track.Config.PaletteBehavior = (uint)value;

            ImGui.SeparatorText("Target Times");
            for (var i = 0; i < track.TargetTimes.Length; i++)
            {
                ImGui.PushID($"time{i}");
                var modified = false;
                var time = track.TargetTimes[i];
                var mins = (time.Hundredths / 100) / 60;
                var secs = (time.Hundredths / 100) % 60;
                var hundredths = time.Hundredths % 100;
                int character = time.Character;
                var name = i switch
                {
                    0 => "Lap Time",
                    _ => $"3 Lap Time {i}"
                };
                ImGui.SetNextItemWidth(ImGui.GetFontSize() * 20);
                ImGui.Text(name);
                ImGui.SetNextItemWidth(ImGui.GetFontSize() * 6);
                ImGui.Combo("##Character", ref character, "Mario\0Luigi\0Peach\0Toad\0Yoshi\0DK\0Wario\0Bowser\0");
                ImGui.SameLine();
                time.Character = (ushort)character;
                ImGui.SetNextItemWidth(ImGui.GetFontSize() * 2);
                modified |= ImGui.InputInt("##Mins", ref mins, 0);
                ImGui.SameLine();
                ImGui.Text("\'");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(ImGui.GetFontSize() * 2);
                modified |= ImGui.InputInt("##Seconds", ref secs, 0);
                ImGui.SameLine();
                ImGui.Text("\"");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(ImGui.GetFontSize() * 2);
                modified |= ImGui.InputInt("##Hundredths", ref hundredths, 0);
                if (modified)
                    time.Hundredths = (ushort)(mins * 100 * 60 + secs * 100 + hundredths);
                ImGui.PopID();
            }

            ImGui.Separator();
            if (ImGui.Button("Close"))
                ImGui.CloseCurrentPopup();

            ImGui.EndPopup();
        }

        return isActive;
    }

    private void ProjectMenu(Project project)
    {
        var screenSize = new Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
        var menuBarHeight = ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.Y * 2;
        var area = new Rectangle(0, menuBarHeight, screenSize.X, screenSize.Y - menuBarHeight);
        ImHelper.BeginEmptyWindow("projectMenu", area);
        if (ImGui.BeginTable("trackSelTable", 3, ImGuiTableFlags.BordersInnerV, ImGui.GetContentRegionAvail()))
        {
            ImGui.TableSetupColumn("Track");
            ImGui.TableSetupColumn("Something else");
            ImGui.TableSetupColumn("Info");
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            {
                ImGui.Text("Track Select");
                ImGui.Separator();
                foreach (var cup in project.Config.Cups)
                {
                    if (string.IsNullOrEmpty(cup.Name)) continue;
                    if (ImGui.CollapsingHeader(cup.Name))
                    {
                        foreach (var track in cup.Tracks)
                        {
                            if (string.IsNullOrEmpty(track.Name)) continue;
                            if (ImGui.Selectable(track.Name))
                            {
                                if (_currentTrack is not null) _projectTrack?.SaveTrackDataAsync(_currentTrack).Wait();
                                _projectTrack = track;
                                track.ResolveFolder(Path.Combine(project.Folder, cup.Name));
                                _currentTrack = track.LoadTrackData();
                                _mode = EditMode.Map;
                                SetView(new TrackView(_currentTrack));
                            }
                        }
                    }
                }
            }
            ImGui.TableSetColumnIndex(1);
            ImGui.Text("Test");
            ImGui.Separator();

            ImGui.TableSetColumnIndex(2);
            ImGui.Text("Info");
            ImGui.Separator();

            ImGui.EndTable();
        }

        ImHelper.EndEmptyWindow();
    }

    public override void Update(ref Project? project)
    {
        var hasFocus = MainMenuBar(ref project);
        if (_editor is null )
        {
            if (project is not null)
                ProjectMenu(project);
        }
        else
        {
            _editor?.Update(!hasFocus);
        }
    }

    public override void Dispose()
    {
        _view?.Dispose();
        _editor?.Dispose();
    }
}