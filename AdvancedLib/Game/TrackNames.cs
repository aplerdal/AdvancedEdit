namespace AdvancedLib.Game;

public static class TrackNames
{
    public static readonly int[] CupTracks =
    {
        32, 33, 34, 35, // SNES    Mushroom
        36, 37, 38, 39, //         Flower
        40, 41, 42, 43, //         Lightning
        44, 45, 46, 47, //         Star
        48, 49, 50, 51, //         Special
        52, 53, 54, 55, //         Battle
        4, 5, 9, 7,     // MKSC    Mushroom
        12, 17, 18, 11, //         Flower 
        8, 20, 13, 6,   //         Lightning
        16, 14, 10, 15, //         Star
        23, 21, 22, 19, //         Special
        24, 25, 26, 27, //         Battle
    };

    private static readonly int[] TrackMapping = 
    [
        0,  1,   2,  3, // SNES Mushroom
        4,  5,   6,  7, //      Flower
        8,  9,  10, 11, //      Lightning
        12, 13, 14, 15, //      Star
        16, 17, 18, 19, //      Special
        20, 21, 22, 23, //      Battle
        24, 25, 29, 27, // MKSC Mushroom
        32, 37, 38, 31, //      Flower 
        28, 40, 33, 26, //      Lightning
        36, 34, 30, 35, //      Star
        43, 41, 42, 39, //      Special
        44, 45, 46, 47, //      Battle
    ];

    private static readonly string[] Pages = 
    [
        "SNES Tracks",
        "MKSC Tracks"
    ];

    public static readonly string[] Cups =
    [
        // MKSC
        "Mushroom Cup",
        "Flower Cup",
        "Lightning Cup",
        "Star Cup",
        "Special Cup",
        "Victory",
        // SNES
        "Retro Mushroom Cup",
        "Retro Flower Cup",
        "Retro Lightning Cup",
        "Retro Star Cup",
        "Retro Special Cup",
        "Retro Battle",
        "Battle",
    ];

    private static readonly string[] TrackNameMap =
    [
        // SNES Mushroom
        "Mario Circuit 1",
        "Donut Plains 1",
        "Ghost Valley 1",
        "Bowser Castle 1",
    // SNES Flower
        "Mario Circuit 2",
        "Choco Island 1",
        "Ghost Valley 2",
        "Donut Plains 2",
    // SNES Lightning
        "Bowser Castle 2",
        "Mario Circuit 3",
        "Koopa Beach 1",
        "Choco Island 2",
    // SNES Star
        "Vanilla Lake 1",
        "Bowser Castle 3",
        "Mario Circuit 4",
        "Donut Plains 3",
    // SNES Special
        "Koopa Beach 2",
        "Ghost Valley 3",
        "Vanilla Lake 2",
        "Rainbow Road",
    // SNES Battle
        "Battle Course 1",
        "Battle Course 2",
        "Battle Course 3",
        "Battle Course 4",
    // Mushroom Cup
        "Peach Circuit",
        "Shy Guy Beach",
        "Riverside Park",
        "Bowser Castle 1",
    // Flower
        "Mario Circuit",
        "Boo Lake",
        "Cheese Land",
        "Bowser Castle 2",
    // Lightning
        "Luigi Circuit",
        "Sky Garden",
        "Cheep Cheep Island",
        "Sunset Wilds",
    // Star
        "Snow Land",
        "Ribbon Road",
        "Yoshi Desert",
        "Bowser Castle 3",
    // Special
        "Lakeside Park",
        "Broken Pier",
        "Bowser Castle 4",
        "Rainbow Road",
    // Battle
        "Battle Course 1",
        "Battle Course 2",
        "Battle Course 3",
        "Battle Course 4"
    ];

    private static int GetHeaderCupIndex(int headerIdx) => Array.IndexOf(CupTracks, headerIdx);

    public static string GetTrackNameFromHeaderIndex(int headerIdx)
    {
        var cupIdx = GetHeaderCupIndex(headerIdx);
        if (cupIdx == -1) return "";
        return TrackNameMap[cupIdx];
    }
}