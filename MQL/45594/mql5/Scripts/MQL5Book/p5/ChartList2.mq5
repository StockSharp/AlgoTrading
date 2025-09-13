//+------------------------------------------------------------------+
//|                                                   ChartList2.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#include <MQL5Book/Periods.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   ChartList();
}

//+------------------------------------------------------------------+
//| Main worker function to enumerate charts                         |
//+------------------------------------------------------------------+
void ChartList()
{
   const long me = ChartID();
   long id = ChartFirst();
   int count = 0;
   
   Print("Chart List\nN, ID, Symbol, TF, *active");
   // keep enumerating all charts until no more found
   while(id != -1)
   {
      const string header = StringFormat("%d %lld %s %s %s",
         count, id, ChartSymbol(id), PeriodToString(ChartPeriod(id)),
         (id == me ? "*" : ""));
    
      // columns: N, id, symbol, period, self-mark
      Print(header);
      count++;
      id = ChartNext(id);
   }
   Print("Total chart number: ", count);
}
//+------------------------------------------------------------------+
