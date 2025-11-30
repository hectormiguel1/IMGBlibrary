using System.IO;
using IMGBlibrary.Support;

namespace IMGBlibrary.Repack.Strategy
{
    internal class CubemapRepacker(IMGBVariables vars, Stream imgb, Stream gtex) : RepackStrategy(vars, imgb, gtex)
    {
        public override void Execute(string extractedDir)
        {
            // Cubemap has 6 faces
            var currentMipTablePos = _vars.GtexStartVal + 24; // Initial offset for Cubemap face 1? (Logic derived from original)

            // Logic to read initial offset from header if needed
            // ...

            for (var i = 1; i <= 6; i++)
            {
                var ddsName = $"{_vars.ImgHeaderBlockFileName}{_vars.GtexImgType}{i}.dds";
                var fullPath = Path.Combine(extractedDir, ddsName);

                ProcessDDS(fullPath, currentMipTablePos);

                // Advance Mip Table Position by (MipCount * 8 bytes)
                var mips = _vars.IsType2Repack ? (int)_vars.OutImgMipCount : _vars.GtexImgMipCount;
                currentMipTablePos += (uint)(mips * 8);
            }
        }
    }
}