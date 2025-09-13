//+------------------------------------------------------------------+
//|                                               EnvPermissions.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "PRTF.mqh"

//+------------------------------------------------------------------+
//| Draft for requesting permissions                                 |
//| (will be completed with applied stuff)                           |
//+------------------------------------------------------------------+
class Permissions
{
public:
   static bool isTradeOnSymbolEnabled(const string symbol, const datetime session)
   {
      // TODO: refine this method, this is a stub now
      return symbol == NULL;
   }
   
   static bool isTradeEnabled(const string symbol = NULL, const datetime session = 0)
   {
      return PRTF(TerminalInfoInteger(TERMINAL_TRADE_ALLOWED))
          && PRTF(MQLInfoInteger(MQL_TRADE_ALLOWED))
          && isTradeOnSymbolEnabled(symbol, session);
   }

   static bool isDllsEnabledByDefault()
   {
      return (bool)PRTF(TerminalInfoInteger(TERMINAL_DLLS_ALLOWED));
   }

   static bool isDllsEnabled()
   {
      return (bool)PRTF(MQLInfoInteger(MQL_DLLS_ALLOWED));
   }
   
   static bool isEmailEnabled()
   {
      return (bool)PRTF(TerminalInfoInteger(TERMINAL_EMAIL_ENABLED));
   }
   
   static bool isFtpEnabled()
   {
      return (bool)PRTF(TerminalInfoInteger(TERMINAL_FTP_ENABLED));
   }
   
   static bool isPushEnabled()
   {
      return (bool)PRTF(TerminalInfoInteger(TERMINAL_NOTIFICATIONS_ENABLED));
   }
   
   static bool isSignalsEnabled()
   {
      return (bool)PRTF(MQLInfoInteger(MQL_SIGNALS_ALLOWED));
   }
};

//+------------------------------------------------------------------+
//| Facultative study:                                               |
//| DLL binding to show DLL-related permissions in action.           |
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
   Permissions::isTradeEnabled();
   Permissions::isDllsEnabledByDefault();
   Permissions::isDllsEnabled();
   Permissions::isEmailEnabled();
   Permissions::isPushEnabled();
   Permissions::isSignalsEnabled();
   
   // Facultative study:
   // uncomment the next instruction to see how DLLs affect the program:
   // we need to call the function in order to activate the import
   // (if the function is not called, it's not compiled, and hence
   // there would be no DLL dependency)
   // ReadClipboard();
}
//+------------------------------------------------------------------+
/*
   example output
   
   TerminalInfoInteger(TERMINAL_TRADE_ALLOWED)=1 / ok
   MQLInfoInteger(MQL_TRADE_ALLOWED)=1 / ok
   TerminalInfoInteger(TERMINAL_DLLS_ALLOWED)=0 / ok
   MQLInfoInteger(MQL_DLLS_ALLOWED)=0 / ok
   TerminalInfoInteger(TERMINAL_EMAIL_ENABLED)=0 / ok
   TerminalInfoInteger(TERMINAL_NOTIFICATIONS_ENABLED)=0 / ok
   MQLInfoInteger(MQL_SIGNALS_ALLOWED)=0 / ok

*/
//+------------------------------------------------------------------+
