//+------------------------------------------------------------------+
//|                                                   CCI-Expert.mq4 |
//|                        Copyright 2020, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2020, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict

extern double MVol = 0.10;//Fixed Volume Size
extern double VolR = 0.0;//Volume based on risk
input double Tp = 150.0; //TakeProfit in Points
input double Sl = 600.0; //StopLoss in Points
input double Mspr = 30; //Max Spread in points
input int    MaNR = 247; //Expert ID

//-- Include modules --
#include <stderror.mqh>
#include <stdlib.mqh>
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   if (MVol > 0) {VolR = 0.0;} else { MVol = 0.0;}
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

   double cciv = iCCI(NULL, PERIOD_CURRENT, 14, PRICE_CLOSE, 0);
   double cciso = iCCI(NULL, PERIOD_CURRENT, 14, PRICE_CLOSE, 1);
   double ccist = iCCI(NULL, PERIOD_CURRENT, 14, PRICE_CLOSE, 2);

   int count = 0;
   for (int i = OrdersTotal() - 1; i >= 0; i--) 
    {
     if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) continue;
     if (OrderSymbol() == Symbol() && OrderMagicNumber() == MaNR) 
      {
       count++;
      }
    }
    
   double sl=0,tp=0;
   double sp = (Ask - Bid)/Point;
   if (sp > Mspr)
    {
     Comment("SPREAD IS HIGH!");
     return;
    }
    
    double Vol = NormalizeDouble(volf(),2);

    //Open
    if (count == 0)
    {     
     RefreshRates(); 
    
    if (cciv > 1 && cciso > 1 && ccist < 1)
     {
      if (Sl > 0) sl = NormalizeDouble(Ask - Sl * Point, Digits);
      if (Tp > 0) tp = NormalizeDouble(Ask + Tp * Point, Digits);
      if (OrderSend( Symbol(), OP_BUY, Vol, Ask, 30, sl, tp, "", MaNR) == -1)
       {
        Print("Error order send "+(string)GetLastError());
       }
     }
    else
     {
      if (cciv < 1 && cciso < 1 && ccist > 1)
       {
        if (Sl > 0) sl = NormalizeDouble(Bid + Sl * Point, Digits);
        if (Tp > 0) tp = NormalizeDouble(Bid - Tp * Point, Digits);
        if (OrderSend( Symbol(), OP_SELL, Vol, Bid, 30, sl, tp, "", MaNR) == -1)
         {
          Print("Error order send "+(string)GetLastError());
         }
       }
     }
   }
   
         //Close
         for(int i = OrdersTotal() - 1; i >= 0; i--){
            if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES))  {
               if ( (OrderMagicNumber() == MaNR ) && (OrderSymbol() == Symbol())) {
                  if(OrderType() == OP_BUY){
                  bool check;
                  if (cciv < 1 && cciso < 1 && ccist > 1 && OrderProfit() > 0 )
                  {
                  check = OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), 40, clrNONE);
                  }
                  continue;
                  }
                  
                  if(OrderType() == OP_SELL){
                  bool check;
                  if (cciv > 1 && cciso > 1 && ccist < 1 && OrderProfit() > 0 )
                  {
                  check = OrderClose(OrderTicket(),OrderLots(), OrderClosePrice(), 40, clrNONE);
                  } 
                  continue;
                  }
               }
            }else{ 
               Print(",OrderSelect(i, SELECT_BY_POS, MODE_TRADES  found no open orders " + IntegerToString(GetLastError()));
            }
            }
  }
//+------------------------------------------------------------------+
double volf() 
{
  double ls;
  double lst=MarketInfo(Symbol(),MODE_LOTSTEP);
  double mil=MarketInfo(Symbol(),MODE_MINLOT);
  double mal=MarketInfo(Symbol(),MODE_MAXLOT);
  double tv=MarketInfo(Symbol(),MODE_TICKVALUE);
  double ts=MarketInfo(Symbol(),MODE_TICKSIZE);
  //---
  if(MVol>0) ls=MVol;//Fixed lot
  else
  ls=(AccountFreeMargin()*(VolR/100))/(Sl*(tv/(ts/Point)));
  //---
  return(MathMin(MathMax(mil,MathRound(ls/lst)*lst),mal));
}




