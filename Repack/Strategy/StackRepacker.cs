using System.IO;
using IMGBlibrary.Extensions;
using IMGBlibrary.Support;

namespace IMGBlibrary.Repack.Strategy
{
    internal class StackRepacker(IMGBVariables vars, Stream imgb, Stream gtex) : RepackStrategy(vars, imgb, gtex)
    {
        public override void Execute(string extractedDir)
        {
            // 1. Initial Checks
            if (!CheckFiles(extractedDir)) return;

            // 2. Read Base Offset (Type 1 only)
            // In Type 1, the Stack usually forms a contiguous block in the IMGB.
            // We read the start position from the GTEX table once.
            uint currentWritePos = 0;
            uint stackMipSize = 0;

            var mipTableOffset = _gtexReader.ReadBytesUInt32(true);
            if (!_vars.IsType2Repack)
            {
                // Read offset and size for the *first* slice from the GTEX Mip Table
                _gtexReader.BaseStream.Position = _vars.GtexStartVal + 16;
                var mipTablePos = _vars.GtexStartVal + mipTableOffset;

                _gtexReader.BaseStream.Position = mipTablePos;
                currentWritePos = _gtexReader.ReadBytesUInt32(true);
                
                _gtexReader.BaseStream.Position = mipTablePos + 4;
                stackMipSize = _gtexReader.ReadBytesUInt32(true);
            }

            // 3. Iterate Slices
            for (var s = 1; s <= _vars.GtexImgDepth; s++)
            {
                var ddsName = $"{_vars.ImgHeaderBlockFileName}{_vars.GtexImgType}{s}.dds";
                var fullPath = Path.Combine(extractedDir, ddsName);

                switch (_vars.IsType2Repack)
                {
                    // For Type 2 (Resize), we need to update the global header using the *first* slice's info
                    case true when s == 1:
                    // Subsequent slices just get appended
                    case true:
                        // We process the first file to get info and write headers
                        ProcessStackSliceType2(fullPath, s);
                        break;
                    default:
                        // Type 1 (Strict)
                        ProcessStackSliceType1(fullPath, currentWritePos, stackMipSize);
                        // Advance position for next slice (they are contiguous)
                        currentWritePos += stackMipSize;
                        break;
                }
            }
        }

        private void ProcessStackSliceType1(string path, uint writePos, uint size)
        {
             using (var ddsStream = new FileStream(path, FileMode.Open, FileAccess.Read))
             using (var ddsReader = new BinaryReader(ddsStream))
             {
                 SharedMethods.GetExtImgInfo(ddsReader, _vars);
                 
                 // Strict checks
                 if(_vars.GtexImgMipCount != _vars.OutImgMipCount || _vars.GtexImgWidth != _vars.OutImgWidth) return; 

                 // Copy Data
                 using (var temp = new MemoryStream())
                 {
                     ddsStream.Seek(128, SeekOrigin.Begin);
                     ddsStream.CopyTo(temp);
                     
                     _imgbStream.Seek(writePos, SeekOrigin.Begin);
                     temp.ExCopyTo(_imgbStream, 0, size);
                 }
             }
             Log.Info($"Repacked {Path.GetFileName(path)}");
        }

        private void ProcessStackSliceType2(string path, int stackIndex)
        {
            using (var ddsStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var ddsReader = new BinaryReader(ddsStream))
            {
                SharedMethods.GetExtImgInfo(ddsReader, _vars);

                // --- Header Update (First Slice Only) ---
                if (stackIndex == 1)
                {
                    // Stack specific header updates at +24 and +28
                    var stackStart = (uint)_imgbStream.Length;
                    
                    // We need to calculate total size. Since we haven't read all files yet, 
                    // we assume all slices are same size as this first one (standard for 3D textures)
                    using var tempCalc = new MemoryStream();
                    ddsStream.Seek(128, SeekOrigin.Begin);
                    ddsStream.CopyTo(tempCalc);
                    var sliceSize = (uint)tempCalc.Length;
                    var totalStackSize = sliceSize * _vars.GtexImgDepth;

                    // Write to GTEX Header
                    _gtexWriter.BaseStream.Position = _vars.GtexStartVal + 10;
                    _gtexWriter.WriteBytesUInt16((ushort)_vars.OutImgWidth, true);
                    _gtexWriter.BaseStream.Position = _vars.GtexStartVal + 12;
                    _gtexWriter.WriteBytesUInt16((ushort)_vars.OutImgHeight, true);

                    _gtexWriter.BaseStream.Position = _vars.GtexStartVal + 24;
                    _gtexWriter.WriteBytesUInt32(stackStart, true);
                    _gtexWriter.BaseStream.Position = _vars.GtexStartVal + 28;
                    _gtexWriter.WriteBytesUInt32(totalStackSize, true);
                }

                // --- Append Data ---
                // Re-read data (or reuse stream if optimized)
                using (var temp = new MemoryStream())
                {
                    ddsStream.Seek(128, SeekOrigin.Begin);
                    ddsStream.CopyTo(temp);
                    
                    var writePos = _imgbStream.Length;
                    _imgbStream.Seek(writePos, SeekOrigin.Begin);
                    temp.ExCopyTo(_imgbStream, 0, temp.Length);
                }
            }
            Log.Info($"Repacked {Path.GetFileName(path)}");
        }

        private bool CheckFiles(string dir)
        {
            var missing = SharedMethods.CheckImgFilesBatch(_vars.GtexImgDepth, dir, _vars.ImgHeaderBlockFileName, _vars);
            if (!missing) return true;
            Log.Error("Missing one or more stack image files.");
            return false;
        }
    }
}
