using MessagePack;

namespace AdvancedLib.Serialization.Objects;

[MessagePackObject]
public class ObstacleOam
{
    [Key(0)]
    public DistanceCellData[] DistanceCellDataTable = [];

    public ObstacleOam(){}
    public ObstacleOam(Stream stream)
    {
        DistanceCellDataTable = LoadCellDataTable(stream);
    }

    public DistanceCellData GetObjectDistanceCells(short type, short param)
        => DistanceCellDataTable[(int)GetObjectCellDataIndex((ushort)type, (ushort)param)];
    
    [IgnoreMember]
    private static readonly Pointer[] PointerTable =
    [
        new(0x080efe88), // Fallback
        new(0x080f06a8), // Type01_0
        new(0x080f06c8), // Type01_1
        new(0x080f0488), // Type02
        new(0x080f0fa8), // Type04
        new(0x080f0208), // Type05_0 / Type50
        new(0x080f0228), // Type05_1
        new(0x080f0248), // Type05_2
        new(0x080f03c8), // Type06_0
        new(0x080f03e8), // Type06_1
        new(0x080f0468), // Type07 / Type70
        new(0x080f04a8), // Type08
        new(0x080f04c8), // Type09
        new(0x080f02c8), // Type0a_0
        new(0x080f02e8), // Type0a_1
        new(0x080f0308), // Type0a_2
        new(0x080f0328), // Type0a_3
        new(0x080f0348), // Type0a_4
        new(0x080f0368), // Type0a_5
        new(0x080f0388), // Type0a_6
        new(0x080f03a8), // Type0a_7
        new(0x080f01a8), // LargeFallback
        new(0x080f0408), // Type0b_0
        new(0x080f0428), // Type0b_1
        new(0x080f09e8), // Type0c_0
        new(0x080f0148), // Type0c_1
        new(0x080f04e8), // Type0c_2
        new(0x080efde8), // Type0c_3
        new(0x080f0508), // Type0c_4
        new(0x080f0168), // Type0c_5
        new(0x080f0188), // Type0c_6
        new(0x080f0d48), // Type0c_8
        new(0x080f0dc8), // Type0c_9
        new(0x080f0e08), // Type0c_a
        new(0x080f0448), // Type0c_b
        new(0x080f0fc8), // Type0e
        new(0x080effc8), // Type80  Crab
        new(0x080efee8), // Type81  Thwomp
        new(0x080eff08), // Type82  Fireball
        new(0x080f02a8), // Type83
        new(0x080f0048), // Type84  Cannonball
        new(0x080efe48), // Type85 / Type8c / Type90
        new(0x080eff88), // Type86  Umbrella
        new(0x080f0528), // Type87
        new(0x080f0628), // Type88
        new(0x080f0648), // Type89
        new(0x080f0588), // Type8a
        new(0x080f07a8), // Type8d
        new(0x080f01c8), // Type8e
        new(0x080f08e8), // Type8f_0
        new(0x080f0968), // Type8f_X
        new(0x080f0268), // Type91
        new(0x080f0668), // Type92
        new(0x080f0848), // Type94
        new(0x080efe88), // Type95 / Type96 (same address as fallback)
        new(0x080f0a08), // Type97
        new(0x080f0aa8), // Type98_0
        new(0x080f0ac8), // Type98_1
        new(0x080f0ae8), // Type99
        new(0x080f0bc8), // Type9a
        new(0x080f0be8), // Type9b
        new(0x080f0c08), // Type9c  Penguin
        new(0x080f0cc8), // Type9d
        new(0x080f0d68), // Type9e
        new(0x080f0de8), // Type9f
        new(0x080f0e28), // TypeA0
        new(0x080f0e68), // TypeA1
        new(0x080f0ee8), // TypeA2
        new(0x080f0fe8), // TypeA3
    ];

    private static DistanceCellData[] LoadCellDataTable(Stream stream)
    {
        var table = new DistanceCellData[(int)DistanceCellDataIndex.Count];
        for (int i = 0; i < (int)DistanceCellDataIndex.Count; i++)
        {
            stream.Seek(PointerTable[i]);
            table[i] = stream.Read<DistanceCellData>();
        }
        return table;
    }

