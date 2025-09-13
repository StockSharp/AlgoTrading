//+------------------------------------------------------------------+
//|                                                  Permissions.mqh |
//|                             Copyright 2021-2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/DateTime.mqh>

#define RESTRICTIONS 6

//+------------------------------------------------------------------+
//| Check various permissions                                        |
//+------------------------------------------------------------------+
class Permissions
{
   static uint lastRestrictionBitMask;
   static bool pass(const uint bitflag) 
   {
      lastRestrictionBitMask |= bitflag;
      return lastRestrictionBitMask == 0;
   }
   
public:
   enum TRADE_RESTRICTIONS
   {
      NO_RESTRICTIONS      = 0,
      TERMINAL_RESTRICTION = 1,  // user disabled trades for all programs
      PROGRAM_RESTRICTION  = 2,  // user disabled trades for specific program
      SYMBOL_RESTRICTION   = 4,  // symbol is not tradable by specification
      SESSION_RESTRICTION  = 8,  // symbol session - market closed
      ACCOUNT_RESTRICTION  = 16, // investment login or broker limitation
      EXPERTS_RESTRICTION  = 32, // algotrading is disabled by broker
   };

   static uint getLastRestrictionBitMask()
   {
      return lastRestrictionBitMask;
   }
   
   static string explainLastRestrictionBitMask()
   {
      string result = "";
      for(int i = 0; i < RESTRICTIONS; ++i)
      {
         if(((1 << i) & lastRestrictionBitMask) != 0)
         {
            result += EnumToString((TRADE_RESTRICTIONS)(1 << i)) + " ";
         }
      }
      return result;
   }

   static uint getTradeRestrictionsOnSymbol(const string symbol, datetime now = 0,
      const ENUM_SYMBOL_TRADE_MODE mode = SYMBOL_TRADE_MODE_FULL)
   {
      // check sessions
      if(now == 0) now = TimeTradeServer();
      bool found = false;
      const static ulong day = 60 * 60 * 24;
      const ulong time = (ulong)now % day;
      datetime from, to;
      int i = 0;
      
      ENUM_DAY_OF_WEEK d = TimeDayOfWeek(now);
      
      while(!found && SymbolInfoSessionTrade(symbol, d, i++, from, to))
      {
         found = time >= (ulong)from && time < (ulong)to;
      }

      // check symbol trade mode
      const ENUM_SYMBOL_TRADE_MODE m = (ENUM_SYMBOL_TRADE_MODE)SymbolInfoInteger(symbol, SYMBOL_TRADE_MODE);
      return (found ? 0 : SESSION_RESTRICTION)
         | (((m & mode) != 0) || (m == SYMBOL_TRADE_MODE_FULL) ? 0 : SYMBOL_RESTRICTION);
   }

   static bool isTradeOnSymbolEnabled(const string symbol, const datetime now = 0,
      const ENUM_SYMBOL_TRADE_MODE mode = SYMBOL_TRADE_MODE_FULL)
   {
      lastRestrictionBitMask = 0;
      return pass(getTradeRestrictionsOnSymbol(symbol, now, mode));
   }
   
   static uint getTradeRestrictionsOnAccount()
   {
      return (AccountInfoInteger(ACCOUNT_TRADE_ALLOWED) ? 0 : ACCOUNT_RESTRICTION)
         | (AccountInfoInteger(ACCOUNT_TRADE_EXPERT) ? 0 : EXPERTS_RESTRICTION);
   }

   static bool isTradeOnAccountEnabled()
   {
      lastRestrictionBitMask = 0;
      return pass(getTradeRestrictionsOnAccount());
   }
   
   static uint getTradeRestrictionsOnProgram()
   {
      return (TerminalInfoInteger(TERMINAL_TRADE_ALLOWED) ? 0 : TERMINAL_RESTRICTION)
         | (MQLInfoInteger(MQL_TRADE_ALLOWED) ? 0 : PROGRAM_RESTRICTION);
   }

   static uint getTradeRestrictions(const string symbol = NULL, const datetime now = 0,
      const ENUM_SYMBOL_TRADE_MODE mode = SYMBOL_TRADE_MODE_FULL)
   {
      return getTradeRestrictionsOnProgram()
         | getTradeRestrictionsOnSymbol(symbol == NULL ? _Symbol : symbol, now, mode)
         | getTradeRestrictionsOnAccount();
   }
   
   static bool isTradeEnabled(const string symbol = NULL, const datetime now = 0,
      const ENUM_SYMBOL_TRADE_MODE mode = SYMBOL_TRADE_MODE_FULL)
   {
      lastRestrictionBitMask = 0;
      return pass(getTradeRestrictions(symbol, now, mode));
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

static uint Permissions::lastRestrictionBitMask;

//+------------------------------------------------------------------+
