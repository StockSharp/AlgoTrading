//+------------------------------------------------------------------+
//|                                               SymbolInfoTick.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Request most recent tick info in different ways.                 |
//+------------------------------------------------------------------+

#include <MQL5Book/PRTF.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   PRTF(TimeToString(SymbolInfoInteger(_Symbol, SYMBOL_TIME), TIME_DATE | TIME_SECONDS));
   PRTF(SymbolInfoDouble(_Symbol, SYMBOL_BID));
   PRTF(SymbolInfoDouble(_Symbol, SYMBOL_ASK));
   PRTF(SymbolInfoDouble(_Symbol, SYMBOL_LAST));
   PRTF(SymbolInfoInteger(_Symbol, SYMBOL_VOLUME));
   PRTF(SymbolInfoInteger(_Symbol, SYMBOL_TIME_MSC));
   PRTF(SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_REAL));

   MqlTick tick[1];
   SymbolInfoTick(_Symbol, tick[0]);
   ArrayPrint(tick);
}
//+------------------------------------------------------------------+
/*
   example output:
   
   TimeToString(SymbolInfoInteger(_Symbol,SYMBOL_TIME),TIME_DATE|TIME_SECONDS)=2022.01.25 13:52:51 / ok
   SymbolInfoDouble(_Symbol,SYMBOL_BID)=1838.44 / ok
   SymbolInfoDouble(_Symbol,SYMBOL_ASK)=1838.49 / ok
   SymbolInfoDouble(_Symbol,SYMBOL_LAST)=0.0 / ok
   SymbolInfoInteger(_Symbol,SYMBOL_VOLUME)=0 / ok
   SymbolInfoInteger(_Symbol,SYMBOL_TIME_MSC)=1643118771166 / ok
   SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_REAL)=0.0 / ok
                    [time]   [bid]   [ask] [last] [volume]    [time_msc] [flags] [volume_real]
   [0] 2022.01.25 13:52:51 1838.44 1838.49   0.00        0 1643118771166       6          0.00

*/
//+------------------------------------------------------------------+
