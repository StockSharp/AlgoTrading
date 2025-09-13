//+------------------------------------------------------------------+
//|                                                  ArrayWorker.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRTS(A) Print(#A, "=", (string)(A) + " / status:" + (string)GetLastError())
#define LIMIT 5000

//+------------------------------------------------------------------+
//| Simple class for array processing with struct sorting support    |
//+------------------------------------------------------------------+
template<typename T,typename R>
class Worker
{
   T array[];

   union Overlay
   {
      T r;
      R d[sizeof(T) / sizeof(R)];
   };
   
   bool simpleSort(R &r[])
   {
      return ArraySort(r);
   }

   template<typename X>
   bool simpleSort(X &x[])
   {
      return false;
   }
   
   bool arrayStructSort(const int field)
   {
      const int n = ArraySize(array);
      const int m = sizeof(T) / sizeof(R);
      if(field < 0 || field >= m) return false;
      
      if(m == 1)
      {
         return simpleSort(array);
      }
      
      R temp[][2];
      Overlay overlay;

      ArrayResize(temp, n);
      for(int i = 0; i < n; ++i)
      {
         overlay.r = array[i];
         temp[i][0] = overlay.d[field];
         temp[i][1] = i;
      }

      if(!ArraySort(temp)) return false;
      
      T result[];
      
      ArrayResize(result, n);
      for(int i = 0; i < n; ++i)
      {
         result[i] = array[(int)(temp[i][1] + 0.1)];
      }
      
      return ArraySwap(result, array);
   }
   
public:
   Worker(T &source[])
   {
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
      default: // sorting by field ('mode')
         arrayStructSort(mode);
         break;
      }
      return true;
   }
   
   T operator[](int i)
   {
      return array[i];
   }
   
   bool get(T &result[])
   {
      return ArraySwap(array, result);
   }
   
   int size() const
   {
      return ArraySize(array);
   }
};

//+------------------------------------------------------------------+
//| Helper function to print a single struct via ArrayPrint          |
//+------------------------------------------------------------------+
template<typename S>
void StructPrint(const S &s)
{
   S temp[1];
   temp[0] = s;
   ArrayPrint(temp);
}

//+------------------------------------------------------------------+
//| Sort MqlRates array by Worker object by given field 'offset'     |
//+------------------------------------------------------------------+
void sort(Worker<MqlRates,double> &worker, const int offset, const string title)
{
   Print(title);
   worker.process(offset);
   Print("First struct");
   StructPrint(worker[0]);
   Print("Last struct");
   StructPrint(worker[worker.size() - 1]);
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   MqlRates rates[];
   int n = CopyRates(_Symbol, _Period, 0, LIMIT, rates);
   if(n <= 0)
   {
      Print("CopyRates error: ", _LastError);
      return;
   }
   
   Worker<MqlRates,double> worker(rates);
   
   sort(worker, offsetof(MqlRates, open) / sizeof(double), "Sorting by open price...");
   sort(worker, offsetof(MqlRates, tick_volume) / sizeof(double), "Sorting by tick volume...");
   sort(worker, offsetof(MqlRates, time) / sizeof(double), "Sorting by time...");
   
   // this will tear down data from internal array:
   //    worker.get(rates);
   //    ArrayPrint(rates); // can view all array (may need to adjust ArrayPrint params)
   // so consecutive calls to worker.process become impossible:
   //    worker.process(...); // will do nothing
   
   /*
   example output (depends from symbol/timeframe settings)
   
   Sorting by open price...
   First struct
                    [time]  [open]  [high]   [low] [close] [tick_volume] [spread] [real_volume]
   [0] 2021.07.21 10:30:00 1.17557 1.17584 1.17519 1.17561          1073        0             0
   Last struct
                    [time]  [open]  [high]   [low] [close] [tick_volume] [spread] [real_volume]
   [0] 2021.05.25 15:15:00 1.22641 1.22664 1.22592 1.22618           852        0             0
   Sorting by tick volume...
   First struct
                    [time]  [open]  [high]   [low] [close] [tick_volume] [spread] [real_volume]
   [0] 2021.05.24 00:00:00 1.21776 1.21811 1.21764 1.21794            52       20             0
   Last struct
                    [time]  [open]  [high]   [low] [close] [tick_volume] [spread] [real_volume]
   [0] 2021.06.16 21:30:00 1.20436 1.20489 1.20149 1.20154          4817        0             0
   Sorting by time...
   First struct
                    [time]  [open]  [high]   [low] [close] [tick_volume] [spread] [real_volume]
   [0] 2021.05.14 16:15:00 1.21305 1.21411 1.21289 1.21333           888        0             0
   Last struct
                    [time]  [open]  [high]   [low] [close] [tick_volume] [spread] [real_volume]
   [0] 2021.07.27 22:45:00 1.18197 1.18227 1.18191 1.18225           382        0             0
   
   */
}
//+------------------------------------------------------------------+
