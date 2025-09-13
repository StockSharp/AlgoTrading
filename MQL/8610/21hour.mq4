//+------------------------------------------------------------------+
//|                                                       21hour.mq4 |
//|                      Copyright © 2008, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"

extern double Lots = 0.1;
extern int ChasStart = 10;
extern int ChasStop  = 22;
extern int Step      = 15;
extern int TP        = 200;
int prevtime;
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


   


   int OrderCountOtl=0;
      int i=0; 
   int total = OrdersTotal();   
   for(i = 0; i <= total; i++) 
     {
            
       OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
       if(OrderMagicNumber() == 12321) 
         {
         if (OrderType()>1) OrderCountOtl++;
         }

      }
      
          if (OrderCountOtl==1) 
     {
   for(i = 0; i <= total; i++) 
     {
       OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
       if(OrderMagicNumber() == 12321) 
         {
         if (OrderType()>1) OrderDelete(OrderTicket());
         }
      }
     } 


   if(Time[0] == prevtime) 
       return(0);
   prevtime = Time[0];
   if(!IsTradeAllowed()) 
     {
       prevtime = Time[1];
       return(0);
     }



if (TimeHour(TimeCurrent())==ChasStart && TimeMinute(TimeCurrent())==0)
  {
  OrderSend(Symbol(),OP_BUYSTOP,Lots,Ask+Step*Point,3,0,Ask+(Step+TP)*Point,"",12321,0,Green);
  OrderSend(Symbol(),OP_SELLSTOP,Lots,Bid-Step*Point,3,0,Bid-(Step+TP)*Point,"",12321,0,Red);
  }

if (TimeHour(TimeCurrent())==ChasStop && TimeMinute(TimeCurrent())==0)
  {
   i=0;  
  total = OrdersTotal();   
   for(i = 0; i <= total; i++) 
     {
       OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
       if(OrderMagicNumber() == 12321) 
         {
          if (OrderType()==OP_BUY)OrderClose(OrderTicket(),OrderLots(),Bid,3,Green);
          if (OrderType()==OP_SELL)OrderClose(OrderTicket(),OrderLots(),Ask,3,Green);
          if (OrderType()>1)OrderDelete(OrderTicket());
         }
      }  
  }
   
//----
   return(0);
  }
//+------------------------------------------------------------------+