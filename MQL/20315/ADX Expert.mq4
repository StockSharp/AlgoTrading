//+------------------------------------------------------------------+
//|                                                  ADX Expert.mq4 |
//|                                                 Tzvetan Jordanov |
//|                            https://www.mql5.com/en/users/seemore |
//+------------------------------------------------------------------+
#property copyright "Tzvetan Jordanov"
#property link      "https://www.mql5.com/en/users/seemore"
#property version   "1.00"
#property strict

extern double Lots = 0.1; // Lot Size
input double SL = 200.0; // Stop Loss Points
input double TP = 400.0; // Take Profit Points
input double MaxSpread = 20; // Max Spread
input int    MagicN = 2018328; // EA ID#

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   
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
   double adxmain= iADX(NULL, 0, 14, PRICE_CLOSE, MODE_MAIN, 1);
   double adxplus= iADX(NULL, 0, 14, PRICE_CLOSE, MODE_PLUSDI, 1);
   double adxplus2= iADX(NULL, 0, 14, PRICE_CLOSE, MODE_PLUSDI, 2);
   double adxminus= iADX(NULL, 0, 14, PRICE_CLOSE, MODE_MINUSDI, 2);

   
   int cont = 0;
   for (int i = OrdersTotal() - 1; i >= 0; i--) 
    {
     if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) continue;
     if (OrderSymbol() == Symbol() && OrderMagicNumber() == MagicN) 
      {
       cont++;
      }
    }
   
   double sl=0,tp=0;
   double sp = (Ask - Bid)/Point;
   if (sp > MaxSpread)
    {
     Comment("HIGH SPREAD !");
     return;
    }
   
   if (cont == 0)
    {     
     RefreshRates(); 
    
    if(adxplus > adxminus && adxplus2 < adxminus && adxmain < 20)
     {
      if (SL > 0) sl = NormalizeDouble(Ask - SL * Point, Digits);
      if (TP > 0) tp = NormalizeDouble(Ask + TP * Point, Digits);
      if (OrderSend( Symbol(), OP_BUY, Lots, Ask, 1, sl, tp, "", MagicN) == -1)
       {
        Print("Error order send "+(string)GetLastError());
       }
     }
    else
     {
      if(adxplus < adxminus && adxplus2 > adxminus && adxmain < 20)
       {
        if (SL > 0) sl = NormalizeDouble(Bid + SL * Point, Digits);
        if (TP > 0) tp = NormalizeDouble(Bid - TP * Point, Digits);
        if (OrderSend( Symbol(), OP_SELL, Lots, Bid, 1, sl, tp, "", MagicN) == -1)
         {
          Print("Error order send "+(string)GetLastError());
         }
       }
     }
   }
  }
//+------------------------------------------------------------------+
