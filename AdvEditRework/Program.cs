using System.Numerics;
using AdvancedEdit;
using AdvancedEdit.UI;
using AdvancedLib.Project;
using AdvEditRework.Scenes;
using Hexa.NET.ImGui;
using Raylib_cs;

namespace AdvEditRework;

static class Program
{
    private static Scene _scene = new MainMenu();

    private static Project? _project;
    private static Font scFont = FontLoader.LoadMkscFont();
    
    static void Main(string[] args)
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.Msaa4xHint);
        
        Raylib.InitWindow(800, 600, "AdvEditRework");
        
        
        
        Raylib.SetTargetFPS(60);
        Raylib.SetExitKey(KeyboardKey.Null);
        ImGuiRenderer.Setup(true, false);
        
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
        ImGuiRenderer.Begin(Raylib.GetFrameTime());
        
        MenuBar.Update(ref _project);
        
        Raylib.DrawTextEx(scFont, " !\"#$%\'&()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[¥]^_`abcdefghijklmnopqrstuvwxyz{|}~ ", new Vector2(0, 128), 32, 0, Color.White);
        Raylib.DrawTextEx(scFont, "ぁあぃいぅうぇえぉおかがきぎくぐけげこごさざしじすずせぜそぞただちぢっつづてでとどなにぬねのはばぱひびぴふぶぷへべぺほぼぽまみむめもゃやゅゆょよらりるれろゎをん", new Vector2(0, 128 + 32), 32, 0, Color.White);
        Raylib.DrawTextEx(scFont, "ァアィイゥウェエォオカガキギクグケゲコゴサザシジスズセゼソゾタダチヂッツヅテデトドナニヌネノハバパヒビピフブプヘベペホボポマミムメモャヤュユョヨラリルレロヮワン", new Vector2(0, 128 + 64), 32, 0, Color.White);

        ImGuiRenderer.End();
        Raylib.EndDrawing();
    }

    static void Close()
    {
        
    }
}