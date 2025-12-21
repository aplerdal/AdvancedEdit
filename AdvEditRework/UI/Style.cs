using System.Numerics;
using Hexa.NET.ImGui;
using Raylib_cs;

namespace AdvEditRework.UI;

public class Style
{
    private const float BaseFontSize = 16.0f;
    public float FontSize => BaseFontSize * Settings.Shared.UIScale;
    public Color BgColor = new Color(43, 43, 59);
    public Color BoxColor = new Color(58, 58, 75);
    public Color BoxOutlineColor = new Color(63, 63, 80);
    public Color TextTint = Color.White;
    public Style Clone() => (Style)MemberwiseClone();

    public static void SetupImGuiStyle()
    {
        var style = ImGui.GetStyle();
    }
}