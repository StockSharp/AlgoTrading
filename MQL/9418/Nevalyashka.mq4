//+------------------------------------------------------------------+
//|                                                  Nevalyashka.mq4 |
//|                              Copyright © 2009, Khlystov Vladimir |
//|                                                cmillion@narod.ru |
//|opens a position opposite to the closed one.                      |
//--------------------------------------------------------------------
#property copyright "Copyright © 2009, Khlystov Vladimir"
#property link      "cmillion@narod.ru"
//--------------------------------------------------------------------
extern int  stoploss       = 50,
            takeprofit     = 50;
double      Lot=1;
int tip;
//--------------------------------------------------------------------
int init()
{
   OrderSend(Symbol(),OP_SELL,Lot,Bid,3,NormalizeDouble(Ask + stoploss*Point,Digits),
                                    NormalizeDouble(Bid - takeprofit*Point,Digits)," ",777,Blue);
}
//--------------------------------------------------------------------
int start()
{
   for (int i=0; i<OrdersTotal(); i++){   
      if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true){
         if (OrderSymbol()==Symbol()){
            tip = OrderType();
            Lot = OrderLots();return;}}}
   if (Lot==0) return;
   if (tip==0) OrderSend(Symbol(),OP_SELL,Lot,Bid,3,NormalizeDouble(Ask + stoploss*Point,Digits),
                                    NormalizeDouble(Bid - takeprofit*Point,Digits)," ",777,Blue);
   if (tip==1) OrderSend(Symbol(),OP_BUY ,Lot,Ask,3,NormalizeDouble(Bid - stoploss*Point,Digits),
                                    NormalizeDouble(Ask + takeprofit*Point,Digits)," ",777,Blue);
   return(0);
}
//-----------------------------------------------------------------

