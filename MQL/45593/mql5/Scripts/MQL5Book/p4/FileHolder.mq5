//+------------------------------------------------------------------+
//|                                                   FileHolder.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#define FILE_DEBUG_PRINT

#include <MQL5Book/MqlError.mqh>
#include <MQL5Book/FileHolder.mqh>
#include "PRTF.mqh"

// NB: we will open the same file many times in this script
// just for demonstration purpose;
// normally you need only one handle for a file in a program

const string dummy = "MQL5Book/dummy";

//+------------------------------------------------------------------+
//| Subfunction where more file handles are opened in local context  |
//+------------------------------------------------------------------+
void SubFunc()
{
   Print(__FUNCTION__, " enter");
   FileHolder holder;
   int h = PRTF(holder.FileOpen(dummy, FILE_BIN | FILE_WRITE | FILE_SHARE_WRITE | FILE_SHARE_READ));
   int f = PRTF(holder.FileOpen(dummy, FILE_BIN | FILE_WRITE | FILE_SHARE_WRITE | FILE_SHARE_READ));
   // use h and f somehow,
   // ...
   // Print(FileHolder::GetFilename(f));
   // no need to close files manually and track early returns
   Print(__FUNCTION__, " exit");
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Print(__FUNCTION__, " enter");

   FileHolder holder;
   int h = PRTF(holder.FileOpen(dummy, FILE_BIN | FILE_WRITE | FILE_SHARE_WRITE | FILE_SHARE_READ));
   // some data writing and other action on 'h' could be here
   // ...
   /*
   int a[] = {1, 2, 3};
   FileWriteArray(h, a);
   */
   
   SubFunc();
   SubFunc();
   
   if(rand() > 32000) // imitate branches on possible conditions
   {
      // we do not need this with the holder
      // FileClose(h);
      Print(__FUNCTION__, " return");
      return; // we can have multiple exits from a function
   }

   /*
      ... more code
   */
   
   // we do not need this with the holder
   // FileClose(h);

   Print(__FUNCTION__, " exit");

   // Note: we do not close any file handles manually
   // FileHolder instances will do it automatically in appropriate destructors
   
   /*
      output:
      
      OnStart enter
      holder.FileOpen(dummy,FILE_BIN|FILE_WRITE|FILE_SHARE_WRITE|FILE_SHARE_READ)=1 / ok
      SubFunc enter
      holder.FileOpen(dummy,FILE_BIN|FILE_WRITE|FILE_SHARE_WRITE|FILE_SHARE_READ)=2 / ok
      holder.FileOpen(dummy,FILE_BIN|FILE_WRITE|FILE_SHARE_WRITE|FILE_SHARE_READ)=3 / ok
      SubFunc exit
      FileOpener::~FileOpener: Automatic close for handle: 3
      FileOpener::~FileOpener: Automatic close for handle: 2
      SubFunc enter
      holder.FileOpen(dummy,FILE_BIN|FILE_WRITE|FILE_SHARE_WRITE|FILE_SHARE_READ)=2 / ok
      holder.FileOpen(dummy,FILE_BIN|FILE_WRITE|FILE_SHARE_WRITE|FILE_SHARE_READ)=3 / ok
      SubFunc exit
      FileOpener::~FileOpener: Automatic close for handle: 3
      FileOpener::~FileOpener: Automatic close for handle: 2
      OnStart exit
      FileOpener::~FileOpener: Automatic close for handle: 1
   */
}
//+------------------------------------------------------------------+
