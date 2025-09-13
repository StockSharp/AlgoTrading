//+------------------------------------------------------------------+
//|                                              TemplateSorting.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

template<typename T>
void Swap(T &array[], const int i, const int j)
{
   const T temp = array[i];
   array[i] = array[j];
   array[j] = temp;
}

template<typename T>
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
         if(!(array[i] > array[end]))
         {
            Swap(array, i, pivot++);
         }
      }

      --pivot;

      QuickSort(array, start, pivot - 1);
      QuickSort(array, pivot + 1, end);
   }
}


struct ABC
{
   int x;
   ABC()
   {
      x = rand();
   }
   bool operator>(const ABC &other) const
   {
      return x > other.x;
   }
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   double numbers[] = {34, 11, -7, 49, 15, -100, 11};
   QuickSort(numbers);
   ArrayPrint(numbers);
   // -100.00000 -7.00000 11.00000 11.00000 15.00000 34.00000 49.00000

   string messages[] = {"usd", "eur", "jpy", "gbp", "chf", "cad", "aud", "nzd"};
   QuickSort(messages);
   ArrayPrint(messages);
   // "aud" "cad" "chf" "eur" "gbp" "jpy" "nzd" "usd"

   ABC abc[10];
   QuickSort(abc);
   ArrayPrint(abc);
   /* Output example:
            [x]
      [0]  1210
      [1]  2458
      [2] 10816
      [3] 13148
      [4] 15393
      [5] 20788
      [6] 24225
      [7] 29919
      [8] 32309
      [9] 32589
   */
}
//+------------------------------------------------------------------+
