//+------------------------------------------------------------------+
//|                                                     AutoSLTP.mq4 |
//|                        Copyright 2015, MetaQuotes Software Corp. |
//|                           http://free-bonus-deposit.blogspot.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015, dXerof"
#property link      "http://free-bonus-deposit.blogspot.com"
#property version   "1.00"

input bool   AllPairs=True;
input double TakeProfit=400; //40-->4 digits; 400-->5 digits
input double StopLoss=150; //15-->4 digits; 150-->5 digits
input double DurasiTime=60;
//---
int ticket;
double poen;
string pair="";
double iTakeProfit,iStopLoss;
double slbuy;
double tpbuy;
double slsell;
double tpsell;
double stoplevel;
double digi;
double poin;
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
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
   for(int cnt=0; cnt<OrdersTotal(); cnt++)
     {
      if(OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderTicket()>0 && OrderMagicNumber()==0 && OrderTakeProfit()==0 && TimeCurrent()-OrderOpenTime()<=DurasiTime)
           {
            if(AllPairs) pair=OrderSymbol(); else pair=Symbol();
            //---
            stoplevel=MarketInfo(pair,MODE_STOPLEVEL);
            digi=MarketInfo(pair,MODE_DIGITS);
            poin=MarketInfo(pair,MODE_POINT);

            iStopLoss=StopLoss;
            iTakeProfit=TakeProfit;
            if(StopLoss<stoplevel) iStopLoss=stoplevel;
            if(TakeProfit<stoplevel) iTakeProfit=stoplevel;
            //---
            slbuy=NormalizeDouble(OrderOpenPrice()-iStopLoss*poin,digi);
            tpbuy=NormalizeDouble(OrderOpenPrice()+iTakeProfit*poin,digi);
            slsell=NormalizeDouble(OrderOpenPrice()+iStopLoss*poin,digi);
            tpsell=NormalizeDouble(OrderOpenPrice()-iTakeProfit*poin,digi);
            //---
            if(OrderSymbol()==pair && (OrderType()==OP_BUY || OrderType()==OP_BUYLIMIT || OrderType()==OP_BUYSTOP))
              {
               ticket=OrderModify(OrderTicket(),OrderOpenPrice(),slbuy,tpbuy,0,Blue);
              }
            if(OrderSymbol()==pair && (OrderType()==OP_SELL || OrderType()==OP_SELLLIMIT || OrderType()==OP_SELLSTOP))
              {
               ticket=OrderModify(OrderTicket(),OrderOpenPrice(),slsell,tpsell,0,Red);
              }
           }
        }
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
