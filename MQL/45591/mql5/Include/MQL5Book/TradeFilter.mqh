//+------------------------------------------------------------------+
//|                                                  TradeFilter.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/MapArray.mqh>
#include <MQL5Book/QuickSortTm.mqh>
#include <MQL5Book/Defines.mqh>
#include <MQL5Book/IS.mqh>

//+------------------------------------------------------------------+
//| Class for collecting orders/deals/positions and their properties |
//| if they match specific conditions.                               |
//| Type T should be one of:                                         |
//|   #include <MQL5Book/PositionMonitor.mqh>                        |
//|   #include <MQL5Book/DealMonitor.mqh>                            |
//|   #include <MQL5Book/OrderMonitor.mqh>                           |
//+------------------------------------------------------------------+
template<typename T,typename I,typename D,typename S>
class TradeFilter
{
protected:
   MapArray<ENUM_ANY,long> longs;
   MapArray<ENUM_ANY,double> doubles;
   MapArray<ENUM_ANY,string> strings;
   MapArray<ENUM_ANY,IS> conditions;

   static bool eps(const double v1, const double v2)
   {
      return fabs(v1 - v2) < DBL_EPSILON * fmax(v1, v2);
   }
   
   template<typename V>
   static bool eps(const V v1, const V v2)
   {
      return false;
   }

   template<typename V>
   static bool equal(const V v1, const V v2)
   {
      return v1 == v2 || eps(v1, v2);
   }

   template<typename V>
   static bool greater(const V v1, const V v2)
   {
      return v1 > v2;
   }
   
   static bool bitwise_or(const long v1, const long v2)
   {
      return (((1 << v1) & v2) != 0);
   }

   static bool bitwise_or(const string v1, const string v2)
   {
      return false;
   }

   template<typename V>
   static bool bitwise_or(const V v1, const V v2)
   {
      return false;
   }
   
   static bool equal(const string v1, const string v2)
   {
      if(StringFind(v2, "*") > -1)
      {
         int previous = 0;
         string words[];
         const int n = StringSplit(v2, '*', words);
         for(int i = 0; i < n; ++i)
         {
            if(StringLen(words[i]) == 0) continue;
            int index = StringFind(v1, words[i], previous);
            if(index == -1)
            {
               return false;
            }
            previous = index + StringLen(words[i]);
         }
         return true;
      }
      return v1 == v2;
   }

   template<typename V>
   bool match(const T &m, const MapArray<ENUM_ANY,V> &data) const
   {
      static const V type = (V)NULL;
      int or_totals = 0, or_matches = 0;
      for(int i = 0; i < data.getSize(); ++i)
      {
         const ENUM_ANY key = data.getKey(i);
         switch(conditions[key])
         {
         case EQUAL_OR_ZERO:
            if(equal(m.get(key, type), type))
            {
               continue; // zero is acceptable
            }
            // otherwise fallthrough
         case EQUAL:
            if(!equal(m.get(key, type), data.getValue(i)))
            {
               return false;
            }
            break;
         case NOT_EQUAL:
            if(equal(m.get(key, type), data.getValue(i)))
            {
               return false;
            }
            break;
         case OR_EQUAL:
            or_totals++;
            if(equal(m.get(key, type), data.getValue(i)))
            {
               or_matches++;
            }
            break;
         case OR_BITWISE:
            if(!bitwise_or(m.get(key, type), data.getValue(i)))
            {
               return false;
            }
            break;
         case GREATER:
            if(!greater(m.get(key, type), data.getValue(i)))
            {
               return false;
            }
            break;
         case LESS:
            if(greater(m.get(key, type), data.getValue(i)))
            {
               return false;
            }
            break;
         }
      }
      
      if(or_totals > 0) return or_matches > 0;
      
      return true;
   }

   template<typename T1,typename U>
   void sortTuple(U &data[], const T1 dummy) const
   {
      // we need aux array to keep track of original locations
      // to reorder records according to new sequence
      T1 array[][2];
      const int p = ArraySize(data);
      ArrayResize(array, p);
      for(int i = 0; i < p; ++i)
      {
         array[i][0] = data[i]._1;
         array[i][1] = (T1)i;
      }
      ArraySort(array);
      U copy[];
      // make a copy of original array and then
      // place elements from it into original array on new locations
      // ArrayCopy(copy, data); is not applicable for possible strings
      ArrayResize(copy, p);
      for(int i = 0; i < p; ++i)
      {
         copy[i] = data[i];
      }
      for(int i = 0; i < p; ++i)
      {
         data[i] = copy[(int)array[i][1]];
      }
   }
   
   virtual int total() const = 0;
   virtual ulong get(const int i) const = 0;
   
public:
   TradeFilter *let(const I property, const long value, const IS cmp = EQUAL)
   {
      longs.put((ENUM_ANY)property, value);
      conditions.put((ENUM_ANY)property, cmp);
      return &this;
   }

