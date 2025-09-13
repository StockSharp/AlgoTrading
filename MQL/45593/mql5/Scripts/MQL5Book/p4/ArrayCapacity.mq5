//+------------------------------------------------------------------+
//|                                                ArrayCapacity.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Dynamic array mockup (simulation in MQL5)                        |
//+------------------------------------------------------------------+
template<typename T>
struct DynArray
{
   int size;
   int capacity;
   T memory[];
   T operator[](int i)
   {
      if(i < 0 || i >= size)
      {
         PrintFormat("Index out of bounds: %d, size: %d", i, size);
         return NULL;
      }
      return memory[i];
   }
};

//+------------------------------------------------------------------+
//| Resize DynArray with optional reserve                            |
//+------------------------------------------------------------------+
template<typename T>
int DynArrayResize(DynArray<T> &array, int size, int reserve = 0)
{
   if(size > array.capacity)
   {
      static int temp;
      temp = array.capacity;
      long ul = (long)GetMicrosecondCount();
      array.capacity = ArrayResize(array.memory, size + reserve);
      array.size = MathMin(size, array.capacity);
      ul -= (long)GetMicrosecondCount();
      PrintFormat("Reallocation: [%d] -> [%d], done in %d µs", temp, array.capacity, -ul);
   }
   else
   {
      array.size = size;
   }
   return array.size;
}

//+------------------------------------------------------------------+
//| Get requested size of DynArray                                   |
//+------------------------------------------------------------------+
template<typename T>
int DynArraySize(DynArray<T> &array)
{
   return array.size;
}

//+------------------------------------------------------------------+
//| Free DynArray                                                    |
//+------------------------------------------------------------------+
template<typename T>
void DynArrayFree(DynArray<T> &array)
{
   ArrayFree(array.memory);
   ZeroMemory(array);
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   ulong start = GetTickCount();
   ulong now;
   int   count = 0;

   DynArray<double> a;
   
   // Check how fast the variant with memory reservation works
   Print("--- Test Fast: ArrayResize(arr,100000,100000)");

   DynArrayResize(a, 100000, 100000);

   for(int i = 1; i <= 300000 && !IsStopped(); i++)
   {
      // Set new array size specifying the reserve of 100,000 elements!
      DynArrayResize(a, i, 100000);
      // When reaching a round number, show the array size and the time spent
      if(DynArraySize(a) % 100000 == 0)
      {
         now = GetTickCount();
         count++;
         PrintFormat("%d. ArraySize(arr)=%d Time=%d ms", count, DynArraySize(a), (now - start));
         start = now;
      }
   }
   DynArrayFree(a);
   
   // Now show, how slow the version without memory reservation is
   count = 0;
   start = GetTickCount();
   Print("---- Test Slow: ArrayResize(slow,100000)");

   DynArrayResize(a, 100000, 100000);
   
   for(int i = 1; i <= 300000 && !IsStopped(); i++)
   {
      // Set new array size, but with 100 times smaller reserve: 1000
      DynArrayResize(a, i, 1000);
      // When reaching a round number, show the array size and the time spent
      if(DynArraySize(a) % 100000 == 0)
      {
         now = GetTickCount();
         count++;
         PrintFormat("%d. ArraySize(arr)=%d Time=%d ms", count, DynArraySize(a), (now - start));
         start = now;
      }
   }
   
   /*
      output (example/excerpt)

   --- Test Fast: ArrayResize(arr,100000,100000)
   Reallocation: [0] -> [200000], done in 17 µs
   1. ArraySize(arr)=100000 Time=0 ms
   2. ArraySize(arr)=200000 Time=0 ms
   Reallocation: [200000] -> [300001], done in 2296 µs
   3. ArraySize(arr)=300000 Time=0 ms
   ---- Test Slow: ArrayResize(slow,100000)
   Reallocation: [0] -> [200000], done in 21 µs
   1. ArraySize(arr)=100000 Time=0 ms
   2. ArraySize(arr)=200000 Time=0 ms
   Reallocation: [200000] -> [201001], done in 1838 µs
   Reallocation: [201001] -> [202002], done in 1994 µs
   Reallocation: [202002] -> [203003], done in 1677 µs
   Reallocation: [203003] -> [204004], done in 1983 µs
   Reallocation: [204004] -> [205005], done in 1637 µs
   ...
   Reallocation: [295095] -> [296096], done in 2921 µs
   Reallocation: [296096] -> [297097], done in 2189 µs
   Reallocation: [297097] -> [298098], done in 2152 µs
   Reallocation: [298098] -> [299099], done in 2767 µs
   Reallocation: [299099] -> [300100], done in 2115 µs
   3. ArraySize(arr)=300000 Time=219 ms
   
   */
}
//+------------------------------------------------------------------+
