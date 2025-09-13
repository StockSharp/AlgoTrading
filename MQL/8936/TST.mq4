//+------------------------------------------------------------------+
//|                                                          TST.mq4 |
//|                                                     Yuriy Tokman |
//|                                            yuriytokman@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Yuriy Tokman"
#property link      "yuriytokman@gmail.com"

extern int    StopLoss         = 500;           // Размер стопа в пунктах
extern int    TakeProfit       = 100;           // Размер тейка в пунктах
extern double Lots             = 0.1;           // Размер лота

extern int timeframe           = 0;             // Выбор ТФ
extern int pips                = 500;           // Установленный уровень отката
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
   double bid  = MarketInfo(Symbol(),MODE_BID);
   double open = iOpen(Symbol(),timeframe,0);
   double high = iHigh(Symbol(),timeframe,0);
   double low  = iLow(Symbol(),timeframe,0);
//----
   if(open-bid>0 && high-bid>pips*Point)
    {
     if(NevBar())
     OrderSend(Symbol(),OP_BUY,Lots,Ask,3,Bid-StopLoss*Point,Ask+TakeProfit*Point,"TST",0,0,Green);
    }
   if(bid-open>0 && bid-low>pips*Point)
    {
     if(NevBar())      
     OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Ask+StopLoss*Point,Bid-TakeProfit*Point,"TST",0,0,Red);
    }
//----
   return(0);
  }
//+----------------------------------------------------------------------------+
// Функция контроля нового бара                                                |
//-----------------------------------------------------------------------------+
 bool NevBar()
  {
   static int PrevTime=0;
   if (PrevTime==Time[0]) return(false);
   PrevTime=Time[0];
   return(true);
  }