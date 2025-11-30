using System.IO;
using IMGBlibrary.Extensions;
using IMGBlibrary.Support;

namespace IMGBlibrary.Repack.Strategy;

internal class ClassicRepacker(IMGBVariables vars, Stream imgb, Stream gtex) : RepackStrategy(vars, imgb, gtex)
{
    public override void Execute(string extractedDir)
    {
        string ddsName = $"{_vars.ImgHeaderBlockFileName}.dds";
        string fullPath = Path.Combine(extractedDir, ddsName);

        // Read the start of the Mip Table from GTEX
        _gtexReader.BaseStream.Position = _vars.GtexStartVal + 16;
        uint mipTableOffset = _gtexReader.ReadBytesUInt32(true);
        uint absoluteMipTablePos = _vars.GtexStartVal + mipTableOffset;

        ProcessDDS(fullPath, absoluteMipTablePos);
    }
}