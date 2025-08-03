using System.Numerics;
using AdvancedLib.Project;
using AdvEditRework.Scenes;
using Hexa.NET.ImGui;
using Raylib_cs;

namespace AdvEditRework;

static class Program
{
    private static Scene _scene = new MainMenu();

    public static void SetScene(Scene scene)
    {
        _scene = scene;
        _scene.Init(ref _project);
    }
    
    private static Project? _project;
    public static ImFontPtr ImFont;
    public static float UIScale;
    
    static void Main(string[] args)
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        
        Raylib.InitWindow(800, 600, "AdvEditRework");
        
        Raylib.SetTargetFPS(60);
        Raylib.SetExitKey(KeyboardKey.Null);
        ImGuiRenderer.Setup(true, false);
        var dpiScale = Raylib.GetWindowScaleDPI();
        UIScale = (dpiScale.X + dpiScale.Y) / 2;
        Console.WriteLine($"UI Scale: {UIScale*100:F0}%");
        ImFont = ImGui.GetIO().Fonts.AddFontFromFileTTF("Resources/Mksc.ttf", 18 * UIScale);
        ImGuiRenderer.ReloadFonts();
        ImGui.GetIO().FontDefault = ImFont;
        
        _scene.Init(ref _project);
        while (!Raylib.WindowShouldClose())
        {
            Update();
        }
        
        Close();
    }

    static void Update()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.White);
        ImGuiRenderer.Begin();
        _scene.Update(ref _project);
        ImGuiRenderer.End();
        Raylib.EndDrawing();
    }

    static void Close()
    {
        ImGuiRenderer.Shutdown();
    }
}