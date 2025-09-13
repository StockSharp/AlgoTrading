//+------------------------------------------------------------------+
//|                                               VariableScopes.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

// global variables
int i, j, k;    // i/j are 0s, k is unknown (unused and eliminated by compiler)
int m = 1;      // m = 1
int n = i + m;  // n = 1

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // local variables
   int x, y, z;
   int k = m; // warning: declaration of 'k' hides global variable
   int j = j; // warning: declaration of 'j' hides global variable

   // use variables in assignment instructions
   x = n;     // ok, 1
   z = y;     // warning: possible use of uninitialized variable 'y'
   j = 10;    // change local j, global j is still 0
}

// compilation error
// int bad = x; // 'x' - undeclared identifier
//+------------------------------------------------------------------+
