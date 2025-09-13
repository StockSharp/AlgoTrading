//+------------------------------------------------------------------+
//|                                                 OrderMonitor.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/TradeBaseMonitor.mqh>
#include <MQL5Book/MqlError.mqh>

//+------------------------------------------------------------------+
//| Stateless class with special handling of order properties        |
//| SUPER should be MonitorInterface<ENUM_ORDER_PROPERTY_INTEGER,    |
//| ENUM_ORDER_PROPERTY_DOUBLE,ENUM_ORDER_PROPERTY_STRING>           |
//+------------------------------------------------------------------+
template<typename SUPER>
class OrderMonitorInterface: public SUPER
{
public:
   // explain properties according to subtypes
   virtual string stringify(const long v, const ENUM_ORDER_PROPERTY_INTEGER property) const override
   {
      switch(property)
      {
         case ORDER_TYPE:
            return enumstr<ENUM_ORDER_TYPE>(v);
         case ORDER_STATE:
            return enumstr<ENUM_ORDER_STATE>(v);
         case ORDER_TYPE_FILLING:
            return enumstr<ENUM_ORDER_TYPE_FILLING>(v);
         case ORDER_TYPE_TIME:
            return enumstr<ENUM_ORDER_TYPE_TIME>(v);
         case ORDER_REASON:
            return enumstr<ENUM_ORDER_REASON>(v);
         
         case ORDER_TIME_SETUP:
         case ORDER_TIME_EXPIRATION:
         case ORDER_TIME_DONE:
            return TimeToString(v, TIME_DATE | TIME_SECONDS);
         
         case ORDER_TIME_SETUP_MSC:
         case ORDER_TIME_DONE_MSC:
            return STR_TIME_MSC(v);
      }
      
      return (string)v;
   }
   
   // all the following methods are required to eliminate compiler warning:
   // deprecated behavior, hidden method calling will be disabled in a future MQL compiler version
   
   virtual string stringify(const ENUM_ORDER_PROPERTY_INTEGER property) const override
   {
      return SUPER::stringify(property);
   }

   virtual string stringify(const ENUM_ORDER_PROPERTY_DOUBLE property, const string format = NULL) const override
   {
      return SUPER::stringify(property, format);
   }
   
   virtual string stringify(const ENUM_ORDER_PROPERTY_STRING property) const override
   {
      return SUPER::stringify(property);
   }
   
   virtual string stringify(const int i) const override
   {
      return SUPER::stringify(i);
   }
};

//+------------------------------------------------------------------+
//| Main class for reading order properties                          |
//+------------------------------------------------------------------+
class OrderMonitor:
   public OrderMonitorInterface<MonitorInterface<ENUM_ORDER_PROPERTY_INTEGER,
   ENUM_ORDER_PROPERTY_DOUBLE,ENUM_ORDER_PROPERTY_STRING>>
{
protected:
   bool historyOrderSelectWeak(const ulong t) const
   {
      return (((HistoryOrderGetInteger(t, ORDER_TICKET) == t) ||
         (HistorySelect(0, LONG_MAX) && (HistoryOrderGetInteger(t, ORDER_TICKET) == t))));
   }
   bool history;

public:
   const ulong ticket;
   OrderMonitor(const ulong t): ticket(t), history(!OrderSelect(t))
   {
      if(history && !historyOrderSelectWeak(ticket))
      {
         PrintFormat("Error: OrderSelect(%lld) failed: %s", ticket, E2S(_LastError));
      }
      else
      {
         ResetLastError();
         ready = true;
      }
   }
   
   bool isHistory() const
   {
      return history;
   }
   
   virtual bool refresh() override
   {
      history = false;
      ready = OrderSelect(ticket) || (history = historyOrderSelectWeak(ticket));
      if(history) ResetLastError();
      return ready;
   }

   virtual long get(const ENUM_ORDER_PROPERTY_INTEGER property) const override
   {
      return history ? HistoryOrderGetInteger(ticket, property) : OrderGetInteger(property);
   }

   virtual double get(const ENUM_ORDER_PROPERTY_DOUBLE property) const override
   {
      return history ? HistoryOrderGetDouble(ticket, property) : OrderGetDouble(property);
   }

   virtual string get(const ENUM_ORDER_PROPERTY_STRING property) const override
   {
      return history ? HistoryOrderGetString(ticket, property) : OrderGetString(property);
   }

   virtual long get(const int property, const long) const override
   {
      return history ? HistoryOrderGetInteger(ticket, (ENUM_ORDER_PROPERTY_INTEGER)property) : OrderGetInteger((ENUM_ORDER_PROPERTY_INTEGER)property);
   }

   virtual double get(const int property, const double) const override
   {
      return history ? HistoryOrderGetDouble(ticket, (ENUM_ORDER_PROPERTY_DOUBLE)property) : OrderGetDouble((ENUM_ORDER_PROPERTY_DOUBLE)property);
   }

   virtual string get(const int property, const string)  const override
   {
      return history ? HistoryOrderGetString(ticket, (ENUM_ORDER_PROPERTY_STRING)property) : OrderGetString((ENUM_ORDER_PROPERTY_STRING)property);
   }
};

//+------------------------------------------------------------------+
//| Main class for reading active order properties                   |
//+------------------------------------------------------------------+
class ActiveOrderMonitor: public OrderMonitor
{
public:
   ActiveOrderMonitor(const ulong t): OrderMonitor(t)
   {
      if(history)
      {
         ready = false;
         history = false;
      }
   }
   
   virtual bool refresh() override
   {
      ready = OrderSelect(ticket);
      return ready;
   }
};

//+------------------------------------------------------------------+
//| Main class for reading order properties from history             |
//+------------------------------------------------------------------+
class HistoryOrderMonitor: public OrderMonitor
{
public:
   HistoryOrderMonitor(const ulong t): OrderMonitor(t) { }
   
   virtual bool refresh() override
   {
      history = true;
      ready = historyOrderSelectWeak(ticket);
      return ready;
   }
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
/* example
void OnStart()
{
   OrderMonitor m(0);
   m.list2log<ENUM_ORDER_PROPERTY_INTEGER>();
   m.list2log<ENUM_ORDER_PROPERTY_DOUBLE>();
   m.list2log<ENUM_ORDER_PROPERTY_STRING>();
}
*/
//+------------------------------------------------------------------+
