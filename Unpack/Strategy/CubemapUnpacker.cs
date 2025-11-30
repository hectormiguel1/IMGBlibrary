using IMGBlibrary.Extensions;
using System.IO;
using IMGBlibrary.Support;

namespace IMGBlibrary.Unpack.Strategy
{
    internal class CubemapUnpacker(IMGBVariables vars, Stream imgb, Stream gtex) : UnpackStrategy(vars, imgb, gtex)
    {
        public override void Execute(string extractDir)
        {
            _gtexReader.BaseStream.Position = _vars.GtexStartVal + 16;
            var mipTableOffset = _gtexReader.ReadBytesUInt32(true);
            var mipReadPos = _vars.GtexStartVal + mipTableOffset;

            for (var i = 1; i <= 6; i++)
            {
                string ddsPath = Path.Combine(extractDir, $"{_vars.ImgHeaderBlockFileName}{_vars.GtexImgType}{i}.dds");

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
}