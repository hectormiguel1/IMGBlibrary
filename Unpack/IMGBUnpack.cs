using IMGBlibrary.Support;

namespace IMGBlibrary.Unpack
{
    /// <summary>
    /// Legacy wrapper for Unpacking.
    /// Maintains backward compatibility for existing library consumers.
    /// </summary>
    public class IMGBUnpack
    {
        public static void UnpackIMGB(string imgHeaderBlockFile, string inImgbFile, string extractIMGBdir, IMGBEnums.Platforms imgbPlatform, bool showLog)
        {
            // Forward calls to the new modern engine
            IMGBUnpacker.Unpack(imgHeaderBlockFile, inImgbFile, extractIMGBdir, imgbPlatform, showLog);
        }
    }
}