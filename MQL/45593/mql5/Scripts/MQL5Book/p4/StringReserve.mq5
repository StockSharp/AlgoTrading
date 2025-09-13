//+------------------------------------------------------------------+
//|                                                StringReserve.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#include <MQL5Book/StringBenchmark.mqh>

//+------------------------------------------------------------------+
//| Benchmarking of string building with/without buffer reservation  |
//+------------------------------------------------------------------+
class StringBuilder : public StrBase
{
protected:
   string filler; // small string to be appended many times to build the result
   const int r;   // size of reserved buffer, 0 by default
   int capacity;  // current buffer capacity of the resulting string
   int allocCount;// buffer reallocation stats

public:
   StringBuilder(const int reserve = 0, const int increment = 200) :
      r(reserve)
   {
      StringInit(filler, increment, ' ');
      capacity = 0;
      allocCount = 0;
   }

   void init() override
   {
      StrBase::init();
      Print(typename(this) + " " + (string)r);
      if(r > 0) StringReserve(t, r);
   }
   
   void calc() override
   {
      t += filler; // append small string to the resulting 't'-string
      
      // detect if buffer was changed and count it up
      if(StringBufferLen(t) != capacity)
      {
         ++allocCount;
         capacity = StringBufferLen(t);
      }
   }
   
   void done() override
   {
      StrBase::done();
      Print("Capacity was reallocated ", allocCount, " times");
   }
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // no inital buffer, some defaulted buffer will be allocated
   // and re-allocated by MT5 core ad hoc many times
   StringBuilder simple;
   // sufficient buffer is allocated beforehand
   StringBuilder reserved(2000000);
   
   // 10000 small strings of 200 blanks each will be added
   // to resulting string in a loop
   Benchmark::runAll(10000);
   
   /*
      output (absolute timing is specific for CPU):

   StringBuilder 0
   3308ms
   L:2000000, B:2000024
   Capacity was reallocated 1672 times
   
   StringBuilder 2000000
   0ms
   L:2000000, B:2000000
   Capacity was reallocated 1 times
   
   */
}
//+------------------------------------------------------------------+