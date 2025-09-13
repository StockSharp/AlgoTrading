//+------------------------------------------------------------------+
//|                                                    StringAdd.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRTE(A) Print(#A, "=", (A) ? "true" : "false:" + (string)GetLastError())

#include <MQL5Book/StringBenchmark.mqh>

//+------------------------------------------------------------------+
//| Helper function to show the given string and its metrics         |
//+------------------------------------------------------------------+
void StrOut(const string &s)
{
   Print("'", s, "' [", StringLen(s), "] ", StringBufferLen(s));
}

//+------------------------------------------------------------------+
//| Operator '+' benchmark                                           |
//+------------------------------------------------------------------+
class StrPlus : public StrBase
{
public:
   void init() override
   {
      StrBase::init();
      Print(typename(this));
   }
   
   void calc() override
   {
      t += (string)i;
   }
};

//+------------------------------------------------------------------+
//| Function StringAdd benchmark                                     |
//+------------------------------------------------------------------+
class StrAdd : public StrBase
{
public:
   void init() override
   {
      StrBase::init();
      Print(typename(this));
   }
   
   void calc() override
   {
      StringAdd(t, (string)i);
   }
};

//+------------------------------------------------------------------+
//| Function StringConcatenate benchmark                             |
//+------------------------------------------------------------------+
class StrConcatenate : public StrBase
{
public:
   void init() override
   {
      StrBase::init();
      Print(typename(this));
   }
   
   void calc() override
   {
      // DO NOT DO THIS:
      // passing a string as the first and the second parameter
      // to append next data (the third parameter) to it
      // will cause memory reallocations and excessive copying,
      // very slow
      StringConcatenate(t, t, i);
   }
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // basic usage
   string s = "message";
   StrOut(s);
   PRTE(StringAdd(s, "r"));
   StrOut(s);
   PRTE(StringConcatenate(s, M_PI * 100, " ", clrBlue, PRICE_CLOSE));
   StrOut(s);
   
   // benchmarking with common repeat limit
   const int n = 50000;
   // instantiate different tests
   StrPlus plus;
   StrAdd add;
   StrConcatenate con;
   // run all tests and get results
   Benchmark::runAll(n);
   
   /*
      output (timing is a subject to change based on your CPU speed):
   
   'message' [7] 0
   StringAdd(s,r)=true
   'messager' [8] 260
   StringConcatenate(s,M_PI*100, ,clrBlue,PRICE_CLOSE)=true
   '314.1592653589793 clrBlue1' [26] 260
   
   StrPlus
   16ms
   L:238890, B:239699
   StrAdd
   15ms
   L:238890, B:239699
   StrConcatenate
   8097ms
   L:238890, B:239926
   
   */
}
//+------------------------------------------------------------------+