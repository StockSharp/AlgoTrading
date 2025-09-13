//+------------------------------------------------------------------+
//|                                                    TypeColor.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

#define PRT(A) Print(#A, "=", (A))

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   color y = clrYellow;         // clrYellow
   color m = C'255,0,255';      // clrFuchsia
   color x = C'0x88,0x55,0x01'; // x = 136,85,1 (no such predefined color)
   color n = 0x808080;          // clrGray

   PRT(y);
   PRT(m);
   PRT(x);
   PRT(n);
}

//+------------------------------------------------------------------+
