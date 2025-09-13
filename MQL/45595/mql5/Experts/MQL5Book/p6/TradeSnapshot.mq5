//+------------------------------------------------------------------+
//|                                                TradeSnapshot.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Builds caches of orders and positions and keep track of changes there, prints all changes in the log"

// this macro for TradeCache.mqh enables print out of all props when adding an element into cache
#define PRINT_DETAILS

#include <MQL5Book/OrderFilter.mqh>
#include <MQL5Book/PositionFilter.mqh>
#include <MQL5Book/AutoPtr.mqh>
#include <MQL5Book/MqlError.mqh>
#include <MQL5Book/TradeState.mqh>
#include <MQL5Book/TradeCache.mqh>

//+------------------------------------------------------------------+
//| Past period to load into cache of historic orders                |
//+------------------------------------------------------------------+
enum ENUM_HISTORY_LOOKUP
{
   LOOKUP_NONE = 1,
   LOOKUP_DAY = 86400,
   LOOKUP_WEEK = 604800,
   LOOKUP_MONTH = 2419200,
   LOOKUP_YEAR = 29030400,
   LOOKUP_ALL = 0,
};

input ENUM_HISTORY_LOOKUP HistoryLookup = LOOKUP_NONE;

// without let-conditions the filters will select all elements
PositionFilter filter0;
PositionCache positions;

OrderFilter filter1;
OrderCache orders;

HistoryOrderFilter filter2;
HistoryOrderCache history;

datetime origin;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
   positions.reset();
   orders.reset();
   history.reset();
   origin = HistoryLookup ? TimeCurrent() - HistoryLookup : 0;
   
   OnTrade(); // self-invocation
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| General trade notification handler                               |
//+------------------------------------------------------------------+
void OnTrade()
{
   static int count = 0;

   PrintFormat(">>> OnTrade(%d)", count++);
   
   positions.scan(filter0);
   orders.scan(filter1);
   // setup history selection right before 'filter' use inside 'scan'
   HistorySelect(origin, LONG_MAX);
   history.scan(filter2);
   
   /*
   // you may try the scanning other way round (test different approaches):
   
      HistorySelect(origin, LONG_MAX);
      history.scan(filter2);
      orders.scan(filter1);
      positions.scan(filter0);
      
   */
   
   PrintFormat(">>> positions: %d, orders: %d, history: %d",
      positions.size(), orders.size(), history.size());
}

//+------------------------------------------------------------------+

