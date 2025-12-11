using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using IMGBlibrary.Repack;
using IMGBlibrary.Support;
using IMGBlibrary.Unpack;
using Native;

namespace IMGBlibrary.Native;

public static class Exports
{
    private const int InvalidArgsError = -1;
    private const int SuccessReturn = 0;
    private const int ExceptionError = 1;
    
    [ModuleInitializer]
    public static void Init()
    {   
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }
    
    

    /// <summary>
    /// Native entry point for C ABI
    /// </summary>
    /// <param name="imgHeaderBlkPtr"></param>
    /// <param name="inFilePtr"></param>
    /// <param name="extractDirPtr"></param>
    /// <param name="platformRaw"></param>
    /// <returns></returns>
    [UnmanagedCallersOnly(EntryPoint = "unpack_imgb", CallConvs = [typeof(CallConvCdecl)])]
    public static int UnpackIMGB(IntPtr imgHeaderBlkPtr, IntPtr inFilePtr, IntPtr extractDirPtr, int platformRaw)
    {
        var imgHeaderBlk = Marshal.PtrToStringUTF8(imgHeaderBlkPtr);
        var inFilePath = Marshal.PtrToStringUTF8(inFilePtr);
        var extractDir = Marshal.PtrToStringUTF8(extractDirPtr);
        var platform = (IMGBEnums.Platforms)platformRaw;

        if (imgHeaderBlk == null || inFilePath == null || extractDir == null)
        {
            Log.Fatal("Either imgHeaderBlk, inFilePath, extractDir are null!" +
                               $"imgHeaderBlk: {imgHeaderBlk},  inFilePath: {inFilePath}, extractDir: {extractDir}, platform: {platform}");
            return InvalidArgsError;
        }
        Log.Info($"Unpacking imgb at inputPath: {inFilePath} to directory: {extractDir} platform: {platform}");
        try
        {
            IMGBUnpacker.Unpack(imgHeaderBlk, inFilePath, extractDir, platform, true);
            Log.Info($"Successfully unpacked imgb {inFilePath}!");
            return SuccessReturn;
        }
        catch (Exception e)
        {
            Log.Fatal($"Failed to unpack {inFilePath} with error:{e.Message}");
            return ExceptionError;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "repack_imgb_strict", CallConvs = [typeof(CallConvCdecl)])]
    public static int RepackStrict(IntPtr imgHeaderBlkPtr, IntPtr outImgbPtr, IntPtr extractedDirPtr, int platformRaw)
    {
        var imgHeaderBlk = Marshal.PtrToStringUTF8(imgHeaderBlkPtr);
        var outImgb = Marshal.PtrToStringUTF8(outImgbPtr);
        var extractedDir = Marshal.PtrToStringUTF8(extractedDirPtr);
        var platform = (IMGBEnums.Platforms)platformRaw;
        if (imgHeaderBlk == null || outImgb == null || extractedDir == null)
        {
            Log.Fatal("Either imgHeaderBlk, outImgb, extractedDir are null!" +
                               $"imgHeaderBlk: {imgHeaderBlk},  outImgb: {outImgb}, extractedDir: {extractedDir}, platform: {platform}");
            return InvalidArgsError;
        }
        Log.Info($"Repacking IMGB in strict mode: imgHeaderBlk: {imgHeaderBlk}, output imgb: {outImgb} source directory: {extractedDir} platform: {platform}");

        try
        {
            IMGBRepacker.Repack(imgHeaderBlk, outImgb, extractedDir, platform, IMGBRepacker.RepackMode.Strict, true);
            Log.Info(
                $"Successfully repacked IMGB imgHeaderBlk: {imgHeaderBlk}, output imgb: {outImgb} source directory: {extractedDir}!");
            return SuccessReturn;
        }
        catch (Exception e)
        {
            Log.Fatal($"Encountered error while repacking  imgHeaderBlk: {imgHeaderBlk}, output imgb: {outImgb} source directory: {extractedDir}. Error: {e.Message}");
            return ExceptionError;
        }
    }
    
    [UnmanagedCallersOnly(EntryPoint = "repack_imgb_resize", CallConvs = [typeof(CallConvCdecl)])]
    public static int RepackResize(IntPtr tmpImgHeaderBlkPtr, IntPtr imgHeaderBlkPtr, IntPtr outImgbPtr, IntPtr extractedDirPtr, int platformRaw)
    {
        var tmpHeaderBlk = Marshal.PtrToStringUTF8(tmpImgHeaderBlkPtr);
        var imgHeaderBlk = Marshal.PtrToStringUTF8(imgHeaderBlkPtr);
        var outImgb = Marshal.PtrToStringUTF8(outImgbPtr);
        var extractedDir = Marshal.PtrToStringUTF8(extractedDirPtr);
        var platform = (IMGBEnums.Platforms)platformRaw;
        if (tmpHeaderBlk == null || imgHeaderBlk == null || outImgb == null || extractedDir == null)
        {
            Log.Fatal("Either tmpHeaderBlk, imgHeaderBlk, outImgb, extractedDir are null!" +
                               $"tmpHeaderBlk: {tmpHeaderBlk}, imgHeaderBlk: {imgHeaderBlk},  outImgb: {outImgb}, extractedDir: {extractedDir}, platform: {platform}");
            return InvalidArgsError;
        }
        Log.Info($"Repacking IMGB in strict mode: tmpHeaderBlk: {tmpHeaderBlk}, imgHeaderBlk: {imgHeaderBlk}, output imgb: {outImgb} source directory: {extractedDir} platform: {platform}");

        try
        {
            IMGBRepacker.Repack(imgHeaderBlk, outImgb, extractedDir, platform, IMGBRepacker.RepackMode.Strict, true);
            Log.Info(
                $"Successfully repacked IMGB tmpHeaderBlk: {tmpHeaderBlk}, imgHeaderBlk: {imgHeaderBlk}, output imgb: {outImgb} source directory: {extractedDir}!");
            return SuccessReturn;
        }
        catch (Exception e)
        {
            Log.Fatal($"Encountered error while repacking  tmpHeaderBlk: {tmpHeaderBlk}, imgHeaderBlk: {imgHeaderBlk}, output imgb: {outImgb} source directory: {extractedDir}. Error: {e.Message}");
            return ExceptionError;
        }
    }

}