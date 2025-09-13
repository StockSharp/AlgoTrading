//+------------------------------------------------------------------+
//|                                                  MACD Expert.mq4 |
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
input int    MagicN = 20182281; // EA ID#
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
   double MacdCurrent_m5=iMACD(NULL,PERIOD_M5,12,26,9,PRICE_CLOSE,MODE_MAIN,1);
   double SignalCurrent_m5=iMACD(NULL,PERIOD_M5,12,26,9,PRICE_CLOSE,MODE_SIGNAL,1);
            
   double MacdCurrent_m15=iMACD(NULL,PERIOD_M15,12,26,9,PRICE_CLOSE,MODE_MAIN,1);
   double SignalCurrent_m15=iMACD(NULL,PERIOD_M15,12,26,9,PRICE_CLOSE,MODE_SIGNAL,1);
            
   double MacdCurrent_h1=iMACD(NULL,PERIOD_H1,12,26,9,PRICE_CLOSE,MODE_MAIN,1);
   double SignalCurrent_h1=iMACD(NULL,PERIOD_H1,12,26,9,PRICE_CLOSE,MODE_SIGNAL,1);
           
   double MacdCurrent_h4=iMACD(NULL,PERIOD_H4,12,26,9,PRICE_CLOSE,MODE_MAIN,1);
   double SignalCurrent_h4=iMACD(NULL,PERIOD_H4,12,26,9,PRICE_CLOSE,MODE_SIGNAL,1);
   
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
    
    if(SignalCurrent_m5 > MacdCurrent_m5 && SignalCurrent_m15 > MacdCurrent_m15 && SignalCurrent_h1 > MacdCurrent_h1 && SignalCurrent_h4 > MacdCurrent_h4)
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
      if(SignalCurrent_m5 < MacdCurrent_m5 && SignalCurrent_m15 < MacdCurrent_m15 && SignalCurrent_h1 < MacdCurrent_h1 && SignalCurrent_h4 < MacdCurrent_h4)
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
