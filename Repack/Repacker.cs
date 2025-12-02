using IMGBlibrary.Support;
using System.IO;
using IMGBlibrary.Repack.Strategy;

namespace IMGBlibrary.Repack
{
    public static class IMGBRepacker
    {
        public enum RepackMode { Strict, Resize }

        public static void Repack(
            string headerFile, 
            string outFile, 
            string inDir, 
            IMGBEnums.Platforms platform, 
            RepackMode mode,
            bool showLog,
            string? ddsSearchName = null)
        {
            
            var actualSearchName = ddsSearchName ?? Path.GetFileName(headerFile);
            var vars = new IMGBVariables 
            { 
                ShowLog = showLog, 
                ImgHeaderBlockFileName = actualSearchName,
                IsType2Repack = (mode == RepackMode.Resize) // Add this bool to IMGBVariables
            };

            // 1. Common Validation (using the helper we made earlier)
            if (!RepackCommon.ValidateAndSetup(headerFile, vars, platform)) return;

            // 2. Prepare Streams
            // Type 1 opens Write (overwrite). Type 2 opens Append.
            var fileMode = (mode == RepackMode.Strict) ? FileMode.Open : FileMode.Append;
            
            // Note: For Type 2, we usually copy the temp header to the original first. 
            // The original logic had "tmpHeader" vs "headerName". 
            // You might need to handle the file copy here before opening streams.

            using var imgbStream = new FileStream(outFile, fileMode, FileAccess.Write);
            using var gtexStream = new FileStream(headerFile, FileMode.Open, FileAccess.ReadWrite);
            // 3. Factory: Choose Strategy
            RepackStrategy? strategy = vars.GtexImgTypeValue switch
            {
                0 or 4 => new ClassicRepacker(vars, imgbStream, gtexStream),
                1 or 5 => new CubemapRepacker(vars, imgbStream, gtexStream),
                2      => new StackRepacker(vars, imgbStream, gtexStream),
                _      => null
            };

            if (strategy == null)
            {
                SharedMethods.DisplayLogMessage("Unsupported Image Type", showLog);
                return;
            }

            // 4. Execute
            strategy.Execute(inDir);
        }
    }
}