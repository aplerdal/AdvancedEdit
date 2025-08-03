using AdvancedLib.Project;

namespace AdvEditRework.Scenes;

public abstract class Scene : IDisposable
{
    public abstract void Init(ref Project? project);
    public abstract void Update(ref Project? project);
    public abstract void Dispose();
}