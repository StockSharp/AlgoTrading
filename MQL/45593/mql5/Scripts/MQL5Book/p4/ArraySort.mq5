//+------------------------------------------------------------------+
//|                                                    ArraySort.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRTS(A)  Print(#A, "=", (string)(A) + " / status:" + (string)GetLastError())
#define LIMIT    10
#define SUBLIMIT 3

//+------------------------------------------------------------------+
//| ArraySort templated overload to sort 2D array by specific column |
//+------------------------------------------------------------------+
template<typename T>
bool ArraySort(T &array[][], const int column)
{
   if(!ArrayIsDynamic(array)) return false;
   
   if(column == 0)
   {
      return ArraySort(array);
   }
   
   const int n = ArrayRange(array, 0);
   const int m = ArrayRange(array, 1);

   if(column < 0 || column >= m) return false;

   T temp[][2];
   
   ArrayResize(temp, n);
   for(int i = 0; i < n; ++i)
   {
      temp[i][0] = array[i][column];
      temp[i][1] = i;
   }

   if(!ArraySort(temp)) return false;
   
   ArrayResize(array, n * 2);
   for(int i = n; i < n * 2; ++i)
   {
      ArrayCopy(array, array, i * m, (int)(temp[i - n][1] + 0.1) * m, m);
      /* equivalent
      for(int j = 0; j < m; ++j)
      {
         array[i][j] = array[(int)(temp[i - n][1] + 0.1)][j];
      }
      */
   }
   
   return ArrayRemove(array, 0, n);
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // generate random data
   int array[][SUBLIMIT];
   ArrayResize(array, LIMIT);
   for(int i = 0; i < LIMIT; ++i)
   {
      for(int j = 0; j < SUBLIMIT; ++j)
      {
         array[i][j] = rand();
      }
   }
   
   Print("Before sort");
   ArrayPrint(array);
   /*
         [,0]  [,1]  [,2]
   [0,]  8955  2836 20011
   [1,]  2860  6153 25032
   [2,] 16314  4036 20406
   [3,] 30366 10462 19364
   [4,] 27506  5527 21671
   [5,]  4207  7649 28701
   [6,]  4838   638 32392
   [7,] 29158 18824 13536
   [8,] 17869 23835 12323
   [9,] 18079  1310 29114
   */

   PRTS(ArraySort(array));
   /*
   ArraySort(array)=true / status:0
   */

   // try yourself to sort 2D array by custom column
   // PRTS(ArraySort(array, 0..2));

   Print("After sort");
   ArrayPrint(array);
   /*
         [,0]  [,1]  [,2]
   [0,]  2860  6153 25032
   [1,]  4207  7649 28701
   [2,]  4838   638 32392
   [3,]  8955  2836 20011
   [4,] 16314  4036 20406
   [5,] 17869 23835 12323
   [6,] 18079  1310 29114
   [7,] 27506  5527 21671
   [8,] 29158 18824 13536
   [9,] 30366 10462 19364
   */
}
//+------------------------------------------------------------------+
