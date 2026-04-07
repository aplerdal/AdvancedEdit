namespace AdvancedLib.Serialization.Objects;

public enum DistanceCellDataIndex
{
    Fallback = 0, // 0x080efe88 — unknown / unused fallback

    Type01_0 = 1, // 0x080f06a8
    Type01_1 = 2, // 0x080f06c8

    Type02 = 3, // 0x080f0488

    Type04 = 4, // 0x080f0fa8

    Type05_0 = 5, // 0x080f0208  (shared with 0x50)
    Type05_1 = 6, // 0x080f0228
    Type05_2 = 7, // 0x080f0248

    Type06_0 = 8, // 0x080f03c8
    Type06_1 = 9, // 0x080f03e8

    Type07 = 10, // 0x080f0468  (shared with 0x70)

    Type08 = 11, // 0x080f04a8
    Type09 = 12, // 0x080f04c8

    Type0a_0 = 13, // 0x080f02c8
    Type0a_1 = 14, // 0x080f02e8
    Type0a_2 = 15, // 0x080f0308
    Type0a_3 = 16, // 0x080f0328
    Type0a_4 = 17, // 0x080f0348
    Type0a_5 = 18, // 0x080f0368
    Type0a_6 = 19, // 0x080f0388
    Type0a_7 = 20, // 0x080f03a8
    LargeFallback = 21, // 0x080f01a8 — large object fallback (shared)

    Type0b_0 = 22, // 0x080f0408
    Type0b_1 = 23, // 0x080f0428

    Type0c_0 = 24, // 0x080f09e8
    Type0c_1 = 25, // 0x080f0148
    Type0c_2 = 26, // 0x080f04e8
    Type0c_3 = 27, // 0x080efde8
    Type0c_4 = 28, // 0x080f0508
    Type0c_5 = 29, // 0x080f0168
    Type0c_6 = 30, // 0x080f0188
    Type0c_8 = 31, // 0x080f0d48
    Type0c_9 = 32, // 0x080f0dc8
    Type0c_a = 33, // 0x080f0e08
    Type0c_b = 34, // 0x080f0448

    Type0e = 35, // 0x080f0fc8

    Type80 = 36, // 0x080effc8  Crab
    Type81 = 37, // 0x080efee8  Thwomp
    Type82 = 38, // 0x080eff08  Fireball
    Type83 = 39, // 0x080f02a8
    Type84 = 40, // 0x080f0048  Cannonball
    Type85 = 41, // 0x080efe48  (shared with 0x8c, 0x90)
    Type86 = 42, // 0x080eff88  Umbrella
    Type87 = 43, // 0x080f0528
    Type88 = 44, // 0x080f0628
    Type89 = 45, // 0x080f0648
    Type8a = 46, // 0x080f0588
    Type8d = 47, // 0x080f07a8
    Type8e = 48, // 0x080f01c8
    Type8f_0 = 49, // 0x080f08e8
    Type8f_X = 50, // 0x080f0968
    Type91 = 51, // 0x080f0268
    Type92 = 52, // 0x080f0668
    Type94 = 53, // 0x080f0848
    Type95_96 = 54, // 0x080efe88  (same address as fallback, aliased for clarity)
    Type97 = 55, // 0x080f0a08
    Type98_0 = 56, // 0x080f0aa8
    Type98_1 = 57, // 0x080f0ac8
    Type99 = 58, // 0x080f0ae8
    Type9a = 59, // 0x080f0bc8
    Type9b = 60, // 0x080f0be8
    Type9c = 61, // 0x080f0c08  Penguin
    Type9d = 62, // 0x080f0cc8
    Type9e = 63, // 0x080f0d68
    Type9f = 64, // 0x080f0de8
    TypeA0 = 65, // 0x080f0e28
    TypeA1 = 66, // 0x080f0e68
    TypeA2 = 67, // 0x080f0ee8
    TypeA3 = 68, // 0x080f0fe8

    Count = 69,
}