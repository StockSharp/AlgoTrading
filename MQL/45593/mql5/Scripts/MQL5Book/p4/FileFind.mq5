//+------------------------------------------------------------------+
//|                                                     FileFind.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "PRTF.mqh"

//+------------------------------------------------------------------+
//| Helper function to build a list of directory                     |
//+------------------------------------------------------------------+
bool DirList(const string filter, string &result[], bool common = false)
{
   string found[1];
   long handle = FileFindFirst(filter, found[0]);
   if(handle == INVALID_HANDLE) return false;
   do
   {
      if(ArrayCopy(result, found, ArraySize(result)) != 1) break;
   }
   while(FileFindNext(handle, found[0]));
   // sometimes FileFindNext sets _LastError to 5002 (dunno why)
   // so clear it up to prevent interference with analysis of reasonable error codes
   ResetLastError();
   FileFindClose(handle);
   return true;
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   string found; // receiving variable
   // start searching and obtain the handle
   long handle = PRTF(FileFindFirst("MQL5Book/*", found)); // 1
   if(handle != INVALID_HANDLE)
   {
      do
      {
         Print(found);
         /*
            output at least (the order may change):
            
            ansi1252.txt
            unicode1.txt
            unicode2.txt
            unicode3.txt
            utf8.txt
         */
      }
      while(FileFindNext(handle, found));
      FileFindClose(handle);
   }
   
   string list[];
   // try to request elements w/o extension
   PRTF(DirList("*.", list)); // false / WRONG_FILENAME(5002)

   // more loose condition requests elements with no extension
   // or 1-symbol extension and it works
   if(PRTF(DirList("*.?", list))) // true
   {
      ArrayPrint(list);
      // example output: "MQL5Book\" "Tester\"
   }
}
//+------------------------------------------------------------------+
