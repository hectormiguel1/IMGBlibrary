using IMGBlibrary.Extensions; // For ReadBytesUInt32 if strictly needed, or standard BinaryReader
using System.IO;
using IMGBlibrary.Support;

namespace IMGBlibrary.Unpack.Strategy
{
    internal class ClassicUnpacker(IMGBVariables vars, Stream imgb, Stream gtex) : UnpackStrategy(vars, imgb, gtex) 
    {
        public override void Execute(string extractDir)
        {
            var ddsPath = Path.Combine(extractDir, $"{_vars.ImgHeaderBlockFileName}.dds");

            _gtexReader.BaseStream.Position = _vars.GtexStartVal + 16;
            var mipTableOffset = _gtexReader.ReadBytesUInt32(true);
            var mipReadPos = _vars.GtexStartVal + mipTableOffset;

            CreateAndWriteDDS(ddsPath, (ddsStream) => 
            {
                for (var m = 0; m < _vars.GtexImgMipCount; m++)
                {
                    _gtexReader.BaseStream.Position = mipReadPos;
                    var mipStart = _gtexReader.ReadBytesUInt32(true);
                    var mipSize = _gtexReader.ReadBytesUInt32(true);

                    CopyMipToDDS(ddsStream, mipStart, mipSize);
                    mipReadPos += 8;
                }
            });
        }
    }
}