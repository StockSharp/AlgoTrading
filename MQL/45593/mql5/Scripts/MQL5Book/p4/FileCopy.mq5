//+------------------------------------------------------------------+
//|                                                     FileCopy.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "PRTF.mqh"

const string source = "MQL5Book/source";
const string destination = "MQL5Book/destination";

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // try copy a nonexistent file
   PRTF(FileCopy(source, 0, destination, FILE_COMMON)); // false / FILE_NOT_EXIST(5019)
   // create new file
   int handle = PRTF(FileOpen(source, FILE_TXT | FILE_WRITE)); // 1
   // write some text
   PRTF(FileWriteString(handle, "Test Text\n")); // 22
   // flush to disk but keep open (locked)
   FileFlush(handle);
   
   // the next call will fail, because file was open without FILE_SHARE_READ
   PRTF(FileCopy(source, 0, destination, FILE_COMMON)); // false / CANNOT_OPEN_FILE(5004)

   // now close it (and unlock)   
   FileClose(handle);

   PRTF(FileGetInteger(source, FILE_CREATE_DATE)); // 1629757115, example
   PRTF(FileGetInteger(source, FILE_MODIFY_DATE)); // 1629757115, example
   
   // wait a bit to make noticable gap
   // between creation and modification times in the copy below
   Sleep(3000);

   // try to copy it again
   PRTF(FileCopy(source, 0, destination, FILE_COMMON)); // true
   PRTF(FileGetInteger(destination, FILE_CREATE_DATE, true)); // 1629757118, +3 seconds
   PRTF(FileGetInteger(destination, FILE_MODIFY_DATE, true)); // 1629757115, example

   // now try to move the copy back over original one
   // this should fail because overwriting is not allowed by default
   PRTF(FileMove(destination, FILE_COMMON, source, 0)); // false / FILE_CANNOT_REWRITE(5020)

   // and now it should succeed thanks to FILE_REWRITE
   PRTF(FileMove(destination, FILE_COMMON, source, FILE_REWRITE)); // true

   // final cleanup for next runs of the test
   FileDelete(source);
}
//+------------------------------------------------------------------+
