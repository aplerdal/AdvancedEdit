using AuroraLib.Core.IO;
using MessagePack;

namespace AdvancedLib.Serialization.Objects;

[MessagePackObject]
public class ObstacleTable
{
    private const uint CaseTableAddress = 0x53de8;

    [Key(0)]
    public List<Obstacle> Obstacles { get; set; } = new();

    [IgnoreMember]
    public uint Size => (uint)(Obstacles.Count * 4 + 8);

    public Obstacle this[int i]
    {
        get => Obstacles[i];
        set => Obstacles[i] = value;
    }

    public int IndexOfObstacle(Obstacle obstacle)
    {
        var index = Obstacles.IndexOf(obstacle);
        if (index == -1) throw new Exception("Obstacle not found in table");
        return index;
    }

    public void OverrideExistingTable(Stream romStream, Stream trackStream, Pointer newAddress, int definitionIndex)
    {
        if (definitionIndex > 50) throw new IndexOutOfRangeException("Table not big enough");
        var tablePointerPointer = new Pointer((uint)(0x8053DFC + definitionIndex * 4));
        WriteObstacleTable(trackStream, Obstacles);

        // Write custom ASM to edit table location
        romStream.Seek(tablePointerPointer);
        romStream.Write(newAddress);
    }

    private static void WriteObstacleTable(Stream writer, List<Obstacle> obstacles)
    {
        // Write basic objects (common to all tracks
        foreach (var obstacle in obstacles)
        {
            writer.Write(obstacle.Parameter);
            writer.Write(obstacle.Type);
        }
    }

    public static ObstacleTable ReadTable(Stream reader, int index)
    {
        var caseIdx = index - 4;
        if (caseIdx < 24)
        {
            reader.Seek(CaseTableAddress + caseIdx * 4, SeekOrigin.Begin);
            var casePtr = new Pointer(reader.ReadUInt32());
            // Read ASM code for case. The pointer to the object table is always 4 bytes after the instructions, except in the default case.
            reader.Seek(casePtr.Address + 4, SeekOrigin.Begin);
            if (casePtr.Address == 0x53ed4) // Default case, use global table
            {
                reader.Seek(new Pointer(0x080f1008));
            }
            else
            {
                var obstacleTablePtr = new Pointer(reader.ReadUInt32());
                reader.Seek(obstacleTablePtr);
            }
        }
        else
        {
            reader.Seek(new Pointer(0x080f1008));
        }

        var table = new ObstacleTable();
        var secondTime = false;
        while (true)
        {
            var param = reader.ReadInt16();
            var obj = reader.ReadInt16();
            // This should always work (even if a bit scuffed)
            if (obj == 0)
            {
                if (secondTime) break;
                secondTime = true;
            }

            table.Obstacles.Add(new Obstacle(obj, param));
        }

        table.Obstacles.Add(new Obstacle(0, 0));
        return table;
    }
}