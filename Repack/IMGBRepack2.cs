using IMGBlibrary.Support;

namespace IMGBlibrary.Repack
{
    /// <summary>
    /// Legacy wrapper for Type 2 Repacking (Resize/Flexible Mode).
    /// Maintains backward compatibility for existing library consumers.
    /// </summary>
    public class IMGBRepack2
    {
        /// <summary>
        /// Use for repacking images that do not require the pixel format, 
        /// mipcount and dimensions to be same as the original image.
        /// </summary>
        /// <param name="tmpImgHeaderBlockFile">Create a copy of the original header block file and provide its path. should have the GTEX chunk.</param>
        /// <param name="imgHeaderBlockFileName">Name of the header block file. this should be the original file's name.</param>
        /// <param name="outImgbFile">IMGB file path. not mandatory for the file to be present.</param>
        /// <param name="extractedIMGBdir">Path to the directory where the image files are present.</param>
        /// <param name="imgbPlatform">Platform of the header block file.</param>
        /// <param name="showLog">Determine whether to show more messages related to this method's process.</param>
        public static void RepackIMGBType2(string tmpImgHeaderBlockFile, string imgHeaderBlockFileName, string outImgbFile, string extractedIMGBdir, IMGBEnums.Platforms imgbPlatform, bool showLog)
        {
            // Forward call to the new engine with Resize mode.
            // We pass 'imgHeaderBlockFileName' as the ddsSearchName.
            // This ensures we edit 'tmpImgHeaderBlockFile' but look for files matching 'imgHeaderBlockFileName'.
            IMGBRepacker.Repack(
                headerFile: tmpImgHeaderBlockFile,
                outFile: outImgbFile,
                inDir: extractedIMGBdir,
                platform: imgbPlatform,
                mode: IMGBRepacker.RepackMode.Resize,
                showLog: showLog,
                ddsSearchName: imgHeaderBlockFileName // <--- The Alias
            );
        }
    }
}