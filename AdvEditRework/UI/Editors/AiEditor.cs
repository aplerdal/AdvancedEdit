using System.Numerics;
using AdvancedLib.RaylibExt;
using AdvancedLib.Serialization.AI;
using AdvEditRework.DearImGui;
using AdvEditRework.UI.Undo;
using Hexa.NET.ImGui;
using Raylib_cs;

namespace AdvEditRework.UI.Editors;

record AiDrag(Vector2 StartPosition, AiZone Zone, DragHandle Handle)
{
    public readonly AiZone OriginalZone = new AiZone
    {
        X = Zone.X,
        Y = Zone.Y,
        Width = Zone.Width,
        Height = Zone.Height,
        Shape = Zone.Shape,
    };
}

public class AiEditor : Editor
{
    public readonly TrackView View;
    public readonly UndoManager UndoManager = new();
    private readonly Texture2D _shapeIcons;
    private readonly Texture2D _zoneIcons;

    private AiDrag? _drag;
    private AiZone? _selectedZone;
    private bool _panelFocused;
    private bool _resetConfirmationShown = false;

    public Vector2 MouseAiBlockPos => (View.MouseTilePos / 2).ToVec2I().AsVector2();

    public AiEditor(TrackView view)
    {
        View = view;
        View.DrawInTrack = TrackDraw;
        _shapeIcons = Program.TextureManager.GetTexture("shapes.png");
        _zoneIcons = Program.TextureManager.GetTexture("zoneIcons.png");
        Gui.SetFontOpenSans();
    }

    public override void Update(bool hasFocus)
    {
        View.Draw();
        UpdatePanel();
        CheckUndo();
    }

