//+------------------------------------------------------------------+
//|                                          SeriesSpreadHighest.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property script_show_inputs

#include <MQL5Book/PRTF.mqh>

// The macro provides more convenient datetime output without seconds
#define ArrayPrintM(A) \
   ArrayPrint(A, _Digits, NULL, 0, -1, \
   ARRAYPRINT_INDEX | ARRAYPRINT_DATE | ARRAYPRINT_MINUTES | ARRAYPRINT_HEADER)

input string WorkSymbol = NULL; // Symbol (leave empty for current)
input ENUM_TIMEFRAMES _TimeFrame = PERIOD_CURRENT; // TimeFrame
input int BarCount = 100;

// we need this replacement variable because inputs are read-only
const ENUM_TIMEFRAMES TimeFrame = _TimeFrame == PERIOD_CURRENT ? _Period : _TimeFrame;

//+------------------------------------------------------------------+
//| Properties of a single bar: timestamp, spread, count and         |
//| throughout indices of M1 bars, which form this timeframe bar     |
//+------------------------------------------------------------------+
struct SpreadPerBar
{
   datetime time;
   int spread;
   int max; // throughout index of M1 bar with a spread, which is maximum
            // among all M1 bars inside the current bar of higher timeframe
   int num; // number of M1 bars in the current bar of higher timeframe
   int pos; // starting index of M1 bar in the current bar of higher timeframe
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
      const datetime next = iTime(WorkSymbol, TimeFrame, i);
      const datetime prev = iTime(WorkSymbol, TimeFrame, i + 1);
      const int p = iBarShift(WorkSymbol, PERIOD_M1, next - 1);
      const int n = Bars(WorkSymbol, PERIOD_M1, prev, next - 1);
      const int m = iHighest(WorkSymbol, PERIOD_M1, MODE_SPREAD, n, p);
      if(m > -1)
      {
         peaks[i].spread = iSpread(WorkSymbol, PERIOD_M1, m);
         peaks[i].time = prev;
         peaks[i].max = m;
         peaks[i].num = n;
         peaks[i].pos = p;
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
                     [time] [spread] [max] [num] [pos]
      [ 0] 2021.10.12 15:00        0     7    60     7
      [ 1] 2021.10.12 14:00        1    89    60    67
      [ 2] 2021.10.12 13:00        1   181    60   127
      [ 3] 2021.10.12 12:00        1   213    60   187
      [ 4] 2021.10.12 11:00        1   248    60   247
      [ 5] 2021.10.12 10:00        0   307    60   307
      [ 6] 2021.10.12 09:00        1   385    60   367
      [ 7] 2021.10.12 08:00        2   469    60   427
      [ 8] 2021.10.12 07:00        2   497    60   487
      [ 9] 2021.10.12 06:00        1   550    60   547
      [10] 2021.10.12 05:00        1   616    60   607
      [11] 2021.10.12 04:00        1   678    60   667
      [12] 2021.10.12 03:00        1   727    60   727
      [13] 2021.10.12 02:00        4   820    60   787
      [14] 2021.10.12 01:00       16   906    60   847
      [15] 2021.10.12 00:00       65   956    60   907
      [16] 2021.10.11 23:00       15   967    60   967
      [17] 2021.10.11 22:00        2  1039    60  1027
      [18] 2021.10.11 21:00        1  1090    60  1087
      [19] 2021.10.11 20:00        1  1148    60  1147
      [20] 2021.10.11 19:00        2  1210    60  1207
      [21] 2021.10.11 18:00        1  1313    60  1267
      [22] 2021.10.11 17:00        1  1345    60  1327
      [23] 2021.10.11 16:00        1  1411    60  1387
      [24] 2021.10.11 15:00        2  1461    60  1447
      [25] 2021.10.11 14:00        1  1526    60  1507
      ...
   */
}
//+------------------------------------------------------------------+
