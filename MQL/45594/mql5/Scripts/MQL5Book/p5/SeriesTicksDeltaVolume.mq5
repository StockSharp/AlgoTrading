//+------------------------------------------------------------------+
//|                                       SeriesTicksDeltaVolume.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property script_show_inputs

#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/TickEnum.mqh>

input string WorkSymbol = NULL; // Symbol (leave empty for current)
input ENUM_TIMEFRAMES _TimeFrame = PERIOD_CURRENT; // TimeFrame
input int BarCount = 100;
input COPY_TICKS TickType = INFO_TICKS;

// we need this replacement variable because inputs are read-only
const ENUM_TIMEFRAMES TimeFrame = _TimeFrame == PERIOD_CURRENT ? _Period : _TimeFrame;

//+------------------------------------------------------------------+
//| Delta volume per single bar: timestamp, buy, sell, delta         |
//+------------------------------------------------------------------+
struct DeltaVolumePerBar
{
   datetime time;
   ulong buy;
   ulong sell;
   long delta;
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   if(TimeFrame >= PERIOD_D1
   || TimeFrame == PERIOD_M1)
   {
      Print("Use intraday timeframe larger than M1 and smaller than D1, please");
      return;
   }
   
   DeltaVolumePerBar deltas[];
   ArrayResize(deltas, BarCount);
   ZeroMemory(deltas);

   for(int i = 0; i < BarCount; ++i)
   {
      MqlTick ticks[];
      const datetime next = iTime(WorkSymbol, TimeFrame, i);
      const datetime prev = iTime(WorkSymbol, TimeFrame, i + 1);
      ResetLastError();
      const int n = CopyTicksRange(WorkSymbol, ticks, COPY_TICKS_ALL, prev * 1000, next * 1000 - 1);
      if(n > -1 && _LastError == 0)
      {
         deltas[i].time = prev;
         for(int j = 0; j < n; ++j)
         {
            // when real volumes are expected to be available, check them in the ticks
            if(TickType == TRADE_TICKS)
            {
               // accumulate volumes for buy and sell deals separately
               if((ticks[j].flags & TICK_FLAG_BUY) != 0)
               {
                  deltas[i].buy += ticks[j].volume;
               }
               if((ticks[j].flags & TICK_FLAG_SELL) != 0)
               {
                  deltas[i].sell += ticks[j].volume;
               }
            }
            else
            if(TickType == INFO_TICKS && j > 0)
            {
               // when real volumes are unavailable, use price moves up/down to estimate volume change
               if((ticks[j].flags & (TICK_FLAG_ASK | TICK_FLAG_BID)) != 0)
               {
                  const long d = (long)(((ticks[j].ask + ticks[j].bid) - (ticks[j - 1].ask + ticks[j - 1].bid)) / _Point);
                  if(d > 0) deltas[i].buy += d;
                  else deltas[i].sell += -d;
               }
            }
         }
         deltas[i].delta = (long)(deltas[i].buy - deltas[i].sell);
      }
      else
      {
         Print(i, ": ", _LastError);
      }
   }

   PrintFormat("Delta volumes per intraday bar\nProcessed %d bars on %s %s %s",
      BarCount, StringLen(WorkSymbol) > 0 ? WorkSymbol : _Symbol,
      EnumToString(TimeFrame), EnumToString(TickType));

   ArrayPrint(deltas);
   
   /*
      output example 1 (excerpt):
      
      Delta volumes per intraday bar
      Processed 100 bars on YNDX.MM PERIOD_H1 TRADE_TICKS
                        [time] [buy] [sell] [delta]
      [ 0] 2021.10.13 11:00:00  7912  14169   -6257
      [ 1] 2021.10.13 10:00:00  8470  11467   -2997
      [ 2] 2021.10.13 09:00:00 10830  13047   -2217
      [ 3] 2021.10.13 08:00:00 23682  19478    4204
      [ 4] 2021.10.13 07:00:00 14538  11600    2938
      [ 5] 2021.10.12 20:00:00  2132   4786   -2654
      [ 6] 2021.10.12 19:00:00  9173  13775   -4602
      [ 7] 2021.10.12 18:00:00  1297   1719    -422
      [ 8] 2021.10.12 17:00:00  3803   2995     808
      [ 9] 2021.10.12 16:00:00  6743   7045    -302
      [10] 2021.10.12 15:00:00 17286  37286  -20000
      ...

      output example 2 (excerpt):
      
      Delta volumes per intraday bar
      Processed 100 bars on YNDX.MM PERIOD_H1 INFO_TICKS
                        [time]  [buy] [sell] [delta]
      [ 0] 2021.10.13 11:00:00   1939   2548    -609
      [ 1] 2021.10.13 10:00:00   2222   2400    -178
      [ 2] 2021.10.13 09:00:00   2903   2909      -6
      [ 3] 2021.10.13 08:00:00   4489   4060     429
      [ 4] 2021.10.13 07:00:00   4999   4285     714
      [ 5] 2021.10.12 20:00:00   1444   1556    -112
      [ 6] 2021.10.12 19:00:00   5464   5867    -403
      [ 7] 2021.10.12 18:00:00   2522   2653    -131
      [ 8] 2021.10.12 17:00:00   2111   2017      94
      [ 9] 2021.10.12 16:00:00   4617   6096   -1479
      [10] 2021.10.12 15:00:00   5716   5411     305
      ...
   */
}
//+------------------------------------------------------------------+
