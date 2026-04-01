namespace AdvEditRework.UI.Editors;

public abstract class Editor : IDisposable
{
    public abstract void Update(bool hasFocus);
    public abstract void Dispose();
}