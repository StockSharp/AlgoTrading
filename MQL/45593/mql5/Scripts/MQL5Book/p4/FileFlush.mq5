//+------------------------------------------------------------------+
//|                                                    FileFlush.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property script_show_inputs

// NB: PRTF clears up _LastError, so don't use it in code blocks
// using conditions based on _LastError
#include "PRTF.mqh"

input bool EnableFlashing = false;
input bool UseCommonFolder = false;

const string dataport = "MQL5Book/dataport";
const int flag = UseCommonFolder ? FILE_COMMON : 0;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Print("\n*");
   // make initial setup
   bool modeWriter = true; // by default script starts to write data
   int count = 0;          // number of writes/reads
   // create new file for writing or empty existing one for "sender" role
   int handle = PRTF(FileOpen(dataport,
      FILE_BIN | FILE_WRITE | FILE_SHARE_READ | flag));
   // if writing is impossible, another instance of the script is
   // already writing most likely, so try to read the same file
   if(handle == INVALID_HANDLE)
   {
      // check if the file can be read, if so then assuming "receiver" role
      handle = PRTF(FileOpen(dataport,
         FILE_BIN | FILE_READ | FILE_SHARE_WRITE | FILE_SHARE_READ | flag));
      if(handle == INVALID_HANDLE)
      {
         Print("Can't open file"); // this shouldn't be, something is wrong
         return;
      }
      modeWriter = false; // switch the mode
   }
   
   // run main loop until user cancels it
   while(!IsStopped())
   {
      long temp = 0;
      if(modeWriter)
      {
         temp = TimeLocal(); // get current local datetime
         // append new timestamp every 5 seconds
         FileWriteLong(handle, temp);
         count++;
         if(EnableFlashing)
         {
            FileFlush(handle);
         }
         Print(StringFormat("Written[%d]: %I64d", count, temp));
      }
      else
      {
         ResetLastError();
         // read out all the file contents written by the sender
         // since we have read the file last time (approx. 5 seconds ago)
         // or from very beginning if this is first time we read the file;
         // NB: you may want to implement another logic, e.g.
         //     skip pre-existed data and start reading new additions only
         while(true) // read until end of data, then break the loop
         {
            bool reportedEndBeforeRead = FileIsEnding(handle);
            ulong reportedTellBeforeRead = FileTell(handle);
            // metadata about file size remains the same
            // from the moment when the file was open for reading,
            // so do not call FileSeek(handle, 0, SEEK_END)
            // to keep reading past the End Of File (EOF)
            temp = FileReadLong(handle);
            // if there is no more data, we'll get error 5015 (ERR_FILE_READERROR)
            if(_LastError) break;
            // here we got data without error
            count++;
            Print(StringFormat("Read[%d]: %I64d\t"
               "(size=%I64d, before=%I64d(%s), after=%I64d)",
               count, temp,
               FileSize(handle), reportedTellBeforeRead,
               (string)reportedEndBeforeRead, FileTell(handle)));
         }
      }
      Sleep(5000);
   }
}
//+------------------------------------------------------------------+
