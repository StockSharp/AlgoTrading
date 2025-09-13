//+------------------------------------------------------------------+
//|                                             TradeBaseMonitor.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/EnumToArray.mqh>
#include <MQL5Book/TradeUtils.mqh>
#include <MQL5Book/Defines.mqh>
#include <MQL5Book/TypeName.mqh>

//+------------------------------------------------------------------+
//| Common names for any of triple ENUMs used for properties types   |
//+------------------------------------------------------------------+
enum PROP_TYPE
{
   PROP_TYPE_INTEGER,
   PROP_TYPE_DOUBLE,
   PROP_TYPE_STRING,
};

//+------------------------------------------------------------------+
//| Base class for orders/deals/positions monitors (props viewer)    |
//| Used by concrete monitors:                                       |
//|   #include <MQL5Book/PositionMonitor.mqh>                        |
//|   #include <MQL5Book/DealMonitor.mqh>                            |
//|   #include <MQL5Book/OrderMonitor.mqh>                           |
//| Types I, D, S stand for 3 ENUM-types of properties               |
//|   I - integers, D - doubles, S - strings                         |
//+------------------------------------------------------------------+
template<typename I,typename D,typename S>
class MonitorInterface
{
protected:
   bool ready;
   
   template<typename E>
   static bool detect(const int v)
   {
      ResetLastError();
      const string s = EnumToString((E)v); // resulting string is not used
      if(_LastError == 0) // only error code is important
      {
         return true;
      }
      return false;
   }

   template<typename E>
   static int boundary(const E dummy = (E)NULL)
   {
      int values[];
      const int n = EnumToArray(dummy, values, 0, 1000);
      ArraySort(values);
      return values[n - 1];
   }

public:

   class TradeState
   {
      static int indices[][2];
      static int j, d, s;
   public:
      const static int limit;
      
      static PROP_TYPE type(const int i)
      {
         return (PROP_TYPE)indices[i][0];
      }

      static int offset(const int i)
      {
         return indices[i][1];
      }
      
      static int size(const PROP_TYPE p)
      {
         switch(p)
         {
            case PROP_TYPE_INTEGER: return j;
            case PROP_TYPE_DOUBLE: return d;
            case PROP_TYPE_STRING: return s;
         }
         return 0;
      }
      
      static string enumname(const PROP_TYPE p)
      {
         switch(p)
         {
            case PROP_TYPE_INTEGER: return TYPENAME(I);
            case PROP_TYPE_DOUBLE: return TYPENAME(D);
            case PROP_TYPE_STRING: return TYPENAME(S);
         }
         return NULL;
      }
      
      static int calcIndices()
      {
         const int size = fmax(boundary<I>(),
            fmax(boundary<D>(), boundary<S>())) + 1;
         ArrayResize(indices, size);
         j = d = s = 0;
         for(int i = 0; i < size; ++i)
         {
            if(detect<I>(i))
            {
               indices[i][0] = PROP_TYPE_INTEGER;
               indices[i][1] = j++;
            }
            else if(detect<D>(i))
            {
               indices[i][0] = PROP_TYPE_DOUBLE;
               indices[i][1] = d++;
            }
            else if(detect<S>(i))
            {
               indices[i][0] = PROP_TYPE_STRING;
               indices[i][1] = s++;
            }
            else
            {
               Print("Unresolved int value as enum: ", i, " for ", TYPENAME(TradeState));
               indices[i][0] = -1;
               indices[i][1] = -1;
            }
         }
         
         return size;
      }
      
      long ulongs[];
      double doubles[];
      string strings[];
      const MonitorInterface *owner;
      
      TradeState(const MonitorInterface *ptr = NULL) : owner(ptr)
      {
         reset();
         cache();
      }

      template<typename T>
      void _get(const int e, T &value) const // overload with a reference of output value
      {
         if(owner)
         {
            value = owner.get(e, value);
         }
         else
         {
            switch(indices[e][0])
            {
            case PROP_TYPE_INTEGER: value = (T)ulongs[indices[e][1]]; break;
            case PROP_TYPE_DOUBLE: value = (T)doubles[indices[e][1]]; break;
            case PROP_TYPE_STRING: value = (T)strings[indices[e][1]]; break;
            }
         }
      }

