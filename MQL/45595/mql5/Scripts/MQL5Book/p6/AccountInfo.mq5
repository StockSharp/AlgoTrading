//+------------------------------------------------------------------+
//|                                                  AccountInfo.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/AccountMonitor.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   AccountMonitor m;
   m.list2log<ENUM_ACCOUNT_INFO_INTEGER>();
   m.list2log<ENUM_ACCOUNT_INFO_DOUBLE>();
   m.list2log<ENUM_ACCOUNT_INFO_STRING>();
}
//+------------------------------------------------------------------+
/*
   example output:

      ENUM_ACCOUNT_INFO_INTEGER Count=10
        0 ACCOUNT_LOGIN=30000003
        1 ACCOUNT_TRADE_MODE=ACCOUNT_TRADE_MODE_DEMO
        2 ACCOUNT_TRADE_ALLOWED=true
        3 ACCOUNT_TRADE_EXPERT=true
        4 ACCOUNT_LEVERAGE=100
        5 ACCOUNT_MARGIN_SO_MODE=ACCOUNT_STOPOUT_MODE_PERCENT
        6 ACCOUNT_LIMIT_ORDERS=200
        7 ACCOUNT_MARGIN_MODE=ACCOUNT_MARGIN_MODE_RETAIL_HEDGING
        8 ACCOUNT_CURRENCY_DIGITS=2
        9 ACCOUNT_FIFO_CLOSE=false
      ENUM_ACCOUNT_INFO_DOUBLE Count=14
        0 ACCOUNT_BALANCE=10000.00
        1 ACCOUNT_CREDIT=0.00
        2 ACCOUNT_PROFIT=-78.76
        3 ACCOUNT_EQUITY=9921.24
        4 ACCOUNT_MARGIN=1000.00
        5 ACCOUNT_MARGIN_FREE=8921.24
        6 ACCOUNT_MARGIN_LEVEL=992.12
        7 ACCOUNT_MARGIN_SO_CALL=50.00
        8 ACCOUNT_MARGIN_SO_SO=30.00
        9 ACCOUNT_MARGIN_INITIAL=0.00
       10 ACCOUNT_MARGIN_MAINTENANCE=0.00
       11 ACCOUNT_ASSETS=0.00
       12 ACCOUNT_LIABILITIES=0.00
       13 ACCOUNT_COMMISSION_BLOCKED=0.00
      ENUM_ACCOUNT_INFO_STRING Count=4
        0 ACCOUNT_NAME=Vincent Silver
        1 ACCOUNT_COMPANY=MetaQuotes Ltd.
        2 ACCOUNT_SERVER=MetaQuotes-Demo
        3 ACCOUNT_CURRENCY=USD

*/
//+------------------------------------------------------------------+
