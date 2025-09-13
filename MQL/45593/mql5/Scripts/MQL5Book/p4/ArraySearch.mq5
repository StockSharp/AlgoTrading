//+------------------------------------------------------------------+
//|                                                  ArraySearch.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRTS(A)  Print(#A, "=", (string)(A) + " / status:" + (string)GetLastError())

//+------------------------------------------------------------------+
//| Fill array with elements and keep it sorted by ArrayBsearch      |
//+------------------------------------------------------------------+
void populateSortedArray(const int limit)
{
   double numbers[];  // the array to fill
   double element[1]; // new value to insert
   
   ArrayResize(numbers, 0, limit); // allocate capacity beforehand

   for(int i = 0; i < limit; ++i)
   {
      // generate a random number
      element[0] = NormalizeDouble(rand() * 1.0 / 32767, 3);
      // find where to place it inside the array
      int cursor = ArrayBsearch(numbers, element[0]);
      if(cursor == -1)
      {
         if(_LastError == 5053) // empty array
         {
            ArrayInsert(numbers, element, 0);
         }
         else break; // error
      }
      else
      if(numbers[cursor] > element[0]) // need to insert at cursor
      {
         ArrayInsert(numbers, element, cursor);
      }
      else // (numbers[cursor] <= value) // need to insert after cursor
      {
         ArrayInsert(numbers, element, cursor + 1);
      }
   }
   ArrayPrint(numbers, 3);
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   int array[] = {1, 5, 11, 17, 23, 23, 37};
     // indices   0  1   2   3   4   5   6
   int data[][2] = {{1, 3}, {3, 2}, {5, 10}, {14, 10}, {21, 8}};
     // indices      0       1       2         3         4
   int empty[];

   PRTS(ArrayBsearch(array, -1)); // 0
   PRTS(ArrayBsearch(array, 11)); // 2
   PRTS(ArrayBsearch(array, 12)); // 2
   PRTS(ArrayBsearch(array, 15)); // 3
   PRTS(ArrayBsearch(array, 23)); // 5
   PRTS(ArrayBsearch(array, 50)); // 6

   PRTS(ArrayBsearch(data, 7));   // 2
   PRTS(ArrayBsearch(data, 9));   // 2
   PRTS(ArrayBsearch(data, 10));  // 3
   PRTS(ArrayBsearch(data, 11));  // 3
   PRTS(ArrayBsearch(data, 14));  // 3

   PRTS(ArrayBsearch(empty, 0));  // -1, 5053, ERR_ZEROSIZE_ARRAY

   populateSortedArray(80);
   /*
     example output (will differ on every run due to randomization)
   [ 0] 0.050 0.065 0.071 0.106 0.119 0.131 0.145 0.148 0.154 0.159
        0.184 0.185 0.200 0.204 0.213 0.216 0.220 0.224 0.236 0.238
   [20] 0.244 0.259 0.267 0.274 0.282 0.293 0.313 0.334 0.346 0.366
        0.386 0.431 0.449 0.461 0.465 0.468 0.520 0.533 0.536 0.541
   [40] 0.597 0.600 0.607 0.612 0.613 0.617 0.621 0.623 0.631 0.634
        0.646 0.658 0.662 0.664 0.670 0.670 0.675 0.686 0.693 0.694
   [60] 0.725 0.739 0.759 0.762 0.768 0.783 0.791 0.791 0.791 0.799
        0.838 0.850 0.854 0.874 0.897 0.912 0.920 0.934 0.944 0.992
   */
}
//+------------------------------------------------------------------+
