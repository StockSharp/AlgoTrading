//+------------------------------------------------------------------+
//|                                          MatrixStdPercentile.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/MatrixProcessor.mqh>

input int BarCount = 1000;
input int BarOffset = 0;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // get current chart quotes
   matrix rates;
   rates.CopyRates(_Symbol, _Period, COPY_RATES_OPEN | COPY_RATES_CLOSE, BarOffset, BarCount);
   
   // calculate price increments per bar
   vector delta = MathRound((rates.Row(1) - rates.Row(0)) / _Point);
   
   // debug printing of starting part of quotes
   rates.Resize(rates.Rows(), 10);
   Normalize(rates);
   Print(rates);
   
   // print metrics of the deltas
   PRTF((int)delta.Std());
   PRTF((int)delta.Percentile(90));
   PRTF((int)delta.Percentile(10));
}
//+------------------------------------------------------------------+
/*

(EURUSD,H1)	[[1.00832,1.00808,1.00901,1.00887,1.00728,1.00577,1.00485,1.00652,1.00538,1.00409]
(EURUSD,H1)	 [1.00808,1.00901,1.00887,1.00728,1.00577,1.00485,1.00655,1.00537,1.00412,1.00372]]
(EURUSD,H1)	(int)delta.Std()=163 / ok
(EURUSD,H1)	(int)delta.Percentile(90)=170 / ok
(EURUSD,H1)	(int)delta.Percentile(10)=-161 / ok

*/
//+------------------------------------------------------------------+
