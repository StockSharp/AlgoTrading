//+------------------------------------------------------------------+
//|                                                          RSI.mq4 |
//|                               Copyright © 2016, Хлыстов Владимир |
//|                                                cmillion@narod.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2016, Хлыстов Владимир"
#property link      "cmillion@narod.ru"
#property strict
#property description "советник по RSI"
#property description "sell при пересечение сверху вниз 70 и на buy снизу вверх 30"
#property description "стопы и тейки можно выстовить в настройках советника"
//--------------------------------------------------------------------
extern int     period_RSI           = 14,
               stoploss             = 100,
               takeprofit           = 200,
               slippage             = 10,
               buy_level            = 30,
               sell_level           = 70,
               Magic                = 777;
extern double  Lot                  = 0.1;
//--------------------------------------------------------------------
void OnTick()
{
   for (int i=0; i<OrdersTotal(); i++)
      if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
         if (OrderSymbol()==Symbol() && Magic==OrderMagicNumber()) return;
   double RSI0  = iRSI(NULL,0,period_RSI,PRICE_OPEN,0);
   double RSI1  = iRSI(NULL,0,period_RSI,PRICE_OPEN,1);
   double SL=0,TP=0;
   if (RSI0 > buy_level && RSI1 < buy_level)
   {
      if (takeprofit!=0) TP  = NormalizeDouble(Ask + takeprofit*Point,Digits);
      if (stoploss!=0)   SL  = NormalizeDouble(Ask - stoploss*  Point,Digits);     
      if (OrderSend(Symbol(),OP_BUY, Lot,NormalizeDouble(Ask,Digits),slippage,SL,TP,NULL,Magic)==-1) Print(GetLastError());
   }
   if (RSI0 < sell_level && RSI1 > sell_level)
   {
      if (takeprofit!=0) TP = NormalizeDouble(Bid - takeprofit*Point,Digits);
      if (stoploss!=0)   SL = NormalizeDouble(Bid + stoploss*  Point,Digits);            
      if (OrderSend(Symbol(),OP_SELL,Lot,NormalizeDouble(Bid,Digits),slippage,SL,TP,NULL,Magic)==-1) Print(GetLastError());
   }
}
//--------------------------------------------------------------------
