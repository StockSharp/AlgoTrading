//+------------------------------------------------------------------+
//|                                                    HFT: Spreader |
//|                                        Copyright 2015, TheJobber |
//|                            http://linkedin.com/in/SerhiyDotsenko |
//+------------------------------------------------------------------+
#property description   "HFT: спредер для FORTS"
#property description   " "
#property description   "HFT: spreader for FORTS"
#property link          "https://www.mql5.com/en/users/thejobber"
#property copyright     "Serhiy Dotsenko"
#property version       "1.00"
//---
#include <Trade\PositionInfo.mqh>
#include <Trade\OrderInfo.mqh>
#include <Trade\Trade.mqh>
//---
input double Lots              = 1; // Размер позиции в лотах
input ushort SpreadToPutOrders = 4; // Спред: количество шагов минимального изменения цены
/*
SpreadToPutOrders = 3 означает, что если у торгового инструмента минимальный шаг изменения цены = 25 денежных едениц,
то спред при котором робот начнёт работать будет равен 3*25=75
*/
input long Magic=99;// Магик
double minStep;
//---
CPositionInfo     CurrentPosition;
CSymbolInfo       CurrentRates;
//---
//+------------------------------------------------------------------+
//| Разовые действия при подключении робота к графику                |
//+------------------------------------------------------------------+
int OnInit()
  {
   if(!MarketBookAdd(_Symbol))
     {
      Alert("не смог получить стакан по тикеру "+_Symbol);
      ExpertRemove();
      return INIT_FAILED;
     }
   CurrentRates.Name(_Symbol);
   minStep=SymbolInfoDouble(_Symbol,SYMBOL_TRADE_TICK_SIZE);
   return INIT_SUCCEEDED;
  }
//+------------------------------------------------------------------+
//| Разовые действия при отключении робота от графика                |
//| Удаляю отложенные ордера и закрываю позции                       |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- если этот код вставить в скрипт, то работает, из советника НЕ работает, кто разберётся, отпишитесь на форуме.
   CTrade cTrade;
   if(CurrentPosition.Volume()>0)
     {
      if(cTrade.PositionClose(_Symbol))
         Alert("закрыл позицию");
      else
         Alert("не смог закрыть позицию");
     }
   for(int i=OrdersTotal()-1;i>=0;i--)
     {
      COrderInfo cOrderInfo;
      cOrderInfo.SelectByIndex(i);
      //for(int j =0; j++ < 3 || !cTrade.OrderDelete(cOrderInfo.Ticket());Sleep(1000) );                        
      if(cTrade.OrderDelete(cOrderInfo.Ticket()))
         Alert("удалил ордер "+IntegerToString(cOrderInfo.Ticket()));
      else
         Alert("не смог удалить ордер "+IntegerToString(cOrderInfo.Ticket()));
     }
  }
//+----------------------------------------------------------------------------------------+
//| Обрабатываем событие стакана цен, при каждом изменении в стакане проверяем все условия |
//+----------------------------------------------------------------------------------------+
void OnBookEvent(const string &symbol)
  {
   ResetLastError();
   deletePendingOrdersWithIncorrectPrice();
   CurrentRates.RefreshRates();
   CurrentPosition.Select(_Symbol);
//---
   if(CurrentPosition.Volume()>0 && OrdersTotal()<1)
      setPendingOrder(CurrentPosition.PositionType()==POSITION_TYPE_BUY ? ORDER_TYPE_SELL_LIMIT : ORDER_TYPE_BUY_LIMIT,CurrentPosition.Volume()*2);
//---
   if(OrdersTotal()<1 && CurrentPosition.Volume()<1 && CurrentRates.Spread()>=SpreadToPutOrders*minStep)
     {
      setPendingOrder(ORDER_TYPE_SELL_LIMIT,Lots);
      setPendingOrder(ORDER_TYPE_BUY_LIMIT,Lots);
     }
  }
//+-----------------------------------------------------------------------------+
//| Удаляем отложенные ордера, которые не соответствуют новым рыночным условиям |
//+-----------------------------------------------------------------------------+
void deletePendingOrdersWithIncorrectPrice()
  {
   for(int i=OrdersTotal()-1;i>=0;i--)
     {
      ulong ticket=OrderGetTicket(i);
      if(checkPriceInPendingOrder(ticket))
        {
         CTrade cTrade;
         cTrade.OrderDelete(ticket);
        }
     }
  }
