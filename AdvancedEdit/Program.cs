using AdvancedLib.Project;
using AdvEditRework.DearImGui;
using AdvEditRework.Resources;
using AdvEditRework.Scenes;
using AdvEditRework.Shaders;
using AdvEditRework.UI;
using Raylib_cs;

namespace AdvEditRework;

internal static class Program
{
    private static Scene _scene = new MainMenu();
    public static TextureManager TextureManager = null!;
    public static bool ShouldClose { get; set; } = false;

    public static void SetScene(Scene scene)
    {
        _scene.Dispose();
        _scene = scene;
        _scene.Init(ref _project);
    }

    private static Project? _project;

    private static void Main(string[] args)
    {
#if !DEBUG
        Raylib.SetTraceLogLevel(TraceLogLevel.Error);
#else
        Raylib.SetTraceLogLevel(TraceLogLevel.All);
#endif
        // Setup Raylib
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(800, 600, "AdvancedEdit");
        Raylib.SetWindowIcon(Raylib.LoadImage(Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, "Resources/icon.png")));
        Raylib.SetTargetFPS(144);
        Raylib.SetExitKey(KeyboardKey.Null);
        PaletteShader.Load();


        RlImGui.Setup();

        Settings.Load();

        TextureManager = new TextureManager();
        FontLoader.LoadOpenSansImGui();

        if (args.Length > 0)
        {
            if (File.Exists(args[0]))
            {
                try
                {
                    var project = Project.Unpack(args[0]);
                    _project = project;
                    SetScene(new TrackEditorScene());
                }
                catch (Exception e)
                {
                    _project = null;
                }
            }
        }
        _scene.Init(ref _project);
        while (!(Raylib.WindowShouldClose() || ShouldClose))
        {
            try
            {
                Update();
            }
            catch (Exception e)
            {
                ExceptionPopup.CreateLogFile(e, "Crash");
            }
        }

        Close();
    }

    private static void Update()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.White);
        RlImGui.Begin();
        _scene.Update(ref _project);
        RlImGui.End();
        Raylib.EndDrawing();
    }

    private static void Close()
    {
        _scene.Dispose();
        TextureManager.Dispose();
        RlImGui.Shutdown();
        Raylib.CloseWindow();
    }
}