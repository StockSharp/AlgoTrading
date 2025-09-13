//+------------------------------------------------------------------+
//|                                           scOnTickMarketWatch.mq5|
//|                                            Copyright 2010, Lizar |
//|                                               lizar-2010@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright 2010, Lizar"
#property link      "lizar-2010@mail.ru"
#property version   "1.00"

int  delay=500;         // Delay time in milliseconds
//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
  {
   datetime prev_time=0; // Previous tick time
//---
   while(!_StopFlag)
     {
     //--- get time of the last tick
      datetime current_tick=TimeCurrent(); 
      if(prev_time<current_tick)
        { 
        //--- if it's a new tick, we search its symbol 
        //--- proceed all MarketWatch symbols
         for(ushort pos=0;pos<SymbolsTotal(true);pos++) 
           {
            string symbol=SymbolName(pos,true); // get symbol name
            
            //--- form and send custom event "New tick on the symbol...":
            if(SymbolInfoInteger(symbol,SYMBOL_TIME)>=current_tick)
               EventChartCustom(ChartID(),pos,SymbolInfoInteger(symbol,SYMBOL_SPREAD),SymbolInfoDouble(symbol,SYMBOL_BID),symbol);
           }
         prev_time=current_tick; // Save last tick time
        }      
      Sleep(delay);
     }
  }
//+------------------------------------------------------------------+
