//+------------------------------------------------------------------+
//|                                                   FileHandle.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#define FILE_DEBUG_PRINT

#include <MQL5Book/MqlError.mqh>
#include <MQL5Book/FileHandle.mqh>
#include "PRTF.mqh"

// NB: we will open the same file twice in this script
// just for demonstration purpose;
// normally you need only one handle for a file per program instance

const string dummy = "MQL5Book/dummy";

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // create new file or open existing one and empty it
   FileHandle fh1(PRTF(FileOpen(dummy, FILE_TXT | FILE_WRITE | FILE_SHARE_WRITE | FILE_SHARE_READ))); // 1
   // another possible use case
   int h = PRTF(FileOpen(dummy, FILE_TXT | FILE_WRITE | FILE_SHARE_WRITE | FILE_SHARE_READ)); // 2
   FileHandle fh2 = h;
   // one more supported syntax:
   //    int f;
   //    FileHandle ff(f, FileOpen(dummy, FILE_TXT|FILE_WRITE|FILE_SHARE_WRITE|FILE_SHARE_READ));

   // some data writing could be here 
   // ...
   
   FileClose(~fh1); // explicit close will be detected and gracefully skipped in FileHandle
   
   // Note: handle h, stored in fh2 is not closed
   // and will be closed automatically in destructor
   
   /*
      output:
      
      FileHandle::~FileHandle: Automatic close for handle: 2
      FileHandle::~FileHandle: handle 1 is incorrect, INVALID_FILEHANDLE(5007)
   */
}
//+------------------------------------------------------------------+
