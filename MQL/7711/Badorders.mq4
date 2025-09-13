//+------------------------------------------------------------------+
//|                                                    BadOrders.mq4 |
//|                      Copyright © 2005, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"
//----
double asdf;
double ticket, t2;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
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
   //This section closes any positions opened on the last tick
   OrderSelect(1,SELECT_BY_POS);
   OrderClose(OrderTicket(),1,Bid,8,Red);
   ticket=OrderSend(Symbol(),OP_BUYSTOP,1,Bid+100*Point,3,0,0,"asdfasdf",16384,0,Green);
   t2=OrderSelect(ticket,SELECT_BY_TICKET);
   OrderModify(OrderTicket(),Bid-100*Point,0,0,0,Green);
   //OrderClose(OrderTicket(),1,Bid,8,Red);
   //OrderSend(Symbol(),OP_BUYSTOP,1,Bid-100*Point,3,0,0,"asdfasdf",16384,0,Green);
//----
   return(0);
  }
//+------------------------------------------------------------------+