   TradeFilter *let(const D property, const double value, const IS cmp = EQUAL)
   {
      doubles.put((ENUM_ANY)property, value);
      conditions.put((ENUM_ANY)property, cmp);
      return &this;
   }

   TradeFilter *let(const S property, const string value, const IS cmp = EQUAL)
   {
      strings.put((ENUM_ANY)property, value);
      conditions.put((ENUM_ANY)property, cmp);
      return &this;
   }

   template<typename E,typename V>
   bool select(const E property, ulong &tickets[], V &data[], const bool sort = false) const
   {
      E properties[1] = {property};
      V tuples[][1];
      
      const bool result = select(properties, tickets, tuples, sort);
      ArrayCopy(data, tuples);
      return result;
   }

   // we need this overload because built-in ArraySort
   // does not support arrays of strings
   void ArraySort(string &s[][]) const
   {
      QuickSortTm<string> qt(s);
   }

   // a handy implementation of select for Tuples with shortened assign(m)
   template<typename U> // U is expected to be a custom Tuple
   bool select(U &data[], const bool sort = false) const
   {
      const int n = total();
      ArrayResize(data, 0);
      // loop through items
      for(int i = 0; i < n; ++i)
      {
         const ulong t = get(i);
         // access all properties via monitor
         T m(t);
         // check all filtering conditions
         if(match(m, longs)
         && match(m, doubles)
         && match(m, strings))
         {
            // for a matching item, feed its properties into output array
            const int k = EXPAND(data);
            data[k].assign(m);
         }
      }
      
      if(sort)
      {
         static const U u;
         sortTuple(data, u._1);
      }
      
      return true;
   }

   template<typename U> // U is expected to be a Tuple<>, for example Tuple3<T1,T2,T3>
   bool select(const int &property[], U &data[], const bool sort = false) const
   {
      const int q = ArraySize(property);
      static const U u; // U::size() does not compile
      if(q != u.size()) return false; // constraint
      
      const int n = total();
      ArrayResize(data, 0);
      // loop through items
      for(int i = 0; i < n; ++i)
      {
         const ulong t = get(i);
         // access all properties via monitor
         T m(t);
         // check all filtering conditions
         if(match(m, longs)
         && match(m, doubles)
         && match(m, strings))
         {
            // for a matching item, feed its properties into output array
            const int k = EXPAND(data);
            data[k].assign(property, m);
         }
      }
      
      if(sort)
      {
         sortTuple(data, u._1);
      }
      
      return true;
   }
   

   template<typename E,typename V>
   bool select(const E &property[], ulong &tickets[], V &data[][], const bool sort = false) const
   {
      // size of array with properties must match output tuple size
      const int q = ArrayRange(data, 1);
      if(ArraySize(property) != q) return false; // error
      
      const int n = total();
      ArrayResize(tickets, 0);
      ArrayResize(data, 0);
      // loop through items
      for(int i = 0; i < n; ++i)
      {
         const ulong t = get(i);
         // access all properties via monitor
         T m(t);
         // check all filtering conditions
         if(match(m, longs)
         && match(m, doubles)
         && match(m, strings))
         {
            // for a matching item, feed its ticket and properties into output arrays
            const int k = EXPAND(data);
            for(int j = 0; j < q; ++j)
            {
               data[k][j] = m.get(property[j]);
            }
            PUSH(tickets, t);
         }
      }
      
      if(sort)
      {
         // we need aux array to keep track of original locations
         // to reorder records according to new sequence
         V array[][2];
         const int p = ArrayRange(data, 0);
         ArrayResize(array, p);
         for(int i = 0; i < p; ++i)
         {
            array[i][0] = data[i][0];
            array[i][1] = (V)i;
         }
         ArraySort(array);
         ulong temp[];
         V array2d[];
         // make a flat copy of original array and then
         // place elements from it into original array on new locations
         ArrayCopy(array2d, data);
         for(int i = 0; i < p; ++i)
         {
            const int k = (int)array[i][1];
            PUSH(temp, tickets[k]);
            for(int j = 0; j < q; ++j)
            {
               data[i][j] = array2d[k * q + j];
            }
         }
         ArraySwap(tickets, temp);
      }
      
      return true;
   }
   
   bool select(ulong &tickets[]) const
   {
      const int n = total();
      ArrayResize(tickets, 0);
      for(int i = 0; i < n; ++i)
      {
         const ulong t = get(i);
         T m(t);
         if(match(m, longs)
         && match(m, doubles)
         && match(m, strings))
         {
            PUSH(tickets, t);
         }
      }
      return ArraySize(tickets) > 0;
   }
};
//+------------------------------------------------------------------+
