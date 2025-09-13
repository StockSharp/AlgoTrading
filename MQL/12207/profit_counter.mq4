//+------------------------------------------------------------------+
//|                                                        count.mq4 |
//|                   Copyright 2014, Abilash kumar balasubramanian. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "http://www.facebook.com/hsalibaa"
#property link      "http://www.facebook.com/hsalibaa"
#property version   "1.00"
#property strict

extern int TakeProfit = 200;
extern int StopLoss   = 400;

extern double lot1   =  0.01;
extern double lot2   =  0.03;
extern double lot3   =  0.09;
extern double lot4   =  0.27;
extern double lot5   =  0.81;
extern double lot6   =  2.43;
extern double lot7   =  7.29;
extern double lot8   =  21.87;
extern double lot9   =  65.61;
extern double lot10  =  196.83;
extern double lot11  =  590.49;

int MagicN=0000;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---

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
   int total=OrdersTotal();
   int i;
   double OpenLongOrders=0,OpenShortOrders=0,PendLongs=0,PendShorts=0;
//---
   if(total==0 && OpenLongOrders==0 && OpenShortOrders==0 && PendLongs==0 && PendShorts==0)
     {
      openbuy();
      sellstop();
     }
//---
   for(i=0;i<total;i++)
     {
      OrderSelect(i,SELECT_BY_POS);
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicN)
        {
         int type=OrderType();

         if(type == OP_BUY )       {OpenLongOrders=OpenLongOrders+1;}
         if(type == OP_SELL )      {OpenShortOrders=OpenShortOrders+1;}
         if(type == OP_BUYSTOP )   {PendLongs=PendLongs+1;}
         if(type == OP_SELLSTOP )  {PendShorts=PendShorts+1;}

         if(total==2 && OpenLongOrders==1 && OpenShortOrders==1 && PendLongs==0 && PendShorts==0)
           {
            buystop();
           }
         if(total==3 && OpenLongOrders==2 && OpenShortOrders==1 && PendLongs==0 && PendShorts==0)
           {
            sellstop1();
           }
         if(total==4 && OpenLongOrders==2 && OpenShortOrders==2 && PendLongs==0 && PendShorts==0)
           {
            buystop1();
           }

         if(total==5 && OpenLongOrders==3 && OpenShortOrders==2 && PendLongs==0 && PendShorts==0)
           {
            sellstop2();
           }
         if(total==6 && OpenLongOrders==3 && OpenShortOrders==3 && PendLongs==0 && PendShorts==0)
           {
            buystop2();
           }
         if(total==7 && OpenLongOrders==4 && OpenShortOrders==3 && PendLongs==0 && PendShorts==0)
           {
            sellstop3();
           }
         if(total==8 && OpenLongOrders==4 && OpenShortOrders==4 && PendLongs==0 && PendShorts==0)
           {
            buystop3();
           }
         if(total==9 && OpenLongOrders==5 && OpenShortOrders==4 && PendLongs==0 && PendShorts==0)
           {
            sellstop4();
           }
         if(total==10 && OpenLongOrders==5 && OpenShortOrders==5 && PendLongs==0 && PendShorts==0)
           {
            buystop4();
           }
         if(total==1 && OpenLongOrders==0 && OpenShortOrders==0 && (PendLongs==1 || PendShorts==1))
           {
            deleteallpendingorders();
           }

         Sleep(20);
        }
     }
  }
//+------------------------------------------------------------------+
//| openbuy                                                          |
//+------------------------------------------------------------------+
void openbuy()
  {
   OrderSend(Symbol(),OP_BUY,lot1,Ask,3,Ask-StopLoss*Point,Ask+TakeProfit*Point,NULL,MagicN,0,clrBlue);
  }
//+------------------------------------------------------------------+
//| sellstop                                                         |
//+------------------------------------------------------------------+
void sellstop()
  {
   OrderSend(Symbol(),OP_SELLSTOP,lot2,Ask-TakeProfit*Point,3,(Ask-TakeProfit*Point)+StopLoss*Point,(Ask-TakeProfit*Point)-TakeProfit*Point,NULL,MagicN,0,clrBlue);
  }
