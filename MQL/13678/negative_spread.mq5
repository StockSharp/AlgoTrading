//+------------------------------------------------------------------+
//|                                                      Session.mq5 |
//|                        Copyright 2015, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "1987pavlov"
#property link      "https://www.mql5.com"
#property version   "1.0"
//+------------------------------------------------------------------+
//| Includes                                                         |
//+------------------------------------------------------------------+
#include <trade/trade.mqh>
//+------------------------------------------------------------------+
//| Inputs                                                           |
//+------------------------------------------------------------------+
input double Lots= 0.01;   //Лот
input int TpPips = 5000;   //Take Profit
input int SlPips = 5000;   //Stop Loss
input ENUM_ORDER_TYPE_FILLING Filling=ORDER_FILLING_RETURN;  //Режим заполнения ордера
bool tradeResult=false,tradeOpened=false;;
//+------------------------------------------------------------------+
//| Global variables                                                 |
//+------------------------------------------------------------------+
double gTickSize;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
void OnInit()
  {
   gTickSize=SymbolInfoDouble(_Symbol,SYMBOL_TRADE_TICK_SIZE);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   double bid   =SymbolInfoDouble(_Symbol,SYMBOL_BID); // Запрос значения Bid
   double ask   =SymbolInfoDouble(_Symbol,SYMBOL_ASK); // Запрос значения Ask
   if(ask-bid<0 && !tradeResult) //OpenPosition
     {
      tradeResult=false;
      tradeOpened=false;
      MqlTradeRequest request={0};
      MqlTradeResult result={0};
      tradeOpened=true;
      bid=NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_BID),_Digits);
      request.action=TRADE_ACTION_DEAL;         // setting a pending order
      request.magic=68975;                      // ORDER_MAGIC
      request.symbol=_Symbol;                   // symbol
      request.volume=Lots;                      // volume in 0.1 lots
      request.sl=NormalizeDouble(bid + SlPips * gTickSize, _Digits);        // Stop Loss is not specified
      request.tp=NormalizeDouble(bid - TpPips * gTickSize, _Digits);        // Take Profit is not specified     
      request.type=ORDER_TYPE_SELL;             // order type
      request.price=bid;                        // open price
      request.type_filling=Filling;
      //--- send a trade request
      int i=PositionsTotal();//Wait openedPosition
      tradeResult=OrderSend(request,result);
      if(tradeResult) while(i==PositionsTotal())//Wait openedPosition
        {
        }
     }
//---
   if((tradeResult) && (tradeOpened)) //ClosePosition
     {
      CTrade trade;
      trade.SetTypeFilling(Filling);
      int i=PositionsTotal()-1;
      while(i>=0)
        {
         if(trade.PositionClose(_Symbol)) i--;
        }
      tradeResult=false;
     }
  }
//+------------------------------------------------------------------+
