using IMGBlibrary.Support;
using IMGBlibrary.Unpack.Strategy;
using System.IO;
using System.Linq;

namespace IMGBlibrary.Unpack
{
    public static class IMGBUnpacker
    {
        public static void Unpack(string headerFile, string imgbFile, string outDir, IMGBEnums.Platforms platform, bool showLog)
        {
            var vars = new IMGBVariables
            {
                ShowLog = showLog,
                ImgHeaderBlockFileName = Path.GetFileName(headerFile),
                GtexStartVal = SharedMethods.GetGTEXChunkPos(headerFile)
            };

            if (vars.GtexStartVal == 0)
            {
                Log.Warning("Unable to find GTEX chunk. Skipped.");
                return;
            }

            // Platform setup
            vars.IsWin32Imgb = (platform == IMGBEnums.Platforms.win32);
            vars.IsPs3Imgb = (platform == IMGBEnums.Platforms.ps3);
            vars.IsX360Imgb = (platform == IMGBEnums.Platforms.x360);

            if (vars.IsX360Imgb) Log.Warning("X360 platform: images will not be unswizzled.");

            // Read Info & Validate
            SharedMethods.GetImageInfo(headerFile, vars);

            if (!IMGBVariables.GtexImgFormatValues.Contains(vars.GtexImgFormatValue) || 
                !IMGBVariables.GtexImgTypeValues.Contains(vars.GtexImgTypeValue))
            {
                Log.Warning("Unknown Format or Type. Skipped.");
                return;
            }

            // Execute Strategy
            using var imgbStream = new FileStream(imgbFile, FileMode.Open, FileAccess.ReadWrite);
            using var gtexStream = new FileStream(headerFile, FileMode.Open, FileAccess.Read);
            UnpackStrategy? strategy = vars.GtexImgTypeValue switch
            {
                0 or 4 => new ClassicUnpacker(vars, imgbStream, gtexStream),
                1 or 5 => new CubemapUnpacker(vars, imgbStream, gtexStream),
                2 => new StackUnpacker(vars, imgbStream, gtexStream),
                _ => null
            };

            if (strategy == null)
            {
                Log.Warning("Unsupported Image Type");
                return;
            }

            if (vars is { GtexImgTypeValue: 2, GtexImgMipCount: > 1 })
            {
                Log.Warning("Stack images with > 1 Mip not supported.");
                return;
            }

            strategy.Execute(outDir);
        }
    }
}