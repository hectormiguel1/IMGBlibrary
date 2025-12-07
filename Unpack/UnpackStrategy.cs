using System;
using System.IO;
using IMGBlibrary.Extensions;
using IMGBlibrary.Support;

namespace IMGBlibrary.Unpack
{
    internal abstract class UnpackStrategy(IMGBVariables vars, Stream imgbStream, Stream gtexStream)
    {
        protected readonly IMGBVariables _vars = vars;
        protected readonly BinaryReader _gtexReader = new(gtexStream);

        public abstract void Execute(string extractDir);

        /// <summary>
        /// Creates a DDS file, writes headers, and allows the specific strategy to fill mips.
        /// </summary>
        protected void CreateAndWriteDDS(string ddsPath, Action<Stream> writeMipsAction)
        {
            using (var ddsStream = new FileStream(ddsPath, FileMode.Append, FileAccess.Write))
            {
                // 1. Write Header
                DDSMethods.WriteHeader(ddsStream, _vars);

                // 2. Write Mip Data (Delegated to concrete class logic)
                writeMipsAction(ddsStream);
            }
            Log.Debug($"Unpacked {Path.GetFileName(ddsPath)}");
        }

        protected void CopyMipToDDS(Stream ddsStream, uint mipStart, uint mipSize)
        {
            // If PS3 and Swizzled/Special
            if (_vars.IsPs3Imgb && IsPS3SpecialCase())
            {
                HandlePS3Swizzle(ddsStream, mipStart, mipSize);
            }
            else
            {
                // Standard Copy
                imgbStream.ExCopyTo(ddsStream, mipStart, mipSize);
            }
        }

        #region PS3 Specific Logic
        private bool IsPS3SpecialCase()
        {
            // Logic from original SpecialPS3ImgMethods
            var isSwizzled = (_vars.GtexImgFormatValue == 4 && _vars.GtexImgTypeValue == 4);
            var isFormat4NoSwizzle = (_vars.GtexImgFormatValue == 4 && _vars.GtexImgTypeValue == 0);
            var isFormat3 = (_vars.GtexImgFormatValue == 3);

            return isSwizzled || isFormat4NoSwizzle || isFormat3;
        }

        private void HandlePS3Swizzle(Stream ddsStream, uint mipStart, uint mipSize)
        {
            imgbStream.Seek(mipStart, SeekOrigin.Begin);
            var buffer = new byte[mipSize];
            imgbStream.Read(buffer, 0, (int)mipSize);

            // 1. Unswizzle (if type 4/format 4)
            if (_vars.GtexImgFormatValue == 4 && _vars.GtexImgTypeValue == 4)
            {
                buffer = MortonUnswizzle(buffer);
            }

            // 2. Color Correct (Swap Red/Blue usually)
            buffer = ColorAsBGRA(buffer);

            // 3. Write
            ddsStream.Write(buffer, 0, buffer.Length);
        }

        private byte[] MortonUnswizzle(byte[] swizzled)
        {
            int w = _vars.GtexImgWidth;
            int h = _vars.GtexImgHeight;
            var unswizzled = new byte[w * h * 4];
            var pixelBuf = new byte[4];

            var readPos = 0;
            // Original logic preserved exactly
            for (var m = 0; m < w * h; m++)
            {
                Array.Copy(swizzled, readPos, pixelBuf, 0, 4);
                
                // Morton Code Logic
                int val1 = 0, val2 = 0;
                int val3, val4 = (val3 = 1);
                var val5 = m;
                int val6 = w, val7 = h;

                while (val6 > 1 || val7 > 1)
                {
                    if (val6 > 1) { val1 += val4 * (val5 & 1); val5 >>= 1; val4 *= 2; val6 >>= 1; }

                    if (val7 <= 1) continue;
                    val2 += val3 * (val5 & 1); val5 >>= 1; val3 *= 2; val7 >>= 1;
                }

                var offset = (val2 * w + val1) * 4;
                Array.Copy(pixelBuf, 0, unswizzled, offset, 4);
                readPos += 4;
            }
            return unswizzled;
        }

        private byte[] ColorAsBGRA(byte[] input)
        {
            // Swaps ARGB/RGBA to BGRA
            var output = new byte[input.Length];
            for (var i = 0; i < input.Length; i += 4)
            {
                var a = input[i];
                var r = input[i + 1];
                var g = input[i + 2];
                var b = input[i + 3];

                output[i] = b;
                output[i + 1] = g;
                output[i + 2] = r;
                output[i + 3] = a;
            }
            return output;
        }
        #endregion
    }
}