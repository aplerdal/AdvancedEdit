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
    private static readonly Font MkscFont = FontLoader.LoadMkscFont();
    private static readonly Font OpenSans = FontLoader.LoadOpenSans();
    public static Font ActiveFont = MkscFont;
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

    public static Vector2 MeasureText(string text)
    {
        return Raylib.MeasureTextEx(ActiveFont, text, Style.FontSize * Scale, 0.0f);
    }

    public static void SetFontMksc()
    {
        Style.TextTint = Color.White;
        ActiveFont = MkscFont;
    }

    public static void SetFontOpenSans()
    {
        Style.TextTint = Color.Black;
        ActiveFont = OpenSans;
    }

    public static void SetCursorPos(Vector2 pos)
    {
        _cursor = pos * Scale;
    }

    public static void TitleBox(string title, Vector2 size)
    {
        var rect = new Rectangle(_cursor.X, _cursor.Y, size.X, size.Y);
        Raylib.DrawRectangleRec(rect, Style.BoxColor);
        Raylib.DrawRectangleLinesEx(rect, 4f * Scale, Style.BoxOutlineColor);
        Raylib.DrawRectangleRounded(rect with { Height = (16.0f + 8.0f) * Scale }, 0.1f, 4, Style.BoxOutlineColor);
        _cursor = rect.Position + new Vector2(4 * Scale);
        Label(title);
        _cursor += new Vector2(4 * Scale);
    }

    public static void Label(string text)
    {
        Raylib.DrawTextEx(ActiveFont, text, _cursor, ActiveFont.BaseSize, 0, Style.TextTint);
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
            PushStyle(style => style.TextTint = new Color(200, 200, 200));
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