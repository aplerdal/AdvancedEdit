namespace AdvEditRework;

public static class Patcher
{
    private const string PatchText = "PATCH";
    private const string EndOfFile = "EOF";

    public static void Apply(string ipsPath, Stream targetStream)
    {
        using var ips = File.OpenRead(ipsPath);
        var reader = new BinaryReader(ips);

        var header = reader.ReadBytes(5);
        if (System.Text.Encoding.ASCII.GetString(header) != PatchText)
            throw new InvalidDataException("Not a valid IPS file.");

        while (true)
        {
            // Check for EOF marker
            var offsetBytes = reader.ReadBytes(3);
            if (offsetBytes.Length < 3) break;

            string marker = System.Text.Encoding.ASCII.GetString(offsetBytes);
            if (marker == EndOfFile)
                break;

            int offset = (offsetBytes[0] << 16) | (offsetBytes[1] << 8) | offsetBytes[2];
            int size = (reader.ReadByte() << 8) | reader.ReadByte();

            if (size == 0)
            {
                // RLE record
                int rleSize = (reader.ReadByte() << 8) | reader.ReadByte();
                byte value = reader.ReadByte();

                targetStream.Position = offset;
                for (int i = 0; i < rleSize; i++)
                    targetStream.WriteByte(value);
            }
            else
            {
                // Normal record
                byte[] data = reader.ReadBytes(size);
                targetStream.Position = offset;
                targetStream.Write(data, 0, size);
            }
        }
    }
}