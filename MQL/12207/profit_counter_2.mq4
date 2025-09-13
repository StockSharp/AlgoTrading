//+------------------------------------------------------------------+
//|                                                        count.mq4 |
//|                          Copyright 2014, Abilash corp. |
//|                                               |
//+------------------------------------------------------------------+
#property copyright "hsaliba420@gmail.com"
#property link      "http://www.facebook.com/hsalibaa"
#property version   "1.00"
#property strict

extern int MagicN = 0000;
extern int TakeProfit = 100 ;
extern int StopLoss   = 200 ;
extern double lot1   =  0.01    ;
extern double lot2   =  0.03    ;
extern double lot3   =  0.09    ;
extern double lot4   =  0.27    ;
extern double lot5   =  0.81    ;
extern double lot6   =  2.43    ;
extern double lot7   =  7.29    ;
extern double lot8   =  21.87   ;
extern double lot9   =  65.61   ;
extern double lot10  =  196.83  ;
extern double lot11  =  590.49  ;
//extern int opentime = 4 ;
//extern int closetime = 13;

 
extern int opentime = 16;


 





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
  int j=0;
   int hourtime= TimeHour(Time[j]);
 
//---
int total = OrdersTotal();
int i;
double OpenLongOrders = 0, OpenShortOrders = 0, PendLongs =0, PendShorts =0;
//
if(hourtime==opentime )
{

if(total == 0 && OpenLongOrders == 0 && OpenShortOrders == 0 && PendLongs == 0 && PendShorts == 0)
      {
             openbuy();
              sellstop();
      }}

//
 for( i=0;i<total;i++)       
   {
     OrderSelect(i, SELECT_BY_POS );
    if ( OrderSymbol() == Symbol() && OrderMagicNumber() == MagicN)
         {
           int type = OrderType();

if (type == OP_BUY )       {OpenLongOrders=OpenLongOrders+1;}
if (type == OP_SELL )      {OpenShortOrders=OpenShortOrders+1;}
if (type == OP_BUYSTOP )   {PendLongs=PendLongs+1;}
if (type == OP_SELLSTOP )  {PendShorts=PendShorts+1;}
  
  if(total == 2 && OpenLongOrders == 1 && OpenShortOrders == 1 && PendLongs == 0 && PendShorts == 0)
      {
             buystop();
      }
    if(total == 3 && OpenLongOrders == 2 && OpenShortOrders == 1 && PendLongs == 0 && PendShorts == 0)
      {
             sellstop1();
      }  
      if(total == 4 && OpenLongOrders == 2 && OpenShortOrders == 2 && PendLongs == 0 && PendShorts == 0)
      {
             buystop1();
      }    
      
      if(total == 5 && OpenLongOrders == 3 && OpenShortOrders == 2 && PendLongs == 0 && PendShorts == 0)
      {
             sellstop2();
      }
      if(total == 6 && OpenLongOrders == 3 && OpenShortOrders == 3 && PendLongs == 0 && PendShorts == 0)
      {
             buystop2();
      }
      if(total == 7 && OpenLongOrders == 4 && OpenShortOrders == 3 && PendLongs == 0 && PendShorts == 0)
      {
             sellstop3();
      }
      if(total == 8 && OpenLongOrders == 4 && OpenShortOrders == 4 && PendLongs == 0 && PendShorts == 0)
      {
             buystop3();
      }
      if(total == 9 && OpenLongOrders == 5 && OpenShortOrders == 4 && PendLongs == 0 && PendShorts == 0)
      {
             sellstop4();
      }
      if(total == 10 && OpenLongOrders == 5 && OpenShortOrders == 5 && PendLongs == 0 && PendShorts == 0)
      {
             buystop4();
      }
      if(total == 1 && OpenLongOrders == 0 && OpenShortOrders == 0 && (PendLongs == 1 || PendShorts == 1))
      {
             deleteallpendingorders();
      }
       
      Sleep(2000);
  }
  
     
      
      
  
  }
     
      
  
  }
  
