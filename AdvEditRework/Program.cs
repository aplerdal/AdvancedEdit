using AdvancedLib.Project;
using AdvEditRework.DearImGui;
using AdvEditRework.Resources;
using AdvEditRework.Scenes;
using AdvEditRework.Shaders;
using Hexa.NET.ImGui;
using Raylib_cs;

namespace AdvEditRework;

static class Program
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

    static void Main()
    {
#if !DEBUG
            Raylib.SetTraceLogLevel(TraceLogLevel.Error);
#endif
        // Setup Raylib
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(800, 600, "AdvEditRework");
        Raylib.SetTargetFPS(144);
        Raylib.SetExitKey(KeyboardKey.Null);

        PaletteShader.Load();

        ImGuiRenderer.Setup(false);
        
        // Load Settings
        // var settings = Settings.Shared;
        Settings.Load();
        
        // Calculate UI scale based on DPI
        var dpiScale = Raylib.GetWindowScaleDPI();
        Settings.Shared.UIScale = (int)Math.Round((dpiScale.X + dpiScale.Y) / 2);
        TextureManager = new TextureManager();
        var imFont = ImGui.GetIO().Fonts.AddFontFromFileTTF("Resources/OpenSans.ttf", 16 * Settings.Shared.UIScale);
        ImGuiRenderer.ReloadFonts();
        ImGui.GetIO().FontDefault = imFont;
        
        _scene.Init(ref _project);
        while (!(Raylib.WindowShouldClose() || ShouldClose))
        {
            Raylib.SetWindowTitle($"AdvEditRework - {Raylib.GetFPS():0000}FPS");
            Update();
        }

        Close();
    }

    static void Update()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);
        ImGuiRenderer.Begin();
        _scene.Update(ref _project);
        ImGuiRenderer.End();
        Raylib.EndDrawing();
    }

    static void Close()
    {
        TextureManager.Dispose();
        ImGuiRenderer.Shutdown();
        Raylib.CloseWindow();
    }
}