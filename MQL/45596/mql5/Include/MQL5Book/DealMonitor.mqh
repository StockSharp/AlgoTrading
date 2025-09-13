//+------------------------------------------------------------------+
//|                                                  DealMonitor.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/TradeBaseMonitor.mqh>

//+------------------------------------------------------------------+
//| Stateless class with special handling of deal properties         |
//| SUPER should be MonitorInterface<ENUM_DEAL_PROPERTY_INTEGER,     |
//| ENUM_DEAL_PROPERTY_DOUBLE,ENUM_DEAL_PROPERTY_STRING>             |
//+------------------------------------------------------------------+
template<typename SUPER>
class DealMonitorInterface: public SUPER
{
public:
   // explain properties according to subtypes
   virtual string stringify(const long v, const ENUM_DEAL_PROPERTY_INTEGER property) const override
   {
      switch(property)
      {
         case DEAL_TYPE:
            return enumstr<ENUM_DEAL_TYPE>(v);
         case DEAL_ENTRY:
            return enumstr<ENUM_DEAL_ENTRY>(v);
         case DEAL_REASON:
            return enumstr<ENUM_DEAL_REASON>(v);
         
         case DEAL_TIME:
            return TimeToString(v, TIME_DATE|TIME_SECONDS);
         
         case DEAL_TIME_MSC:
            return STR_TIME_MSC(v);
      }
      
      return (string)v;
   }

   // all the following methods are required to eliminate compiler warning:
   // deprecated behavior, hidden method calling will be disabled in a future MQL compiler version
   
   virtual string stringify(const ENUM_DEAL_PROPERTY_INTEGER property) const override
   {
      return SUPER::stringify(property);
   }

   virtual string stringify(const ENUM_DEAL_PROPERTY_DOUBLE property, const string format = NULL) const override
   {
      return SUPER::stringify(property, format);
   }
   
   virtual string stringify(const ENUM_DEAL_PROPERTY_STRING property) const override
   {
      return SUPER::stringify(property);
   }
   
   virtual string stringify(const int i) const override
   {
      return SUPER::stringify(i);
   }
};

//+------------------------------------------------------------------+
//| Main class for reading deal properties                           |
//+------------------------------------------------------------------+
class DealMonitor:
   public DealMonitorInterface<MonitorInterface<ENUM_DEAL_PROPERTY_INTEGER,
   ENUM_DEAL_PROPERTY_DOUBLE,ENUM_DEAL_PROPERTY_STRING>>
{
   bool historyDealSelectWeak(const ulong t) const
   {
      return (((HistoryDealGetInteger(t, DEAL_TICKET) == t) ||
         (HistorySelect(0, LONG_MAX) && (HistoryDealGetInteger(t, DEAL_TICKET) == t))));
   }
public:
   const ulong ticket;
   DealMonitor(const ulong t): ticket(t)
   {
      if(!historyDealSelectWeak(ticket))
      {
         PrintFormat("Error: HistoryDealSelect(%lld) failed", ticket);
      }
      else
      {
         ready = true;
      }
   }
   
   virtual bool refresh() override
   {
      ready = historyDealSelectWeak(ticket);
      return ready;
   }

   virtual long get(const ENUM_DEAL_PROPERTY_INTEGER property) const override
   {
      return HistoryDealGetInteger(ticket, property);
   }

   virtual double get(const ENUM_DEAL_PROPERTY_DOUBLE property) const override
   {
      return HistoryDealGetDouble(ticket, property);
   }

   virtual string get(const ENUM_DEAL_PROPERTY_STRING property) const override
   {
      return HistoryDealGetString(ticket, property);
   }

   virtual long get(const int property, const long) const override
   {
      return HistoryDealGetInteger(ticket, (ENUM_DEAL_PROPERTY_INTEGER)property);
   }

   virtual double get(const int property, const double) const override
   {
      return HistoryDealGetDouble(ticket, (ENUM_DEAL_PROPERTY_DOUBLE)property);
   }

   virtual string get(const int property, const string) const override
   {
      return HistoryDealGetString(ticket, (ENUM_DEAL_PROPERTY_STRING)property);
   }

};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
/* example
void OnStart()
{
   DealMonitor m(0);
   m.list2log<ENUM_DEAL_PROPERTY_INTEGER>();
   m.list2log<ENUM_DEAL_PROPERTY_DOUBLE>();
   m.list2log<ENUM_DEAL_PROPERTY_STRING>();
}
*/
//+------------------------------------------------------------------+
