//+------------------------------------------------------------------+
//|                                               SwapMajorPairs.mq4 |
//|                    Copyright 2020, FXFledgling Forex Study Group |
//|                     https://www.facebook.com/groups/FXFledgling/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2020, FXFledgling Forex Study Group"
#property link      "https://www.facebook.com/groups/FXFledgling/"
#property version   "1.00"
#property strict

//=====================================================================//
//    Variables
//=====================================================================//
int    PairCount =7;
string Pairs[7]={"EURUSD","GBPUSD","AUDUSD","NZDUSD","USDCAD","USDCHF","USDJPY"};
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   string c = "\n\nSWAP Info - Major Pairs\n";
   for(int x=0;x<PairCount; x++)
     {
      string s = SwapIs(MarketInfo(Pairs[x],MODE_SWAPSHORT));
      string b = SwapIs(MarketInfo(Pairs[x],MODE_SWAPLONG));
      c+="\n"+Pairs[x]+":  Short "+s+",  Long "+b;
     }
   Comment(c);
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
