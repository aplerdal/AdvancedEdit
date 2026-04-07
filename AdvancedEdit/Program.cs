using AdvancedLib.Project;
using AdvEditRework.DearImGui;
using AdvEditRework.Resources;
using AdvEditRework.Scenes;
using AdvEditRework.Shaders;
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

    private static void Main()
    {
#if !DEBUG
        Raylib.SetTraceLogLevel(TraceLogLevel.Error);
#else
        Raylib.SetTraceLogLevel(TraceLogLevel.All);
#endif
        // Setup Raylib
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(800, 600, "AdvancedEdit");
        Raylib.SetWindowIcon(Raylib.LoadImage("Resources/icon.png"));
        Raylib.SetTargetFPS(144);
        Raylib.SetExitKey(KeyboardKey.Null);
        PaletteShader.Load();

        RlImGui.Setup();

        Settings.Load();
        
        TextureManager = new TextureManager();
        FontLoader.LoadOpenSansImGui();

        _scene.Init(ref _project);
        while (!(Raylib.WindowShouldClose() || ShouldClose))
        {
            Raylib.SetWindowTitle($"AdvancedEdit - {Raylib.GetFPS():0000}FPS");
            Update();
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
        TextureManager.Dispose();
        RlImGui.Shutdown();
        Raylib.CloseWindow();
    }
}