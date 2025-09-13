//+------------------------------------------------------------------+
//|                                                      IndStub.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double &data[])
{
   static int count = 0;
   ++count;
   // compare number of bars on previuos and current invocation
   if(prev_calculated != rates_total)
   {
      // display the difference if detected
      PrintFormat("calculated=%d rates=%d; %d ticks",
         prev_calculated, rates_total, count);
   }
   return rates_total; // report number of processed bars
}
//+------------------------------------------------------------------+
