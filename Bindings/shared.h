#ifndef IMGBLIB_SHARED_H
#define IMGBLIB_SHARED_H

/* =======================================================================
 * Platform & Visibility Macros
 * ======================================================================= */
#if defined(_WIN32)
    #ifdef IMGBLIB_EXPORTS
        #define IMGBLIB_API __declspec(dllexport)
    #else
        #define IMGBLIB_API __declspec(dllimport)
    #endif
#else
    #define IMGBLIB_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif
    
    
    typedef enum
    {
        InvalidArgs = -1, 
        Success = 0, 
        Exception = 1
    } Status;
    
    typedef enum
    {
        Strict = 0,
        Resize = 1,
    } RepackMode;
    
    typedef enum
    {
        WIN32 = 0, 
        PS3 = 1, 
        X360 = 2
    } Platforms;
    
    typedef unsigned char IMGB_BOOL;
    
    typedef void (*LogCallback)(const char * msg);
    
    IMGBLIB_API void set_logging_callback(LogCallback cb);
    
    IMGBLIB_API void free_log_memory(void* ptr);
    
    //Unpack 
    IMGBLIB_API Status unpack_imgb(char* imgHeaderBlkPtr, char* inFilePtr, char* extractDirPtr, Platforms platform );
    
    
#ifdef __cplusplus
}
#endif

#endif