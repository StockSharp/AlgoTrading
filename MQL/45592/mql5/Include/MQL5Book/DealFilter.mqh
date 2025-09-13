//+------------------------------------------------------------------+
//|                                                   DealFilter.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/DealMonitor.mqh>
#include <MQL5Book/TradeFilter.mqh>

class DealFilter: public TradeFilter<DealMonitor,
   ENUM_DEAL_PROPERTY_INTEGER,
   ENUM_DEAL_PROPERTY_DOUBLE,
   ENUM_DEAL_PROPERTY_STRING>
{
protected:
   virtual int total() const override
   {
      return HistoryDealsTotal();
   }
   virtual ulong get(const int i) const override
   {
      return HistoryDealGetTicket(i);
   }
};
//+------------------------------------------------------------------+
