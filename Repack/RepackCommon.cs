using IMGBlibrary.Support;
using System.Linq;

namespace IMGBlibrary.Repack
{
    internal static class RepackCommon
    {
        /// <summary>
        /// Validates the Header Block file and populates the IMGBVariables.
        /// Returns false if validation fails (logging is handled internally).
        /// </summary>
        public static bool ValidateAndSetup(string imgHeaderBlockFile, IMGBVariables vars, IMGBEnums.Platforms platform)
        {
            // 1. Locate GTEX Chunk
            vars.GtexStartVal = SharedMethods.GetGTEXChunkPos(imgHeaderBlockFile);
            if (vars.GtexStartVal == 0)
            {
                SharedMethods.DisplayLogMessage("Unable to find GTEX chunk. Skipped.", vars.ShowLog);
                return false;
            }

            // 2. Read Image Info
            SharedMethods.GetImageInfo(imgHeaderBlockFile, vars);

            // 3. Validate Format and Type
            if (!IMGBVariables.GtexImgFormatValues.Contains(vars.GtexImgFormatValue))
            {
                SharedMethods.DisplayLogMessage("Detected unknown image format. Skipped.", vars.ShowLog);
                return false;
            }

            if (!IMGBVariables.GtexImgTypeValues.Contains(vars.GtexImgTypeValue))
            {
                SharedMethods.DisplayLogMessage("Detected unknown image type. Skipped.", vars.ShowLog);
                return false;
            }

            // 4. Check Platform Support (Repack only supports Win32 currently)
            if (platform == IMGBEnums.Platforms.ps3 || platform == IMGBEnums.Platforms.x360)
            {
                SharedMethods.DisplayLogMessage($"Detected {platform} version. Repacking is not supported.", vars.ShowLog);
                return false;
            }

            return true;
        }
    }
}