      template<typename T>
      T _get(const int e) const // read cache directly
      {
         T value = (T)NULL;
         switch(indices[e][0])
         {
         case PROP_TYPE_INTEGER: value = (T)ulongs[indices[e][1]]; break;
         case PROP_TYPE_DOUBLE: value = (T)doubles[indices[e][1]]; break;
         case PROP_TYPE_STRING: value = (T)strings[indices[e][1]]; break;
         }
         return value;
      }

      template<typename T>
      void _set(const int e, T value) const // write cache directly
      {
         switch(indices[e][0])
         {
         case PROP_TYPE_INTEGER: ulongs[indices[e][1]] = (long)value; break;
         case PROP_TYPE_DOUBLE: doubles[indices[e][1]] = (double)value; break;
         case PROP_TYPE_STRING: strings[indices[e][1]] = (string)value; break;
         }
      }
      
      void cache()
      {
         for(int i = 0; i < limit; ++i)
         {
            switch(indices[i][0])
            {
            case PROP_TYPE_INTEGER: _get(i, ulongs[indices[i][1]]); break;
            case PROP_TYPE_DOUBLE: _get(i, doubles[indices[i][1]]); break;
            case PROP_TYPE_STRING: _get(i, strings[indices[i][1]]); break;
            }
         }
      }
      
      void reset()
      {
         ArrayResize(ulongs, j);
         ArrayResize(doubles, d);
         ArrayResize(strings, s);

         ArrayInitialize(ulongs, 0);
         ArrayInitialize(doubles, 0.0);
         ArrayResize(strings, 0);
         ArrayResize(strings, s);
      }
   };


   MonitorInterface(): ready(false) { }
   
   bool isReady() const
   {
      return ready;
   }
   
   template<typename E>
   static string enumstr(const long v)
   {
      return EnumToString((E)v);
   }

   // all properties of type enum E (slow straightforward version)
   template<typename E>
   void list2log() const
   {
      E e = (E)0; // disable warning 'possible use of uninitialized variable'
      int array[];
      const int n = EnumToArray(e, array, 0, USHORT_MAX);
      ResetLastError();
      Print(TYPENAME(E), " Count=", n);
      for(int i = 0; i < n; ++i)
      {
         e = (E)array[i];
         PrintFormat("% 3d %s=%s", i, EnumToString(e), stringify(e));
      }
   }

   // all properties of type enum I/D/S (fast indexed version)
   void list2log(const PROP_TYPE super) const
   {
      Print(TradeState::enumname(super), " Count=", TradeState::size(super));
      
      for(int k = 0, i = 0; k < TradeState::limit; ++k)
      {
         if(TradeState::type(k) == super)
         {
            switch(super)
            {
            case PROP_TYPE_INTEGER:
               PrintFormat("% 3d %s=%s", i++, EnumToString((I)k), stringify((I)k));
               break;
            case PROP_TYPE_DOUBLE:
               PrintFormat("% 3d %s=%s", i++, EnumToString((D)k), stringify((D)k));
               break;
            case PROP_TYPE_STRING:
               PrintFormat("% 3d %s=%s", i++, EnumToString((S)k), stringify((S)k));
               break;
            }
         }
      }
   }
   
   // on false check _LastError for one of the following errors:
   // - 4753 ERR_TRADE_POSITION_NOT_FOUND
   // - 4754 ERR_TRADE_ORDER_NOT_FOUND
   // - 4755 ERR_TRADE_DEAL_NOT_FOUND
   // also 'ready' should be dropped to false
   virtual bool refresh() = 0;

   virtual void print() const
   {
      Print(TYPENAME(this), (ready ? "" : " not ready!"));
      if(!ready) return;
      list2log(PROP_TYPE_INTEGER);
      list2log(PROP_TYPE_DOUBLE);
      list2log(PROP_TYPE_STRING);
      /*
      list2log<I>();
      list2log<D>();
      list2log<S>();
      */
   }

   virtual long get(const I property) const = 0;
   virtual double get(const D property) const = 0;
   virtual string get(const S property) const = 0;
   virtual long get(const int property, const long) const = 0;
   virtual double get(const int property, const double) const = 0;
   virtual string get(const int property, const string) const = 0;
   
   virtual string stringify(const long v, const I property) const = 0;

   virtual string stringify(const I property) const
   {
      return stringify(get(property), property);
   }
   
   virtual string stringify(const D property, const string format = NULL) const
   {
      if(format == NULL) return (string)get(property);
      return StringFormat(format, get(property));
   }

   virtual string stringify(const S property) const
   {
      return get(property);
   }

