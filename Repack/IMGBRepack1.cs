using IMGBlibrary.Support;

namespace IMGBlibrary.Repack
{
    /// <summary>
    /// Legacy wrapper for Type 1 Repacking (Strict Mode).
    /// Maintains backward compatibility for existing library consumers.
    /// </summary>
    public class IMGBRepack1
    {
        /// <summary>
        /// Use for repacking images that require the pixel format, 
        /// mipcount and dimensions to be same as the original.
        /// </summary>
        /// <param name="imgHeaderBlockFile">Header Block file path. should have the GTEX chunk.</param>
        /// <param name="outImgbFile">IMGB file path. the file has to be present.</param>
        /// <param name="extractedIMGBdir">Path to the directory where the image files are present.</param>
        /// <param name="imgbPlatform">Platform of the header block file.</param>
        /// <param name="showLog">Determine whether to show more messages related to this method's process.</param>
        public static void RepackIMGBType1(string imgHeaderBlockFile, string outImgbFile, string extractedIMGBdir, IMGBEnums.Platforms imgbPlatform, bool showLog)
        {
            // Forward call to the new engine with Strict mode.
            // In Strict mode, the DDS search name defaults to the header filename.
            IMGBRepacker.Repack(
                headerFile: imgHeaderBlockFile,
                outFile: outImgbFile,
                inDir: extractedIMGBdir,
                platform: imgbPlatform,
                mode: IMGBRepacker.RepackMode.Strict,
                showLog: showLog,
                ddsSearchName: null // Null triggers default behavior (use headerFile name)
            );
        }
    }
}