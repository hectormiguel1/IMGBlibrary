using System;
using System.IO;
using System.Text;
using IMGBlibrary.Extensions;

namespace IMGBlibrary.Support
{
    internal static class SharedMethods
    {
        public static uint GetGTEXChunkPos(string inImgHeaderBlockFile)
        {
            uint gtexPos = 0;
            const string gtexChunkString = "GTEX";
            var gtexChunkStringArray = new byte[4];
            var imgHeaderBlockFileData = File.ReadAllBytes(inImgHeaderBlockFile);

            for (var g = 0; g < imgHeaderBlockFileData.Length; g++)
            {
                if ((char)imgHeaderBlockFileData[g] != gtexChunkString[0]) continue;
                Buffer.BlockCopy(imgHeaderBlockFileData, g, gtexChunkStringArray, 0, 4);
                var gtex = Encoding.ASCII.GetString(gtexChunkStringArray, 0, 4);

                if (gtex != gtexChunkString) continue;
                gtexPos = (uint)g;
                break;
            }

            return gtexPos;
        }


        public static void GetImageInfo(string inImgHeaderBlockFile, IMGBVariables imgbVars)
        {
            using var gtexStream = new FileStream(inImgHeaderBlockFile, FileMode.Open, FileAccess.Read);
            using var gtexReader = new BinaryReader(gtexStream);
            gtexReader.BaseStream.Position = imgbVars.GtexStartVal + 6;
            imgbVars.GtexImgFormatValue = gtexReader.ReadByte();
            imgbVars.GtexImgMipCount = gtexReader.ReadByte();

            imgbVars.GtexImgMipCount = imgbVars.GtexImgMipCount.Equals(0) ? (byte)1 : imgbVars.GtexImgMipCount;

            gtexReader.BaseStream.Position = imgbVars.GtexStartVal + 9;
            imgbVars.GtexImgTypeValue = gtexReader.ReadByte();
            imgbVars.GtexImgWidth = gtexReader.ReadBytesUInt16(true);
            imgbVars.GtexImgHeight = gtexReader.ReadBytesUInt16(true);
            imgbVars.GtexImgDepth = gtexReader.ReadBytesUInt16(true);

            imgbVars.GtexImgType = imgbVars.GtexImgTypeValue switch
            {
                1 or 5 => "_cbmap_",
                2 => "_stack_",
                _ => imgbVars.GtexImgType
            };
        }


        public static void GetExtImgInfo(BinaryReader ddsReader, IMGBVariables imgbVars)
        {
            ddsReader.BaseStream.Position = 12;
            imgbVars.OutImgHeight = ddsReader.ReadUInt32();
            imgbVars.OutImgWidth = ddsReader.ReadUInt32();

            ddsReader.BaseStream.Position = 28;
            imgbVars.OutImgMipCount = ddsReader.ReadUInt32();

            ddsReader.BaseStream.Position = 84;
            var imgFormatString = Encoding.ASCII.GetString(ddsReader.ReadBytes(4)).Replace("\0", "");

            imgbVars.OutImgFormatValue = imgFormatString switch
            {
                "" => imgbVars.OutImgMipCount > 1 ? (byte)3 : (byte)4,
                "DXT1" => 24,
                "DXT3" => 25,
                "DXT5" => 26,
                _ => 0
            };
        }


        public static bool CheckImgFilesBatch(int fileAmount, string extractImgbDir, string imgHeaderBlockFileName, IMGBVariables imgbVars)
        {
            var isMissingAnImg = false;
            var imgFileCount = 1;

            for (var i = 0; i < fileAmount; i++)
            {
                var fileToCheck = Path.Combine(extractImgbDir, imgHeaderBlockFileName + imgbVars.GtexImgType + imgFileCount + ".dds");

                if (!File.Exists(fileToCheck))
                {
                    isMissingAnImg = true;
                }

                imgFileCount++;
            }

            return isMissingAnImg;
        }
    }
}