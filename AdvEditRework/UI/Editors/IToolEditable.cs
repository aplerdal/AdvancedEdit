using System.Numerics;
using AdvEditRework.UI.Undo;
using Raylib_cs;

namespace AdvEditRework.UI.Editors;

public record struct CellEntry(Vector2 Position, byte Id);

public interface IToolEditable
{
    public bool ViewportHovered { get; }
    public bool Focused { get; }
    public byte? ActiveIndex { get; set; }
    
    public Vector2 CellMousePos { get; }
    public Vector2 GridSize { get; }
    public CellEntry[]? Stamp { get; set; }
    public int CellSize { get;}
    
    public void DrawCell(Vector2 position, byte id, Color color);
    public void OutlineCell(Vector2 position, Color color);
    public bool ValidCell(Vector2 position);
    public byte GetCell(Vector2 position);
    
    public UndoActions SetCellsUndoable(HashSet<Vector2> positions, byte id);
    public UndoActions SetCellsUndoable(List<CellEntry> cells);
    public UndoActions SetCellsUndoable(Rectangle area, byte id);

    public void SetTool(MapEditorToolType tool);

    public void PushUndoable(UndoActions action);
}