//+------------------------------------------------------------------+
//| buystop                                                          |
//+------------------------------------------------------------------+
void buystop()
  {
   OrderSend(Symbol(),OP_BUYSTOP,lot3,Ask+TakeProfit*Point,3,(Ask+TakeProfit*Point)-StopLoss*Point,(Ask+TakeProfit*Point)+TakeProfit*Point,NULL,MagicN,0,clrBlue);
  }
//+------------------------------------------------------------------+
//| sellstop1                                                        |
//+------------------------------------------------------------------+
void sellstop1()
  {
   OrderSend(Symbol(),OP_SELLSTOP,lot4,Ask-TakeProfit*Point,3,(Ask-TakeProfit*Point)+StopLoss*Point,(Ask-TakeProfit*Point)-TakeProfit*Point,NULL,MagicN,0,clrBlue);
  }
//+------------------------------------------------------------------+
//| buystop1                                                         |
//+------------------------------------------------------------------+
void buystop1()
  {
   OrderSend(Symbol(),OP_BUYSTOP,lot5,Ask+TakeProfit*Point,3,(Ask+TakeProfit*Point)-StopLoss*Point,(Ask+TakeProfit*Point)+TakeProfit*Point,NULL,MagicN,0,clrBlue);
  }
//+------------------------------------------------------------------+
//| sellstop2                                                        |
//+------------------------------------------------------------------+
void sellstop2()
  {
   OrderSend(Symbol(),OP_SELLSTOP,lot6,Ask-TakeProfit*Point,3,(Ask-TakeProfit*Point)+StopLoss*Point,(Ask-TakeProfit*Point)-TakeProfit*Point,NULL,MagicN,0,clrBlue);
  }
//+------------------------------------------------------------------+
//| buystop2                                                         |
//+------------------------------------------------------------------+
void buystop2()
  {
   OrderSend(Symbol(),OP_BUYSTOP,lot7,Ask+TakeProfit*Point,3,(Ask+TakeProfit*Point)-StopLoss*Point,(Ask+TakeProfit*Point)+TakeProfit*Point,NULL,MagicN,0,clrBlue);
  }
//+------------------------------------------------------------------+
//| sellstop3                                                        |
//+------------------------------------------------------------------+
void sellstop3()
  {
   OrderSend(Symbol(),OP_SELLSTOP,lot8,Ask-TakeProfit*Point,3,(Ask-TakeProfit*Point)+StopLoss*Point,(Ask-TakeProfit*Point)-TakeProfit*Point,NULL,MagicN,0,clrBlue);
  }
//+------------------------------------------------------------------+
//| buystop3                                                         |
//+------------------------------------------------------------------+
void buystop3()
  {
   OrderSend(Symbol(),OP_BUYSTOP,lot9,Ask+TakeProfit*Point,3,(Ask+TakeProfit*Point)-StopLoss*Point,(Ask+TakeProfit*Point)+TakeProfit*Point,NULL,MagicN,0,clrBlue);
  }
//+------------------------------------------------------------------+
//| sellstop4                                                        |
//+------------------------------------------------------------------+
void sellstop4()
  {
   OrderSend(Symbol(),OP_SELLSTOP,lot10,Ask-TakeProfit*Point,3,(Ask-TakeProfit*Point)+StopLoss*Point,(Ask-TakeProfit*Point)-TakeProfit*Point,NULL,MagicN,0,clrBlue);
  }
//+------------------------------------------------------------------+
//| buystop4                                                         |
//+------------------------------------------------------------------+
void buystop4()
  {
   OrderSend(Symbol(),OP_BUYSTOP,lot11,Ask+TakeProfit*Point,3,(Ask+TakeProfit*Point)-StopLoss*Point,(Ask+TakeProfit*Point)+TakeProfit*Point,NULL,MagicN,0,clrBlue);
  }
//+------------------------------------------------------------------+
//| deleteallpendingorders                                           |
//+------------------------------------------------------------------+
void deleteallpendingorders()
  {
//----
   for(int x=OrdersTotal()-1;x>=0;x--)
     {
      OrderSelect(x,SELECT_BY_POS,MODE_TRADES);
      if(OrderType()==OP_BUY || OrderType()==OP_SELL)
        {
         OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),666,CLR_NONE);
           }else{OrderDelete(OrderTicket());
        }
     }
  }
//+------------------------------------------------------------------+
