using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using IMGBlibrary.Support;
using IMGBlibrary.Unpack;
using WhiteBinTools.Native;

namespace IMGBlibrary.Native;

public static class Exports
{
    const int InvalidArgsError = -1;
    const int SuccessReturn = 0;
    const int ExceptionError = 1;
    
    [ModuleInitializer]
    public static void Init()
    {   
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void LoggerCallback(IntPtr msgPtr);
    /// <summary>
    /// Registers a callback function for logging.
    /// C Signature: void set_logging_callback(void (*callback)(const char*));
    /// </summary>
    [UnmanagedCallersOnly(EntryPoint = "set_logging_callback", CallConvs = [typeof(CallConvCdecl)])]
    public static void SetLoggingCallback(IntPtr callbackPtr)
    {
        if (callbackPtr == IntPtr.Zero)
        {
            NativeLogger.LoggingCallback = Console.WriteLine;
            return;
        }

        var nativeCallback = Marshal.GetDelegateForFunctionPointer<LoggerCallback>(callbackPtr);

        NativeLogger.LoggingCallback = (msg) =>
        {
            // ALLOCATE: Create a UTF-8 copy on the Heap (Unmanaged Memory).
            // This memory persists until explicitly freed.
            var ptr = Marshal.StringToCoTaskMemUTF8(msg);

            // CALL: Pass the pointer to Caller. 
            // Since it's heap memory, it's safe even if Caller processes it asynchronously.
            nativeCallback(ptr);
        };
    }
    
    // 3. The Cleanup Function (CRITICAL NEW EXPORT)
    // Caller must call this after it reads the string.
    [UnmanagedCallersOnly(EntryPoint = "free_log_memory", CallConvs = [typeof(CallConvCdecl)])]
    public static void FreeLogMemory(IntPtr ptr)
    {
        if (ptr != IntPtr.Zero)
        {
            Marshal.FreeCoTaskMem(ptr);
        }
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
            NativeLogger.Error("Either imgHeaderBlk, inFilePath, extractDir are null!" +
                               $"imgHeaderBlk: {imgHeaderBlk},  inFilePath: {inFilePath}, extractDir: {extractDir}, platform: {platform}");
            return InvalidArgsError;
        }
        NativeLogger.Debug($"Unpacking imgb at inputPath: {inFilePath} to directory: {extractDir} platform: {platform}");
        try
        {
            IMGBUnpacker.Unpack(imgHeaderBlk, inFilePath, extractDir, platform, true);
            NativeLogger.Info($"Successfully unpacked imgb {inFilePath}!");
            return SuccessReturn;
        }
        catch (Exception e)
        {
            NativeLogger.Error($"Failed to unpack {inFilePath} with error:{e.Message}");
            return ExceptionError;
        }

    }

}