   virtual string stringify(const int i) const
   {
      switch(TradeState::type(i))
      {
      case PROP_TYPE_INTEGER: return stringify((I)i);
      case PROP_TYPE_DOUBLE: return stringify((D)i);
      case PROP_TYPE_STRING: return stringify((S)i);
      }
      return NULL;
   }
   
   static string label(const int i)
   {
      switch(TradeState::type(i))
      {
      case PROP_TYPE_INTEGER: return EnumToString((I)i);
      case PROP_TYPE_DOUBLE: return EnumToString((D)i);
      case PROP_TYPE_STRING: return EnumToString((S)i);
      }
      return NULL;
   }

   bool operator==(const MonitorInterface &that) const
   {
      for(int i = 0; i < TradeState::limit; ++i)
      {
         switch(TradeState::type(i))
         {
         case PROP_TYPE_INTEGER:
            if(this.get((I)i) != that.get((I)i)) return false;
            break;
         case PROP_TYPE_DOUBLE:
            if(!TU::Equal(this.get((D)i), that.get((D)i))) return false;
            break;
         case PROP_TYPE_STRING:
            if(this.get((S)i) != that.get((S)i)) return false;
            break;
         }
      }
      return true;
   }
   
   bool operator!=(const MonitorInterface &that) const
   {
      return !(this == that);
   }
   
   bool diff(const MonitorInterface &that, int &changes[])
   {
      int required[], r = 0;
      if(ArraySize(changes) > 0) // optionally can request only part of properties
      {
         ArraySwap(required, changes);
         ArraySort(required);
         ResetLastError();
      }
      const int rtotal = ArraySize(required);
      
      ArrayResize(changes, 0);
      for(int i = 0; i < TradeState::limit; ++i)
      {
         if(rtotal > 0)
         {
            if(required[r] == i)
            {
               ++r; // found, fall down to comparison and mark next property to find
            }
            else
            {
               continue; // skip if not required
            }
         }
         
         switch(TradeState::type(i))
         {
         case PROP_TYPE_INTEGER:
            if(this.get((I)i) != that.get((I)i))
            {
               PUSH(changes, i);
            }
            break;
         case PROP_TYPE_DOUBLE:
            if(!TU::Equal(this.get((D)i), that.get((D)i)))
            {
               PUSH(changes, i);
            }
            break;
         case PROP_TYPE_STRING:
            if(this.get((S)i) != that.get((S)i))
            {
               PUSH(changes, i);
            }
            break;
         }
      }
      return ArraySize(changes) > 0;
   }

   bool operator==(const TradeState &that) const
   {
      for(int i = 0; i < TradeState::limit; ++i)
      {
         switch(TradeState::type(i))
         {
         case PROP_TYPE_INTEGER:
            if(this.get((I)i) != that.ulongs[TradeState::offset(i)]) return false;
            break;
         case PROP_TYPE_DOUBLE:
            if(!TU::Equal(this.get((D)i), that.doubles[TradeState::offset(i)])) return false;
            break;
         case PROP_TYPE_STRING:
            if(this.get((S)i) != that.strings[TradeState::offset(i)]) return false;
            break;
         }
      }
      return true;
   }
   
   bool operator!=(const TradeState &that) const
   {
      return !(this == that);
   }
   
   bool diff(const TradeState &that, int &changes[])
   {
      ArrayResize(changes, 0);
      for(int i = 0; i < TradeState::limit; ++i)
      {
         switch(TradeState::type(i))
         {
         case PROP_TYPE_INTEGER:
            if(this.get((I)i) != that.ulongs[TradeState::offset(i)])
            {
               PUSH(changes, i);
            }
            break;
         case PROP_TYPE_DOUBLE:
            if(!TU::Equal(this.get((D)i), that.doubles[TradeState::offset(i)]))
            {
               PUSH(changes, i);
            }
            break;
         case PROP_TYPE_STRING:
            if(this.get((S)i) != that.strings[TradeState::offset(i)])
            {
               PUSH(changes, i);
            }
            break;
         }
      }
      return ArraySize(changes) > 0;
   }

};
//+------------------------------------------------------------------+
template<typename I,typename D,typename S>
static int MonitorInterface::TradeState::indices[][2];
template<typename I,typename D,typename S>
static int MonitorInterface::TradeState::j, MonitorInterface::TradeState::d, MonitorInterface::TradeState::s;
template<typename I,typename D,typename S>
const static int MonitorInterface::TradeState::limit = MonitorInterface::TradeState::calcIndices();
