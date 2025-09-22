namespace AdvEditRework.UI.Undo;

public class UndoManager
{
    private readonly Stack<UndoActions> _undoStack = new();
    private readonly Stack<UndoActions> _redoStack = new();

    /// <summary>
    /// Add the UndoActions to the undo stack
    /// </summary>
    /// <param name="actions">The actions to be run on undo and redo</param>
    public void Push(UndoActions actions)
    {
        _undoStack.Push(actions);
        _redoStack.Clear();
    }

    /// <summary>
    /// Undo the previous action
    /// </summary>
    public void Undo()
    {
        if (_undoStack.Count <= 0) return;
        var actions = _undoStack.Pop();
        actions.UndoAction();
        _redoStack.Push(actions);
    }

    /// <summary>
    /// Redo the previous undone action
    /// </summary>
    public void Redo()
    {
        if (_redoStack.Count <= 0) return;
        var actions = _redoStack.Pop();
        actions.DoAction();
        _undoStack.Push(actions);
    }
}