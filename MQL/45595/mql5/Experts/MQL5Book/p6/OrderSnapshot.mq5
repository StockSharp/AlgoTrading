//+------------------------------------------------------------------+
//|                                                OrderSnapshot.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Pick up an existing order and monitors its changes in the log"

#include <MQL5Book/AutoPtr.mqh>
#include <MQL5Book/MqlError.mqh>
#include <MQL5Book/TradeState.mqh>

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
   if(AccountInfoInteger(ACCOUNT_TRADE_MODE) != ACCOUNT_TRADE_MODE_DEMO)
   {
      Alert("This is a test EA! Run it on a DEMO account only!");
      return INIT_FAILED;
   } 

   if(OrdersTotal() == 0)
   {
      Alert("Please, create a pending order or open/close a position");
   }
   else
   {
      OnTrade(); // self-invocation
   }
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| General trade notification handler                               |
//+------------------------------------------------------------------+
void OnTrade()
{
   static int count = 0;
   static AutoPtr<OrderState> auto;

   OrderState *state = auto[];

   PrintFormat(">>> OnTrade(%d)", count++);
   
   if(OrdersTotal() > 0 && state == NULL)
   {
      const ulong ticket = OrderGetTicket(OrdersTotal() - 1);
      auto = new OrderState(ticket);
      PrintFormat("Order picked up: %lld %s", ticket, auto[].isReady() ? "true" : "false");
      auto[].print(); // initial state on acquisition
   }
   else if(state)
   {
      int changes[];
      if(state.getChanges(changes))
      {
         if(ArraySize(changes) > 0)
         {
            Print("Order properties changed:");
            ArrayPrint(changes);
         }
         for(int k = 0; k < ArraySize(changes); ++k)
         {
            switch(OrderState::TradeState::type(changes[k]))
            {
            case PROP_TYPE_INTEGER:
               Print(EnumToString((ENUM_ORDER_PROPERTY_INTEGER)changes[k]), ": ",
                  state.stringify(changes[k]), " -> ",
                  state.stringifyRaw(changes[k]));
                  break;
            case PROP_TYPE_DOUBLE:
               Print(EnumToString((ENUM_ORDER_PROPERTY_DOUBLE)changes[k]), ": ",
                  state.stringify(changes[k]), " -> ",
                  state.stringifyRaw(changes[k]));
                  break;
            case PROP_TYPE_STRING:
               Print(EnumToString((ENUM_ORDER_PROPERTY_STRING)changes[k]), ": ",
                  state.stringify(changes[k]), " -> ",
                  state.stringifyRaw(changes[k]));
                  break;
            }
         }
         state.update();
      }
      
      if(_LastError != 0) Print(E2S(_LastError));
   }
}

//+------------------------------------------------------------------+
/*
   example output (run on an account without pending orders,
   create buy-limit order, then set TP, then change its expiration, then remove it)
   
   Alert: Please, create a pending order or open/close a position
   >>> OnTrade(0)
   Order picked up: 1311736135 true
   MonitorInterface<ENUM_ORDER_PROPERTY_INTEGER,ENUM_ORDER_PROPERTY_DOUBLE,ENUM_ORDER_PROPERTY_STRING>
   ENUM_ORDER_PROPERTY_INTEGER Count=14
     0 ORDER_TIME_SETUP=2022.04.11 11:42:39
     1 ORDER_TIME_EXPIRATION=1970.01.01 00:00:00
     2 ORDER_TIME_DONE=1970.01.01 00:00:00
     3 ORDER_TYPE=ORDER_TYPE_BUY_LIMIT
     4 ORDER_TYPE_FILLING=ORDER_FILLING_RETURN
     5 ORDER_TYPE_TIME=ORDER_TIME_GTC
     6 ORDER_STATE=ORDER_STATE_STARTED
     7 ORDER_MAGIC=0
     8 ORDER_POSITION_ID=0
     9 ORDER_TIME_SETUP_MSC=2022.04.11 11:42:39'729
    10 ORDER_TIME_DONE_MSC=1970.01.01 00:00:00'000
    11 ORDER_POSITION_BY_ID=0
    12 ORDER_TICKET=1311736135
    13 ORDER_REASON=ORDER_REASON_CLIENT
   ENUM_ORDER_PROPERTY_DOUBLE Count=7
     0 ORDER_VOLUME_INITIAL=0.01
     1 ORDER_VOLUME_CURRENT=0.01
     2 ORDER_PRICE_OPEN=1.087
     3 ORDER_PRICE_CURRENT=1.087
     4 ORDER_PRICE_STOPLIMIT=0.0
     5 ORDER_SL=0.0
     6 ORDER_TP=0.0
   ENUM_ORDER_PROPERTY_STRING Count=3
     0 ORDER_SYMBOL=EURUSD
     1 ORDER_COMMENT=
     2 ORDER_EXTERNAL_ID=
   >>> OnTrade(1)
   Order properties changed:
   10 14
   ORDER_PRICE_CURRENT: 1.087 -> 1.09073
   ORDER_STATE: ORDER_STATE_STARTED -> ORDER_STATE_PLACED
   >>> OnTrade(2)
   >>> OnTrade(3)
   >>> OnTrade(4)
   
   >>> OnTrade(5)
   Order properties changed:
   10 13
   ORDER_PRICE_CURRENT: 1.09073 -> 1.09079
   ORDER_TP: 0.0 -> 1.097
   >>> OnTrade(6)
   >>> OnTrade(7)
   
   >>> OnTrade(8)
   Order properties changed:
   10
   ORDER_PRICE_CURRENT: 1.09079 -> 1.09082
   >>> OnTrade(9)
   >>> OnTrade(10)
   Order properties changed:
   2 6
   ORDER_TIME_EXPIRATION: 1970.01.01 00:00:00 -> 2022.04.11 00:00:00
   ORDER_TYPE_TIME: ORDER_TIME_GTC -> ORDER_TIME_DAY
   >>> OnTrade(11)
   
   >>> OnTrade(12)
   TRADE_ORDER_NOT_FOUND
   >>> OnTrade(13)
   TRADE_ORDER_NOT_FOUND
   >>> OnTrade(14)
   TRADE_ORDER_NOT_FOUND
   >>> OnTrade(15)
   TRADE_ORDER_NOT_FOUND
   >>> OnTrade(16)
   TRADE_ORDER_NOT_FOUND
   >>> OnTrade(17)
   TRADE_ORDER_NOT_FOUND

*/
//+------------------------------------------------------------------+
