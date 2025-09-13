#property copyright "cloud666@rbcmail.ru"
extern int Kperiod=            8;
extern int Dperiod=            8;
extern int Slowing=             4;
extern int Method=              3 ;
extern int PriceUsing=          1;
extern double MaxLot=         0.0;
extern double TakeProfit=   0.000;
extern double TrailingStop= 0.01;
extern double StopLoss=     0.05;
extern double MinProfit=   0.0000;
extern double ProfitPoints= 0.000;
extern int Condition1=          1;
extern int Condition2=          1;
extern double LotSpliter=     0.1;
extern int CloseByOtherSideCondition=1;
double Lot;
double PP=0;
double slu,sld,a,b;
double tp,sl;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   Alert("V2");
   tp=TakeProfit;
   sl=StopLoss;
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
//   if(AccountServer()!="SIG-Demo.com"){return(0);}
   OrderSelect(OrdersHistoryTotal()-1,SELECT_BY_POS,MODE_HISTORY);
   if(DayOfYear() == TimeDayOfYear(OrderCloseTime()))
   {
      return(0);
   }
   if(OrdersTotal()==0)
   {   
      PP=0;
      preinit();
      if(U()==1)
      {
         OrderBuy();
         return(0);
      }
      if(U()==2)
      {
         OrderSell();
         return(0);
      }
      return(0);
   }
   if(OrdersTotal()==1)
   {
      SelectOnlyOrder();
      slu=Bid-OrderOpenPrice();
      b=Bid;
      sld=OrderOpenPrice()-Ask;
      a=Ask;
      if(OrderType()==0)
      {
         if((slu)>PP)
         {
            PP=slu;
         }
         if(((slu)>0.001) && (OrderStopLoss()<(b-TrailingStop)) && (OrderOpenPrice()<(b-TrailingStop)) && (OrderProfit()>MathAbs(OrderSwap())))
         {
            if(TrailingStop!=0)
            {
               OrderModify(OrderTicket(), 0, b-TrailingStop, 0, 0, 0);
            }
         }
      }
      if(OrderType()==1)
      {
         if((sld)>PP)
         {
            PP=sld;
         }
         if(((sld)>0.001) && (OrderStopLoss()>(a+TrailingStop)) && (OrderOpenPrice()>(a+TrailingStop)))
         {
            if(TrailingStop!=0)
            {
               OrderModify(OrderTicket(), 0, a+TrailingStop, 0, 0, 0);
            }
         }
      }
      if(ProfitPoints!=0)
      {
         if(OrderType()==0 && PP>=ProfitPoints && (slu)<=MinProfit)
         {
            CloseOnlyOrder();
            return(0);
         }
         if(OrderType()==1 && PP>=ProfitPoints && (sld)<=MinProfit)
         {
            CloseOnlyOrder();
            return(0);
         }
      }
      if(CloseByOtherSideCondition==1)
      {
         if(OrderType()==0 && U()==2)
         {
            CloseOnlyOrder();
            return(0);
         }
         if(OrderType()==1 && U()==1)
         {
            CloseOnlyOrder();
            return(0);
         }
      }
   }
//----
   return(0);
  }
