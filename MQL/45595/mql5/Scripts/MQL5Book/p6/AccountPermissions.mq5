//+------------------------------------------------------------------+
//|                                           AccountPermissions.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Check complete set of trading permissions,                       |
//| including account settings, such as investor login.              |
//+------------------------------------------------------------------+
#include <MQL5Book/Permissions.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   PrintFormat("Run on %s", _Symbol);
   if(!Permissions::isTradeEnabled()) // check default (current) symbol
   {
      Print("Trade is disabled for following reasons:");
      Print(Permissions::explainLastRestrictionBitMask());
   }
   else
   {
      Print("Trade is enabled");
   }
}
//+------------------------------------------------------------------+
/*
   example output (algotrading is disabled in the terminal, Russian market is closed):

      Run on USDRUB
      Trade is disabled for following reasons:
      TERMINAL_RESTRICTION PROGRAM_RESTRICTION SESSION_RESTRICTION 
   
   example output (algotrading is disabled in the terminal, symbol is not traded):

      Run on SP500m
      Trade is disabled for following reasons:
      TERMINAL_RESTRICTION PROGRAM_RESTRICTION SYMBOL_RESTRICTION

   example output (algotrading is enabled, but inverstor login is used):

      Run on XAUUSD
      Trade is disabled for following reasons:
      ACCOUNT_RESTRICTION 

*/
//+------------------------------------------------------------------+
