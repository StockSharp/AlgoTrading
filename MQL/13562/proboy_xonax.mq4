//+------------------------------------------------------------------+
//|                                                 ProBoy XonaX.mq4 |
//|                                        Copyright 2015, Xonax.ru. |
//|                                             https://www.xonax.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015, Xonax.ru."
#property link      "https://www.xonax.ru"
#property version   "1.00"
#property strict
//--- input parameters
input int      StLoss=50;
//--- помимо столосса будем 1 раз использовать в качестве трейлингстопа, для перевода ордера в безубыток
input int      TProf=1000;
input int      TimeFr=240;
input double   Lot=0.1;
input int      Magik=100004;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
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
   double Dmax,Dmin;
   static datetime BarT=0; //введем эти переменные,
   datetime TempT; // чтобы вычисления проводились 1 раз за указанный таймфрейм а не в каждом тике
   int total=OrdersTotal();
   Dmax=iHigh(NULL,TimeFr,1);
   Dmin=iLow(NULL,TimeFr,1);
   TempT=iTime(NULL,TimeFr,1);
   if(Bid>Dmax && BarT!=TempT)
     {
      int ticket=OrderSend(NULL,OP_BUY,Lot,Ask,30,Ask-StLoss*_Point,Ask+TProf*_Point,NULL,Magik,0,clrBlue);
      if(ticket<0)
        {
         Print("OrderSend error #",GetLastError());
        }
      BarT=TempT;
     }
   if(Ask<Dmin && BarT!=TempT)
     {
      int ticket=OrderSend(NULL,OP_SELL,Lot,Bid,30,Bid+StLoss*_Point,Ask-TProf*_Point,NULL,Magik,0,clrRed);
      if(ticket<0)
        {
         Print("OrderSend error #",GetLastError());
        }
      BarT=TempT;
     }
//--- Trailing Stop operation
   for(int cni=0;cni<total;cni++)
     {
      if(!OrderSelect(cni,SELECT_BY_POS,MODE_TRADES))
         continue;
      if(OrderMagicNumber()==Magik)
        {
         if(OrderType()==OP_BUY)
           {
            if(Bid-OrderOpenPrice()>_Point*StLoss)
              {
               if(OrderStopLoss()<OrderOpenPrice())
                 {
                  //--- modify order and exit
                  if(!OrderModify(OrderTicket(),OrderOpenPrice(),Bid-_Point*StLoss,OrderTakeProfit(),0,clrBlue))
                     Print("OrderModify error ",GetLastError());
                  return;
                 }
              }
           }
         if(OrderType()==OP_SELL)
           {
            if((OrderOpenPrice()-Ask)>_Point*StLoss)
              {
               if(OrderStopLoss()>OrderOpenPrice())
                 {
                  //--- modify order and exit
                  if(!OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*StLoss,OrderTakeProfit(),0,clrRed))
                     Print("OrderModify error ",GetLastError());
                  return;
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
