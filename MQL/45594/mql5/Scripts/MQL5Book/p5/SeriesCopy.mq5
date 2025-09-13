//+------------------------------------------------------------------+
//|                                                   SeriesCopy.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/PRTF.mqh>

// The macro provides more convenient datetime output without seconds
#define ArrayPrintM(A) \
   ArrayPrint(A, _Digits, NULL, 0, -1, \
   ARRAYPRINT_INDEX | ARRAYPRINT_DATE | ARRAYPRINT_MINUTES)

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // Define the array to receive requested data
   datetime times[];

   Print(""); // separator line
   
   // Request 10 bars from 5-th of September and earlier
   // because it's Sunday, we'll get bars for Friday
   PRTF(CopyTime("EURUSD", PERIOD_H1, D'2021.09.05', 10, times));
   ArrayPrintM(times);
   PRTF(ArraySetAsSeries(times, true)); // switch to timeseries order
   ArrayPrintM(times);
   PRTF(ArraySetAsSeries(times, false)); // restore default ordering mode
   
   // Request some bars (we can't be sure that all H1 bars exist in the range)
   // between 2 timestamps, and do it in 2 ways:
   // first - from future to past, second - from past to future
   // both results will be the same         FROM                 TO
   PRTF(CopyTime("EURUSD", PERIOD_H1, D'2021.09.06 03:00', D'2021.09.05 03:00', times));
   ArrayPrintM(times); //                   FROM                 TO
   PRTF(CopyTime("EURUSD", PERIOD_H1, D'2021.09.05 03:00', D'2021.09.06 03:00', times));
   ArrayPrintM(times);
   // Change direction of indexing
   PRTF(ArraySetAsSeries(times, true));
   ArrayPrintM(times);
   // Despite the fact that 2 timestamps define 24-hour range,
   // the function will return 4 bars, not 25 (25 is expected because FROM/TO are both included)
   // This is because 5-th of September is Sunday,
   // so only early morning of September 6-th is counted
   
   // Also note that the array was shrinked to 4 elements from 10
   
   // Request 10 bars from 100-th bar
   PRTF(CopyTime("EURUSD", PERIOD_H1, 100, 10, times));
   // Remember the array is still in timeseries mode
   ArrayPrintM(times);
}
//+------------------------------------------------------------------+
/*
   example output
   
   CopyTime(EURUSD,PERIOD_H1,D'2021.09.05',10,times)=10 / ok
   [0] 2021.09.03 14:00 2021.09.03 15:00 2021.09.03 16:00 2021.09.03 17:00 2021.09.03 18:00
   [5] 2021.09.03 19:00 2021.09.03 20:00 2021.09.03 21:00 2021.09.03 22:00 2021.09.03 23:00
   ArraySetAsSeries(times,true)=true / ok
   [0] 2021.09.03 23:00 2021.09.03 22:00 2021.09.03 21:00 2021.09.03 20:00 2021.09.03 19:00
   [5] 2021.09.03 18:00 2021.09.03 17:00 2021.09.03 16:00 2021.09.03 15:00 2021.09.03 14:00
   ArraySetAsSeries(times,false)=true / ok
   CopyTime(EURUSD,PERIOD_H1,D'2021.09.06 03:00',D'2021.09.05 03:00',times)=4 / ok
   2021.09.06 00:00 2021.09.06 01:00 2021.09.06 02:00 2021.09.06 03:00
   CopyTime(EURUSD,PERIOD_H1,D'2021.09.05 03:00',D'2021.09.06 03:00',times)=4 / ok
   2021.09.06 00:00 2021.09.06 01:00 2021.09.06 02:00 2021.09.06 03:00
   ArraySetAsSeries(times,true)=true / ok
   2021.09.06 03:00 2021.09.06 02:00 2021.09.06 01:00 2021.09.06 00:00
   CopyTime(EURUSD,PERIOD_H1,100,10,times)=10 / ok
   [0] 2021.10.04 19:00 2021.10.04 18:00 2021.10.04 17:00 2021.10.04 16:00 2021.10.04 15:00
   [5] 2021.10.04 14:00 2021.10.04 13:00 2021.10.04 12:00 2021.10.04 11:00 2021.10.04 10:00
*/
//+------------------------------------------------------------------+
