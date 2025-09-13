//+------------------------------------------------------------------+
//|                                                   FileFolder.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "PRTF.mqh"

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const string filename = "MQL5Book/ABC/DEF/dummy";
   // create an empty file and 2 levels of container-subfolders (as by-product)
   uchar dummy[];
   PRTF(FileSave(filename, dummy)); // true
   
   // now create another subfolder on it sown
   PRTF(FolderCreate("MQL5Book/ABC/GHI")); // true
   // an attempt to remove the first subfolder will fail,
   // because it's not empty (there is the file there)
   PRTF(FolderDelete("MQL5Book/ABC/DEF")); // false / CANNOT_DELETE_DIRECTORY(5024)
   
   // if we're absolutely sure that the subfolder can be wiped out,
   // we should call FolderClean first, and FolderDelete next
   // but some files can be open (locked), which prevents cleaning
   // and here we emulate a locked file
   int handle = PRTF(FileOpen(filename, FILE_READ)); // 1
   
   // this try will fail, because the file is open
   PRTF(FolderClean("MQL5Book/ABC")); // false / CANNOT_CLEAN_DIRECTORY(5025)
   
   // after we close the file...
   FileClose(handle);
   // ...the folder can be cleared successfully
   PRTF(FolderClean("MQL5Book/ABC")); // true
   // and not folder deletion will work
   PRTF(FolderDelete("MQL5Book/ABC")); // true
}
//+------------------------------------------------------------------+
