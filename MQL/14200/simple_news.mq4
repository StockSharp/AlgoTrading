//+------------------------------------------------------------------+
//|                                                      Simple News |
//|                                Copyright 2015, Vladimir V. Tkach |
//+------------------------------------------------------------------+
#property version "1.0"
#property copyright "Copyright © 2015, Vladimir V. Tkach"
#property description "Expert set pending orders from current price at selected time and day,"
#property description "then trails their stop loss."
#property strict

input datetime news_time=D'2015.11.06 14:30';

input int
deals=3,       //amount of deals
delta=50,      //step between them (in pips)
distance=300,  //distance from current price
sl=150,        //stop loss from current price
trail=200,     //trail sl (in pips)
tp=900,        //take profit from order price
slip=50,       //tolerance for trail
magic=220;     //magic number

input double
lot=0.01;      //lot size
//+------------------------------------------------------------------+
//|Main function                                                     |
//+------------------------------------------------------------------+
void start()
  {
//set pending orders before the news
   if(TimeCurrent()<(int)news_time && TimeCurrent()>(int)news_time-5*60 && !PendingExcist()) SetPending();

//delete pending orders which were not started
   if(TimeCurrent()>(int)news_time+10*60) DeletePending();

   Comment(news_time," ",TimeCurrent());

//trail stop loss if orders in the market 
   if(InMarket() && trail!=0)
     {
      for(int i=0; i<OrdersTotal(); i++)
        {
         if(OrderSelect(i,SELECT_BY_POS)==false) continue;
         if(OrderSymbol()==Symbol() && OrderMagicNumber()==magic && OrderType()<2)
           {
            if(OrderType()==OP_BUY)
              {
               if(OrderStopLoss()<Bid-trail*Point && OrderStopLoss()<Bid-trail*Point-slip*Point) if(OrderModify(OrderTicket(),OrderOpenPrice(),Bid-trail*Point,OrderTakeProfit(),0)==false) continue;
              }
            else if(OrderType()==OP_SELL)
              {
               if(OrderStopLoss()>Ask+trail*Point && OrderStopLoss()>Ask+trail*Point+slip*Point) if(OrderModify(OrderTicket(),OrderOpenPrice(),Ask+trail*Point,OrderTakeProfit(),0)==false) continue;
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|Check if pending orders run into the market                       |
//+------------------------------------------------------------------+
bool InMarket()
  {
   for(int i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS)==false) continue;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==magic && OrderType()<2) return(true);
     }
   return(false);
  }
//+------------------------------------------------------------------+
//|Set pending orders                                                |
//+------------------------------------------------------------------+
void SetPending()
  {
   for(int i=0; i<deals; i++)
     {
      double price,sl_=0,tp_=0;

      price=Ask+distance*Point;
      if(sl!=0) sl_=Ask-sl*Point;
      if(tp!=0) tp_=price+tp*Point;

      if(OrderSend(Symbol(),OP_BUYSTOP,lot,price+i*delta*Point,slip,sl_,tp_,"",magic)==false) continue;

      price=Bid-distance*Point;
      if(sl!=0) sl_=Bid+sl*Point;
      if(tp!=0) tp_=price-tp*Point;

      if(OrderSend(Symbol(),OP_SELLSTOP,lot,price-i*delta*Point,slip,sl_,tp_,"",magic)==false) continue;
     }
  }
//+------------------------------------------------------------------+
//|Delete pending orders                                             |
//+------------------------------------------------------------------+
void DeletePending()
  {
   for(int i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS)==false) continue;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==magic && OrderType()>1) if(OrderDelete(OrderTicket())==false) continue;
     }
  }
//+------------------------------------------------------------------+
//|Check if pending orders were set                                  |
//+------------------------------------------------------------------+ 
bool PendingExcist()
  {
   for(int i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS)==false) continue;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==magic && OrderType()>1) return(true);
     }
   return(false);
  }
//+------------------------------------------------------------------+
//|Initialisation function                                           |
//+------------------------------------------------------------------+  
void init()
  {
  }
//+------------------------------------------------------------------+
//|Deinitialisation function                                         |
//+------------------------------------------------------------------+
void deinit()
  {
   Comment("");
  }
//+------------------------------------------------------------------+
