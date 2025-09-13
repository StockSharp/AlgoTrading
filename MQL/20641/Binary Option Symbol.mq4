//+------------------------------------------------------------------+
//|                                         Binary Option Symbol.mq4 |
//|                        Copyright 2015, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
  /* for(int i=0;i<SymbolsTotal(false);i++)
   {
    if(StringSubstr(SymbolName(i,false),6)=="bo")
    Print(" ",SymbolName(i,false));
    
    
   }*/

   for(int i=0;i<SymbolsTotal(false);i++)
   {
    double profitcalc = MarketInfo(SymbolName(i,false),MODE_PROFITCALCMODE);
    double stoplevel = MarketInfo(SymbolName(i,false),MODE_STOPLEVEL);
    if(profitcalc==2 && stoplevel==0)
    Print(" ",SymbolName(i,false));
   }
    
    
    
 /*Print("EURUSDbo ",MarketInfo("EURUSD",MODE_EXPIRATION));
   Print("NAS100 ",MarketInfo("NAS100",MODE_EXPIRATION));
   Print("XAUUSD ",MarketInfo("XAUUSD",MODE_EXPIRATION));
   Print("EURGBP ",MarketInfo("EURGBP",MODE_EXPIRATION));*/
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   
  }
//+------------------------------------------------------------------+
