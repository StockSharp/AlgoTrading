//+------------------------------------------------------------------+
//|                                                FileOpenClose.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#include <MQL5Book/MqlError.mqh>
#include "PRTF.mqh"

// NB: we will open some files twice in the same script
// just for demonstration purpose;
// normally you need only one handle for a file per program instance

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   int ha[4] = {}; // reserve some space to hold file handles for all tests
   
   // the next file should exist, because it's left from FileSaveLoad.mq5
   const string rawdata = "MQL5Book/rawdata";

   // this should succeed because it's 1-st time we open the file
   // and it's existing and not locked by other software;
   // Note: obtained handles are just integer numbers
   ha[0] = PRTF(FileOpen(rawdata, FILE_BIN | FILE_READ)); // 1 / ok
   if(ha[0] == INVALID_HANDLE)
   {
      PrintFormat("File %s is inaccessible,"
         " but should exist after FileSaveLoad test run.", rawdata);
      return;
   }
   
   // this will fail, because neither of the open calls allow sharing
   ha[1] = PRTF(FileOpen(rawdata, FILE_BIN | FILE_READ)); // -1 / CANNOT_OPEN_FILE(5004)
   
   // let's close initial handle and reopen it with sharing for read
   FileClose(ha[0]);
   ha[0] = PRTF(FileOpen(rawdata, FILE_BIN | FILE_READ | FILE_SHARE_READ)); // 1 / ok
   
   // now the second call will succeed
   ha[1] = PRTF(FileOpen(rawdata, FILE_BIN | FILE_READ | FILE_SHARE_READ)); // 2 / ok
   
   // now try to open it for modification (reading + writing)
   // writing was not allowed for sharing in previous handles, so the new request fails
   ha[2] = PRTF(FileOpen(rawdata, FILE_BIN | FILE_READ | FILE_WRITE | FILE_SHARE_READ)); // -1 / CANNOT_OPEN_FILE(5004)

   const string newdata = "MQL5Book/newdata";

   // NB: if you run the script second time the file "newdata"
   // might be existed, so we need to simulate clean environment
   // and delete the file
   if(FileDelete(newdata)) // cleanup for next execution of the test
   {
      PrintFormat("Note: file %s was left from previous run,"
         " deleted for proper test case.", newdata);
   }
   
   // let's try to open non-existing file for reading
   ha[3] = PRTF(FileOpen(newdata, FILE_BIN | FILE_READ)); // -1 / CANNOT_OPEN_FILE(5004)
   
   // and now create such new file
   ha[3] = PRTF(FileOpen(newdata, FILE_BIN | FILE_READ | FILE_WRITE)); // 3 / ok
   
   // try to write some data into the same file using FileSave,
   // this fails because sharing was not specified above
   long x[1] = {0x123456789ABCDEF0};
   PRTF(FileSave(newdata, x)); // false
   
   // let's close the file and reopen it with all sharing options
   FileClose(ha[3]);
   ha[3] = PRTF(FileOpen(newdata, FILE_BIN | FILE_READ | FILE_WRITE | FILE_SHARE_READ | FILE_SHARE_WRITE)); // 3 / ok
   
   // this time we can write into the same file using FileSave,
   // which simulates "external access" to the file
   // in the sense that it's performed bypassing our handle in ha[3]
   PRTF(FileSave(newdata, x)); // true
   // you may look into MQL5/Files/MQL5Book/ folder to find 8-bytes length "newdata" file
   
   // graceful close of all files
   for(int i = 0; i < ArraySize(ha); ++i)
   {
      if(ha[i] != INVALID_HANDLE)
      {
        FileClose(ha[i]);
      }
   }
}
//+------------------------------------------------------------------+
