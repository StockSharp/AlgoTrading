//+------------------------------------------------------------------+
//|                                                     MapArray.mqh |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Generic pair [key;value]                                         |
//+------------------------------------------------------------------+
template<typename K,typename V>
struct Pair
{
   K key;
   V value;
};

//+------------------------------------------------------------------+
//| Array of pairs [key;value]                                       |
//+------------------------------------------------------------------+
template<typename K,typename V>
class MapArray
{
protected:
   Pair<K,V> array[];
   
   int add(const K k, const V v)
   {
      const int n = ArraySize(array);
      ArrayResize(array, n + 1);
      array[n].key = k;
      array[n].value = v;
      return n;
   }

public:
   int getSize() const
   {
      return ArraySize(array);
   }
   
   K getKey(const int i) const
   {
      return array[i].key;
   }
   
   V getValue(const int i) const
   {
      return array[i].value;
   }
   
   int find(const K k) const
   {
      for(int i = 0; i < ArraySize(array); ++i)
      {
         if(array[i].key == k)
         {
            return i;
         }
      }
      return -1;
   }
   
   int put(const K k, const V v)
   {
      const int i = find(k);
      if(i != -1)
      {
         array[i].value = v;
         return i;
      }
      else return add(k, v);
   }

   int inc(const K k, const V plus = (V)1)
   {
      const int i = find(k);
      if(i != -1)
      {
         array[i].value += plus;
         return i;
      }
      else return add(k, plus);
   }
   
   void remove(const K k)
   {
      const int i = find(k);
      if(i != -1)
      {
         for(int j = i + 1; j < ArraySize(array); ++j)
         {
            array[j - 1] = array[j];
         }
         ArrayResize(array, ArraySize(array) - 1);
      }
   }
   
   V operator[](const int i)
   {
      return array[i].value;
   }

   V operator[](const K k)
   {
      const int i = find(k);
      if(i != -1) return array[i].value;
      return (V)NULL;
   }

   void reset()
   {
      ArrayResize(array, 0);
   }
   
   void print()
   {
      ArrayPrint(array);
   }
};
//+------------------------------------------------------------------+
