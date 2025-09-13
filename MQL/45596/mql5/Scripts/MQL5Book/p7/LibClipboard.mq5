//+------------------------------------------------------------------+
//|                                                 LibClipboard.mq5 |
//|                             Copyright 2021-2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| DLL-related permissions are required!                            |
//| Clipboard reading is the task implemented by DLLs.               |
//+------------------------------------------------------------------+
#include <WinApi/winuser.mqh>
#include <WinApi/winbase.mqh>

//+------------------------------------------------------------------+
//| We need this define and import for accessing Windows clipboard   |
//+------------------------------------------------------------------+
#define CF_UNICODETEXT 13 // one of standard clipboard formats
#import "kernel32.dll"
string lstrcatW(PVOID string1, const string string2);
#import

//+------------------------------------------------------------------+
//| Example function to use DLL for reading Windows clipboard        |
//+------------------------------------------------------------------+
void ReadClipboard()
{
   if(OpenClipboard(NULL))
   {
      HANDLE h = GetClipboardData(CF_UNICODETEXT);
      PVOID p = GlobalLock(h);
      if(p != 0)
      {
         const string text = lstrcatW(p, "");
         Print("Clipboard: ", text);
         GlobalUnlock(h);
      }
      CloseClipboard();
   }
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   ReadClipboard();
}
//+------------------------------------------------------------------+
