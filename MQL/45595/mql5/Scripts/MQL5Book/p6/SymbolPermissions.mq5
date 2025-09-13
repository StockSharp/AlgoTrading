//+------------------------------------------------------------------+
//|                                            SymbolPermissions.mq5 |
//|                             Copyright 2021-2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/DateTime.mqh>

//+------------------------------------------------------------------+
//| Draft for requesting permissions                                 |
//| (will be complemented with applied stuff)                        |
//+------------------------------------------------------------------+
class Permissions
{
   enum TRADE_RESTRICTIONS
   {
      TERMINAL_RESTRICTION = 1,
      PROGRAM_RESTRICTION = 2,
      SYMBOL_RESTRICTION = 4,
      SESSION_RESTRICTION = 8,
   };

   static uint lastFailReasonBitMask;
   static bool pass(const bool value, const uint bitflag) 
   {
      if(!value) lastFailReasonBitMask |= bitflag;
      return value;
   }
   
public:
   static uint getFailReasonBitMask()
   {
      return lastFailReasonBitMask;
   }
   
   static string explainBitMask()
   {
      string result = "";
      for(int i = 0; i < 4; ++i)
      {
         if(((1 << i) & lastFailReasonBitMask) != 0)
         {
            result += EnumToString((TRADE_RESTRICTIONS)(1 << i)) + " ";
         }
      }
      return result;
   }
   
   static bool isTradeOnSymbolEnabled(string symbol, const datetime now = 0,
      const ENUM_SYMBOL_TRADE_MODE mode = SYMBOL_TRADE_MODE_FULL)
   {
      // check sessions
      bool found = now == 0;
      if(!found)
      {
         const static ulong day = 60 * 60 * 24;
         const ulong time = (ulong)now % day;
         datetime from, to;
         int i = 0;
         
         ENUM_DAY_OF_WEEK d = TimeDayOfWeek(now);
         
         while(!found && SymbolInfoSessionTrade(symbol, d, i++, from, to))
         {
            found = time >= (ulong)from && time < (ulong)to;
         }
      }
      // check symbol trade mode
      return pass(found, SESSION_RESTRICTION)
         && pass(SymbolInfoInteger(symbol, SYMBOL_TRADE_MODE) == mode, SYMBOL_RESTRICTION);
   }
   
   static bool isTradeEnabled(const string symbol = NULL, const datetime now = 0)
   {
      lastFailReasonBitMask = 0;
      // TODO: refine this method: add account settings check-up
      return pass(TerminalInfoInteger(TERMINAL_TRADE_ALLOWED), TERMINAL_RESTRICTION)
          && pass(MQLInfoInteger(MQL_TRADE_ALLOWED), PROGRAM_RESTRICTION)
          && isTradeOnSymbolEnabled(symbol == NULL ? _Symbol : symbol, now);
   }

   static bool isDllsEnabledByDefault()
   {
      return (bool)TerminalInfoInteger(TERMINAL_DLLS_ALLOWED);
   }

   static bool isDllsEnabled()
   {
      return (bool)MQLInfoInteger(MQL_DLLS_ALLOWED);
   }
   
   static bool isEmailEnabled()
   {
      return (bool)TerminalInfoInteger(TERMINAL_EMAIL_ENABLED);
   }
   
   static bool isFtpEnabled()
   {
      return (bool)TerminalInfoInteger(TERMINAL_FTP_ENABLED);
   }
   
   static bool isPushEnabled()
   {
      return (bool)TerminalInfoInteger(TERMINAL_NOTIFICATIONS_ENABLED);
   }
   
   static bool isSignalsEnabled()
   {
      return (bool)MQLInfoInteger(MQL_SIGNALS_ALLOWED);
   }
};

static uint Permissions::lastFailReasonBitMask;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   string disabled = "";
   
   const int n = SymbolsTotal(true);
   // check all symbols in Market Watch
   for(int i = 0; i < n; ++i)
   {
      const string s = SymbolName(i, true);
      if(!Permissions::isTradeEnabled(s, TimeCurrent()))
      {
         disabled += s + "=" + Permissions::explainBitMask() +"\n";
      }
   }
   if(disabled != "")
   {
      Print("Trade is disabled for following symbols and origins:");
      Print(disabled);
   }
}
//+------------------------------------------------------------------+
/*
   example output
   
   Trade is disabled for following symbols and origins:
   USDRUB=SESSION_RESTRICTION
   SP500m=SYMBOL_RESTRICTION

*/
//+------------------------------------------------------------------+
