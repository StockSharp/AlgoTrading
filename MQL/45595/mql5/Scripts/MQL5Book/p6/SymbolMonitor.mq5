//+------------------------------------------------------------------+
//|                                                SymbolMonitor.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| List all symbol properties in the log                            |
//+------------------------------------------------------------------+
#include <MQL5Book/SymbolMonitor.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   SymbolMonitor m;
   m.list2log<ENUM_SYMBOL_INFO_INTEGER>();
   m.list2log<ENUM_SYMBOL_INFO_DOUBLE>();
   m.list2log<ENUM_SYMBOL_INFO_STRING>();
}
//+------------------------------------------------------------------+
