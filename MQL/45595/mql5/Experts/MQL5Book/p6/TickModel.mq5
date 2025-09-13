//+------------------------------------------------------------------+
//|                                                    TickModel.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/TickModel.mqh>

input int TickCount = 5;
input TICK_MODEL RequireTickModel = TICK_MODEL_UNKNOWN;

//+------------------------------------------------------------------+
//| Tick event handler                                               |
//+------------------------------------------------------------------+
void OnTick()
{
   if(MQLInfoInteger(MQL_TESTER))
   {
      static int count = 0;
      if(count++ < TickCount)
      {
         static MqlTick tick[1];
         SymbolInfoTick(_Symbol, tick[0]);
         ArrayPrint(tick);
         const TICK_MODEL model = getTickModel();
         PrintFormat("%d %s", count, EnumToString(model));
         if(count >= 2)
         {
            if(RequireTickModel != TICK_MODEL_UNKNOWN
            && RequireTickModel < model)
            {
               PrintFormat("Tick model is incorrect (%s %sis required), terminating",
                  EnumToString(RequireTickModel),
                  (RequireTickModel != TICK_MODEL_REAL ? "or better " : ""));
               ExpertRemove();
            }
         }
      }
   }
}
//+------------------------------------------------------------------+
/*
   example 1: EURUSD, H1, "Every tick"
   output for RequireTickModel=OHLC M1
   
                    [time]   [bid]   [ask]  [last] [volume]    [time_msc] [flags] [volume_real]
   [0] 2022.04.01 00:00:30 1.10656 1.10679 1.10656        0 1648771230000      14       0.00000
   NOT_ENOUGH_MEMORY
   1 TICK_MODEL_GENERATED
                    [time]   [bid]   [ask]  [last] [volume]    [time_msc] [flags] [volume_real]
   [0] 2022.04.01 00:01:00 1.10656 1.10680 1.10656        0 1648771260000      12       0.00000
   2 TICK_MODEL_GENERATED
                    [time]   [bid]   [ask]  [last] [volume]    [time_msc] [flags] [volume_real]
   [0] 2022.04.01 00:01:30 1.10608 1.10632 1.10608        0 1648771290000      14       0.00000
   3 TICK_MODEL_GENERATED
   
   example 2: EURUSD, H1, "Every tick based on real ticks"
   output for RequireTickModel=OHLC M1
   
                    [time]   [bid]   [ask] [last] [volume]    [time_msc] [flags] [volume_real]
   [0] 2022.04.01 00:00:00 1.10656 1.10687 0.0000        0 1648771200122     134       0.00000
   1 TICK_MODEL_REAL
                    [time]   [bid]   [ask] [last] [volume]    [time_msc] [flags] [volume_real]
   [0] 2022.04.01 00:00:00 1.10656 1.10694 0.0000        0 1648771200417       4       0.00000
   2 TICK_MODEL_REAL
                    [time]   [bid]   [ask] [last] [volume]    [time_msc] [flags] [volume_real]
   [0] 2022.04.01 00:00:00 1.10656 1.10691 0.0000        0 1648771200816       4       0.00000
   3 TICK_MODEL_REAL
   
   example 3: EURUSD, H1, "Open prices"
   output for RequireTickModel=OHLC M1
   
                    [time]   [bid]   [ask]  [last] [volume]    [time_msc] [flags] [volume_real]
   [0] 2022.04.01 00:00:00 1.10656 1.10679 1.10656        0 1648771200000      14       0.00000
   FUNCTION_NOT_ALLOWED
   1 TICK_MODEL_OPEN_PRICES
                    [time]   [bid]   [ask]  [last] [volume]    [time_msc] [flags] [volume_real]
   [0] 2022.04.01 01:00:00 1.10660 1.10679 1.10660        0 1648774800000      14       0.00000
   2 TICK_MODEL_OPEN_PRICES
   Tick model is incorrect (TICK_MODEL_OHLC_M1 or better is required), terminating
   ExpertRemove() function called

*/
//+------------------------------------------------------------------+
