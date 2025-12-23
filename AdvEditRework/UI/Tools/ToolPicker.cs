using System.Numerics;
using AdvEditRework.UI.Editors;
using Raylib_cs;

namespace AdvEditRework.UI.Tools;

// This probably should be refactored to be somewhere else
public static class ToolPicker
{
    private static readonly Texture2D IconAtlas = Program.TextureManager.GetTexture("tools.png");
    
    public static void Draw(Vector2 position, float width, ref MapEditorToolType activeTool)
    {
        var scale = Settings.Shared.UIScale;
        Rectangle dest = new Rectangle(position, new Vector2(32 * scale));
        Color fillColor = new Color(0.75f, 0.75f, 0.75f);
        Color outlineColor = new Color(0.65f, 0.65f, 0.65f);
        Color shadowColor = new Color(0.50f, 0.50f, 0.50f);
        foreach (var tool in Enum.GetValues(typeof(MapEditorToolType)).Cast<MapEditorToolType>())
        {
            var atlasSrc = new Rectangle(16 * ((int)tool % 4), 16 * (int)((int)tool / 4), 16, 16);
            var hovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), dest);
            if (hovered || tool == activeTool)
            {
                Raylib.DrawRectangleRec(dest with { Y = dest.Y + 2 * scale }, fillColor);
                Raylib.DrawTexturePro(IconAtlas, atlasSrc, dest with { Y = dest.Y + 2 * scale }, Vector2.Zero, 0, Color.White);
                Raylib.DrawRectangleLinesEx(dest with { Y = dest.Y + 2 * scale }, 2 * scale, outlineColor);
                if (hovered && Raylib.IsMouseButtonPressed(MouseButton.Left)) activeTool = tool;
            }
            else
            {
                Raylib.DrawRectangleLinesEx(dest with { Y = dest.Y + 2 * scale }, 2 * scale, shadowColor);
                Raylib.DrawRectangleRec(dest, fillColor);
                Raylib.DrawTexturePro(IconAtlas, atlasSrc, dest, Vector2.Zero, 0, Color.White);
                Raylib.DrawRectangleLinesEx(dest, 2 * scale, outlineColor);
            }

            //Raylib.DrawRectangleRec(dest, Color.Red);
            var newDest = new Rectangle(dest.X + 32, dest.Y, dest.Size);
            if (newDest.X + newDest.Width - position.X > width) newDest = new Rectangle(position.X, position.Y + 32, dest.Size);
            dest = newDest;
        }
    }
}