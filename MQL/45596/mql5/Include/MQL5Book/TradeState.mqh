//+------------------------------------------------------------------+
//|                                                   TradeState.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/PositionMonitor.mqh>
#include <MQL5Book/OrderMonitor.mqh>
#include <MQL5Book/DealMonitor.mqh>

//+------------------------------------------------------------------+
//| In-memory storage for orders/deals/positions with all properties |
//| Type M should be one of monitors:                                |
//|   #include <MQL5Book/PositionMonitor.mqh>                        |
//|   #include <MQL5Book/DealMonitor.mqh>                            |
//|   #include <MQL5Book/OrderMonitor.mqh>                           |
//| Types I, D, S stand for 3 ENUM-types of properties               |
//|   I - integers, D - doubles, S - strings                         |
//+------------------------------------------------------------------+
template<typename M,typename I,typename D,typename S>
class TradeBaseState: public M
{
   M::TradeState state;
   bool cached;
   
public:
   TradeBaseState(const ulong t) : M(t), state(&this), cached(ready)
   {
   }
   
   void passthrough(const bool b)   // enable/disable caching ad hoc
   {
      cached = b;
   }
   
   string stringifyRaw(const int i) // get property 'i' as a string bypassing the cache
   {
      const bool previous = cached;
      cached = false;
      const string s = stringify(i);
      cached = previous;
      return s;
   }
   
   bool update()
   {
      if(refresh())
      {
         cached = false; // disable reading from cache
         state.cache();  // read real properties into cache
         cached = true;  // enable cache back
         return true;
      }
      return false;
   }
   
   // NB! The 'ready' state change (when element becomes not selectable anymore)
   // will happen only once from true to false during the calls to 'getChanges'/'isChanged'.
   // The 'ready' state is not a normal property. Changes in all standard properties are applied
   // to the cache only after explicit 'update' method call. Until 'update' is called,
   // all successive invocations of 'getChanges'/'isChanged' will detect the same changes
   // (and probably new additional).
   
   bool getChanges(int &changes[])
   {
      const bool previous = ready;
      if(refresh())
      {
         // object selected = ready to read and compare
         cached = false;
         const bool result = M::diff(state, changes);
         cached = true;
         return result;
      }
      // became not ready = removed or gone out of the selected context
      return previous != ready; // ready changed
   }
   
   bool isChanged()
   {
      const bool previous = ready;
      if(refresh())
      {
         // object selected = ready to read and compare
         cached = false;
         const bool result = this == state; // use overloaded operator '=='
         cached = true;
         return result;
      }
      // became not ready = removed
      return previous != ready; // ready changed
   }
   
   void reset()
   {
      state.reset();
   }
   
   virtual long get(const I property) const override
   {
      return cached ? state.ulongs[M::TradeState::offset(property)] : M::get(property);
   }

   virtual double get(const D property) const override
   {
      return cached ? state.doubles[M::TradeState::offset(property)] : M::get(property);
   }

   virtual string get(const S property) const override
   {
      return cached ? state.strings[M::TradeState::offset(property)] : M::get(property);
   }

   virtual long get(const int property, const long) const override
   {
      return cached ? state.ulongs[M::TradeState::offset(property)] : M::get((I)property);
   }

   virtual double get(const int property, const double) const override
   {
      return cached ? state.doubles[M::TradeState::offset(property)] : M::get((D)property);
   }

   virtual string get(const int property, const string) const override
   {
      return cached ? state.strings[M::TradeState::offset(property)] : M::get((S)property);
   }
};

//+------------------------------------------------------------------+
//| Concrete in-memory storage for orders properties                 |
//+------------------------------------------------------------------+
class OrderState: public TradeBaseState<ActiveOrderMonitor,
   ENUM_ORDER_PROPERTY_INTEGER,
   ENUM_ORDER_PROPERTY_DOUBLE,
   ENUM_ORDER_PROPERTY_STRING>
{
public:
   OrderState(const long t): TradeBaseState(t) { }
};

class HistoryOrderState: public TradeBaseState<HistoryOrderMonitor,
   ENUM_ORDER_PROPERTY_INTEGER,
   ENUM_ORDER_PROPERTY_DOUBLE,
   ENUM_ORDER_PROPERTY_STRING>
{
public:
   HistoryOrderState(const long t): TradeBaseState(t) { }
};

//+------------------------------------------------------------------+
//| Concrete in-memory storage for deal properties                   |
//+------------------------------------------------------------------+
class DealState: public TradeBaseState<DealMonitor,
   ENUM_DEAL_PROPERTY_INTEGER,
   ENUM_DEAL_PROPERTY_DOUBLE,
   ENUM_DEAL_PROPERTY_STRING>
{
public:
   DealState(const long t): TradeBaseState(t) { }
};

//+------------------------------------------------------------------+
//| Concrete in-memory storage for positions properties              |
//+------------------------------------------------------------------+
class PositionState: public TradeBaseState<PositionMonitor,
   ENUM_POSITION_PROPERTY_INTEGER,
   ENUM_POSITION_PROPERTY_DOUBLE,
   ENUM_POSITION_PROPERTY_STRING>
{
public:
   PositionState(const long t): TradeBaseState(t) { }
};
//+------------------------------------------------------------------+
