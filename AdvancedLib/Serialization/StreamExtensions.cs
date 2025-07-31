namespace AdvancedLib.Serialization;

public static class StreamExtensions
{
    /// <summary>
    /// Seeks stream to the address of the given pointer
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="pointer"></param>
    public static void Seek(this Stream stream, Pointer pointer)
    {
        stream.Seek(pointer.Address, SeekOrigin.Begin);
    }

    public static T Read<T>(this Stream stream) where T : ISerializable, new()
    {
        var obj = new T();
        obj.Deserialize(stream);
        return obj;
    }

    public static void Write<T>(this Stream stream, T obj) where T : ISerializable
    {
        obj.Serialize(stream);
    }
}