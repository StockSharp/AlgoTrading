//+------------------------------------------------------------------+
//|                                                   5_8MACROSS.mq4 |
//|                      Copyright © 2006, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"
//----
extern double Lots=0.1;
extern int StopLoss=0;
extern int TrailingStop=0;
extern int TakeProfit=40;
extern int mafastperiod=5;
extern int mafastshift=-1;
extern int mafastmethod =MODE_EMA;
extern int mafastprice=PRICE_CLOSE;
extern int maslowperiod=8;
extern int maslowshift=0;
extern int maslowmethod =MODE_EMA;
extern int maslowprice=PRICE_OPEN;
//----
datetime TimePrev=0;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int init()
  {
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int deinit()
  {
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   //Trailing Stop
   TrailingAlls(TrailingStop);
   //close/Open
   if (TimePrev==Time[0]) return(0);
   TimePrev=Time[0];
   //Caculate indicators
   double fast1=iMA(NULL,0,mafastperiod,mafastshift,mafastmethod,mafastprice,1);
   double fast2=iMA(NULL,0,mafastperiod,mafastshift,mafastmethod,mafastprice,2);
   double slow1=iMA(NULL,0,maslowperiod,maslowshift,maslowmethod,maslowprice,1);
   double slow2=iMA(NULL,0,maslowperiod,maslowshift,maslowmethod,maslowprice,2);
   //LONG
   if (fast1>slow1&&fast2<slow2)
     {
      closeshrts();
      OrderSend(Symbol(),OP_BUY,Lots,Ask,0,Stoplong(Ask,StopLoss),Takelong(Ask,TakeProfit),NULL,0,0,Blue);
     }//long
   //shrt
   if(fast1<slow1&&fast2>slow2)
     {
      closelongs();
      OrderSend(Symbol(),OP_SELL,Lots,Bid,0,Stopshrt(Bid,StopLoss),Takeshrt(Bid,TakeProfit),NULL,0,0,Red);
     }//shrt
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double Stoplong(double price,int stop)
  {
   if(stop==0)
      return(0.0);
   return(price-(stop*Point));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double Stopshrt(double price,int stop)
  {
   if (stop==0)
      return(0.0);
   return(price+(stop*Point));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double Takelong(double price,int Take)
  {
   if (Take==0)
      return(0.0);
   return(price+(Take*Point));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double Takeshrt(double price,int Take)
  {
   if (Take==0)
      return(0.0);
   return(price-(Take*Point));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void closelongs()
  {
   int trade;
   int trades=OrdersTotal();
   for(trade=0;trade<trades;trade++)
     {
      OrderSelect(trade,SELECT_BY_POS,MODE_TRADES);
      if(OrderSymbol()!=Symbol())
         continue;
      if(OrderType()==OP_BUY)
         OrderClose(OrderTicket(),OrderLots(),Bid,0,Blue);
     }//for
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void closeshrts()
  {
   int trade;
   int trades=OrdersTotal();
   for(trade=0;trade<trades;trade++)
     {
      OrderSelect(trade,SELECT_BY_POS,MODE_TRADES);
      if(OrderSymbol()!=Symbol())
         continue;
      if(OrderType()==OP_SELL)
         OrderClose(OrderTicket(),OrderLots(),Ask,0,Red);
     }//for
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TrailingAlls(int trail)
  {
   if(trail==0)
      return;
//----
   double stopcrnt;
   double stopcal;
   int trade;
   int trades=OrdersTotal();
   for(trade=0;trade<trades;trade++)
     {
      OrderSelect(trade,SELECT_BY_POS,MODE_TRADES);
      if(OrderSymbol()!=Symbol())
         //continue;
         //LONG
         if(OrderType()==OP_BUY)
           {
            stopcrnt=OrderStopLoss();
            stopcal=Bid-(trail*Point);
            if (stopcrnt==0)
              {
               OrderModify(OrderTicket(),OrderOpenPrice(),stopcal,OrderTakeProfit(),0,Blue);
              }
            else
               if(stopcal>stopcrnt)
                 {
                  OrderModify(OrderTicket(),OrderOpenPrice(),stopcal,OrderTakeProfit(),0,Blue);
                 }
           }
     }//LONG
   //Shrt
   if(OrderType()==OP_SELL)
     {
      stopcrnt=OrderStopLoss();
      stopcal=Ask+(trail*Point);
      if (stopcrnt==0)
        {
         OrderModify(OrderTicket(),OrderOpenPrice(),stopcal,OrderTakeProfit(),0,Red);
        }
      else
         if(stopcal<stopcrnt)
           {
            OrderModify(OrderTicket(),OrderOpenPrice(),stopcal,OrderTakeProfit(),0,Red);
           }
     }
  }//Shrt
//----
return(0);
//+------------------------------------------------------------------+