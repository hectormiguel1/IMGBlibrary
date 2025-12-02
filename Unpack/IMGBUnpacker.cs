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
                SharedMethods.DisplayLogMessage("Unable to find GTEX chunk. Skipped.", showLog);
                return;
            }

            // Platform setup
            vars.IsWin32Imgb = (platform == IMGBEnums.Platforms.win32);
            vars.IsPs3Imgb = (platform == IMGBEnums.Platforms.ps3);
            vars.IsX360Imgb = (platform == IMGBEnums.Platforms.x360);

            if (vars.IsX360Imgb) SharedMethods.DisplayLogMessage("X360 platform: images will not be unswizzled.", showLog);

            // Read Info & Validate
            SharedMethods.GetImageInfo(headerFile, vars);

            if (!IMGBVariables.GtexImgFormatValues.Contains(vars.GtexImgFormatValue) || 
                !IMGBVariables.GtexImgTypeValues.Contains(vars.GtexImgTypeValue))
            {
                SharedMethods.DisplayLogMessage("Unknown Format or Type. Skipped.", showLog);
                return;
            }

            // Execute Strategy
            using (var imgbStream = new FileStream(imgbFile, FileMode.Open, FileAccess.ReadWrite))
            using (var gtexStream = new FileStream(headerFile, FileMode.Open, FileAccess.Read))
            {
                UnpackStrategy? strategy = vars.GtexImgTypeValue switch
                {
                    0 or 4 => new ClassicUnpacker(vars, imgbStream, gtexStream),
                    1 or 5 => new CubemapUnpacker(vars, imgbStream, gtexStream),
                    2 => new StackUnpacker(vars, imgbStream, gtexStream),
                    _ => null
                };

                if (strategy == null)
                {
                    SharedMethods.DisplayLogMessage("Unsupported Image Type", showLog);
                    return;
                }

                if (vars.GtexImgTypeValue == 2 && vars.GtexImgMipCount > 1)
                {
                    SharedMethods.DisplayLogMessage("Stack images with > 1 Mip not supported.", showLog);
                    return;
                }

                strategy.Execute(outDir);
            }
        }
    }
}