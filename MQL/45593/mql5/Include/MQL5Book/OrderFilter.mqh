//+------------------------------------------------------------------+
//|                                                  OrderFilter.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/OrderMonitor.mqh>
#include <MQL5Book/TradeFilter.mqh>

class OrderFilter: public TradeFilter<OrderMonitor,
   ENUM_ORDER_PROPERTY_INTEGER,
   ENUM_ORDER_PROPERTY_DOUBLE,
   ENUM_ORDER_PROPERTY_STRING>
{
protected:
   virtual int total() const override
   {
      return OrdersTotal();
   }
   virtual ulong get(const int i) const override
   {
      return OrderGetTicket(i);
   }
};

class HistoryOrderFilter: public TradeFilter<OrderMonitor,
   ENUM_ORDER_PROPERTY_INTEGER,
   ENUM_ORDER_PROPERTY_DOUBLE,
   ENUM_ORDER_PROPERTY_STRING>
{
protected:
   virtual int total() const override
   {
      return HistoryOrdersTotal();
   }
   virtual ulong get(const int i) const override
   {
      return HistoryOrderGetTicket(i);
   }
};
//+------------------------------------------------------------------+
