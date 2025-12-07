using System;
using System.IO;
using IMGBlibrary.Support;

namespace IMGBlibrary.Unpack
{
    internal static class DDSMethods
    {
        public static void WriteHeader(Stream ddsStream, IMGBVariables vars)
        {
            using var writer = new BinaryWriter(ddsStream, System.Text.Encoding.Default, leaveOpen: true);
            
            // Zero out header space (128 bytes)
            for (var h = 0; h < 128; h++) ddsStream.WriteByte(0);

            // 1. Base Header (Magic + Dimensions)
            writer.BaseStream.Position = 0;
            writer.Write(0x20534444); // "DDS "
            writer.Write(124);        // Header Size
            writer.BaseStream.Position = 12;
            writer.Write((uint)vars.GtexImgHeight);
            writer.Write((uint)vars.GtexImgWidth);
            writer.BaseStream.Position = 28;
            writer.Write((uint)vars.GtexImgMipCount);
            writer.BaseStream.Position = 76;
            writer.Write(32);         // Reserved?

            // Flags (Caps)
            writer.BaseStream.Position = 108;
            writer.Write((uint)(vars.GtexImgMipCount > 1 ? 0x401008 : 0x1000));

            // 2. Pixel Format Header
            WritePixelFormat(writer, vars);
        }

        private static void WritePixelFormat(BinaryWriter writer, IMGBVariables vars)
        {
            uint pitch = 0;
            uint width = vars.GtexImgWidth;
            uint height = vars.GtexImgHeight;

            // Flags location
            writer.BaseStream.Position = 80; 

            switch (vars.GtexImgFormatValue)
            {
                case 3: // R8G8B8A8 (with mips)
                case 4: // R8G8B8A8
                    pitch = (width * 32 + 7) / 8;
                    writer.Write(0x41); // Flags: RGB | ALPHA
                    writer.BaseStream.Position = 88;
                    writer.Write(32); // RGB bit count
                    writer.Write(0x00FF0000); // R Mask
                    writer.Write(0x0000FF00); // G Mask
                    writer.Write(0x000000FF); // B Mask
                    writer.Write(0xFF000000); // A Mask
                    break;

                case 24: // DXT1
                case 25: // DXT3
                case 26: // DXT5
                    int blockSize = (vars.GtexImgFormatValue == 24) ? 8 : 16;
                    pitch = Math.Max(1, ((width + 3) / 4)) * Math.Max(1, ((height + 3) / 4)) * (uint)blockSize;
                    
                    writer.Write(0x04); // Flags: FOURCC
                    writer.BaseStream.Position = 84;
                    // Write FourCC Codes
                    if (vars.GtexImgFormatValue == 24) writer.Write(0x31545844); // DXT1
                    if (vars.GtexImgFormatValue == 25) writer.Write(0x33545844); // DXT3
                    if (vars.GtexImgFormatValue == 26) writer.Write(0x35545844); // DXT5
                    break;
            }

            // Write Pitch
            writer.BaseStream.Position = 20;
            writer.Write(pitch);

            // Write Common Mip Flag
            writer.BaseStream.Position = 8;
            if (vars.GtexImgFormatValue == 3 || vars.GtexImgFormatValue == 4)
                writer.Write(vars.GtexImgMipCount > 1 ? 0x2100F : 0x100F); // Complex uncompressed flags
            else
                writer.Write(vars.GtexImgMipCount > 1 ? 0xA1007 : 0x81007); // Complex compressed flags
        }
    }
}