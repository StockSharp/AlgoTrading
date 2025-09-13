//+------------------------------------------------------------------+
//|                                                 SeriesSpread.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property script_show_inputs

#include <MQL5Book/PRTF.mqh>

// The macro provides more convenient datetime output without seconds
#define ArrayPrintM(A) \
   ArrayPrint(A, _Digits, NULL, 0, -1, \
   ARRAYPRINT_INDEX | ARRAYPRINT_DATE | ARRAYPRINT_MINUTES)

input string WorkSymbol = NULL; // Symbol (leave empty for current)
input ENUM_TIMEFRAMES _TimeFrame = PERIOD_CURRENT; // TimeFrame
input int BarCount = 100;

// we need this replacement variable because inputs are read-only
const ENUM_TIMEFRAMES TimeFrame = _TimeFrame == PERIOD_CURRENT ? _Period : _TimeFrame;

//+------------------------------------------------------------------+
//| Pair of properties per single bar: timestamp and spread          |
//+------------------------------------------------------------------+
struct SpreadPerBar
{
   datetime time;
   int spread;
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
   
   SpreadPerBar peaks[];
   ArrayResize(peaks, BarCount);
   ZeroMemory(peaks);

   for(int i = 0; i < BarCount; ++i)
   {
      int spreads[];
      const datetime next = iTime(WorkSymbol, TimeFrame, i);
      const datetime prev = iTime(WorkSymbol, TimeFrame, i + 1);
      const int n = CopySpread(WorkSymbol, PERIOD_M1, prev, next - 1, spreads);
      const int m = ArrayMaximum(spreads);
      if(m > -1)
      {
         peaks[i].spread = spreads[m];
         peaks[i].time = prev;
      }
   }

   PrintFormat("Maximal speads per intraday bar\nProcessed %d bars on %s %s",
      BarCount, StringLen(WorkSymbol) > 0 ? WorkSymbol : _Symbol,
      EnumToString(TimeFrame));

   ArrayPrintM(peaks);
   
   /*
      output example (excerpt):
      
      Maximal speads per intraday bar
      Processed 100 bars on EURUSD PERIOD_H1
      [ 0] 2021.10.12 14:00        1
      [ 1] 2021.10.12 13:00        1
      [ 2] 2021.10.12 12:00        1
      [ 3] 2021.10.12 11:00        1
      [ 4] 2021.10.12 10:00        0
      [ 5] 2021.10.12 09:00        1
      [ 6] 2021.10.12 08:00        2
      [ 7] 2021.10.12 07:00        2
      [ 8] 2021.10.12 06:00        1
      [ 9] 2021.10.12 05:00        1
      [10] 2021.10.12 04:00        1
      [11] 2021.10.12 03:00        1
      [12] 2021.10.12 02:00        4
      [13] 2021.10.12 01:00       16
      [14] 2021.10.12 00:00       65
      [15] 2021.10.11 23:00       15
      [16] 2021.10.11 22:00        2
      [17] 2021.10.11 21:00        1
      [18] 2021.10.11 20:00        1
      [19] 2021.10.11 19:00        2
      [20] 2021.10.11 18:00        1
      [21] 2021.10.11 17:00        1
      [22] 2021.10.11 16:00        1
      [23] 2021.10.11 15:00        2
      [24] 2021.10.11 14:00        1
      ...
   */
}
//+------------------------------------------------------------------+