//+------------------------------------------------------------------+
int U()
{
   if((U1()==2 && Condition1==1) || (U2()==2 && Condition2==1)){return(2);}
   if((U1()==1 && Condition1==1) || (U2()==1 && Condition2==1)){return(1);}
   return(0);
}
int U1()
{
   if(iStochastic(Symbol(),Period(),Kperiod,Dperiod,Slowing,Method,PriceUsing,MODE_SIGNAL,1)>=80)
   {
      if(iStochastic(Symbol(),Period(),Kperiod,Dperiod,Slowing,Method,PriceUsing,MODE_SIGNAL,2)<=iStochastic(Symbol(),Period(),Kperiod,Dperiod,Slowing,Method,PriceUsing,MODE_MAIN,2))
      {
         if(iStochastic(Symbol(),Period(),Kperiod,Dperiod,Slowing,Method,PriceUsing,MODE_SIGNAL,1)>=iStochastic(Symbol(),Period(),Kperiod,Dperiod,Slowing,Method,PriceUsing,MODE_MAIN,1))
         {
            return(2);
         }
      }
   }
   if(iStochastic(Symbol(),Period(),Kperiod,Dperiod,Slowing,Method,PriceUsing,MODE_SIGNAL,1)<=20)
   {
      if(iStochastic(Symbol(),Period(),Kperiod,Dperiod,Slowing,Method,PriceUsing,MODE_SIGNAL,2)>=iStochastic(Symbol(),Period(),Kperiod,Dperiod,Slowing,Method,PriceUsing,MODE_MAIN,2))
      {
         if(iStochastic(Symbol(),Period(),Kperiod,Dperiod,Slowing,Method,PriceUsing,MODE_SIGNAL,1)<=iStochastic(Symbol(),Period(),Kperiod,Dperiod,Slowing,Method,PriceUsing,MODE_MAIN,1))
         {
            return(1);
         }
      }
   }
   return(0);
}
int U2()
{
   double fu=0,fd=0;
   int f=0,shift=2;
   while(f<2)
   {
      if(iFractals(Symbol(),Period(),MODE_UPPER,shift)>0)
      {
         fu=fu+1;
         f=f+1;
      }
      if(iFractals(Symbol(),Period(),MODE_LOWER,shift)>0)
      {
         fd=fd+1;
         f=f+1;
      }
      shift=shift+1;
   }
   if(fu==2){return(2);}
   if(fd==2){return(1);}
   return(0);
}
int preinit()
{
   Lot=NormalizeDouble(MathFloor(LotSpliter*AccountBalance()*AccountLeverage()/Ask/MathPow(10,Digits+1)*10)/10,1);
   if(MaxLot>0 && Lot>MaxLot){Lot=MaxLot;}
   if(Lot>MarketInfo(Symbol(),25)){Lot=MarketInfo(Symbol(),25);}
   return(0);
}
int OrderBuy()
{
   if(StopLoss!=0 && TakeProfit!=0)
   {
      OrderSend(Symbol(), 0, NormalizeDouble(Lot,1), Ask, 0, NormalizeDouble(Ask-StopLoss,4), NormalizeDouble(Ask+TakeProfit,4), 0, 0, 0, 0);
      return(0);
   }
   if(StopLoss==0 && TakeProfit!=0)
   {
      OrderSend(Symbol(), 0, NormalizeDouble(Lot,1), Ask, 0, 0, NormalizeDouble(Ask+TakeProfit,4), 0, 0, 0, 0);
      return(0);
   }
   if(StopLoss==0 && TakeProfit==0)
   {
      OrderSend(Symbol(), 0, NormalizeDouble(Lot,1), Ask, 0, 0, 0, 0, 0, 0, 0);
      return(0);
   }
   if(StopLoss!=0 && TakeProfit==0)
   {
      OrderSend(Symbol(), 0, NormalizeDouble(Lot,1), Ask, 0, NormalizeDouble(Ask-StopLoss,4), 0, 0, 0, 0, 0);
      return(0);
   }
   return(0);
}
int OrderSell()
{
   if(StopLoss!=0 && TakeProfit!=0)
   {
      OrderSend(Symbol(), 1, NormalizeDouble(Lot,1), Bid, 0, NormalizeDouble(Bid+StopLoss,4), NormalizeDouble(Bid-TakeProfit,4), 0, 0, 0, 0);
      return(0);
   }
   if(StopLoss==0 && TakeProfit!=0)
   {
      OrderSend(Symbol(), 1, NormalizeDouble(Lot,1), Bid, 0, 0, NormalizeDouble(Bid-TakeProfit,4), 0, 0, 0, 0);
      return(0);
   }
   if(StopLoss==0 && TakeProfit==0)
   {
      OrderSend(Symbol(), 1, NormalizeDouble(Lot,1), Bid, 0, 0, 0, 0, 0, 0, 0);
      return(0);
   }
   if(StopLoss!=0 && TakeProfit==0)
   {
      OrderSend(Symbol(), 1, NormalizeDouble(Lot,1), Bid, 0, NormalizeDouble(Bid+StopLoss,4), 0, 0, 0, 0, 0);
      return(0);
   }
   return(0);
}
int CloseOnlyOrder()
{
   SelectOnlyOrder();
   RefreshRates();
   if(OrderType()==0)
   {
      OrderClose(OrderTicket(), OrderLots(), Bid, 0, 0);
   }
   else if(OrderType()==0)
   {
      OrderClose(OrderTicket(), OrderLots(), Ask, 0, 0);
   }
   return(0);
}
int SelectOnlyOrder()
{
   OrderSelect(0,SELECT_BY_POS,MODE_TRADES);
   return(0);
}