//+------------------------------------------------------------------+
//|                                                   QuickSortT.mqh |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Templatized class for quick sorting of anything                  |
//+------------------------------------------------------------------+
template<typename T>
class QuickSortT
{
public:
   void Swap(T &array[], const int i, const int j)
   {
      const T temp = array[i];
      array[i] = array[j];
      array[j] = temp;
   }
   
   virtual int Compare(T &a, T &b)
   {
      return a > b ? +1 : (a < b ? -1 : 0);
   }
   
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
            // previous implementation of comparison with opeartor '>'
            // is replaced with the virtual method Compare to allow overrides
            // if(!(array[i] > array[end])) - 
            if(Compare(array[i], array[end]) <= 0)
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