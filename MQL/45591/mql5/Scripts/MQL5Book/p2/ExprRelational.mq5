//+------------------------------------------------------------------+
//|                                               ExprRelational.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

#define PRT(A) Print(#A, "=", (A))

//+------------------------------------------------------------------+
//| Compare 2 numbers, return true on practical equality             |
//+------------------------------------------------------------------+
bool isEqual(const double x, const double y)
{
   const double diff = MathAbs(x - y);
   const double eps = MathMax(MathAbs(x), MathAbs(y)) * DBL_EPSILON;
   return diff < eps;
}

//+------------------------------------------------------------------+
//| Compare number for 0 with default tolerance, returns true for 0  |
//+------------------------------------------------------------------+
bool isZero(const double x)
{
   return MathAbs(x) < DBL_EPSILON;
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   int x = 10, y = 5, z = 2;

   // warnings: unsafe use of type 'bool' in operation
   bool range1 = x < y < z;          // true (!)
   bool range2 = x < y && y < z;     // false

   PRT(x < y < z);
   PRT(x < y && y < z);

   double p = 0.3, q = 0.6;
   bool eq = p + q == 0.9;           // false
   double diff = p + q - 0.9;        // -0.000000000000000111

   PRT(0.3 + 0.6 == 0.9);
   PRT(p + q - 0.9);
   PRT(isEqual(0.3 + 0.6, 0.9));     // true

   bool zero = 0.1 + 0.2 - 0.3 == 0; // false
   PRT(isEqual(0.1 + 0.2 - 0.3, 0)); // false
   PRT(0.1 + 0.2 - 0.3 == 0);        // false
   PRT(isZero(0.1 + 0.2 - 0.3));     // true

   bool cmp1 = "abcdef" > "abs";     // false, [2]: 's' > 'c'
   bool cmp2 = "abcdef" > "abc";     // true,  by length
   bool cmp3 = "ABCdef" > "abcdef";  // false, by caps
   bool cmp4 = "" == NULL;           // false

   PRT("abcdef" > "abs");
   PRT("abcdef" > "abc");
   PRT("ABCdef" > "abcdef");
   PRT("" == NULL);
}
//+------------------------------------------------------------------+
