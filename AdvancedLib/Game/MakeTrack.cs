using AdvancedLib.Graphics;
using AdvancedLib.Project;
using AdvancedLib.Serialization.AI;

namespace AdvancedLib.Game;

/// <summary>
/// A class for importing MAKE (.smkc) tracks
/// </summary>
public static class MakeTrack
{
    // TODO: Custom object importing. We have track themes, so this should be doable with a little asm hacking.
    public static Track ModifyFromStream(Stream stream, Project.Project project)
    {
        using var reader = new StreamReader(stream);
        byte[]? map = null, aiData = null;
        int? theme = null;

        while (reader.EndOfStream != true)
        {
            var line = reader.ReadLine()!.Trim();
            if (line.Length == 0 || line[0] != '#') continue;
            line = line.Substring(1);
            if (line.Contains(' '))
            {
                var parts = line.Split(' ');
                var name = parts[0];
                var dataStr = parts[1];
                switch (name)
                {
                    case "SP_REGION":
                    {
                        var data = HexStringToBytes(dataStr);
                        if (data.Length != 2) throw new InvalidDataException("Invalid Parameter \"SP_REGION\"");
                        (data[0], data[1]) = (data[1], data[0]); // Reverse endianess
                        theme = BitConverter.ToUInt16(data);
                    } break;
                }
            } else {
                var name = line;
                switch (name)
                {
                    case "MAP":
                        map = ReadDataLines(reader, 16384); 
                        break;
                    case "AREA":
                        aiData = ReadDataLines(reader, 4064); 
                        break;
                }
            }
        }
        if (map is null || aiData is null || !theme.HasValue) throw new InvalidDataException("Missing Parameter(s)");

        var baseProjectTrack = new ProjectTrack(Enum.GetNames(typeof(RetroTheme))[theme.Value >> 1]);
        baseProjectTrack.ResolveFolder(Path.Combine(project.Folder, "themeBase"));
        var baseTrack = baseProjectTrack.LoadTrackData();
        
        // Parse data
        baseTrack.Ai.TargetSets.Clear();
        baseTrack.Ai.Checkpoints.Clear();
        var aiTargets = new List<AiTarget>();
        for (int i = 0; i < (4064 / 32) && aiData[32 * i] != 0xff; i++)
        {
            var checkpoint = new Checkpoint
            {
                Shape = aiData[32 * i + 16] switch
                {
                    0 => CheckpointShape.Rectangle,
                    2 => CheckpointShape.TriangleTopLeft,
                    4 => CheckpointShape.TriangleTopRight,
                    6 => CheckpointShape.TriangleBottomRight,
                    8 => CheckpointShape.TriangleBottomLeft,
                    _ => CheckpointShape.Rectangle,
                },
                X = (byte)(aiData[32 * i + 17]),
                Y = (byte)(aiData[32 * i + 18]),
                Width = (byte)(aiData[32 * i + 19] - 1),
                Height = (byte)(aiData[32 * i + 20] - 1),
            };
            var target = new AiTarget
            {
                Intersection = (aiData[32 * i + 0] & (1 << 7)) != 0,
                Speed = (byte)(aiData[32 * i + 0] & 3),
                X = aiData[32 * i + 1],
                Y = aiData[32 * i + 2],
            };
            baseTrack.Ai.Checkpoints.Add(checkpoint);
            aiTargets.Add(target);
        }

        baseTrack.Ai.TargetSets.AddRange([aiTargets, aiTargets, aiTargets]); // Clone base set 3 times to match SC's format
        baseTrack.Tilemap = new AffineTilemap(map, 128, 128);
        baseTrack.Config.Size = new Vec2I(1, 1);

        return baseTrack;
    }

    private static byte[] HexStringToBytes(string hex)
    {
        if (hex.Length % 2 == 1)
            throw new Exception("The binary key cannot have an odd number of digits");

        byte[] arr = new byte[hex.Length >> 1];

        for (int i = 0; i < hex.Length >> 1; ++i)
        {
            arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
        }

        return arr;
    }

    private static int GetHexVal(char hex)
    {
        int val = (int)hex;
        // Reads uppercase ascii codes.
        return val - (val < 58 ? 48 : 55);
    }

    private static byte[] ReadDataLines(StreamReader reader, int size)
    {
        var data = new byte[size];
        const int bytesPerLine = 32;
        for (int i = 0; i < size / bytesPerLine; i++)
        {
            var line = reader.ReadLine()?[1..];
            if (string.IsNullOrWhiteSpace(line)) throw new InvalidDataException("Data for parameter is invalid");
            var lineData = HexStringToBytes(line);
            if (lineData.Length != bytesPerLine) throw new InvalidDataException("Line for parameter is invalid length");
            Array.Copy(lineData, 0, data, i * bytesPerLine, bytesPerLine);
        }

        return data;
    }
}