//+------------------------------------------------------------------+
//| Проверка условий для удаления ордеров                            |
//+------------------------------------------------------------------+
bool checkPriceInPendingOrder(ulong ticket)
  {
   return OrderSelect(ticket) && (isBestBidOrOfferOnlyMy(ticket) || needMove(ticket));
  }
//+----------------------------------------------------------------------------------------------------+
//| Проверяем, стоят ли лучшими ценами и объёмом только мои ордера и есть ли куда их отдодвинуть назад |
//+----------------------------------------------------------------------------------------------------+
bool isBestBidOrOfferOnlyMy(ulong ticket)
  {
   COrderInfo pendingOrder;
   MqlBookInfo book[];
   if(MarketBookGet(_Symbol,book) && OrderSelect(ticket))
     {
      CurrentRates.RefreshRates();
      ENUM_ORDER_TYPE ot=pendingOrder.OrderType();
      ushort index=findIndexOfBestPriceInOrderBook(ot,book);
      return
      ((ot==ORDER_TYPE_SELL_LIMIT && book[index].type==BOOK_TYPE_SELL)
       || (ot==ORDER_TYPE_BUY_LIMIT && book[index].type==BOOK_TYPE_BUY))
      && pendingOrder.VolumeCurrent()==book[index].volume
      && calcDiff(ot,book,index);
      //double diff = ot == ORDER_TYPE_SELL_LIMIT ? book[index].price-book[index+1].price:book[index+1].price-book[index].price;                
     }
   return false;
  }
//+------------------------------------------------------------------+
//| Считаем разницу в ценах между моими ордерами и чужими            |
//+------------------------------------------------------------------+
bool calcDiff(ENUM_ORDER_TYPE &ot,MqlBookInfo &ob[],ushort index)
  {
   return ot==ORDER_TYPE_SELL_LIMIT ? ob[index-1].price - ob[index].price > minStep : ob[index].price-ob[index+1].price > minStep;
  }
//+------------------------------------------------------------------+
//| Проверяем, есть ли куда отодвинуться назад                       |
//+------------------------------------------------------------------+
bool needMove(ulong ticket)
  {
   if(OrderSelect(ticket))
     {
      COrderInfo pendingOrder;
      double orderPrice=pendingOrder.PriceOpen();
      CurrentRates.RefreshRates();
      double currentAsk = CurrentRates.Ask();
      double currentBid = CurrentRates.Bid();
      //---
      if(pendingOrder.OrderType()==ORDER_TYPE_SELL_LIMIT)
        {
         if(orderPrice>currentAsk || currentAsk-orderPrice>minStep)
            return true;
           }else{
         if(orderPrice<currentBid || orderPrice-currentBid>minStep)
            return true;
        }
     }
   return false;
  }
//+------------------------------------------------------------------+
//| Ищем индекс лучших бида и аска в стакане                         |
//+------------------------------------------------------------------+
ushort findIndexOfBestPriceInOrderBook(ENUM_ORDER_TYPE ot,MqlBookInfo &ob[])
  {
   ushort index=0;
   for(;ob[index].type==BOOK_TYPE_SELL;index++);
   return ot == ORDER_TYPE_SELL_LIMIT ? index-1:index;
  }
//+--------------------------------------------------------------------------------------------+
//| Устанавливаю отложенные ордера. MQL STL не использовал, т.к. не все параметры можно задать |
//+--------------------------------------------------------------------------------------------+
void setPendingOrder(ENUM_ORDER_TYPE eot,double vol)
  {
   MqlTradeRequest request={0};
   MqlTradeResult result={0};
   request.action=TRADE_ACTION_PENDING;
   request.symbol=_Symbol;
   request.volume=vol;
   request.deviation=10;
   request.magic=Magic;
   request.type_filling=ORDER_FILLING_RETURN;
//request.sl=10;                  
//request.tp=10;
   request.type=eot;
//---
   CSymbolInfo si;
   si.Name(_Symbol);
   si.RefreshRates();
//---
   request.price=eot==ORDER_TYPE_SELL_LIMIT ? si.Ask()-minStep:si.Bid()+minStep;
   request.expiration=ORDER_TIME_DAY;
   request.type_time=ORDER_TIME_DAY;
   if(OrderSend(request,result)){}
  }
//+------------------------------------------------------------------+