//+------------------------------------------------------------------+
void openbuy()
{
 OrderSend(Symbol(),OP_BUY,lot1,Ask,3,Ask-StopLoss*Point,Ask+TakeProfit*Point,NULL,MagicN,0,clrBlue);
}

void sellstop()
{
 OrderSend(Symbol(),OP_SELLSTOP,lot2,Ask-TakeProfit*Point,3,(Ask-TakeProfit*Point)+StopLoss*Point,(Ask-TakeProfit*Point)-TakeProfit*Point,NULL,MagicN,0,clrBlue);
}

void buystop()
{
 OrderSend(Symbol(),OP_BUYSTOP,lot3,Ask+TakeProfit*Point,3,(Ask+TakeProfit*Point)-StopLoss*Point,(Ask+TakeProfit*Point)+TakeProfit*Point,NULL,MagicN,0,clrBlue);
}
void sellstop1()
{
 OrderSend(Symbol(),OP_SELLSTOP,lot4,Ask-TakeProfit*Point,3,(Ask-TakeProfit*Point)+StopLoss*Point,(Ask-TakeProfit*Point)-TakeProfit*Point,NULL,MagicN,0,clrBlue);
}

void buystop1()
{
 OrderSend(Symbol(),OP_BUYSTOP,lot5,Ask+TakeProfit*Point,3,(Ask+TakeProfit*Point)-StopLoss*Point,(Ask+TakeProfit*Point)+TakeProfit*Point,NULL,MagicN,0,clrBlue);
}
void sellstop2()
{
 OrderSend(Symbol(),OP_SELLSTOP,lot6,Ask-TakeProfit*Point,3,(Ask-TakeProfit*Point)+StopLoss*Point,(Ask-TakeProfit*Point)-TakeProfit*Point,NULL,MagicN,0,clrBlue);
}

void buystop2()
{
 OrderSend(Symbol(),OP_BUYSTOP,lot7,Ask+TakeProfit*Point,3,(Ask+TakeProfit*Point)-StopLoss*Point,(Ask+TakeProfit*Point)+TakeProfit*Point,NULL,MagicN,0,clrBlue);
}

void sellstop3()
{
 OrderSend(Symbol(),OP_SELLSTOP,lot8,Ask-TakeProfit*Point,3,(Ask-TakeProfit*Point)+StopLoss*Point,(Ask-TakeProfit*Point)-TakeProfit*Point,NULL,MagicN,0,clrBlue);
}

void buystop3()
{
 OrderSend(Symbol(),OP_BUYSTOP,lot9,Ask+TakeProfit*Point,3,(Ask+TakeProfit*Point)-StopLoss*Point,(Ask+TakeProfit*Point)+TakeProfit*Point,NULL,MagicN,0,clrBlue);
}

void sellstop4()
{
 OrderSend(Symbol(),OP_SELLSTOP,lot10,Ask-TakeProfit*Point,3,(Ask-TakeProfit*Point)+StopLoss*Point,(Ask-TakeProfit*Point)-TakeProfit*Point,NULL,MagicN,0,clrBlue);
}

void buystop4()
{
 OrderSend(Symbol(),OP_BUYSTOP,lot11,Ask+TakeProfit*Point,3,(Ask+TakeProfit*Point)-StopLoss*Point,(Ask+TakeProfit*Point)+TakeProfit*Point,NULL,MagicN,0,clrBlue);
}



//=========================================================
//
//=========================================================

void deleteallpendingorders()
{
   for(int i=0;i<OrdersTotal();i++)
   {
     OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
     if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicN && ((OrderType()==OP_BUY) || (OrderType()==OP_SELL) ||(OrderType()==OP_BUYSTOP) || (OrderType()==OP_SELLSTOP) || (OrderType()==OP_BUYLIMIT) || (OrderType()==OP_SELLLIMIT)))
     {
       OrderDelete(OrderTicket());
     }   
   }
}

//============================================
//////////////////////////////////////////////
//============================================
/*bool checktime()
{
  int i=0;
  int hourtime=TimeHour(Time[i]);
  if (hourtime==opentime){return(true);}
  else if (hourtime==closetime){return(false);}
  else
  return (EMPTY_VALUE);

}*/