using AuroraLib.Core.IO;

namespace AdvancedLib.Serialization.Tracks;

public class TargetOptions : ISerializable
{
    private const int PlaceOptions = 5;
    private const int Places = 8;
    private byte[] _options = new byte[PlaceOptions * Places];
    public void Serialize(Stream stream)
    {
        for (int place = 0; place < Places; place++)
            for(int option = 0; option < PlaceOptions; option++)
                stream.Write(_options[option + place * PlaceOptions]);
    }

    public void Deserialize(Stream stream)
    {
        for (int place = 0; place < Places; place++)
            for (int option = 0; option < PlaceOptions; option++)
                _options[option + place * PlaceOptions] = stream.ReadUInt8();
    }

    public byte this[int option, int place] => _options[option + place * PlaceOptions];
    public int SetCount => _options.Max() + 1;
}