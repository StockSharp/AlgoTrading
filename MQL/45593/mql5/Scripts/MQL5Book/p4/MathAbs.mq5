//+------------------------------------------------------------------+
//|                                                      MathAbs.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRT(A) Print(#A, "=", (A))

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   double x = 123.45;
   double y = -123.45;
   int i = -1;
   
   PRT(MathAbs(x)); // 123.45, kept "as is"
   PRT(MathAbs(y)); // 123.45, negative sign is gone
   PRT(MathAbs(i)); // 1, int is handled natively
   
   int k = MathAbs(i);  // no warning: ints are used both for input/result
   
   // need to convert double to long
   long j = MathAbs(x); // possible loss of data due to type conversion
   
   // need to convert 4-byte int to 2-byte short
   short c = MathAbs(i); // possible loss of data due to type conversion

   // compare casting to unsigned vs taking abs
   uint u_cast = i;
   uint u_abs = MathAbs(i);
   PRT(u_cast);             // 4294967295, 0xFFFFFFFF
   PRT(u_abs);              // 1

   // zero can be positive or negative   
   double n = 0;
   double z = i * n;
   PRT(z);               // -0.0
   PRT(MathAbs(z));      //  0.0
   PRT(z == MathAbs(z)); // true
}
//+------------------------------------------------------------------+
