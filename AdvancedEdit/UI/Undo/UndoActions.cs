namespace AdvEditRework.UI.Undo;

public delegate void DoAction();

public delegate void UndoAction();

public record UndoActions(DoAction DoAction, UndoAction UndoAction);