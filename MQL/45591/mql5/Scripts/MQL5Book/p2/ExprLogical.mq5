//+------------------------------------------------------------------+
//|                                                  ExprLogical.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

#define PRT(A) Print(#A, "=", (A))

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   int x = 3, y = 4, z = 5;

   bool expr1 = x == y && z > 0;  // false, x != y, z does'n matter
   bool expr2 = x != y && z > 0;  // true,  both correct
   bool expr3 = x == y || z > 0;  // true,  due to z > 0

   bool expr4 = !x;               // false, need x == 0 to be true

   bool expr5 = x > 0 && y > 0 && z > 0; // true, all 3 correct
   // warning: check operator precedence for possible error;
   // use parentheses to clarify precedence
   bool expr6 = x < 0 || y > 0 && z > 0; // true, y and z suffice
   bool expr7 = x < 0 || y < 0 || z > 0; // true, z suffices
   
   PRT(x == y && z > 0);
   PRT(x != y && z > 0);
   PRT(x == y || z > 0);

   PRT(!x);
   
   PRT(x > 0 && y > 0 && z > 0);
   PRT(x < 0 || y > 0 && z > 0);
   PRT(x < 0 || y < 0 || z > 0);
}
//+------------------------------------------------------------------+
