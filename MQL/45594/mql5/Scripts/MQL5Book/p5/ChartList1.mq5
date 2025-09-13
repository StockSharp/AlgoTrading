//+------------------------------------------------------------------+
//|                                                   ChartList1.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

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
   // long id = ChartNext(0); - this is the same as ChartFirst()
   int count = 0;
   Print("Chart List\nN, ID, *active");
   // keep enumerating all charts until no more found
   while(id != -1)
   {
      const string header = StringFormat("%d %lld %s",
         count, id, (id == me ? "*" : ""));
    
      // columns: N, id, self-mark
      Print(header);
      count++;
      id = ChartNext(id);
   }
   Print("Total chart number: ", count);
}
//+------------------------------------------------------------------+
