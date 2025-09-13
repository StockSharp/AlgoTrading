//+------------------------------------------------------------------+
//|                                              ArraySwapSimple.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRT(A) Print(#A, "=", (A))

//+------------------------------------------------------------------+
//| Simple class for array processing                                |
//+------------------------------------------------------------------+
template<typename T>
class Worker
{
   T array[];
   
public:
   Worker(T &source[])
   {
      // ArrayCopy(array, source); // overheads
      ArraySwap(source, array);
   }

   bool process(const int mode)
   {
      if(ArraySize(array) == 0) return false;
      switch(mode)
      {
      case -4:
         // TODO: shuffle
         break;
      case -3:
         // TODO: apply logarithmic scale
         break;
      case -2:
         // TODO: add gaussian noise to array
         break;
      case -1:
         ArrayReverse(array);
         break;
      // ...
      }
      return true;
   }

   T operator[](int i)
   {
      return array[i];
   }
   
   void get(T &result[])
   {
      ArraySwap(array, result);
   }
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   double data[];
   ArrayResize(data, 3);
   data[0] = 1;
   data[1] = 2;
   data[2] = 3;
   
   PRT(ArraySize(data));        // 3
   Worker<double> simple(data);
   PRT(ArraySize(data));        // 0
   simple.process(-1);
   
   // can access elements by index
   // Print(simple[0]);
   
   double res[];
   simple.get(res);
   ArrayPrint(res); // 3.00000 2.00000 1.00000
}
//+------------------------------------------------------------------+
