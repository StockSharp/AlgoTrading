//+------------------------------------------------------------------+
//|                                                    Manual_EA.mq4 |
//|                      Copyright © 2007, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2007, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.ru/"
 
//---- input parameters
extern int       Kperiod=5;
extern int       Dperiod=3;
extern int       slowPeriod=3;
extern int       UpLevel=90;
extern int       DownLevel=10;
extern int       StopLoss=100;
extern int       TakeProfit=100;
extern double    Lots=0.1;
 
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
   int res=-1;
//----
   double prevValue = iStochastic(Symbol(),0,Kperiod,Dperiod,slowPeriod,MODE_SMA,0,MODE_SIGNAL,2);
   double currValue = iStochastic(Symbol(),0,Kperiod,Dperiod,slowPeriod,MODE_SMA,0,MODE_SIGNAL,1);
   int i,type;
   double SL,TP;
 
   if (currValue>DownLevel && prevValue<DownLevel) res=OP_BUY;
   if (currValue<UpLevel && prevValue>UpLevel) res=OP_SELL;
   if (res==OP_BUY) // откроем покупки, закроем продажи
      {
      if (OrdersTotal()>0)
         {
         for (i=OrdersTotal()-1; i>=0;i--)
            {
            if (OrderSelect(i,SELECT_BY_POS))
               {
               if (OrderType()==OP_SELL) OrderClose(OrderTicket(),OrderLots(),Ask,3);
               }
            }
         }
      if (StopLoss!=0)    SL=Bid-StopLoss*Point;   else SL=0;         
      if (TakeProfit!=0)  TP=Bid+TakeProfit*Point; else TP=0;         
      OrderSend(Symbol(),OP_BUY,Lots,Ask,3,SL,TP);
      }
   
 
   if (res==OP_SELL) // откроем продажи, закроем покупки
      {
      if (OrdersTotal()>0)
         {
         for (i=OrdersTotal()-1; i>=0;i--)
            {
            if (OrderSelect(i,SELECT_BY_POS))
               {
               if (OrderType()==OP_BUY) OrderClose(OrderTicket(),OrderLots(),Bid,3);
               }
            }
         }   
      if (StopLoss!=0)    SL=Ask+StopLoss*Point;   else SL=0;         
      if (TakeProfit!=0)  TP=Ask-TakeProfit*Point; else TP=0;         
      OrderSend(Symbol(),OP_SELL,Lots,Bid,3,SL,TP);
      }
 
//----
   return(0);
  }
//+------------------------------------------------------------------+