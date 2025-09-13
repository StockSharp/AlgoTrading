//+------------------------------------------------------------------+
//|                                                    neroTrade.mq4 |
//|                                  Copyright © 2008 Gryb Alexander |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008 Gryb Alexander"
#property link      ""

extern int stop = 120;
extern double lots = 1;

int slippage = 3;

datetime curTime;
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
if((Time[0]!=curTime))
{   
   curTime=Time[0];
   
   if(OrdersTotal()==0)
   {
     if(Open[0]>Open[3])  OrderSend(Symbol(),OP_BUY,lots,Ask,slippage,Ask-stop*Point,0);    
     if(Open[0]<=Open[3])   OrderSend(Symbol(),OP_SELL,lots,Bid,slippage,Bid+stop*Point,0); 
   }
   else
   {
     OrderSelect(0,SELECT_BY_POS);
     if(OrderType()==OP_BUY)
       OrderClose(OrderTicket(),OrderLots(),Bid,3);
     if(OrderType()==OP_SELL)
       OrderClose(OrderTicket(),OrderLots(),Ask,3);
     if(Open[0]>Open[3])  OrderSend(Symbol(),OP_BUY,lots,Ask,slippage,Ask-stop*Point,0);    
     if(Open[0]<=Open[3])   OrderSend(Symbol(),OP_SELL,lots,Bid,slippage,Bid+stop*Point,0); 

   }
}

//----
   return(0);
  }
//+------------------------------------------------------------------+