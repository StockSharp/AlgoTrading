//+------------------------------------------------------------------+
//|                                                  QuickSortTm.mqh |
//|                             Copyright 2021-2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Templatized class for quick sorting of multidimentional arrays   |
//+------------------------------------------------------------------+
template<typename T>
class QuickSortTm
{
protected:
   int indices[];
   
   void Swap(const int i, const int j)
   {
      const int k = indices[i];
      indices[i] = indices[j];
      indices[j] = k;
   }
   
public:
   virtual int Compare(T &a, T &b)
   {
      return a > b ? +1 : (a < b ? -1 : 0);
   }
   
   void QuickSortTm(T &array[][])
   {
      const int n = ArrayResize(indices, ArrayRange(array, 0));
      for(int i = 0; i < n; ++i)
      {
         indices[i] = i;
      }
      
      QuickSort(array);
      
      int reorder[];
      ArrayResize(reorder, n);
      for(int i = 0; i < n; ++i)
      {
         reorder[indices[i]] = i;
      }

      const int k = ArrayRange(array, 1);
      T temp;
      for(int i = 0; i < n; ++i)
      {
         if(reorder[i] == i) continue;
         while(reorder[i] != i)
         {
            for(int j = 0; j < k; ++j)
            {
               temp = array[i][j];
               array[i][j] = array[reorder[i]][j];
               array[reorder[i]][j] = temp;
            }
            const int m = reorder[reorder[i]];
            reorder[reorder[i]] = reorder[i];
            reorder[i] = m;
         }
      }
   }
   
protected:
   void QuickSort(T &array[][], const int start = 0, int end = INT_MAX)
   {
      if(end == INT_MAX)
      {
         end = start + ArrayRange(array, 0) - 1;
      }
      if(start < end)
      {
         int pivot = start;
   
         for(int i = start; i <= end; i++)
         {
            if(Compare(array[indices[i]][0], array[indices[end]][0]) <= 0)
            {
               Swap(i, pivot++);
            }
         }
   
         --pivot;
   
         QuickSort(array, start, pivot - 1);
         QuickSort(array, pivot + 1, end);
      }
   }
};
//+------------------------------------------------------------------+