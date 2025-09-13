//+------------------------------------------------------------------+
//|                                                  ExprBitwise.mq5 |
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
   short v = ~1;      // 0xfffe = -2
   ushort w = ~1;     // 0xfffe = 65534
   
   short q = v << 5;  // 0xffc0 = -64
   ushort p = w << 5; // 0xffc0 = 65472

   short r = q >> 5;  // 0xfffe = -2
   ushort s = p >> 5; // 0x07fe = 2046
   
   uchar x = 154;     // 10011010
   uchar y =  55;     // 00110111
   
   uchar and = x & y; // 00010010 = 18
   uchar or  = x | y; // 10111111 = 191
   uchar xor = x ^ y; // 10101101 = 173
   
   PRT(v);
   PRT(w);
   
   PRT(q);
   PRT(p);
   
   PRT(x & y);
   PRT(x | y);
   PRT(x ^ y);
   
   // MT5 bug?
   // v is shown in debugger and printed as int (FFFFFFFE)
   // C++ outputs FFFE
   PrintFormat("%X %X", v, w); 
}
//+------------------------------------------------------------------+
