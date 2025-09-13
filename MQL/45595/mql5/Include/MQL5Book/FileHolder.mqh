//+------------------------------------------------------------------+
//|                                                   FileHolder.mqh |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#include <MQL5Book/MqlError.mqh>

//+------------------------------------------------------------------+
//| Base auxiliry class just to clean up internal error flags,       |
//| which may remain after previous function calls                   |
//+------------------------------------------------------------------+
class FileBase
{
protected:
   FileBase()
   {
      ResetLastError();
   }
};

//+------------------------------------------------------------------+
//| Single file-handle guard to prevent resource leak,               |
//| closes file automatically on object deletion                     |
//+------------------------------------------------------------------+
class FileOpener : public FileBase
{
public:
   const int handle;
   const string name;
   FileOpener(const string filename, const int flags, const ushort delimiter = '\t', const uint codepage = CP_ACP) :
     handle(FileOpen(filename, flags, delimiter, codepage)), name(filename)
   {
   }

   ~FileOpener()
   {
      ResetLastError();
      FileGetInteger(handle, FILE_SIZE); // marks internal error if handle is incorrect
      if(handle != INVALID_HANDLE)
      {
         if(_LastError == 0)
         {
            #ifdef FILE_DEBUG_PRINT
               Print(__FUNCTION__, ": Automatic close for handle: ", handle);
            #endif
            FileClose(handle);
         }
         else
         {
            PrintFormat("%s: handle %d is incorrect, %s(%d)",
               __FUNCTION__, handle, E2S(_LastError), _LastError);
         }
      }
   }
};

#define CONTEXT_MARK NULL
#define CLEAR(P) if(CheckPointer(P) == POINTER_DYNAMIC) delete P;

//+------------------------------------------------------------------+
//| Multiple file-handle guard to prevent resource leak,             |
//| generates FileOpener objects upon requests, then delete them     |
//| automatically on leaving local contexts                          |
//+------------------------------------------------------------------+
class FileHolder
{
   static FileOpener *files[];
   int expand()
   {
      return ArrayResize(files, ArraySize(files) + 1) - 1;
   }
public:
   FileHolder()
   {
      const int n = expand();
      if(n > -1)
      {
         files[n] = CONTEXT_MARK;
      }
   }
   
   ~FileHolder()
   {
      for(int i = ArraySize(files) - 1; i >= 0; --i)
      {
         if(files[i] == CONTEXT_MARK)
         {
            // shrink array and exit
            ArrayResize(files, i);
            return;
         }
         
         CLEAR(files[i]);
      }
   }
   
   int FileOpen(const string filename, const int flags, const ushort delimiter = '\t', const uint codepage = CP_ACP)
   {
      const int n = expand();
      if(n > -1)
      {
         files[n] = new FileOpener(filename, flags, delimiter, codepage);
         return files[n].handle;
      }
      return INVALID_HANDLE;
   }
   
   static string GetFilename(const int h)
   {
      for(int i = 0; i < ArraySize(files); ++i)
      {
         if(files[i] != NULL && files[i].handle == h)
         {
            return files[i].name;
         }
      }
      return NULL;
   }
};

static FileOpener *FileHolder::files[];
//+------------------------------------------------------------------+
