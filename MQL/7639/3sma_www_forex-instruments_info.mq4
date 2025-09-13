//+------------------------------------------------------------------+
//|                                                         3sma.mq4 |
//|                                                         emsjoflo |
//|                                  automaticforex.invisionzone.com |
//+------------------------------------------------------------------+
#property copyright "emsjoflo"
#property link      "automaticforex.invisionzone.com"
//---- input parameters
extern int       SMA1=9;
extern int       SMA2=14;
extern int       SMA3=29;
extern double    lots=0.1;
extern int       SMAspread=0;
extern int       StopLoss=0;
extern int       Slippage=4;
double   ma1,ma2,ma3;
int      i, buys, sells;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
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
   //get moving average info
   ma1=iMA(Symbol(),0,SMA1,1,MODE_SMA,PRICE_CLOSE,0);
   ma2=iMA(NULL,0,SMA2,1,MODE_SMA,PRICE_CLOSE,0);
   ma3=iMA(NULL,0,SMA3,1,MODE_SMA,PRICE_CLOSE,0);
   //check for open orders first 
   if (OrdersTotal()>0)
     {
      buys=0;
      sells=0;
      for(i=0;i<OrdersTotal();i++)
        {
         OrderSelect(i,SELECT_BY_POS);
         if (OrderSymbol()==Symbol())
           {
            if (OrderType()== OP_BUY)
              {
               if (ma1 < ma2) OrderClose(OrderTicket(),OrderLots(),Bid,Slippage,Orange);
               else buys++;
              }
            if (OrderType()== OP_SELL)
              {
               if (ma1 > ma2) OrderClose(OrderTicket(),OrderLots(),Ask,Slippage,Yellow);
               else sells++;
              }
           }
        }
     }
   if (ma1>(ma2+SMAspread*Point) && ma2 > (ma3+SMAspread*Point) && buys==0)
     {
      Print("Buy condition");
   OrderSend(Symbol(),OP_BUY,lots,Ask,Slippage,0/*(Ask-StopLoss*Point)*/,0,"3SMA",123,0,Green);
     }
   if (ma1<(ma2-SMAspread*Point) && ma2 < (ma3-SMAspread*Point) && sells ==0)
     {
      Print ("Sell condition");
   OrderSend(Symbol(),OP_SELL,lots,Bid,Slippage,0/*(Bid+StopLoss*Point)*/,0,"3SMA",123,0,Red);
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+