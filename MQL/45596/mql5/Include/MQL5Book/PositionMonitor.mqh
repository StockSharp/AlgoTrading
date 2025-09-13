//+------------------------------------------------------------------+
//|                                              PositionMonitor.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/TradeBaseMonitor.mqh>
#include <MQL5Book/MqlError.mqh>

//+------------------------------------------------------------------+
//| Stateless class with special handling of position properties     |
//| SUPER should be MonitorInterface<ENUM_POSITION_PROPERTY_INTEGER, |
//| ENUM_POSITION_PROPERTY_DOUBLE,ENUM_POSITION_PROPERTY_STRING>     |
//+------------------------------------------------------------------+
template<typename SUPER>
class PositionMonitorInterface: public SUPER
{
// NB: MQL5 does not support yet typedefs except for function pointers
// typedef MonitorInterface<ENUM_POSITION_PROPERTY_INTEGER,ENUM_POSITION_PROPERTY_DOUBLE,ENUM_POSITION_PROPERTY_STRING> super;
// could use define instead
// #define SUPER MonitorInterface<ENUM_POSITION_PROPERTY_INTEGER,ENUM_POSITION_PROPERTY_DOUBLE,ENUM_POSITION_PROPERTY_STRING>
public:
   // explain properties according to subtypes
   virtual string stringify(const long v, const ENUM_POSITION_PROPERTY_INTEGER property) const override
   {
      switch(property)
      {
         case POSITION_TYPE:
            return enumstr<ENUM_POSITION_TYPE>(v);
         case POSITION_REASON:
            return enumstr<ENUM_POSITION_REASON>(v);
         
         case POSITION_TIME:
         case POSITION_TIME_UPDATE:
            return TimeToString(v, TIME_DATE|TIME_SECONDS);
         
         case POSITION_TIME_MSC:
         case POSITION_TIME_UPDATE_MSC:
            return STR_TIME_MSC(v);
      }
      
      return (string)v;
   }

   virtual string stringify(const ENUM_POSITION_PROPERTY_DOUBLE property, const string format = NULL) const override
   {
      if(format == NULL)
      {
         if(property == POSITION_PRICE_OPEN || property == POSITION_PRICE_CURRENT
            || property == POSITION_SL || property == POSITION_TP)
         {
            const int digits = (int)SymbolInfoInteger(PositionGetString(POSITION_SYMBOL), SYMBOL_DIGITS);
            return DoubleToString(get(property), digits);
         }
         else if(property == POSITION_SWAP || property == POSITION_PROFIT)
         {
            return SUPER::stringify(property, "%.2f");
         }
      }
      return SUPER::stringify(property, format);
   }

   // all the following methods are required to eliminate compiler warning:
   // deprecated behavior, hidden method calling will be disabled in a future MQL compiler version
   
   virtual string stringify(const ENUM_POSITION_PROPERTY_INTEGER property) const override
   {
      return SUPER::stringify(property);
   }

   virtual string stringify(const ENUM_POSITION_PROPERTY_STRING property) const override
   {
      return SUPER::stringify(property);
   }

   virtual string stringify(const int i) const override
   {
      return SUPER::stringify(i);
   }
};

//+------------------------------------------------------------------+
//| Main class for reading position properties                       |
//+------------------------------------------------------------------+
class PositionMonitor:
   public PositionMonitorInterface<MonitorInterface<ENUM_POSITION_PROPERTY_INTEGER,
   ENUM_POSITION_PROPERTY_DOUBLE,ENUM_POSITION_PROPERTY_STRING>>
{
public:
   const ulong ticket;
   PositionMonitor(const ulong t): ticket(t)
   {
      if(!PositionSelectByTicket(ticket))
      {
         PrintFormat("Error: PositionSelectByTicket(%lld) failed: %s", ticket, E2S(_LastError));
      }
      else
      {
         ready = true;
      }
   }
   
   virtual bool refresh() override
   {
      ready = PositionSelectByTicket(ticket);
      return ready;
   }
   
   virtual long get(const ENUM_POSITION_PROPERTY_INTEGER property) const override
   {
      return PositionGetInteger(property);
   }

   virtual double get(const ENUM_POSITION_PROPERTY_DOUBLE property) const override
   {
      return PositionGetDouble(property);
   }

   virtual string get(const ENUM_POSITION_PROPERTY_STRING property) const override
   {
      return PositionGetString(property);
   }

   virtual long get(const int property, const long) const override
   {
      return PositionGetInteger((ENUM_POSITION_PROPERTY_INTEGER)property);
   }

   virtual double get(const int property, const double) const override
   {
      return PositionGetDouble((ENUM_POSITION_PROPERTY_DOUBLE)property);
   }

   virtual string get(const int property, const string) const override
   {
      return PositionGetString((ENUM_POSITION_PROPERTY_STRING)property);
   }

};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
/* example
void OnStart()
{
   PositionMonitor m(0);
   m.list2log<ENUM_POSITION_PROPERTY_INTEGER>();
   m.list2log<ENUM_POSITION_PROPERTY_DOUBLE>();
   m.list2log<ENUM_POSITION_PROPERTY_STRING>();
}
*/
//+------------------------------------------------------------------+
