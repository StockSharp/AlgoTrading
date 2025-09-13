//+------------------------------------------------------------------+
//|                                                   FileHandle.mqh |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#include <MQL5Book/MqlError.mqh>

//+------------------------------------------------------------------+
//| Helper guard to prevent resource leak                            |
//| and unblock file automatically on local context exit             |
//+------------------------------------------------------------------+
class FileHandle
{
   int handle;
   
public:
   FileHandle(const int h = INVALID_HANDLE) : handle(h)
   {
   }

   FileHandle(int &holder, const int h) : handle(h)
   {
      holder = h;
   }
   
   ~FileHandle()
   {
      close();
   }

   void close()
   {
      if(handle != INVALID_HANDLE)
      {
         ResetLastError();
         FileGetInteger(handle, FILE_SIZE); // marks internal error if handle is incorrect
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
      handle = INVALID_HANDLE;
   }
   
   int operator=(const int h)
   {
      // NOTE: if 'handle' is already holding a file reference,
      // we should either close previous one before new assignment
      // or decline the assignment: otherwise the old handle leaks
      close();
      handle = h;
      return h;
   }
   
   int operator~() const
   {
      return handle;
   }
};
//+------------------------------------------------------------------+
