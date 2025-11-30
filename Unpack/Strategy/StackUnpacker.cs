using IMGBlibrary.Extensions;
using System.IO;
using IMGBlibrary.Support;

namespace IMGBlibrary.Unpack.Strategy
{
    internal class StackUnpacker(IMGBVariables vars, Stream imgb, Stream gtex) : UnpackStrategy(vars, imgb, gtex)
    {
        public override void Execute(string extractDir)
        {
            _gtexReader.BaseStream.Position = _vars.GtexStartVal + 16;
            var mipTableOffset = _gtexReader.ReadBytesUInt32(true);
            var mipReadPos = _vars.GtexStartVal + mipTableOffset;

            // Read the single continuous block definition
            _gtexReader.BaseStream.Position = mipReadPos;
            var mipStart = _gtexReader.ReadBytesUInt32(true);
            var totalSize = _gtexReader.ReadBytesUInt32(true);

            // Note: Original logic divides total size by 4 to get slice size.
            // This logic is preserved from IMGBUnpackTypes.cs
            var sliceSize = totalSize / 4; 

            for (var i = 1; i <= _vars.GtexImgDepth; i++)
            {
                var ddsPath = Path.Combine(extractDir, $"{_vars.ImgHeaderBlockFileName}{_vars.GtexImgType}{i}.dds");

                CreateAndWriteDDS(ddsPath, (ddsStream) =>
                {
                    CopyMipToDDS(ddsStream, mipStart, sliceSize);
                    mipStart += sliceSize;
                });
            }
        }
    }
}