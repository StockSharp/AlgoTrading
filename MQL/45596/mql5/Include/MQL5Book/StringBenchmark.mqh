//+------------------------------------------------------------------+
//|                                              StringBenchmark.mqh |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Facultative study: Benchmark                                     |
//| Base class which provides common benchmarking functionalty       |
//+------------------------------------------------------------------+
class Benchmark
{
protected:
   int i;                     // loop variable
   static Benchmark *array[]; // all test-cases are accumulated here
   
public:
   Benchmark()
   {
      // store all instances of Benchmark in the static array
      const int n = ArraySize(array);
      ArrayResize(array, n + 1);
      array[n] = &this;
   }
   
   virtual void init() = 0; // abstract method to prepare algorithm
   virtual void calc() = 0; // abstract method to do main job
   virtual void done() = 0; // abstract method to finalize
   
   // main method for testing all created instances
   static void runAll(const int n)
   {
      for(int k = 0; k < ArraySize(array); ++k)
      {
         array[k].run(n);
      }
   }
   
   // main method of every specific test instance
   // it provides the same job scheme for all kinds of tests:
   // 'init', 'calc' in the loop, 'done'
   void run(const int n)
   {
      init();
      uint time0 = GetTickCount();
      for(i = 0; i < n; ++i)
      {
         calc();
      }
      Print(GetTickCount() - time0, "ms");
      done();
   }
};

static Benchmark *Benchmark::array[];

//+------------------------------------------------------------------+
//| Base class for benchmarking of string building                   |
//+------------------------------------------------------------------+
class StrBase : public Benchmark
{
protected:
   string t;   // output string, will take result

public:
   void init() override
   {
      t = NULL;
   }
   
   void done() override
   {
      Print("L:", StringLen(t), ", B:", StringBufferLen(t));
   }
};
//+------------------------------------------------------------------+