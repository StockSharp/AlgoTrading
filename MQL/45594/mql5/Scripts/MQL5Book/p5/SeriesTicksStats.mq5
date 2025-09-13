//+------------------------------------------------------------------+
//|                                             SeriesTicksStats.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property script_show_inputs

#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/TickEnum.mqh>

input string WorkSymbol = NULL; // Symbol (leave empty for current)
input int TickCount = 10000;
input COPY_TICKS TickType = ALL_TICKS;

//+------------------------------------------------------------------+
//| Storage for tick count with specific flags                       |
//+------------------------------------------------------------------+
struct TickFlagStats
{
   TICK_FLAGS flag;
   int count;
   string legend;
};

//+------------------------------------------------------------------+
//| Calculate tick stats by specific flags                           |
//+------------------------------------------------------------------+
int CalcTickStats(const string symbol, const COPY_TICKS type,
   const datetime start, const int count,
   TickFlagStats &stats[])
{
   MqlTick ticks[];
   ResetLastError();
   const int nf = ArraySize(stats);
   const int nt = CopyTicks(symbol, ticks, type, start * 1000, count);
   if(nt > -1 && _LastError == 0)
   {
      PrintFormat("Ticks range: %s'%03d - %s'%03d",
         TimeToString(ticks[0].time, TIME_DATE | TIME_SECONDS),
         ticks[0].time_msc % 1000,
         TimeToString(ticks[nt - 1].time, TIME_DATE | TIME_SECONDS),
         ticks[nt - 1].time_msc % 1000);
      
      // loop through ticks
      for(int j = 0; j < nt; ++j)
      {
         // loop through TICK_FLAGs (2 4 8 16 32 64) and combos
         for(int k = 0; k < nf; ++k)
         {
            if((ticks[j].flags & stats[k].flag) == stats[k].flag)
            {
               stats[k].count++;
            }
         }
      }
   }
   return nt;
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Print("");

   TickFlagStats stats[8] = {};
   for(int k = 1; k < 7; ++k)
   {
      stats[k].flag = (TICK_FLAGS)(1 << k);
      stats[k].legend = EnumToString(stats[k].flag);
   }
   stats[0].flag = TF_BID_ASK;  // BID & ASK combination stats
   stats[7].flag = TF_BUY_SELL; // BUY & SELL combination stats
   stats[0].legend = "TF_BID_ASK (COMBO)";
   stats[7].legend = "TF_BUY_SELL (COMBO)";
   
   const int count = CalcTickStats(WorkSymbol, TickType, 0, TickCount, stats);
   
   PrintFormat("%s stats requested: %d (got: %d) on %s",
      EnumToString(TickType),
      TickCount, count, StringLen(WorkSymbol) > 0 ? WorkSymbol : _Symbol);
   ArrayPrint(stats);

   /*
      output example 1:

      Ticks range: 2021.10.11 07:39:53'278 - 2021.10.13 11:51:29'428
      ALL_TICKS stats requested: 100000 (got: 100000) on YNDX.MM
          [flag] [count]              [legend]
      [0]      6   11323 "TF_BID_ASK (COMBO)" 
      [1]      2   26700 "TF_BID"             
      [2]      4   33541 "TF_ASK"             
      [3]      8   51082 "TF_LAST"            
      [4]     16   51082 "TF_VOLUME"          
      [5]     32   25654 "TF_BUY"             
      [6]     64   28802 "TF_SELL"            
      [7]     96    3374 "TF_BUY_SELL (COMBO)"

      output example 2:
      Ticks range: 2021.10.06 20:43:40'024 - 2021.10.13 11:52:40'044
      TRADE_TICKS stats requested: 100000 (got: 100000) on YNDX.MM
          [flag] [count]              [legend]
      [0]      6       0 "TF_BID_ASK (COMBO)" 
      [1]      2       0 "TF_BID"             
      [2]      4       0 "TF_ASK"             
      [3]      8  100000 "TF_LAST"            
      [4]     16  100000 "TF_VOLUME"          
      [5]     32   51674 "TF_BUY"             
      [6]     64   55634 "TF_SELL"            
      [7]     96    7308 "TF_BUY_SELL (COMBO)"

      output example 3:
      Ticks range: 2021.10.07 07:08:24'692 - 2021.10.13 11:54:01'297
      INFO_TICKS stats requested: 100000 (got: 100000) on YNDX.MM
          [flag] [count]              [legend]
      [0]      6   23115 "TF_BID_ASK (COMBO)" 
      [1]      2   60860 "TF_BID"             
      [2]      4   62255 "TF_ASK"             
      [3]      8       0 "TF_LAST"            
      [4]     16       0 "TF_VOLUME"          
      [5]     32       0 "TF_BUY"             
      [6]     64       0 "TF_SELL"            
      [7]     96       0 "TF_BUY_SELL (COMBO)"
   */
}
//+------------------------------------------------------------------+