    bool ZoneShapeButton(Vector2 position, ZoneShape shape, bool selected)
    {
        Color fillColor = new Color(0.75f, 0.75f, 0.75f);
        Color outlineColor = new Color(0.65f, 0.65f, 0.65f);
        Color shadowColor = new Color(0.50f, 0.50f, 0.50f);
        var scale = Settings.Shared.UIScale;
        Rectangle dest = new Rectangle(position, new Vector2(32 * scale));
        var atlasSrc = new Rectangle(16 * (int)shape, 0, 16, 16);
        var hovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), dest);
        if (selected || hovered)
        {
            Raylib.DrawRectangleRec(dest with { Y = dest.Y + 2 * scale }, fillColor);
            Raylib.DrawTexturePro(_shapeIcons, atlasSrc, dest with { Y = dest.Y + 2 * scale }, Vector2.Zero, 0, Color.White);
            Raylib.DrawRectangleLinesEx(dest with { Y = dest.Y + 2 * scale }, 2 * scale, outlineColor);
        }
        else
        {
            Raylib.DrawRectangleLinesEx(dest with { Y = dest.Y + 2 * scale }, 2 * scale, shadowColor);
            Raylib.DrawRectangleRec(dest, fillColor);
            Raylib.DrawTexturePro(_shapeIcons, atlasSrc, dest, Vector2.Zero, 0, Color.White);
            Raylib.DrawRectangleLinesEx(dest, 2 * scale, outlineColor);
        }

        return (hovered && Raylib.IsMouseButtonPressed(MouseButton.Left));
    }

    bool ZoneOptionsButton(Vector2 position, AiEditorIcon icon)
    {
        Color fillColor = new Color(0.75f, 0.75f, 0.75f);
        Color outlineColor = new Color(0.65f, 0.65f, 0.65f);
        Color shadowColor = new Color(0.50f, 0.50f, 0.50f);
        var scale = Settings.Shared.UIScale;
        Rectangle dest = new Rectangle(position, new Vector2(32 * scale));
        var atlasSrc = new Rectangle(16 * ((int)icon % 4), 16 * (int)((int)icon / 4), 16, 16);
        var hovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), dest);
        if (hovered && Raylib.IsMouseButtonDown(MouseButton.Left))
        {
            Raylib.DrawRectangleRec(dest with { Y = dest.Y + 2 * scale }, fillColor);
            Raylib.DrawTexturePro(_zoneIcons, atlasSrc, dest with { Y = dest.Y + 2 * scale }, Vector2.Zero, 0, Color.White);
            Raylib.DrawRectangleLinesEx(dest with { Y = dest.Y + 2 * scale }, 2 * scale, outlineColor);
        }
        else
        {
            Raylib.DrawRectangleLinesEx(dest with { Y = dest.Y + 2 * scale }, 2 * scale, shadowColor);
            Raylib.DrawRectangleRec(dest, fillColor);
            Raylib.DrawTexturePro(_zoneIcons, atlasSrc, dest, Vector2.Zero, 0, Color.White);
            Raylib.DrawRectangleLinesEx(dest, 2 * scale, outlineColor);
        }

        return (hovered && Raylib.IsMouseButtonPressed(MouseButton.Left));
    }

    void ShapeSelector(Vector2 position, float width)
    {
        var scale = Settings.Shared.UIScale;
        var dest = position;
        foreach (var shape in Enum.GetValues(typeof(ZoneShape)).Cast<ZoneShape>())
        {
            if (ZoneShapeButton(dest, shape, _selectedZone?.Shape == shape))
                if (_selectedZone is not null)
                    UndoManager.Push(ModifyZoneShapeUndoable(_selectedZone, shape));
            dest.X += 32 * scale;
            if (dest.X + (32 * scale) - position.X > width) dest = new Vector2(position.X, position.Y + 32 * scale);
        }
    }

    void ZoneOptions(Vector2 position)
    {
        var scale = Settings.Shared.UIScale;
        var dest = position;
        if (ZoneOptionsButton(dest + new Vector2(0 * 32 * scale, 0), AiEditorIcon.NewZone))
        {
            UndoManager.Push(CreateZoneUndoable());
        }

        if (ZoneOptionsButton(dest + new Vector2(1 * 32 * scale, 0), AiEditorIcon.DeleteZone) || Raylib.IsKeyPressed(KeyboardKey.Delete))
        {
            if (_selectedZone is not null)
            {
                UndoManager.Push(DeleteZoneUndoable(_selectedZone));
                _selectedZone = null;
            }
        }
    }

    void UpdatePanel()
    {
        var scale = Settings.Shared.UIScale;
        var mousePos = Raylib.GetMousePosition();
        var windowSize = new Vector2(Raylib.GetRenderWidth(), Raylib.GetRenderHeight());

        var panelWidth = scale * 262;
        var panelRect = new Rectangle(windowSize.X - panelWidth, ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.Y * 2, panelWidth, windowSize.Y);

        Raylib.DrawRectangleRec(panelRect, ImHelper.Color(ImGuiCol.WindowBg));
        Raylib.DrawRectangleLinesEx(panelRect, 1 * scale, ImHelper.Color(ImGuiCol.Border));
        var pos = panelRect.Position + new Vector2(3 * scale);
        Gui.SetCursorPos(pos);
        Gui.Label("AI Options");
        pos.Y += Gui.MeasureText("AI Options").Y + 3 * scale;
        ZoneOptions(pos);
        if (_selectedZone is not null)
        {
            pos.Y += 32 * scale + 6 * scale;
            var selectionRect = new Rectangle(pos, panelWidth - 6 * scale, 256);
            Raylib.DrawRectangleLinesEx(selectionRect, 3 * scale, ImHelper.Color(ImGuiCol.Border));
            pos += new Vector2(6 * scale);
            Gui.SetCursorPos(pos);
            Gui.Label("Selection Settings");
            pos.Y += Gui.MeasureText("Selection Settings").Y + scale * 3;
            ShapeSelector(pos, panelWidth - 12 * scale);
        }

        _panelFocused = Raylib.CheckCollisionPointRec(mousePos, panelRect);
    }

    void CheckUndo()
    {
        var ctrl = Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl);
        var shift = Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift);
        if (ctrl && !shift && Raylib.IsKeyPressed(KeyboardKey.Z)) UndoManager.Undo();
        if (ctrl && shift && Raylib.IsKeyPressed(KeyboardKey.Z)) UndoManager.Redo();
    }

    void TrackDraw()
    {
        var hoveredZone = _drag is not null ? _drag.Zone : (_panelFocused || _resetConfirmationShown ? null : GetHoveredZone());
        var handle = hoveredZone?.GetResizeHandle(View.MouseTilePos) ?? DragHandle.None;
        DrawZones(hoveredZone);
        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && hoveredZone is null && !(_panelFocused || _resetConfirmationShown) && _selectedZone is not null)
        {
            _selectedZone = null;
        }

        if (Raylib.IsMouseButtonDown(MouseButton.Left))
        {
            if (_drag is null && hoveredZone is not null)
            {
                _drag = new AiDrag(MouseAiBlockPos, hoveredZone, handle);
            }

            if (_drag is not null)
            {
                var dragDelta = MouseAiBlockPos - _drag.StartPosition;
                if (_drag.Handle == DragHandle.None) // Dragging entire zone
                {
                    _drag.Zone.X = (ushort)Math.Clamp(_drag.OriginalZone.X + dragDelta.X, 0, ushort.MaxValue);
                    _drag.Zone.Y = (ushort)Math.Clamp(_drag.OriginalZone.Y + dragDelta.Y, 0, ushort.MaxValue);
                }
                else
                {
                    _drag.Zone.Resize(View.MouseTilePos, _drag.Handle);
                }
            }
        }
        else if (_drag is not null)
        {
            AiZone zoneRef = _drag.Zone;
            AiZone newZone = new AiZone
            {
                X = _drag.Zone.X,
                Y = _drag.Zone.Y,
                Width = _drag.Zone.Width,
                Height = _drag.Zone.Height,
                Shape = _drag.Zone.Shape,
            };
            AiZone originalZone = new AiZone
            {
                X = _drag.OriginalZone.X,
                Y = _drag.OriginalZone.Y,
                Width = _drag.OriginalZone.Width,
                Height = _drag.OriginalZone.Height,
                Shape = _drag.OriginalZone.Shape,
            };

            if (newZone.Equals(originalZone))
            {
                _selectedZone = zoneRef;
            }
            else
            {
                UndoManager.Push(new UndoActions(
                    () =>
                    {
                        zoneRef.X = newZone.X;
                        zoneRef.Y = newZone.Y;
                        zoneRef.Width = newZone.Width;
                        zoneRef.Height = newZone.Height;
                        zoneRef.Shape = newZone.Shape;
                    },
                    () =>
                    {
                        zoneRef.X = originalZone.X;
                        zoneRef.Y = originalZone.Y;
                        zoneRef.Width = originalZone.Width;
                        zoneRef.Height = originalZone.Height;
                        zoneRef.Shape = originalZone.Shape;
                    }
                ));
            }

            _drag = null;
        }

        Raylib.SetMouseCursor(handle switch
        {
            DragHandle.Bottom or DragHandle.Top => MouseCursor.ResizeNs,
            DragHandle.Left or DragHandle.Right => MouseCursor.ResizeEw,
            DragHandle.TopLeft or DragHandle.BottomRight => MouseCursor.ResizeNwse,
            DragHandle.TopRight or DragHandle.BottomLeft => MouseCursor.ResizeNesw,
            _ => hoveredZone is null ? MouseCursor.Default : MouseCursor.ResizeAll
        });
    }

    private AiZone? GetHoveredZone()
    {
        if (_selectedZone is not null && _selectedZone.IsHovered(View.MouseTilePos)) return _selectedZone;
        return View.Track.Ai.Zones.FirstOrDefault(zone => zone.IsHovered(View.MouseTilePos));
    }

    private void DrawZones(AiZone? hovered)
    {
        foreach (var zone in View.Track.Ai.Zones)
        {
            Color color = Color.Blue;
            if (ReferenceEquals(zone, hovered)) color = Color.SkyBlue;
            if (ReferenceEquals(zone, _selectedZone)) color = Color.White;
            DrawZone(zone, color);
        }
    }

    private void DrawZone(AiZone zone, Color color)
    {
        zone.Draw(color with { A = 255 });
    }

    private UndoActions ModifyZoneShapeUndoable(AiZone zone, ZoneShape newShape)
    {
        var oldShape = zone.Shape;
        zone.Shape = newShape;
        if (newShape == ZoneShape.Rectangle && oldShape != ZoneShape.Rectangle) zone.Height = zone.Width;
        return new UndoActions(
            (() => zone.Shape = newShape),
            (() => zone.Shape = oldShape)
        );
    }

    private UndoActions CreateZoneUndoable()
    {
        var ai = View.Track.Ai;
        AiZone newZone;
        int newIndex;
        if (_selectedZone is not null)
        {
            newZone = new AiZone((ushort)(_selectedZone.X + 2), (ushort)(_selectedZone.Y + 2), _selectedZone.Width, _selectedZone.Height, ZoneShape.Rectangle);
            newIndex = ai.Zones.IndexOf(_selectedZone) + 1;
        }
        else
        {
            newZone = AiZone.Default;
            newIndex = ai.Zones.Count;

            // Move camera over created zone. We don't do this if there is a selected zone as it looks too jittery.
            var newZoneRec = newZone.GetZoneRect();
            var zoneCenter = new Vector2(newZoneRec.X + newZoneRec.Width / 2, newZoneRec.Y + newZoneRec.Height / 2) * 16;
            var windowSize = new Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
            var viewportSize = Raylib.GetScreenToWorld2D(windowSize - Vector2.One, View.Camera) - Raylib.GetScreenToWorld2D(Vector2.Zero, View.Camera);
            View.Camera.Target = zoneCenter - viewportSize / 2;
        }

        ai.Zones.Insert(newIndex, newZone);
        foreach (var set in ai.TargetSets)
        {
            set.Insert(newIndex, AiTarget.Default);
        }

        _selectedZone = newZone;

        return new UndoActions(
            () =>
            {
                View.Track.Ai.Zones.Insert(newIndex, newZone);
                foreach (var set in View.Track.Ai.TargetSets)
                {
                    set.Insert(newIndex, AiTarget.Default);
                }
            },
            () =>
            {
                if (ReferenceEquals(View.Track.Ai.Zones[newIndex], _selectedZone)) _selectedZone = null;
                View.Track.Ai.Zones.RemoveAt(newIndex);
                foreach (var set in View.Track.Ai.TargetSets)
                {
                    set.RemoveAt(newIndex);
                }
            }
        );
    }

    private UndoActions DeleteZoneUndoable(AiZone zone)
    {
        var ai = View.Track.Ai;
        var index = ai.Zones.IndexOf(zone);
        var originalZone = ai.Zones[index];
        List<AiTarget> originalTargets = new();
        ai.Zones.RemoveAt(index);
        foreach (var set in ai.TargetSets)
        {
            originalTargets.Add(set[index]);
            set.RemoveAt(index);
        }

        return new UndoActions(
            () =>
            {
                View.Track.Ai.Zones.RemoveAt(index);
                foreach (var set in View.Track.Ai.TargetSets)
                {
                    set.RemoveAt(index);
                }
            },
            () =>
            {
                View.Track.Ai.Zones.Insert(index, originalZone);
                for (var i = 0; i < ai.TargetSets.Count; i++)
                {
                    ai.TargetSets[i].Insert(index, originalTargets[i]);
                }
            }
        );
    }

    public override void Dispose()
    {
        //
    }
}