/*
   EXAMPLE OUTPUT
   
   1. open new position (market order buy added, position added, order moved to history)

   >>> OnTrade(0)
   >>> positions: 0, orders: 0, history: 0
   >>> OnTrade(1)
   OrderCache added: 1311792104
   MonitorInterface<ENUM_ORDER_PROPERTY_INTEGER,ENUM_ORDER_PROPERTY_DOUBLE,ENUM_ORDER_PROPERTY_STRING>
   ENUM_ORDER_PROPERTY_INTEGER Count=14
     0 ORDER_TIME_SETUP=2022.04.11 12:34:51
     1 ORDER_TIME_EXPIRATION=1970.01.01 00:00:00
     2 ORDER_TIME_DONE=1970.01.01 00:00:00
     3 ORDER_TYPE=ORDER_TYPE_BUY
     4 ORDER_TYPE_FILLING=ORDER_FILLING_FOK
     5 ORDER_TYPE_TIME=ORDER_TIME_GTC
     6 ORDER_STATE=ORDER_STATE_STARTED
     7 ORDER_MAGIC=0
     8 ORDER_POSITION_ID=0
     9 ORDER_TIME_SETUP_MSC=2022.04.11 12:34:51'096
    10 ORDER_TIME_DONE_MSC=1970.01.01 00:00:00'000
    11 ORDER_POSITION_BY_ID=0
    12 ORDER_TICKET=1311792104
    13 ORDER_REASON=ORDER_REASON_CLIENT
   ENUM_ORDER_PROPERTY_DOUBLE Count=7
     0 ORDER_VOLUME_INITIAL=0.01
     1 ORDER_VOLUME_CURRENT=0.01
     2 ORDER_PRICE_OPEN=1.09218
     3 ORDER_PRICE_CURRENT=1.09218
     4 ORDER_PRICE_STOPLIMIT=0.0
     5 ORDER_SL=0.0
     6 ORDER_TP=0.0
   ENUM_ORDER_PROPERTY_STRING Count=3
     0 ORDER_SYMBOL=EURUSD
     1 ORDER_COMMENT=
     2 ORDER_EXTERNAL_ID=
   HistoryOrderCache added: 1311792104
   MonitorInterface<ENUM_ORDER_PROPERTY_INTEGER,ENUM_ORDER_PROPERTY_DOUBLE,ENUM_ORDER_PROPERTY_STRING>
   ENUM_ORDER_PROPERTY_INTEGER Count=14
     0 ORDER_TIME_SETUP=2022.04.11 12:34:51
     1 ORDER_TIME_EXPIRATION=1970.01.01 00:00:00
     2 ORDER_TIME_DONE=2022.04.11 12:34:51
     3 ORDER_TYPE=ORDER_TYPE_BUY
     4 ORDER_TYPE_FILLING=ORDER_FILLING_FOK
     5 ORDER_TYPE_TIME=ORDER_TIME_GTC
     6 ORDER_STATE=ORDER_STATE_FILLED
     7 ORDER_MAGIC=0
     8 ORDER_POSITION_ID=1311792104
     9 ORDER_TIME_SETUP_MSC=2022.04.11 12:34:51'096
    10 ORDER_TIME_DONE_MSC=2022.04.11 12:34:51'097
    11 ORDER_POSITION_BY_ID=0
    12 ORDER_TICKET=1311792104
    13 ORDER_REASON=ORDER_REASON_CLIENT
   ENUM_ORDER_PROPERTY_DOUBLE Count=7
     0 ORDER_VOLUME_INITIAL=0.01
     1 ORDER_VOLUME_CURRENT=0.0
     2 ORDER_PRICE_OPEN=1.09218
     3 ORDER_PRICE_CURRENT=1.09218
     4 ORDER_PRICE_STOPLIMIT=0.0
     5 ORDER_SL=0.0
     6 ORDER_TP=0.0
   ENUM_ORDER_PROPERTY_STRING Count=3
     0 ORDER_SYMBOL=EURUSD
     1 ORDER_COMMENT=
     2 ORDER_EXTERNAL_ID=
   >>> positions: 0, orders: 1, history: 1
   >>> OnTrade(2)
   PositionCache added: 1311792104
   MonitorInterface<ENUM_POSITION_PROPERTY_INTEGER,ENUM_POSITION_PROPERTY_DOUBLE,ENUM_POSITION_PROPERTY_STRING>
   ENUM_POSITION_PROPERTY_INTEGER Count=9
     0 POSITION_TIME=2022.04.11 12:34:51
     1 POSITION_TYPE=POSITION_TYPE_BUY
     2 POSITION_MAGIC=0
     3 POSITION_IDENTIFIER=1311792104
     4 POSITION_TIME_MSC=2022.04.11 12:34:51'097
     5 POSITION_TIME_UPDATE=2022.04.11 12:34:51
     6 POSITION_TIME_UPDATE_MSC=2022.04.11 12:34:51'097
     7 POSITION_TICKET=1311792104
     8 POSITION_REASON=POSITION_REASON_CLIENT
   ENUM_POSITION_PROPERTY_DOUBLE Count=8
     0 POSITION_VOLUME=0.01
     1 POSITION_PRICE_OPEN=1.09218
     2 POSITION_PRICE_CURRENT=1.09214
     3 POSITION_SL=0.00000
     4 POSITION_TP=0.00000
     5 POSITION_COMMISSION=0.0
     6 POSITION_SWAP=0.00
     7 POSITION_PROFIT=-0.04
   ENUM_POSITION_PROPERTY_STRING Count=3
     0 POSITION_SYMBOL=EURUSD
     1 POSITION_COMMENT=
     2 POSITION_EXTERNAL_ID=
   OrderCache removed: 1311792104
   >>> positions: 1, orders: 0, history: 1
   >>> OnTrade(3)
   >>> positions: 1, orders: 0, history: 1
   >>> OnTrade(4)
   >>> positions: 1, orders: 0, history: 1
   >>> OnTrade(5)
   >>> positions: 1, orders: 0, history: 1
   >>> OnTrade(6)
   >>> positions: 1, orders: 0, history: 1
   >>> OnTrade(7)
   >>> positions: 1, orders: 0, history: 1
   
   2. close the existing position (market order added, position changed and closed, order moved to history)
   
   >>> OnTrade(8)
   PositionCache changed: 1311792104
   POSITION_PRICE_CURRENT: 1.09214 -> 1.09222
   POSITION_PROFIT: -0.04 -> 0.04
   OrderCache added: 1311796883
   MonitorInterface<ENUM_ORDER_PROPERTY_INTEGER,ENUM_ORDER_PROPERTY_DOUBLE,ENUM_ORDER_PROPERTY_STRING>
   ENUM_ORDER_PROPERTY_INTEGER Count=14
     0 ORDER_TIME_SETUP=2022.04.11 12:39:55
     1 ORDER_TIME_EXPIRATION=1970.01.01 00:00:00
     2 ORDER_TIME_DONE=1970.01.01 00:00:00
     3 ORDER_TYPE=ORDER_TYPE_SELL
     4 ORDER_TYPE_FILLING=ORDER_FILLING_FOK
     5 ORDER_TYPE_TIME=ORDER_TIME_GTC
     6 ORDER_STATE=ORDER_STATE_STARTED
     7 ORDER_MAGIC=0
     8 ORDER_POSITION_ID=1311792104
     9 ORDER_TIME_SETUP_MSC=2022.04.11 12:39:55'710
    10 ORDER_TIME_DONE_MSC=1970.01.01 00:00:00'000
    11 ORDER_POSITION_BY_ID=0
    12 ORDER_TICKET=1311796883
    13 ORDER_REASON=ORDER_REASON_CLIENT
   ENUM_ORDER_PROPERTY_DOUBLE Count=7
     0 ORDER_VOLUME_INITIAL=0.01
     1 ORDER_VOLUME_CURRENT=0.01
     2 ORDER_PRICE_OPEN=1.09222
     3 ORDER_PRICE_CURRENT=1.09222
     4 ORDER_PRICE_STOPLIMIT=0.0
     5 ORDER_SL=0.0
     6 ORDER_TP=0.0
   ENUM_ORDER_PROPERTY_STRING Count=3
     0 ORDER_SYMBOL=EURUSD
     1 ORDER_COMMENT=
     2 ORDER_EXTERNAL_ID=
   HistoryOrderCache added: 1311796883
   MonitorInterface<ENUM_ORDER_PROPERTY_INTEGER,ENUM_ORDER_PROPERTY_DOUBLE,ENUM_ORDER_PROPERTY_STRING>
   ENUM_ORDER_PROPERTY_INTEGER Count=14
     0 ORDER_TIME_SETUP=2022.04.11 12:39:55
     1 ORDER_TIME_EXPIRATION=1970.01.01 00:00:00
     2 ORDER_TIME_DONE=2022.04.11 12:39:55
     3 ORDER_TYPE=ORDER_TYPE_SELL
     4 ORDER_TYPE_FILLING=ORDER_FILLING_FOK
     5 ORDER_TYPE_TIME=ORDER_TIME_GTC
     6 ORDER_STATE=ORDER_STATE_FILLED
     7 ORDER_MAGIC=0
     8 ORDER_POSITION_ID=1311792104
     9 ORDER_TIME_SETUP_MSC=2022.04.11 12:39:55'710
    10 ORDER_TIME_DONE_MSC=2022.04.11 12:39:55'711
    11 ORDER_POSITION_BY_ID=0
    12 ORDER_TICKET=1311796883
    13 ORDER_REASON=ORDER_REASON_CLIENT
   ENUM_ORDER_PROPERTY_DOUBLE Count=7
     0 ORDER_VOLUME_INITIAL=0.01
     1 ORDER_VOLUME_CURRENT=0.0
     2 ORDER_PRICE_OPEN=1.09222
     3 ORDER_PRICE_CURRENT=1.09222
     4 ORDER_PRICE_STOPLIMIT=0.0
     5 ORDER_SL=0.0
     6 ORDER_TP=0.0
   ENUM_ORDER_PROPERTY_STRING Count=3
     0 ORDER_SYMBOL=EURUSD
     1 ORDER_COMMENT=
     2 ORDER_EXTERNAL_ID=
   >>> positions: 1, orders: 1, history: 2
   >>> OnTrade(9)
   PositionCache removed: 1311792104
   OrderCache removed: 1311796883
   >>> positions: 0, orders: 0, history: 2

*/