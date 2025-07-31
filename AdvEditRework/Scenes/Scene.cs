namespace AdvEditRework.Scenes;

public abstract class Scene : IDisposable
{
    public abstract void Init(RomManager rom);
    public abstract void Update(RomManager rom);
    public abstract void Dispose();
}