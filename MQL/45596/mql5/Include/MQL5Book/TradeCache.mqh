//+------------------------------------------------------------------+
//|                                                   TradeState.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/TradeState.mqh>
#include <MQL5Book/PositionFilter.mqh>
#include <MQL5Book/OrderFilter.mqh>
#include <MQL5Book/DealFilter.mqh>
#include <MQL5Book/AutoPtr.mqh>
#include <MQL5Book/ArrayUtils.mqh>
#include <MQL5Book/TypeName.mqh>

//+------------------------------------------------------------------+
//| Base class with array of objects storing trade properties for    |
//| orders/deals/positions.                                          |
//| T should be a TradeState class, F should be a TradeFilter,       |
//| E is a property with identifier (usually SOME_TICKET) from ENUM  |
//+------------------------------------------------------------------+
template<typename T,typename F,typename E>
class TradeCache
{
   AutoPtr<T> data[];
   const E property;
   const int NOT_FOUND_ERROR;

   static bool isNull(const AutoPtr<T> &element)
   {
      return element[] == NULL;
   }

public:
   TradeCache(const E id, const int error): property(id), NOT_FOUND_ERROR(error) { }
   
   virtual string rtti() const
   {
      return TYPENAME(this);
   }
   
   virtual void onAdded(const T &state)
   {
      Print(rtti(), " added: ", state.get(property));
      #ifdef PRINT_DETAILS
      state.print();
      #endif
   }
   
   virtual bool onRemoved(const T &state)
   {
      Print(rtti(), " removed: ", state.get(property));
      return true; // please, wipe out the reference (false would keep it)
   }
   
   virtual void onUpdated(T &state, const int &changes[], const bool unexpected = false)
   {
      const ulong ticket = state.get(property);
      Print(rtti(), " changed: ", ticket, (unexpected ? " unexpectedly" : ""));
      for(int k = 0; k < ArraySize(changes); ++k)
      {
         switch(T::TradeState::type(changes[k]))
         {
         case PROP_TYPE_INTEGER:
            Print(T::label(changes[k]), ": ",
               state.stringify(changes[k]), " -> ",
               state.stringifyRaw(changes[k]));
               break;
         case PROP_TYPE_DOUBLE:
            Print(T::label(changes[k]), ": ",
               state.stringify(changes[k]), " -> ",
               state.stringifyRaw(changes[k]));
               break;
         case PROP_TYPE_STRING:
            Print(T::label(changes[k]), ": ",
               state.stringify(changes[k]), " -> ",
               state.stringifyRaw(changes[k]));
               break;
         }
      }
   }
   
   int size() const
   {
      return ArraySize(data);
   }
   
   T *operator[](int i) const
   {
      return data[i][]; // return pure pointer (T*) from inside AutoPtr object
   }
   
   void reset()
   {
      ArrayResize(data, 0); // this will call destructors of objects on pointers
   }

   void scan(F &f)
   {
      const int existedBefore = ArraySize(data);
      
      ulong tickets[];
      ArrayResize(tickets, existedBefore);
      for(int i = 0; i < existedBefore; ++i)
      {
         tickets[i] = data[i][].get(property);
      }
   
      ulong objects[];
      f.select(objects);
      // if(_LastError != 0) Print(rtti(), " scan error: ", E2S(_LastError));
      for(int i = 0, j; i < ArraySize(objects); ++i)
      {
         const ulong ticket = objects[i];
         for(j = 0; j < existedBefore; ++j)
         {
            if(tickets[j] == ticket)
            {
               tickets[j] = 0; // mark as found
               break;
            }
         }
         
         if(j == existedBefore) // not found
         {
            const T *ptr = new T(ticket);
            PUSH(data, ptr);
            onAdded(*ptr);
         }
         else
         {
            ResetLastError();
            int changes[];
            if(data[j][].getChanges(changes))
            {
               onUpdated(data[j][], changes);
               data[j][].update();
            }
            if(_LastError)
            {
               PrintFormat("%s: %lld (%s)", rtti(), ticket, E2S(_LastError));
               if(_LastError == NOT_FOUND_ERROR) // for example, ERR_TRADE_POSITION_NOT_FOUND
               {
                  if(onRemoved(data[j][]))
                  {
                     data[j] = NULL;             // free up the object and array element
                  }
               }
            }
         }
      }
      
      for(int j = 0; j < existedBefore; ++j)
      {
         if(tickets[j] == 0) continue; // skip already processed element
         
         // this ticket was not found anymore, most likely deleted
         int changes[];
         ResetLastError();
         if(data[j][].getChanges(changes))
         {
            if(_LastError == NOT_FOUND_ERROR) // for example, ERR_TRADE_POSITION_NOT_FOUND
            {
               if(onRemoved(data[j][]))
               {
                  data[j] = NULL;             // free up the object and array element
               }
               continue;
            }
            
            // NB! We should not normally fall down here
            PrintFormat("Unexpected ticket: %lld (%s) %s", tickets[j], E2S(_LastError), rtti());
            onUpdated(data[j][], changes, true);
            data[j][].update();
         }
         else
         {
            #ifdef PRINT_DETAILS
            PrintFormat("Orphaned element: %lld (%s) %s", tickets[j], E2S(_LastError), rtti());
            #endif
         }
      }
      
      ArrayPurger<AutoPtr<T>> p(data, isNull);
   }
};

//+------------------------------------------------------------------+
//| Concrete class with array of positions and their properties      |
//+------------------------------------------------------------------+
class PositionCache: public TradeCache<PositionState,PositionFilter,ENUM_POSITION_PROPERTY_INTEGER>
{
public:
   PositionCache(const ENUM_POSITION_PROPERTY_INTEGER selector = POSITION_TICKET,
      const int error = ERR_TRADE_POSITION_NOT_FOUND): TradeCache(selector, error) { }
   virtual string rtti() const override
   {
      return TYPENAME(this);
   }
};

//+------------------------------------------------------------------+
//| Concrete class with array of active orders and their properties  |
//+------------------------------------------------------------------+
class OrderCache: public TradeCache<OrderState,OrderFilter,ENUM_ORDER_PROPERTY_INTEGER>
{
public:
   OrderCache(const ENUM_ORDER_PROPERTY_INTEGER selector = ORDER_TICKET,
      const int error = ERR_TRADE_ORDER_NOT_FOUND): TradeCache(selector, error) { }
   virtual string rtti() const override
   {
      return TYPENAME(this);
   }
};

//+------------------------------------------------------------------+
//| Concrete class with array of history orders and their properties |
//+------------------------------------------------------------------+
class HistoryOrderCache: public TradeCache<HistoryOrderState,HistoryOrderFilter,ENUM_ORDER_PROPERTY_INTEGER>
{
public:
   HistoryOrderCache(const ENUM_ORDER_PROPERTY_INTEGER selector = ORDER_TICKET,
      const int error = ERR_TRADE_ORDER_NOT_FOUND): TradeCache(selector, error) { }
   virtual string rtti() const override
   {
      return TYPENAME(this);
   }
};

//+------------------------------------------------------------------+
