namespace AdvEditRework;

public class RomManager : IDisposable
{
    public Stream? RomStream { get; private set; }
    public string FileName { get; set; }
    public bool FileLoaded => RomStream is null;

    public RomManager()
    {
        
    }

    public void Load(string file)
    {
        RomStream = File.OpenRead(file);
        FileName = Path.GetFileName(file);
    }

    public void Dispose()
    {
        RomStream?.Dispose();
    }
}