    private static DistanceCellDataIndex GetObjectCellDataIndex(ushort type, ushort param) => type switch
    {
        // 0x01 — Unknown
        0x1 => param switch
        {
            0x1 => DistanceCellDataIndex.Type01_1,
            _   => DistanceCellDataIndex.Type01_0,
        },

        // 0x02 — Unknown
        0x2 => DistanceCellDataIndex.Type02,

        // 0x04 — Unknown
        0x4 => DistanceCellDataIndex.Type04,

        // 0x05 — Unknown
        0x5 => param switch
        {
            0x0 => DistanceCellDataIndex.Type05_0,
            0x1 => DistanceCellDataIndex.Type05_1,
            0x2 => DistanceCellDataIndex.Type05_2,
            _   => DistanceCellDataIndex.LargeFallback,
        },

        // 0x06 — Unknown
        0x6 => param switch
        {
            0x0 => DistanceCellDataIndex.Type06_0,
            0x1 => DistanceCellDataIndex.Type06_1,
            _   => DistanceCellDataIndex.LargeFallback,
        },

        // 0x07 — Unknown
        0x7 => DistanceCellDataIndex.Type07,

        // 0x08 — Unknown
        0x8 => DistanceCellDataIndex.Type08,

        // 0x09 — Unknown
        0x9 => DistanceCellDataIndex.Type09,

        // 0x0a — Rock / under-track pillar variants
        0xa => param switch
        {
            0x0 => DistanceCellDataIndex.Type0a_0,
            0x1 => DistanceCellDataIndex.Type0a_1,
            0x2 => DistanceCellDataIndex.Type0a_2,
            0x3 => DistanceCellDataIndex.Type0a_3,
            0x4 => DistanceCellDataIndex.Type0a_4,
            0x5 => DistanceCellDataIndex.Type0a_5,
            0x6 => DistanceCellDataIndex.Type0a_6,
            0x7 => DistanceCellDataIndex.Type0a_7,
            _   => DistanceCellDataIndex.LargeFallback,
        },

        // 0x0b — Ghost
        0xb => param switch
        {
            0x0 => DistanceCellDataIndex.Type0b_0,
            _   => DistanceCellDataIndex.Type0b_1,
        },

        // 0x0c — Tree variants
        0xc => param switch
        {
            0x0 => DistanceCellDataIndex.Type0c_0,
            0x1 => DistanceCellDataIndex.Type0c_1,
            0x2 => DistanceCellDataIndex.Type0c_2,
            0x3 => DistanceCellDataIndex.Type0c_3,
            0x4 => DistanceCellDataIndex.Type0c_4,
            0x5 => DistanceCellDataIndex.Type0c_5,
            0x6 => DistanceCellDataIndex.Type0c_6,
            0x7 => DistanceCellDataIndex.LargeFallback,
            0x8 => DistanceCellDataIndex.Type0c_8,
            0x9 => DistanceCellDataIndex.Type0c_9,
            0xa => DistanceCellDataIndex.Type0c_a,
            0xb => DistanceCellDataIndex.Type0c_b,
            _   => DistanceCellDataIndex.LargeFallback,
        },

        // 0x0e — Unknown
        0xe => DistanceCellDataIndex.Type0e,

        // 0x50 — Unknown (shares OAM with 0x05 param 0)
        0x50 => DistanceCellDataIndex.Type05_0,

        // 0x70 — Unknown (shares OAM with 0x07)
        0x70 => DistanceCellDataIndex.Type07,

        // 0x80 — Crab
        0x80 => DistanceCellDataIndex.Type80,

        // 0x81 — Thwomp
        0x81 => DistanceCellDataIndex.Type81,

        // 0x82 — Fireball
        0x82 => DistanceCellDataIndex.Type82,

        // 0x83 — Unknown
        0x83 => DistanceCellDataIndex.Type83,

        // 0x84 — Cannonball
        0x84 => DistanceCellDataIndex.Type84,

        // 0x85 — Unknown (shares OAM with 0x8c, 0x90)
        0x85 => DistanceCellDataIndex.Type85,

        // 0x86 — Umbrella
        0x86 => DistanceCellDataIndex.Type86,

        // 0x87 — Unknown
        0x87 => DistanceCellDataIndex.Type87,

        // 0x88 — Unknown
        0x88 => DistanceCellDataIndex.Type88,

        // 0x89 — Unknown
        0x89 => DistanceCellDataIndex.Type89,

        // 0x8a — Unknown
        0x8a => DistanceCellDataIndex.Type8a,

        // 0x8c — Unknown (shares OAM with 0x85, 0x90)
        0x8c => DistanceCellDataIndex.Type85,

        // 0x8d — Unknown
        0x8d => DistanceCellDataIndex.Type8d,

        // 0x8e — Unknown
        0x8e => DistanceCellDataIndex.Type8e,

        // 0x8f — Unknown
        0x8f => param switch
        {
            0 or 1 => DistanceCellDataIndex.Type8f_0,
            _      => DistanceCellDataIndex.Type8f_X,
        },

        // 0x90 — Unknown (shares OAM with 0x85, 0x8c)
        0x90 => DistanceCellDataIndex.Type85,

        // 0x91 — Unknown
        0x91 => DistanceCellDataIndex.Type91,

        // 0x92 — Unknown
        0x92 => DistanceCellDataIndex.Type92,

        // 0x94 — Unknown
        0x94 => DistanceCellDataIndex.Type94,

        // 0x95 — Unknown
        0x95 => DistanceCellDataIndex.Type95_96,

        // 0x96 — Unknown
        0x96 => DistanceCellDataIndex.Type95_96,

        // 0x97 — Unknown
        0x97 => DistanceCellDataIndex.Type97,

        // 0x98 — Unknown
        0x98 => param switch
        {
            0x1 => DistanceCellDataIndex.Type98_1,
            _   => DistanceCellDataIndex.Type98_0,
        },

        // 0x99 — Unknown
        0x99 => DistanceCellDataIndex.Type99,

        // 0x9a — Unknown
        0x9a => DistanceCellDataIndex.Type9a,

        // 0x9b — Unknown
        0x9b => DistanceCellDataIndex.Type9b,

        // 0x9c — Penguin
        0x9c => DistanceCellDataIndex.Type9c,

        // 0x9d — Unknown
        0x9d => DistanceCellDataIndex.Type9d,

        // 0x9e — Unknown
        0x9e => DistanceCellDataIndex.Type9e,

        // 0x9f — Unknown
        0x9f => DistanceCellDataIndex.Type9f,

        // 0xa0 — Unknown
        0xa0 => DistanceCellDataIndex.TypeA0,

        // 0xa1 — Unknown
        0xa1 => DistanceCellDataIndex.TypeA1,

        // 0xa2 — Unknown
        0xa2 => DistanceCellDataIndex.TypeA2,

        // 0xa3 — Unknown
        0xa3 => DistanceCellDataIndex.TypeA3,

        _ => DistanceCellDataIndex.Fallback,
    };
}