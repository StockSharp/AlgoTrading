//+------------------------------------------------------------------+
//|                                                    FileExist.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "PRTF.mqh"

const string filetemp = "MQL5Book/temp";

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   PRTF(FileIsExist(filetemp)); // false / FILE_NOT_EXIST(5019)
   PRTF(FileDelete(filetemp));  // false / FILE_NOT_EXIST(5019)
   
   // create new file or empty existing one
   int handle = PRTF(FileOpen(filetemp, FILE_TXT | FILE_WRITE | FILE_ANSI)); // 1
   
   PRTF(FileIsExist(filetemp)); // true
   // file is currently open, so it's locked for deletion
   PRTF(FileDelete(filetemp));  // false / CANNOT_DELETE_FILE(5006)
   
   FileClose(handle);
   
   PRTF(FileIsExist(filetemp)); // true
   PRTF(FileDelete(filetemp));  // true
   PRTF(FileIsExist(filetemp)); // false / FILE_NOT_EXIST(5019)
   
   PRTF(FileIsExist("MQL5Book")); // false / FILE_IS_DIRECTORY(5018)
   PRTF(FileDelete("MQL5Book"));  // false / FILE_IS_DIRECTORY(5018)
}
//+------------------------------------------------------------------+
