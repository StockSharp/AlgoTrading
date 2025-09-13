//+------------------------------------------------------------------+
//|                                                 SymbolFilter.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/MapArray.mqh>
#include <MQL5Book/SymbolMonitor.mqh>
#include <MQL5Book/QuickSortTm.mqh>
#include <MQL5Book/IS.mqh>
#include <MQL5Book/Defines.mqh>

//+------------------------------------------------------------------+
//| Class for collecting symbols and their properties if they match  |
//| specific conditions                                              |
//+------------------------------------------------------------------+
class SymbolFilter
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
   bool match(const SymbolMonitor &m, const MapArray<ENUM_ANY,V> &data) const
   {
      static const V type = (V)NULL;
      for(int i = 0; i < data.getSize(); ++i)
      {
         const ENUM_ANY key = data.getKey(i);
         switch(conditions[key])
         {
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
      return true;
   }
   
public:
   SymbolFilter *let(const ENUM_SYMBOL_INFO_INTEGER property, const long value, const IS cmp = EQUAL)
   {
      longs.put((ENUM_ANY)property, value);
      conditions.put((ENUM_ANY)property, cmp);
      return &this;
   }

   SymbolFilter *let(const ENUM_SYMBOL_INFO_DOUBLE property, const double value, const IS cmp = EQUAL)
   {
      doubles.put((ENUM_ANY)property, value);
      conditions.put((ENUM_ANY)property, cmp);
      return &this;
   }

   SymbolFilter *let(const ENUM_SYMBOL_INFO_STRING property, const string value, const IS cmp = EQUAL)
   {
      strings.put((ENUM_ANY)property, value);
      conditions.put((ENUM_ANY)property, cmp);
      return &this;
   }

   template<typename E,typename V>
   bool select(const bool watch, const E property, string &symbols[], V &data[], const bool sort = false) const
   {
      E properties[1] = {property};
      V tuples[][1];
      
      const bool result = select(watch, properties, symbols, tuples, sort);
      // NB: MQL5 supports copying arrays with different dimensions,
      // here we copy (and unfold) 2D array into 1D array
      ArrayCopy(data, tuples);
      /* // otherwise we could rewrite it like this:
      const int n = ArraySize(tuples);
      ArrayResize(data, n);
      for(int i = 0; i < n; ++i)
      {
         data[i] = tuples[i][0];
      }
      */

      return result;
   }

   // we need this overload because built-in ArraySort
   // does not support arrays of strings
   void ArraySort(string &s[][]) const
   {
      QuickSortTm<string> qt(s);
   }

   template<typename E,typename V>
   bool select(const bool watch, const E &property[], string &symbols[], V &data[][], const bool sort = false) const
   {
      // size of array with properties must match output tuple size
      const int q = ArrayRange(data, 1);
      if(ArraySize(property) != q) return false; // error
      
      const int n = SymbolsTotal(watch);
      // loop through symbols
      for(int i = 0; i < n; ++i)
      {
         const string s = SymbolName(i, watch);
         // access all symbol properties via monitor
         SymbolMonitor m(s);
         // check all filtering conditions
         if(match(m, longs)
         && match(m, doubles)
         && match(m, strings))
         {
            // for a matching symbol feed its name and properties into output arrays
            const int k = EXPAND(data);
            for(int j = 0; j < q; ++j)
            {
               data[k][j] = m.get(property[j]);
            }
            PUSH(symbols, s);
         }
      }
      
      if(sort)
      {
         // we need aux array to keep track of original positions
         // to reorder symbols/records according to new order
         V array[][2];
         const int p = ArrayRange(data, 0);
         ArrayResize(array, p);
         for(int i = 0; i < p; ++i)
         {
            array[i][0] = data[i][0];
            array[i][1] = (V)i;
         }
         ArraySort(array);
         string temp[];
         V array2d[];
         // make a flat copy of original array and then
         // place elements from it into original array on new positions
         ArrayCopy(array2d, data);
         for(int i = 0; i < p; ++i)
         {
            const int k = (int)array[i][1];
            PUSH(temp, symbols[k]);
            for(int j = 0; j < q; ++j)
            {
               data[i][j] = array2d[k * q + j];
            }
         }
         ArraySwap(symbols, temp);
      }
      
      return true;
   }
   
   void select(const bool watch, string &symbols[]) const
   {
      const int n = SymbolsTotal(watch);
      for(int i = 0; i < n; ++i)
      {
         const string s = SymbolName(i, watch);
         SymbolMonitor m(s);
         if(match(m, longs)
         && match(m, doubles)
         && match(m, strings))
         {
            PUSH(symbols, s);
         }
      }
   }
};
//+------------------------------------------------------------------+
