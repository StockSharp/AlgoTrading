//+------------------------------------------------------------------+
//|                                              ExprConditional.mq5 |
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
   bool A = false, B = false, C = true;
   int x = 1, y = 2, z = 3, p = 4, q = 5, f = 6, h = 7;

   int r0 = x > y ? z : p != 0 && q != 0 ? f / (p + q) : h; // 0
   int r1 = A ? x : C ? p : q;                              // 4
   int r2 = A ? B ? x : y : z;                              // 3
   int r3 = A ? B ? C ? p : q : y : z;                      // 3
   int r4 = A ? B ? x : y : C ? p : q;                      // 4
   int r5 = A ? f : h ? B ? x : y : C ? p : q;              // 2

   // errors:
   // ';' - unexpected token
   // ';' - ':' colon sign expected
   // int r6 = A ? B ? x : y; // one of conditions is incomplete

   PRT(x > y ? z : p != 0 && q != 0 ? f / (p + q) : h);
   PRT(A ? x : C ? p : q);
   PRT(A ? B ? x : y : z);
   PRT(A ? B ? C ? p : q : y : z);
   PRT(A ? B ? x : y : C ? p : q);
   PRT(A ? f : h ? B ? x : y : C ? p : q);

   // warning: expression not boolean
   //   treated as: A ? f : ((h + B) ? x : y),
   //   where (h + B) goes for boolean
   int w = A ? f : h + B ? x : y;                           // 1
   
   // ok: explicit parentheses
   int v = (A ? f : h) + (B ? x : y);                       // 9

   PRT(A ? f : h + B ? x : y);
   PRT((A ? f : h) + (B ? x : y));

}
//+------------------------------------------------------------------+
