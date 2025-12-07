using System.IO;
using IMGBlibrary.Support;
using IMGBlibrary.Extensions; // For BinaryWriterHelpers

namespace IMGBlibrary.Repack
{
    internal abstract class RepackStrategy
    {
        protected readonly IMGBVariables _vars;
        internal readonly Stream _imgbStream;
        protected readonly BinaryReader _gtexReader;
        internal readonly BinaryWriter _gtexWriter; // Null if strict mode (usually)

        protected RepackStrategy(IMGBVariables vars, Stream imgbStream, Stream gtexStream)
        {
            _vars = vars;
            _imgbStream = imgbStream;
            _gtexReader = new BinaryReader(gtexStream);
            if (gtexStream.CanWrite) _gtexWriter = new BinaryWriter(gtexStream);
        }

        public abstract void Execute(string extractedDir);

        /// <summary>
        /// Processes a single DDS file. 
        /// For Classic/Cubemap: Iterates through Mips.
        /// For Stack: Often called once per slice (as stacks usually have 1 mip).
        /// </summary>
        protected void ProcessDDS(string ddsPath, uint mipTableEntryPos, bool isStackSlice = false, int stackIndex = 0)
        {
            if (!File.Exists(ddsPath))
            {
                Log.Error($"Missing file: {Path.GetFileName(ddsPath)}");
                return;
            }

            using (var ddsStream = new FileStream(ddsPath, FileMode.Open, FileAccess.Read))
            using (var ddsReader = new BinaryReader(ddsStream))
            {
                SharedMethods.GetExtImgInfo(ddsReader, _vars);

                // --- Validation ---
                if (!_vars.IsType2Repack && !ValidateStrict()) return;
                
                // --- Type 2 Header Updates ---
                // Only update header once (for the first file/slice)
                if (_vars.IsType2Repack && (stackIndex == 0))
                {
                    UpdateGtexHeader();
                }

                // --- Data Copy ---
                using (var tempDDS = new MemoryStream())
                {
                    ddsStream.Seek(128, SeekOrigin.Begin); // Skip Header
                    ddsStream.CopyTo(tempDDS);

                    var loopCount = _vars.IsType2Repack ? (int)_vars.OutImgMipCount : _vars.GtexImgMipCount;
                    
                    // Pointers
                    var currentMipTablePos = mipTableEntryPos;
                    uint ddsReadOffset = 0;
                    uint totalMipSize = 0;

                    for (var m = 0; m < loopCount; m++)
                    {
                        uint mipSize = 0;
                        long writePosition = 0;

                        if (_vars.IsType2Repack)
                        {
                            // --- Type 2 (Resize/Append) ---
                            mipSize = CalculateMipSize(m);
                            writePosition = _imgbStream.Length; // Append to end of IMGB

                            // Update GTEX Mip Table (Offset + Size)
                            // Note: Stack types handle their own table logic in their override, 
                            // but for standard images, we write to the table here.
                            if (!isStackSlice) 
                            {
                                _gtexWriter.BaseStream.Position = currentMipTablePos;
                                _gtexWriter.WriteBytesUInt32((uint)writePosition, true);
                                _gtexWriter.BaseStream.Position = currentMipTablePos + 4;
                                _gtexWriter.WriteBytesUInt32(mipSize, true);
                            }
                        }
                        else
                        {
                            // --- Type 1 (Overwrite) ---
                            // In strict mode, we trust the GTEX table matches the DDS structure
                            _gtexReader.BaseStream.Position = currentMipTablePos;
                            writePosition = _gtexReader.ReadBytesUInt32(true);
                            mipSize = _gtexReader.ReadBytesUInt32(true);
                        }

                        // Write Data
                        _imgbStream.Seek(writePosition, SeekOrigin.Begin);
                        tempDDS.ExCopyTo(_imgbStream, ddsReadOffset, mipSize);
                        
                        // Prep for next mip
                        ddsReadOffset += mipSize;
                        totalMipSize += mipSize;
                        currentMipTablePos += 8; // Advance to next Mip Table entry

                        // Recalculate dimensions for next mip level (used for size calc)
                        if (_vars.IsType2Repack) ResizeVarsForNextMip(); 
                    }
                    
                    // Pad last mips if needed (Type 2 specific)
                    if (_vars.IsType2Repack && !isStackSlice)
                    {
                         PadNullsForLastMips(totalMipSize);
                    }
                }
            }
            Log.Info($"Repacked {Path.GetFileName(ddsPath)}");
        }

