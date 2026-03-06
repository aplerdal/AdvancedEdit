using Raylib_cs;

namespace AdvEditRework.UI;

public class Style
{
    private const float BaseFontSize = 16.0f;
    public float FontSize => BaseFontSize;
    public Color BgColor = new(43, 43, 59);
    public Color BoxColor = new(58, 58, 75);
    public Color BoxOutlineColor = new(63, 63, 80);
    public Color TextTint = Color.White;

    public Style Clone()
    {
        return (Style)MemberwiseClone();
    }
}