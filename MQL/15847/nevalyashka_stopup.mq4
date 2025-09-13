//+------------------------------------------------------------------+
//|                                       Nevalyashka_Martingail.mq4 |
//|                               Copyright © 2010, Хлыстов Владимир |
//|                                                cmillion@narod.ru |
//|                                                                  |
//--------------------------------------------------------------------
#property copyright "Copyright © 2016, Хлыстов Владимир"
#property link      "cmillion@narod.ru"
#property version   "1.00"
#property strict
#property description "Советник открывает после убытка ордера с увеличенными на коэффициент стопами"
//--------------------------------------------------------------------
extern int    stoploss     = 150,
              takeprofit   = 50;
extern double Lot          = 0.1;
extern double KoeffMartin  = 1.5;//коэффициент увеличения стопов
extern int    Magic        = 0;
extern bool   StopAfteProfit = false;//остановка после профита
double sl=0,tp=0;
//--------------------------------------------------------------------
int OnInit()
  {
   if(Digits==5 || Digits==3)
     {
      stoploss*=10;
      takeprofit*=10;
     }
   sl=stoploss*Point;
   tp=takeprofit*Point;
   return(INIT_SUCCEEDED);
  }
//--------------------------------------------------------------------
void OnTick()
  {
   int tip=0,i;
   for(i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderSymbol()==Symbol() && OrderMagicNumber()==Magic)
           {
            return;
           }
        }
     }
   for(i=OrdersHistoryTotal()-1; i>=0; i--)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY))
        {
         if(OrderSymbol()==Symbol() && OrderMagicNumber()==Magic)
           {
            if(OrderProfit()<0)
              {
               sl = MathAbs(OrderStopLoss()-OrderOpenPrice())*KoeffMartin;
               tp = MathAbs(OrderTakeProfit()-OrderOpenPrice())*KoeffMartin;
              }
            else
              {
               if(StopAfteProfit) ExpertRemove();
               sl=stoploss*Point;
               tp=takeprofit*Point;
              }
            tip=OrderType();
            break;
           }
        }
     }
   if(tip==OP_BUY)
      if(OrderSend(Symbol(),OP_SELL,Lot,Bid,3,NormalizeDouble(Ask+sl,Digits),NormalizeDouble(Bid-tp,Digits)," ",Magic,Blue)==-1)
         Print("Error ",GetLastError()," Bid=",DoubleToStr(Bid,Digits)," sl=",DoubleToStr(Ask+sl,Digits)," tp=",DoubleToStr(Bid-tp,Digits));
   if(tip==OP_SELL)
      if(OrderSend(Symbol(),OP_BUY,Lot,Ask,3,NormalizeDouble(Bid-sl,Digits),NormalizeDouble(Ask+tp,Digits)," ",Magic,Blue)==-1)
         Print("Error ",GetLastError()," Ask=",DoubleToStr(Ask,Digits)," sl=",DoubleToStr(Bid-sl,Digits)," tp=",DoubleToStr(Ask+tp,Digits));
   return;
  }
//-----------------------------------------------------------------
