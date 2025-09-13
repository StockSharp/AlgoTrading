//+------------------------------------------------------------------+
//|                                             QuickSortStructT.mqh |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Templatized class for quick sorting of anything                  |
//+------------------------------------------------------------------+
template<typename T>
class QuickSortStructT
{
public:
   void Swap(T &array[], const int i, const int j)
   {
      const T temp = array[i];
      array[i] = array[j];
      array[j] = temp;
   }
   
   // should override this with operator '>' equivalent
   // applied to any field of struct, object or array column
   virtual bool Compare(const T &a, const T &b) = 0;
   
   void QuickSort(T &array[], const int start = 0, int end = INT_MAX)
   {
      if(end == INT_MAX)
      {
         end = start + ArraySize(array) - 1;
      }
      if(start < end)
      {
         int pivot = start;
   
         for(int i = start; i <= end; i++)
         {
            // use custom implementation of virtual Compare override
            if(!Compare(array[i], array[end]))
            {
               Swap(array, i, pivot++);
            }
         }
   
         --pivot;
   
         QuickSort(array, start, pivot - 1);
         QuickSort(array, pivot + 1, end);
      }
   }
};

//+------------------------------------------------------------------+
//| Convenient macro to sort 'A'rray of 'T'ype by 'F'ield            |
//+------------------------------------------------------------------+
#define SORT_STRUCT(T,A,F)                                           \
{                                                                    \
   class InternalSort : public QuickSortStructT<T>                   \
   {                                                                 \
      virtual bool Compare(const T &a, const T &b) override          \
      {                                                              \
         return a.##F > b.##F;                                       \
      }                                                              \
   } sort;                                                           \
   sort.QuickSort(A);                                                \
}

//+------------------------------------------------------------------+
//| Example of macro usage to sort MqlRates struct by 'high' price   |
//+------------------------------------------------------------------+
/*
   MqlRates rates[];
   CopyRates(_Symbol, _Period, 0, 10000, rates);
   SORT_STRUCT(MqlRates, rates, high);
*/