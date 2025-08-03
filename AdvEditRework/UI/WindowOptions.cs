namespace AdvEditRework.UI;
[Flags]
public enum WindowOptions
{
    AlignCenter = (1 << 0),
    AlightRight = (1 << 1),
    NoInteract = (1 << 2),
    NoFrame = (1 << 3),
    NoResize = (1 << 4),
    NoScroll = (1 << 5),
    NoClose = (1 << 6),
    NoTitle = (1 << 7),
    HoldFocus = (1 << 8),
    AutoSize = (1 << 9),
    Popup = (1 << 10),
    Closed = (1 << 11),
    Expanded = (1 << 12),
}