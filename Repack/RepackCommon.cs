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
                Log.Warning("Unable to find GTEX chunk. Skipped.");
                return false;
            }

            // 2. Read Image Info
            SharedMethods.GetImageInfo(imgHeaderBlockFile, vars);

            // 3. Validate Format and Type
            if (!IMGBVariables.GtexImgFormatValues.Contains(vars.GtexImgFormatValue))
            {
                Log.Warning("Detected unknown image format. Skipped.");
                return false;
            }

            if (!IMGBVariables.GtexImgTypeValues.Contains(vars.GtexImgTypeValue))
            {
                Log.Warning("Detected unknown image type. Skipped.");
                return false;
            }

            // 4. Check Platform Support (Repack only supports Win32 currently)
            if (platform is not (IMGBEnums.Platforms.ps3 or IMGBEnums.Platforms.x360)) return true;
            Log.Fatal($"Detected {platform} version. Repacking is not supported.");
            return false;

        }
    }
}