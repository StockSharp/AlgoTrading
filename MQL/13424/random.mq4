//+------------------------------------------------------------------+
//|                                                       random.mq4 |
//|                        Copyright 2014, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2014, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"
#property version   "1.00"
#property strict
extern double lots=0.1;
extern int TakeProfit=10;
extern int StopLoss=10;
extern int magic=579656;
string c="Связаться с разработчиком можно по почте genino.belaev@yandex.ru";
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   MathSrand(GetTickCount());
   if(Digits==5 || Digits==3)
     {
      TakeProfit  *=10;
      StopLoss    *=10;
     }
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
   int ticet;
   int t=MathRand();
   Comment(c);
   if(CountVsego()==0)
     {
      if(t>16384)
        {
         ticet=OrderSend(Symbol(),OP_BUY,lots,Ask,3,Bid-StopLoss*Point,Ask+TakeProfit*Point,"покупка",magic,0,clrGreen);
        }
      if(t<16384)
        {
         ticet=OrderSend(Symbol(),OP_SELL,lots,Bid,3,Ask+StopLoss*Point,Bid-TakeProfit*Point,"продажа",magic,0,clrRed);
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CountSELL()
  {
   int count=0;
   int i;
   for(i=OrdersTotal()-1;i>=0;i--)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderSymbol()==Symbol() && OrderMagicNumber()==magic && OrderType()==OP_SELL)
            count++;
        }
     }
   return(count);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CountBUY()
  {
   int count=0;
   int i;
   for(i=OrdersTotal()-1;i>=0;i--)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderSymbol()==Symbol() && OrderMagicNumber()==magic && OrderType()==OP_BUY)
            count++;
        }
     }
   return(count);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CountVsego()
  {
   int Vsego;
   Vsego=CountBUY()+CountSELL();

   return(Vsego);
  }
//+------------------------------------------------------------------+