        protected virtual void UpdateGtexHeader()
        {
            // Standard GTEX Header Update (Type 2)
            // 1. Check for extra mips (if new image has more mips than original)
            if (_vars.OutImgMipCount > _vars.GtexImgMipCount)
            {
                // This is a complex operation: we need to extend the file to add zero-filled entries 
                // to the mip table before we start writing data.
                var endPos = _gtexWriter.BaseStream.Length;
                _gtexWriter.BaseStream.Position = endPos;
                var extra = (int)(_vars.OutImgMipCount - _vars.GtexImgMipCount);
                for(var i=0; i<extra; i++) _gtexWriter.Write(new byte[8]); // 8 bytes per mip entry
                
                // Update total chunk size at 0x10? 
                // Original code: gtexWriter.BaseStream.Position = 16; WriteBytesUInt32((uint)gtexStream.Length...
                _gtexWriter.BaseStream.Position = 16;
                _gtexWriter.WriteBytesUInt32((uint)_gtexWriter.BaseStream.Length, false); // Little endian? Check original: false implies LE.
            }

            // 2. Update basic info
            _gtexWriter.BaseStream.Position = _vars.GtexStartVal + 6;
            _gtexWriter.Write(_vars.OutImgFormatValue);
            _gtexWriter.BaseStream.Position = _vars.GtexStartVal + 7;
            _gtexWriter.Write((byte)_vars.OutImgMipCount);
            
            _gtexWriter.BaseStream.Position = _vars.GtexStartVal + 10;
            _gtexWriter.WriteBytesUInt16((ushort)_vars.OutImgWidth, true);
            _gtexWriter.BaseStream.Position = _vars.GtexStartVal + 12;
            _gtexWriter.WriteBytesUInt16((ushort)_vars.OutImgHeight, true);
        }

        private uint CalculateMipSize(int mipIndex)
        {
            // Uses current _vars.OutImgHeight/Width which are decremented by ResizeVarsForNextMip
            var h = _vars.OutImgHeight;
            var w = _vars.OutImgWidth;

            switch (_vars.OutImgFormatValue)
            {
                case 3: // R8G8B8A8
                case 4:
                    return h * w * 4;

                case 24: // DXT1
                    // Align to 4x4 blocks
                    h += ((4 - h % 4) % 4);
                    w += ((4 - w % 4) % 4);
                    return (h * w * 4) / 8; // 0.5 bytes per pixel effective

                case 25: // DXT3
                case 26: // DXT5
                    h += ((4 - h % 4) % 4);
                    w += ((4 - w % 4) % 4);
                    return (h * w * 4) / 4; // 1 byte per pixel effective (16 bytes per 16 pixels)
                    
                default: return 0;
            }
        }

        private void ResizeVarsForNextMip()
        {
            // Logic from NextMipHeightWidth in source
            if (_vars.OutImgFormatValue == 3) // Uncompressed
            {
                _vars.OutImgHeight = (_vars.OutImgHeight > 1) ? _vars.OutImgHeight / 2 : 1;
                _vars.OutImgWidth = (_vars.OutImgWidth > 1) ? _vars.OutImgWidth / 2 : 1;
            }
            else // Compressed (DXT)
            {
                _vars.OutImgHeight = _vars.OutImgHeight / 2;
                if (_vars.OutImgHeight < 4) _vars.OutImgHeight = 4;

                _vars.OutImgWidth = _vars.OutImgWidth / 2;
                if (_vars.OutImgWidth < 4) _vars.OutImgWidth = 4;
            }
        }
        
        private void PadNullsForLastMips(uint mipSize)
        {
            if (mipSize >= 16) return;
            var pad = 16 - (int)mipSize;
            for (var i = 0; i < pad; i++) _imgbStream.WriteByte(0);
        }

        private bool ValidateStrict()
        {
            if (_vars.GtexImgMipCount == _vars.OutImgMipCount &&
                _vars.GtexImgWidth == _vars.OutImgWidth &&
                _vars.GtexImgHeight == _vars.OutImgHeight) return true;
            Log.Warn("Mismatch in strict mode. Skipping.");
            return false;
        }
    }
}