using System.Numerics;
using System.Resources;
using System.Runtime.InteropServices;
using AdvancedLib.Project;
using AdvEditRework.DearImGui;
using AdvEditRework.Resources;
using AdvEditRework.Scenes;
using AdvEditRework.Shaders;
using AdvEditRework.UI;
using Hexa.NET.ImGui;
using Raylib_cs;

namespace AdvEditRework;

static class Program
{
    private static Scene _scene = new MainMenu();
    public static TextureManager TextureManager;
    public static bool ShouldClose { get; set; } = false;

    public static void SetScene(Scene scene)
    {
        _scene.Dispose();
        _scene = scene;
        _scene.Init(ref _project);
    }
    
    private static Project? _project;
    
    static void Main(string[] args)
    {
        #if !DEBUG
            Raylib.SetTraceLogLevel(TraceLogLevel.Error);
        #endif
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(800, 600, "AdvEditRework");
        Raylib.SetTargetFPS(144);
        Raylib.SetExitKey(KeyboardKey.Null);
        
        PaletteShader.Load();
        
        ImGuiRenderer.Setup(false, false);
        var dpiScale = Raylib.GetWindowScaleDPI();
        var settings = Settings.Shared;
        settings.UIScale = (int)Math.Round((dpiScale.X + dpiScale.Y) / 2);
        TextureManager = new TextureManager();
        var imFont = ImGui.GetIO().Fonts.AddFontFromFileTTF("Resources/OpenSans.ttf", 16 * settings.UIScale);
        ImGuiRenderer.ReloadFonts();
        ImGui.GetIO().FontDefault = imFont;
        Style.SetupImGuiStyle();
        
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