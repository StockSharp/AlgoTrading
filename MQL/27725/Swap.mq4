//+------------------------------------------------------------------+
//|                                                         Swap.mq4 |
//|                    Copyright 2020, FXFledgling Forex Study Group |
//|                     https://www.facebook.com/groups/FXFledgling/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2020, FXFledgling Forex Study Group"
#property link      "https://www.facebook.com/groups/FXFledgling/"
#property version   "1.00"
#property strict
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   double swaps = MarketInfo(Symbol(),MODE_SWAPSHORT);
   double swapb = MarketInfo(Symbol(),MODE_SWAPLONG);
   string s = SwapIs(swaps);
   string b = SwapIs(swapb);
   Comment("\n\nSwap Short: "+s+
           "\nSwap Long: "+b);
//---
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   Comment("");
  }
//+------------------------------------------------------------------+
//| Status                                                           |
//+------------------------------------------------------------------+
string SwapIs(double v)
  {
//---
   string s="Zero";
   if(v>0) s="Positive";
   else if(v<0) s="Negative";
   return(s);
  }
//+------------------------------------------------------------------+
