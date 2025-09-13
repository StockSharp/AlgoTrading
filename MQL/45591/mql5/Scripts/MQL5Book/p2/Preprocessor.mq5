//+------------------------------------------------------------------+
//|                                                 Preprocessor.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

//+------------------------------------------------------------------+
//| Includes                                                         |
//+------------------------------------------------------------------+
#include "Preprocessor.mqh"

// error: can't open "...\MQL5\Include\mssing.mqh" include file
// #include <missing.mqh>

//+------------------------------------------------------------------+
//| Defines                                                          |
//+------------------------------------------------------------------+
#define PRT(A) Print(#A, "=", (A))
#define MAX_FIBO 10
#define XYZ ABC
#define SQ3(X) (X * X * X)
#define ABS(X) MathAbs(SQ3(X))
#define INC(Y) (++Y)
#define LOOP   for( ; !IsStopped() ; )

//+------------------------------------------------------------------+
//| Consts                                                           |
//+------------------------------------------------------------------+
const int MAX_FIBO_VAR = 10;
enum
{
   MAX_FIBO_ENUM = 10
};

//+------------------------------------------------------------------+
//| Global variables                                                 |
//+------------------------------------------------------------------+
int fibo[MAX_FIBO];         // ok: 10
// int fibo2[MAX_FIBO_VAR]; // error: invalid index value
int fibo3[MAX_FIBO_ENUM];   // ok: 10
int XYZAXES = 3;            // int XYZAXES = 3
int XYZ = 0;                // int ABC = 0

//+------------------------------------------------------------------+
//| Calculate Fibonacci numbers for predefined 'fibo' array size     |
//+------------------------------------------------------------------+
void FillFibo()
{
   int prev = 0;
   int result = 1;

   for(int i = 0; i < MAX_FIBO; ++i) // i < 10
   {
      int temp = result;
      result = result + prev;
      fibo[i] = result;
      prev = temp;
   }

   // int max = MAX(prev, result); // error: 'MAX' - undeclared identifier
}

// if definition goes after lines of code, it's undeclared above
#define MAX(A,B) ((A) > (B) ? (A) : (B))

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Print(__FUNCTION__, " started");

   LOOP // expands to for( ; !IsStopped() ; )
   {
      // ... some real loop job here
      break; // just exist in this demo
   }

#ifdef DEMO
   Print("Fibo is disabled in the demo"); // the line is not processed
#else
   FillFibo();                            // the line has effect
#endif

   PRT(XYZAXES);
   PRT(XYZ);
   PRT(MAX(XYZAXES, XYZ + 10));
   int x = -10;
   PRT(ABS(INC(x)));
   
   Print(__FUNCTION__, " done");
}
//+------------------------------------------------------------------+
