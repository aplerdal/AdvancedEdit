using AdvancedLib.Serialization.Allocator;
using AuroraLib.Core.IO;

namespace AdvancedLib.Serialization.Objects;

public class ObstacleTable
{
    private const uint CaseTableAddress = 0x53de8;

    public List<Obstacle> Obstacles { get; set; } = new();

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
        return index + 2;
    }

    public void OverrideExistingTable(Stream writer, int definitionIndex)
    {
        if (definitionIndex < 4 || definitionIndex >= 30) throw new IndexOutOfRangeException();
        var caseReplacementAddress = RomAllocator.Allocate(0x10);
        var newTableAddress = RomAllocator.Allocate(Size);
        writer.Seek(newTableAddress);
        WriteObstacleTable(writer, Obstacles);

        // Write custom ASM to edit table location
        writer.Seek(caseReplacementAddress);
        writer.Write([0x01, 0x49, 0x02, 0x48, 0x00, 0x47, 0x00, 0x00]);
        writer.Write(newTableAddress.Raw);
        writer.Write([0xD7, 0x3E, 0x05, 0x08]);
    }

    private void WriteObstacleTable(Stream writer, List<Obstacle> obstacles)
    {
        // Write basic objects (common to all tracks
        writer.Write((short)0);
        writer.Write((short)0);
        writer.Write((short)0);
        writer.Write((short)-1);
        foreach (var obstacle in obstacles)
        {
            writer.Write(obstacle.Parameter);
            writer.Write(obstacle.Type);
        }
    }
    
    public static ObstacleTable ReadTable(Stream reader, int index)
    {
        //TODO: This way of managing objects sucks. Only some tracks can have custom tables.
        int caseIdx = index - 4;
        if (caseIdx < 24)
        {
            reader.Seek(CaseTableAddress + caseIdx * 4, SeekOrigin.Begin);
            Pointer casePtr = new Pointer(reader.ReadUInt32());
            // Read ASM code for case. The pointer to the object table is always 4 bytes after the instructions, except in the default case.
            reader.Seek(casePtr.Address + 4, SeekOrigin.Begin);
            if (casePtr.Address == 0x53ed4) // Default case, use global table
            {
                reader.Seek(new Pointer(0x080f1008));
            }
            else
            {
                Pointer obstacleTablePtr = new Pointer(reader.ReadUInt32());
                reader.Seek(obstacleTablePtr);
            }
        }
        else
        {
            reader.Seek(new Pointer(0x080f1008));
        }
        
        var table = new ObstacleTable();
        bool secondTime = false;
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
        table.Obstacles.Add(new Obstacle(0,0));
        return table;
    }
}