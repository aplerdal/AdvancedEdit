using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Raylib_cs;

namespace AdvEditRework.UI;

/// <summary>
/// A custom immediate mode GUI
/// </summary>
public static class Gui
{
    private static readonly Font _font = FontLoader.LoadMkscFont();
    private static float Scale => Style.FontSize / 16.0f;
    private static Vector2 _cursor;
    
    private static Stack<Style> _styleStack = new Stack<Style>();
    public static Style Style { get; private set; } = new Style();
    private static void OpenUrl(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else
            {
                throw new PlatformNotSupportedException("Unsupported OS");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to open URL: {ex.Message}");
        }
    }
    
    private static Vector2 MeasureText(string text)
    {
        float height = 16.0f;
        float width = 0.0f;
        foreach (char c in text)
        {
            // Icons have double width
            if (c >= 0xE000 && c <= 0xF8FF) width += 16.0f;
            else if (c == '\n') height += 16.0f;
            else width += 8.0f;
        }
        return new Vector2(width, height) * Scale;
    }

    public static void SetCursorPos(Vector2 pos)
    {
        _cursor = pos * Scale;
    }
    public static void TitleBox(string title, Vector2 size)
    {
        var rect = new Rectangle(_cursor.X, _cursor.Y, size.X, size.Y);
        Raylib.DrawRectangleRec(rect, Style.BoxColor);
        Raylib.DrawRectangleLinesEx(rect,4f*Scale, Style.BoxOutlineColor);
        Raylib.DrawRectangleRounded(rect with{Height = (16.0f + 8.0f) * Scale},0.1f, 4, Style.BoxOutlineColor);
        _cursor = rect.Position + new Vector2(4*Scale);
        Label(title);
        _cursor += new Vector2(4*Scale);
    }
    
    public static void Label(string text)
    {
        Raylib.DrawTextEx(_font, text, _cursor, Style.FontSize, 0, Style.TextTint);
        _cursor.Y += MeasureText(text).Y;
    }
    
    public static bool LabelButton(string text)
    {
        bool clicked = false;
        bool hovered = false;
        var textSize = MeasureText(text);
        var textRect = new Rectangle(_cursor.X, _cursor.Y, textSize.X, textSize.Y);
        if (Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), textRect))
        {
            hovered = true;
            if (Raylib.IsMouseButtonPressed(MouseButton.Left)) clicked = true;
            PushStyle(style=>style.TextTint = new Color(200, 200, 200));
        }
        Label(text);
        if (hovered) PopStyle();
        return clicked;
    }

    public static void LabelLink(string text, string url)
    {
        if (LabelButton(text))
        {
            OpenUrl(url);
        }
    }
    
    public static void PushStyle(Action<Style> modifier)
    {
        _styleStack.Push(Style.Clone());
        modifier(Style);
    }

    public static void PopStyle()
    {
        if (_styleStack.Count > 0)
        {
            Style = _styleStack.Pop();
        }
        else
        {
            throw new InvalidOperationException("Style stack underflow");
        }
    }
}