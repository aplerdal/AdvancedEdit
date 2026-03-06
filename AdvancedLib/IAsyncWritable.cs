namespace AdvancedLib;

public interface IAsyncWritable
{
    public Task WriteAsync(Stream stream);
}